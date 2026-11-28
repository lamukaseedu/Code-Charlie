using UnityEngine;

public class TerminalUISounds : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private AudioClip logClickSound;

    public void PlayTypingSound(string _)
    {
        GameAudio.PlayOneShot(audioSource, typingSound);
    }

    public void PlayLogClickSound()
    {
        GameAudio.PlayOneShot(audioSource, logClickSound);
    }
}