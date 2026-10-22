/*
 * Author: Savio Xavier
 * Created: 10/20/2026
 */

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip footstepLoop;
    [SerializeField] private AudioClip damageClip;

    [Header("Footsteps")]
    [SerializeField] private float walkPitch = 1f;
    [SerializeField] private float sprintPitch = 1.2f;

    private AudioSource oneShotSource;
    private AudioSource footstepSource;
    private PlayerMovement movement;
    private CharacterController controller;
    private Health health;

    private void Awake()
    {
        oneShotSource = GetComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.loop = false;

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.loop = true;
        footstepSource.clip = footstepLoop;

        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged.AddListener(PlayDamage);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged.RemoveListener(PlayDamage);

        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Pause();
    }

    private void Update()
    {
        bool shouldPlay =
            Time.timeScale > 0f &&
            footstepLoop != null &&
            movement != null &&
            movement.CheckMovement() &&
            controller != null &&
            controller.isGrounded;

        if (shouldPlay)
        {
            footstepSource.pitch = movement.IsSprinting ? sprintPitch : walkPitch;
            if (!footstepSource.isPlaying)
                footstepSource.Play();
        }
        else if (footstepSource.isPlaying)
        {
            footstepSource.Pause();
        }
    }

    private void PlayDamage()
    {
        GameAudio.PlayOneShot(oneShotSource, damageClip);
    }
}
