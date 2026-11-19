using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class LightFlicker : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/18/2026
     */

    [SerializeField, Min(0.1f)] private float minTimeBetweenBursts = 4f;
    [SerializeField, Min(0.1f)] private float maxTimeBetweenBursts = 9f;

    [SerializeField, Range(0f, 1f)] private float dimIntensityMultiplier = 0.15f;

    [SerializeField] private Renderer bulbRenderer;
    
    [SerializeField] private Material blackoutMaterial;

    
    [SerializeField] private ParticleSystemRenderer volumetricRenderer;

    private Light bulb;
    private float normalIntensity;
    private Coroutine flickerRoutine;
    private Renderer flickeringRenderer;
    private Material[] normalMaterials;
    private Material[] blackoutMaterials;
    private ParticleSystemRenderer flickeringVolumetricRenderer;
    private bool normalVolumetricRendererEnabled;

    private void Awake()
    {
        bulb = GetComponent<Light>();
    }

    private void OnEnable()
    {
        normalIntensity = bulb.intensity;
        flickeringVolumetricRenderer = volumetricRenderer;
        if (flickeringVolumetricRenderer != null)
            normalVolumetricRendererEnabled = flickeringVolumetricRenderer.enabled;

        if (bulbRenderer != null && blackoutMaterial != null)
        {
            flickeringRenderer = bulbRenderer;
            normalMaterials = flickeringRenderer.sharedMaterials;
            blackoutMaterials = new Material[normalMaterials.Length];
            for (int i = 0; i < blackoutMaterials.Length; i++)
                blackoutMaterials[i] = blackoutMaterial;
        }

        flickerRoutine = StartCoroutine(Flicker());
    }

    private void OnDisable()
    {
        if (flickerRoutine == null)
            return;

        StopCoroutine(flickerRoutine);
        flickerRoutine = null;
        SetFlickerState(false);
        flickeringRenderer = null;
        normalMaterials = null;
        blackoutMaterials = null;
        flickeringVolumetricRenderer = null;
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            float longestWait = Mathf.Max(minTimeBetweenBursts, maxTimeBetweenBursts);
            yield return new WaitForSeconds(Random.Range(minTimeBetweenBursts, longestWait));

            int dipCount = Random.Range(1, 3);
            for (int i = 0; i < dipCount; i++)
            {
                SetFlickerState(true);
                yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));

                SetFlickerState(false);
                if (i + 1 < dipCount)
                    yield return new WaitForSeconds(Random.Range(0.12f, 0.22f));
            }
        }
    }

    private void SetFlickerState(bool isFlickering)
    {
        bulb.intensity = isFlickering ? normalIntensity * dimIntensityMultiplier : normalIntensity;

        if (flickeringRenderer != null)
            flickeringRenderer.sharedMaterials = isFlickering ? blackoutMaterials : normalMaterials;

        if (flickeringVolumetricRenderer != null)
            flickeringVolumetricRenderer.enabled = !isFlickering && normalVolumetricRendererEnabled;
    }
}
