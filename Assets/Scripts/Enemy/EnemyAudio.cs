/*
 * Author: Savio Xavier
 * Created: 10/20/2026
 */

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;

    private AudioSource audioSource;
    private Health health;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

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
    }

    public void PlayAttack()
    {
        GameAudio.PlayRandomOneShot(audioSource, attackClips);
    }

    private void PlayDamage()
    {
        GameAudio.PlayRandomOneShot(audioSource, damageClips);
    }
}
