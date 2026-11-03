/*
 * Author: Savio Xavier
 * Created: 10/20/2026
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

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.volume = volume;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.clip = clip;
    }

    private void OnEnable()
    {
        if (audioSource.clip == null)
            audioSource.clip = clip;

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
}
