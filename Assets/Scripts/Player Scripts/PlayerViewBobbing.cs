/*
 * Author: Lam Nguyen
 * Created: 9/1/2026
 */

using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerViewBobbing : MonoBehaviour
{
    private CharacterController controller;
    private CinemachineBasicMultiChannelPerlin noise;
    private PlayerMovement movement;
    private float speed;

    // Assign variables
    private void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        movement = GetComponentInParent<PlayerMovement>();
    }

    // Check if the player is moving, then adjust amplitude and frequency of bobbing based on speed
    void Update()
    {
        if (movement.CheckMovement())
        {
            speed = controller.velocity.magnitude;
            noise.AmplitudeGain = Mathf.Lerp(0, 0.2f, Mathf.Round((speed / 10f) * 100f) / 100f);
            noise.FrequencyGain = Mathf.Lerp(0, 2f, Mathf.Round((speed / 10f) * 100f) / 100f);
        }
        else
        {
            noise.AmplitudeGain = 0f;
            noise.FrequencyGain = 0f;
        }
    }
}
