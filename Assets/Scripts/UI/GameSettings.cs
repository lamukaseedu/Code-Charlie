/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 */

using UnityEngine;

public static class GameSettings
{
    public const string HorizontalSensitivityKey = "HorizontalSensitivity";
    public const string VerticalSensitivityKey = "VerticalSensitivity";
    public const string MasterVolumeKey = "MasterVolume";
    public const string BrightnessKey = "Brightness";

    public const float DefaultSensitivity = 0.1f;
    public const float DefaultVolume = 1f;
    public const float DefaultBrightness = 0.5f;

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

    public static float GetVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
    }

    public static void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
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
