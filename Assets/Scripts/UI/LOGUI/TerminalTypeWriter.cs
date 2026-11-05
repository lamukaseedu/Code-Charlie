using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TerminalTypewriter : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/08/2026
     */
    [Header("Text")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTypingComplete;

    private Coroutine typingCoroutine;

    // Starts typing the provided text
    public void TypeText(string text)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeRoutine(text));
    }

    // Displays each character one at a time
    private IEnumerator TypeRoutine(string text)
    {
        targetText.text = "";

        foreach (char letter in text)
        {
            targetText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
        onTypingComplete?.Invoke();
    }

    // Instantly displays the provided text
    public void SetText(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        targetText.text = text;
    }

    private void OnDisable()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }
}