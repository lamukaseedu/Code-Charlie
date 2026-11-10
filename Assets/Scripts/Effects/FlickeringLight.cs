using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    //[SerializeField] private float minIntensity = 0.1f;
    //[SerializeField] private float maxIntensity = 2f;

    [SerializeField] private float minWaitTime = 0.05f;
    [SerializeField] private float maxWaitTime = 0.3f;

    [Header("Optional")]
    [SerializeField] private bool canTurnOff = true;
    [SerializeField, Range(0f, 1f)]
    private float turnOffChance = 0.4f;

    private Light pointLight;

    private void Awake()
    {
        pointLight = GetComponent<Light>();
    }

    private void Start()
    {
        StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // Occasionally turn the light completely off.
            if (canTurnOff && Random.value < turnOffChance)
            {
                pointLight.intensity = 0f;
            }
            else
            {
                pointLight.intensity = 1; //Random.Range(minIntensity, maxIntensity);
            }
        }
    }
}
