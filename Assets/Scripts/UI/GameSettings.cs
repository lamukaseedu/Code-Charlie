/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 * Edited By: Savio Xavier
 * Edited: 11/12/2026
 */

using System;
using UnityEngine;

public static class GameSettings
{
    public const string HorizontalSensitivityKey = "HorizontalSensitivity";
    public const string VerticalSensitivityKey = "VerticalSensitivity";
    public const string MusicVolumeKey = "MusicVolume";
    public const string SoundVolumeKey = "SoundVolume";
    public const string BrightnessKey = "Brightness";

    public const float DefaultSensitivity = 0.1f;
    public const float DefaultVolume = 1f;
    public const float DefaultBrightness = 0.5f;

    public static event Action VolumesChanged;

    public static float GetSensitivity()
    {
        return PlayerPrefs.GetFloat(HorizontalSensitivityKey, DefaultSensitivity);
    }

    public static void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(HorizontalSensitivityKey, value);
        PlayerPrefs.SetFloat(VerticalSensitivityKey, value);
        PlayerPrefs.Save();
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
    }

    public static void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
        VolumesChanged?.Invoke();
    }

    public static float GetSoundVolume()
    {
        return PlayerPrefs.GetFloat(SoundVolumeKey, DefaultVolume);
    }

    public static void SetSoundVolume(float value)
    {
        PlayerPrefs.SetFloat(SoundVolumeKey, value);
        PlayerPrefs.Save();
        VolumesChanged?.Invoke();
    }

    public static float GetBrightness()
    {
        return PlayerPrefs.GetFloat(BrightnessKey, DefaultBrightness);
    }

    public static void SetBrightness(float value)
    {
        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
    }

    public static float BrightnessToPostExposure(float brightness)
    {
        return Mathf.Lerp(-2f, 2f, brightness);
    }
}
