
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ShootSparks : MonoBehaviour
{
    [SerializeField] private float minWaitTime = 3.0f;
    [SerializeField] private float maxWaitTime = 5.0f;

    private ParticleSystem sparks;

    private void Awake()
    {
        sparks = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        StartCoroutine(Spark());
    }

    private IEnumerator Spark()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            sparks.Play();
        }
    }
}
