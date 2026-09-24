/*
 * Author: Savio Xavier
 * Created: 9/10/2026
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthPercentText;

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged.AddListener(UpdateDisplay);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(UpdateDisplay);
        }
    }

    private void Start()
    {
        if (health != null)
        {
            UpdateDisplay(health.HealthPercent);
        }
    }

    private void UpdateDisplay(float percent)
    {
        if (healthSlider != null)
        {
            healthSlider.value = percent;
        }

        if (healthPercentText != null)
        {
            healthPercentText.text = $"{Mathf.RoundToInt(percent * 100f)}%";
        }
    }
}
