using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TerminalLoginButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/20/2026
     */

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text buttonText;

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 1.1f;
    [SerializeField] private float transitionDuration = 0.12f;

    [Header("Background Colors")]
    [SerializeField] private Color normalBackground = Color.black;
    [SerializeField] private Color hoverBackground = Color.green;
    [SerializeField] private Color pressedBackground = Color.white;

    [Header("Text Colors")]
    [SerializeField] private Color normalText = Color.green;
    [SerializeField] private Color hoverText = Color.black;
    [SerializeField] private Color pressedText = Color.black;

    [Header("Login Result Colors")]
    [SerializeField] private Color successTextColor = Color.green;
    [SerializeField] private Color failureTextColor = Color.red;

    private bool isShowingResult;
    private Color resultTextColor;

    private Vector3 originalScale;
    private Coroutine transitionCoroutine;

    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        isHovered = false;
        isPressed = false;
        isShowingResult = false;

        UpdateVisuals(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (!isShowingResult)
            UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!isShowingResult)
            UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (isShowingResult)
            return;

        isPressed = true;
        UpdateVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = false;

        if (!isShowingResult)
            UpdateVisuals();
    }

    private void UpdateVisuals(bool instant = false)
    {
        float targetScale = 1f;
        Color targetBackground = normalBackground;
        Color targetText = normalText;

        if (isShowingResult)
        {
            // Lock the button in its pressed appearance.
            targetScale = pressedScale;
            targetBackground = pressedBackground;
            targetText = resultTextColor;
        }
        else if (isPressed)
        {
            targetScale = pressedScale;
            targetBackground = pressedBackground;
            targetText = pressedText;
        }
        else if (isHovered)
        {
            targetScale = hoverScale;
            targetBackground = hoverBackground;
            targetText = hoverText;
        }

        Vector3 targetSize = originalScale * targetScale;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (instant || transitionDuration <= 0f)
        {
            transform.localScale = targetSize;
            backgroundImage.color = targetBackground;
            buttonText.color = targetText;
        }
        else
        {
            transitionCoroutine = StartCoroutine(
                AnimateVisuals(targetSize, targetBackground, targetText)
            );
        }
    }

    private IEnumerator AnimateVisuals(
        Vector3 targetScale,
        Color targetBackground,
        Color targetText)
    {
        Vector3 startScale = transform.localScale;
        Color startBackground = backgroundImage.color;
        Color startText = buttonText.color;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(
                startScale, targetScale, t);

            backgroundImage.color = Color.Lerp(
                startBackground, targetBackground, t);

            buttonText.color = Color.Lerp(
                startText, targetText, t);

            yield return null;
        }

        transform.localScale = targetScale;
        backgroundImage.color = targetBackground;
        buttonText.color = targetText;

        transitionCoroutine = null;
    }

    public void ShowLoginResult(bool success)
    {
        isShowingResult = true;
        isPressed = false;

        resultTextColor = success
            ? successTextColor
            : failureTextColor;

        UpdateVisuals();
    }

    public void ClearLoginResult()
    {
        isShowingResult = false;
        isPressed = false;
        isHovered = false;

        // Reset to original size and normal colors.
        UpdateVisuals();
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }
}
