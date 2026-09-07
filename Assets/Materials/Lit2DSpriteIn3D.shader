Shader "Custom/Lit 2D Sprite in 3D"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [Toggle] _AlphaClip("Alpha Clipping", Float) = 1
        _Cutoff("Alpha Clip Threshold", Range(0,1)) = 0.1

        _Smoothness("Smoothness", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _AlphaClip;
                float _Cutoff;
                float _Smoothness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);

                // SpriteRenderer sprites face along their local -Z direction.
                output.normalWS =
                    normalize(TransformObjectToWorldNormal(float3(0, 0, 1)));

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                sprite *= input.color * _Color;

                if (_AlphaClip > 0.5)
                {
                    clip(sprite.a - _Cutoff);
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);

                inputData.shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);

                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI =
                    SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = sprite.rgb;
                surfaceData.alpha = sprite.a;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.emission = half3(0, 0, 0);

                half4 color =
                    UniversalFragmentPBR(inputData, surfaceData);

                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = sprite.a;

                return color;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _AlphaClip;
                float _Cutoff;
                float _Smoothness;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                float3 positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                float3 normalWS =
                    normalize(TransformObjectToWorldNormal(float3(0, 0, 1)));

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS =
                        normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                positionWS = ApplyShadowBias(
                    positionWS,
                    normalWS,
                    lightDirectionWS
                );

                output.positionCS =
                    TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                    output.positionCS.z =
                        min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z =
                        max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half4 sprite =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                sprite *= input.color * _Color;
                clip(sprite.a - _Cutoff);

                return 0;
            }

            ENDHLSL
        }
    }
}