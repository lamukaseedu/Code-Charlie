using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class throwItemAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] impactClips;

    [SerializeField] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;

    [SerializeField] private Collider objectCollider;

    GameObject noiseSource;

    private AudioSource audioSource;
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
        GameAudio.PlayRandomOneShot(audioSource, impactClips);
        GameObject noiseSource = this.gameObject;
        NoiseEmitter.EmitAt(transform.position,
            NoiseEmitter.NoiseLevel.Strong, noiseSource);
    }

}
