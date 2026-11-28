/*
 * Author: Savio Xavier
 * Created: 10/20/2026
 * Edited By: Savio Xavier
 * Edited: 11/12/2026
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
    [Tooltip("Crouching footstep volume relative to walking. 0.5 means 50%.")]
    [SerializeField, Range(0f, 1f)] private float crouchVolumeMultiplier = 0.5f;

    [SerializeField, Min(0f)] private float footstepFadeOutTime = 0.15f;
    private float footstepGain;

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
        ApplySoundVolume();
    }

    private void OnEnable()
    {
        GameSettings.VolumesChanged += ApplySoundVolume;
        ApplySoundVolume();

        if (health != null)
            health.OnDamaged.AddListener(PlayDamage);
    }

    private void OnDisable()
    {
        GameSettings.VolumesChanged -= ApplySoundVolume;

        if (health != null)
            health.OnDamaged.RemoveListener(PlayDamage);

        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Pause();
    }

    private void Update()
    {
        bool gamePaused = Time.timeScale <= 0f;
        bool shouldPlay =
            !gamePaused &&
            footstepLoop != null &&
            movement != null &&
            movement.CheckMovement() &&
            controller != null &&
            controller.isGrounded;

        if (shouldPlay)
        {
            footstepGain = 1f;
            footstepSource.pitch = movement.IsSprinting ? sprintPitch : walkPitch;
        }
        else if (gamePaused || footstepFadeOutTime <= 0f)
        {
            footstepGain = 0f;
        }
        else
        {
            footstepGain = Mathf.MoveTowards(
                footstepGain, 0f, Time.deltaTime / footstepFadeOutTime);
        }

        ApplyFootstepVolume();

        if (shouldPlay && !footstepSource.isPlaying)
            footstepSource.Play();
        else if (footstepGain <= 0f && footstepSource.isPlaying)
            footstepSource.Pause();
    }

    private void PlayDamage()
    {
        GameAudio.PlayOneShot(oneShotSource, damageClip);
    }

    private void ApplySoundVolume()
    {
        float soundVolume = GameSettings.GetSoundVolume();
        ApplyFootstepVolume();
        if (oneShotSource != null)
            oneShotSource.volume = soundVolume;
    }

    private void ApplyFootstepVolume()
    {
        if (footstepSource == null) return;
        float multiplier = movement != null && movement.IsCrouching
            ? Mathf.Clamp01(crouchVolumeMultiplier) : 1f;
        footstepSource.volume = GameSettings.GetSoundVolume() * multiplier * footstepGain;
    }
}
