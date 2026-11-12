/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 * Edited By: Savio Xavier
 * Edited: 11/12/2026
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;
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

        AudioListener.volume = 1f;

        float sensitivity = GameSettings.GetSensitivity();
        float musicVolume = GameSettings.GetMusicVolume();
        float soundVolume = GameSettings.GetSoundVolume();
        float brightness = GameSettings.GetBrightness();

        ApplySensitivity(sensitivity);
        ApplyMusicVolume(musicVolume);
        ApplySoundVolume(soundVolume);
        ApplyBrightness(brightness);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(sensitivity);
            mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (soundSlider != null)
        {
            soundSlider.SetValueWithoutNotify(soundVolume);
            soundSlider.onValueChanged.AddListener(OnSoundChanged);
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

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        }

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
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

    private void OnMusicChanged(float value)
    {
        GameSettings.SetMusicVolume(value);
    }

    private void OnSoundChanged(float value)
    {
        GameSettings.SetSoundVolume(value);
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

    private void ApplyMusicVolume(float value)
    {
        GameSettings.SetMusicVolume(value);
    }

    private void ApplySoundVolume(float value)
    {
        GameSettings.SetSoundVolume(value);
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

    public void ResetDefault()
    {
        mouseSensitivitySlider.value = GameSettings.DefaultSensitivity;
        if (musicSlider != null)
            musicSlider.value = GameSettings.DefaultVolume;
        if (soundSlider != null)
            soundSlider.value = GameSettings.DefaultVolume;
        brightnessSlider.value = GameSettings.DefaultBrightness;
    }

}
