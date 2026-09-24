/*
 * Author: Lam Nguyen
 * Created: 9/1/2026
 */

using Unity.Cinemachine;
using UnityEngine;

public class PlayerViewBobbing : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float dampingTime = 0.2f;

    private CharacterController controller;
    private CinemachineBasicMultiChannelPerlin noise;
    private PlayerMovement movement;
    private float amplitudeVelocity;
    private float frequencyVelocity;

    // Assign variables
    private void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        movement = GetComponentInParent<PlayerMovement>();
    }

    // Ease the bobbing in and out as movement speed changes.
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

        noise.AmplitudeGain = Mathf.SmoothDamp(
            noise.AmplitudeGain, targetAmplitude, ref amplitudeVelocity, dampingTime);
        noise.FrequencyGain = Mathf.SmoothDamp(
            noise.FrequencyGain, targetFrequency, ref frequencyVelocity, dampingTime);
    }
}
