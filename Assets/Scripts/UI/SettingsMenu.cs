/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private Volume globalVolume;

    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
        }

        float sensitivity = GameSettings.GetSensitivity();
        float volume = GameSettings.GetVolume();
        float brightness = GameSettings.GetBrightness();

        ApplySensitivity(sensitivity);
        ApplyVolume(volume);
        ApplyBrightness(brightness);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(sensitivity);
            mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.SetValueWithoutNotify(brightness);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }
    }

    private void OnDestroy()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        }
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettings.SetSensitivity(value);
        ApplySensitivity(value);
    }

    private void OnVolumeChanged(float value)
    {
        GameSettings.SetVolume(value);
        ApplyVolume(value);
    }

    private void OnBrightnessChanged(float value)
    {
        GameSettings.SetBrightness(value);
        ApplyBrightness(value);
    }

    private void ApplySensitivity(float value)
    {
        if (playerCamera != null)
        {
            playerCamera.SetSensitivity(value);
        }
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;
    }

    private void ApplyBrightness(float value)
    {
        if (colorAdjustments == null)
        {
            return;
        }

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = GameSettings.BrightnessToPostExposure(value);
    }
}
