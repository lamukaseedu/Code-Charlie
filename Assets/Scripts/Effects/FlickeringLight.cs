using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    //[SerializeField] private float minIntensity = 0.1f;
    //[SerializeField] private float maxIntensity = 1f;

    [SerializeField] private float minWaitTime = 0.1f;
    [SerializeField] private float maxWaitTime = 3f;

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

            pointLight.enabled = false;
            yield return new WaitForSeconds(0.1f);
            pointLight.enabled = true;
        }
    }
}
