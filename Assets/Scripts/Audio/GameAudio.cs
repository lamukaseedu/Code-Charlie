/*
 * Author: Savio Xavier
 * Created: 10/20/2026
 * Edited By: Savio Xavier
 * Edited: 11/12/2026
 */

using UnityEngine;

public static class GameAudio
{
    public static void PlayOneShot(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(clip, GameSettings.GetSoundVolume());
    }

    public static void PlayRandomOneShot(AudioSource source, AudioClip[] clips)
    {
        if (source == null || clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlayOneShot(source, clip);
    }
}
