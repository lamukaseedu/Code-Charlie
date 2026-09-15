/*
 * Author: Lam Nguyen
 * Created: 9/1/2026
 * Edited: 9/15/2026
 */

using Unity.Cinemachine;
using UnityEngine;

public class PlayerViewBobbing : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float dampingTime = 0.2f;

    [Header("Damage Shake")]
    [SerializeField] private float shakeAmplitude = 1.5f;
    [SerializeField] private float shakeFrequency = 8f;
    [SerializeField] private float shakeDuration = 0.25f;

    private CharacterController controller;
    private CinemachineBasicMultiChannelPerlin noise;
    private PlayerMovement movement;
    private float amplitudeVelocity;
    private float frequencyVelocity;
    private float currentBobAmplitude;
    private float currentBobFrequency;
    private float shakeTimeRemaining;

    private void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        movement = GetComponentInParent<PlayerMovement>();
    }

    public void Shake()
    {
        shakeTimeRemaining = shakeDuration;
    }

    void Update()
    {
        float targetAmplitude = 0f;
        float targetFrequency = 0f;

        if (movement.CheckMovement())
        {
            Vector3 velocity = controller.velocity;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            float speedFactor = Mathf.Clamp01(speed / 10f);
            targetAmplitude = 0.2f * speedFactor;
            targetFrequency = 2f * speedFactor;
        }

        currentBobAmplitude = Mathf.SmoothDamp(
            currentBobAmplitude, targetAmplitude, ref amplitudeVelocity, dampingTime);
        currentBobFrequency = Mathf.SmoothDamp(
            currentBobFrequency, targetFrequency, ref frequencyVelocity, dampingTime);

        float shakeAmp = 0f;
        float shakeFreq = 0f;

        if (shakeTimeRemaining > 0f && shakeDuration > 0f)
        {
            shakeTimeRemaining -= Time.deltaTime;
            float shakeT = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
            shakeAmp = shakeAmplitude * shakeT;
            shakeFreq = shakeFrequency;
        }

        noise.AmplitudeGain = currentBobAmplitude + shakeAmp;
        noise.FrequencyGain = Mathf.Max(currentBobFrequency, shakeFreq);
    }
}
