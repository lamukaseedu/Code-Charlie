using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class KeypadController : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/12/2026
     */

    [Header("Code")]
    [SerializeField] private string correctCode = "0427";
    [SerializeField, Min(1)] private int maximumDigits = 8;

    [Header("Display")]
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private string emptyDisplayText = "----";
    [SerializeField] private string grantedText = "OPEN";
    [SerializeField] private string deniedText = "ERROR";
    [SerializeField, Min(0f)] private float resultDisplayTime = 1f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip digitSound;
    [SerializeField] private AudioClip grantedSound;
    [SerializeField] private AudioClip deniedSound;

    [Header("Events")]
    [SerializeField] private UnityEvent onAccessGranted;
    [SerializeField] private UnityEvent onAccessDenied;

    public bool IsUnlocked { get; private set; }
    public bool IsBusy { get; private set; }

    private string currentInput = string.Empty;
    private Coroutine resultRoutine;

    private void Awake()
    {
        RefreshDisplay();
    }

    public void EnterDigit(string digit)
    {
        if (IsBusy || IsUnlocked || string.IsNullOrEmpty(digit))
            return;

        // Each number button should contribute exactly one numeric character.
        if (digit.Length != 1 || !char.IsDigit(digit[0]))
        {
            Debug.LogWarning($"'{digit}' is not a valid keypad digit.", this);
            return;
        }

        if (currentInput.Length >= maximumDigits)
            return;

        currentInput += digit;
        PlaySound(digitSound);
        RefreshDisplay();
    }

    public void Backspace()
    {
        if (IsBusy || IsUnlocked || currentInput.Length == 0)
            return;

        currentInput = currentInput[..^1];
        PlaySound(digitSound);
        RefreshDisplay();
    }

    public void Clear()
    {
        if (IsBusy || IsUnlocked)
            return;

        currentInput = string.Empty;
        PlaySound(digitSound);
        RefreshDisplay();
    }

    public void Submit()
    {
        if (IsBusy || IsUnlocked)
            return;

        bool isCorrect = currentInput == correctCode;

        if (resultRoutine != null)
            StopCoroutine(resultRoutine);

        resultRoutine = StartCoroutine(ShowResult(isCorrect));
    }

    private IEnumerator ShowResult(bool granted)
    {
        IsBusy = true;
        if (displayText != null)
            displayText.text = granted ? grantedText : deniedText;

        if (granted)
        {
            IsUnlocked = true;
            PlaySound(grantedSound);
            onAccessGranted?.Invoke();
        }
        else
        {
            PlaySound(deniedSound);
            onAccessDenied?.Invoke();
        }

        yield return new WaitForSeconds(resultDisplayTime);

        if (!granted)
        {
            currentInput = string.Empty;
            RefreshDisplay();
        }

        IsBusy = false;
        resultRoutine = null;
    }

    private void RefreshDisplay()
    {
        if (displayText != null)
            displayText.text = currentInput.Length == 0 ? emptyDisplayText : currentInput;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnValidate()
    {
        maximumDigits = Mathf.Max(1, maximumDigits);
    }
}
