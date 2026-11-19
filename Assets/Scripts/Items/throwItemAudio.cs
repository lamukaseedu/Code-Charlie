using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class throwItemAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] impactClips;

    [Tooltip("Minimum relative impact speed in metres per second required to play a landing sound and alert enemies.")]
    [Min(0f)]
    [SerializeField] private float minimumImpactSpeed = 3f;

    [Tooltip("Minimum seconds between impact sounds and AI noise emissions from this item.")]
    [Min(0f)]
    [SerializeField] private float impactCooldown = 1f;

    [SerializeField] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;

    [SerializeField] private Collider objectCollider;

    GameObject noiseSource;

    private AudioSource audioSource;
    private float nextImpactTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float minimumSpeed = Mathf.Max(0f, minimumImpactSpeed);
        if (!isActiveAndEnabled || Time.time < nextImpactTime ||
            collision.relativeVelocity.sqrMagnitude < minimumSpeed * minimumSpeed)
            return;

        nextImpactTime = Time.time + Mathf.Max(0f, impactCooldown);
        GameAudio.PlayRandomOneShot(audioSource, impactClips);
        GameObject noiseSource = this.gameObject;
        NoiseEmitter.EmitAt(transform.position,
            NoiseEmitter.NoiseLevel.Strong, noiseSource);
    }

}
