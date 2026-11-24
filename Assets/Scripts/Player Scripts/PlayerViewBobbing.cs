/*
 * Author: Lam Nguyen
 * Created: 9/1/2026
 */

using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineBasicMultiChannelPerlin))]
public class PlayerViewBobbing : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float dampingTime = 0.2f;

    [Header("Screen Shake")]
    [Tooltip("Seconds for a damage shake to fade out. Repeated calls restart the shake.")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.3f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.6f;
    [SerializeField, Min(0f)] private float shakeFrequency = 12f;

    private CharacterController controller;
    private CinemachineBasicMultiChannelPerlin noise;
    private PlayerMovement movement;
    private float amplitudeVelocity;
    private float frequencyVelocity;
    private float bobAmplitude;
    private float bobFrequency;
    private float shakeTimeRemaining;

    // Assign variables
    private void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        movement = GetComponentInParent<PlayerMovement>();
        bobAmplitude = noise.AmplitudeGain;
        bobFrequency = noise.FrequencyGain;
    }

    // Select PlayerViewBobbing.ShakeScreen in a Health On Damaged Unity Event.
    public void ShakeScreen()
    {
        if (!isActiveAndEnabled || noise == null) return;
        shakeTimeRemaining = Mathf.Max(0f, shakeDuration);
        ApplyNoise();
    }

    public void StopShake()
    {
        shakeTimeRemaining = 0f;
        if (noise != null) ApplyNoise();
    }

    // Ease the bobbing in and out as movement speed changes.
    void Update()
    {
        float targetAmplitude = 0f;
        float targetFrequency = 0f;

        if (movement != null && controller != null && movement.CheckMovement())
        {
            Vector3 velocity = controller.velocity;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            float speedFactor = Mathf.Clamp01(speed / 10f);
            targetAmplitude = 0.2f * speedFactor;
            targetFrequency = 2f * speedFactor;
        }

        // Smooth only the locomotion gains so the shake cannot feed back into the bobbing.
        bobAmplitude = Mathf.SmoothDamp(
            bobAmplitude, targetAmplitude, ref amplitudeVelocity, dampingTime);
        bobFrequency = Mathf.SmoothDamp(
            bobFrequency, targetFrequency, ref frequencyVelocity, dampingTime);
        shakeTimeRemaining = Mathf.Max(0f, shakeTimeRemaining - Time.deltaTime);
        ApplyNoise();
    }

    private void ApplyNoise()
    {
        float weight = shakeDuration > 0f ? Mathf.Clamp01(shakeTimeRemaining / shakeDuration) : 0f;
        weight = Mathf.SmoothStep(0f, 1f, weight);
        noise.AmplitudeGain = bobAmplitude + Mathf.Max(0f, shakeAmplitude) * weight;
        noise.FrequencyGain = Mathf.Lerp(bobFrequency, Mathf.Max(bobFrequency, shakeFrequency), weight);
    }

    private void OnDisable()
    {
        shakeTimeRemaining = 0f;
        bobAmplitude = bobFrequency = 0f;
        amplitudeVelocity = frequencyVelocity = 0f;
        if (noise == null) return;
        noise.AmplitudeGain = 0f;
        noise.FrequencyGain = 0f;
    }
}
