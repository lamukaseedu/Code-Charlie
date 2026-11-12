/*
 * Author: Savio Xavier
 * Created: 10/20/2026
 * Edited By: Savio Xavier
 * Edited: 11/12/2026
 */

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LoopingWorldAudio : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool isMusic = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.clip = clip;
        ApplyVolume();
    }

    private void OnEnable()
    {
        GameSettings.VolumesChanged += ApplyVolume;
        ApplyVolume();

        if (audioSource.clip == null)
            audioSource.clip = clip;

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnDisable()
    {
        GameSettings.VolumesChanged -= ApplyVolume;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void ApplyVolume()
    {
        if (audioSource == null)
            return;

        float mix = isMusic ? GameSettings.GetMusicVolume() : GameSettings.GetSoundVolume();
        audioSource.volume = volume * mix;
    }
}
