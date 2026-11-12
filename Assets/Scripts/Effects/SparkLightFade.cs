using System.Collections;
using UnityEngine;

public class SparkLightFade : MonoBehaviour
{
    private Light targetLight;

    void Awake()
    {
        targetLight = GetComponent<Light>();
        FadeOut(0.2f);
    }
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeLightRoutine(0f, duration));
    }

    private IEnumerator FadeLightRoutine(float targetIntensity, float duration)
    {
        float startIntensity = targetLight.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            targetLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsedTime / duration);
            yield return null; 
        }
    }
}
