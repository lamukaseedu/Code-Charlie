using System.Collections;
using UnityEngine;

public class KeypadButton : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/12/2026
     */

    public enum ButtonAction
    {
        Digit,
        Submit,
        Clear,
        Backspace
    }

    [Header("Button Command")]
    [SerializeField] private ButtonAction action = ButtonAction.Digit;
    [SerializeField] private string digit = "0";
    [SerializeField] private KeypadController keypad;

    [Header("Hover")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Color hoverColor = new(0.15f, 0.8f, 1f, 1f);

    [Header("Press Animation")]
    [SerializeField] private Vector3 localPressDirection = Vector3.back;
    [SerializeField, Min(0f)] private float pressDistance = 0.006f;
    [SerializeField, Min(0.01f)] private float moveTime = 0.06f;
    [SerializeField, Min(0f)] private float heldTime = 0.05f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Color normalColor = Color.white;
    private int colorPropertyId;
    private Vector3 restingLocalPosition;
    private bool isAnimating;

    private void Awake()
    {
        restingLocalPosition = transform.localPosition;

        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();

        if (keypad == null)
            keypad = GetComponentInParent<KeypadController>();

        CacheMaterialColor();
    }

    public void SetHovered(bool hovered)
    {
        if (buttonRenderer == null || colorPropertyId == 0)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        buttonRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, hovered ? hoverColor : normalColor);
        buttonRenderer.SetPropertyBlock(propertyBlock);
    }

    public void Press()
    {
        if (isAnimating || keypad == null || keypad.IsBusy || keypad.IsUnlocked)
            return;

        switch (action)
        {
            case ButtonAction.Digit:
                keypad.EnterDigit(digit);
                break;
            case ButtonAction.Submit:
                keypad.Submit();
                break;
            case ButtonAction.Clear:
                keypad.Clear();
                break;
            case ButtonAction.Backspace:
                keypad.Backspace();
                break;
        }

        StartCoroutine(AnimatePress());
    }

    private IEnumerator AnimatePress()
    {
        isAnimating = true;
        Vector3 pressedPosition = restingLocalPosition +
                                  localPressDirection.normalized * pressDistance;

        yield return MoveTo(pressedPosition);
        yield return new WaitForSeconds(heldTime);
        yield return MoveTo(restingLocalPosition);

        transform.localPosition = restingLocalPosition;
        isAnimating = false;
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        Vector3 start = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveTime);
            transform.localPosition = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localPosition = target;
    }

    private void CacheMaterialColor()
    {
        if (buttonRenderer == null || buttonRenderer.sharedMaterial == null)
            return;

        Material material = buttonRenderer.sharedMaterial;

        if (material.HasProperty(BaseColorId))
            colorPropertyId = BaseColorId;
        else if (material.HasProperty(ColorId))
            colorPropertyId = ColorId;
        else
        {
            Debug.LogWarning("The keypad button material has no _BaseColor or _Color property.", this);
            return;
        }

        normalColor = material.GetColor(colorPropertyId);
    }

    private void OnDisable()
    {
        SetHovered(false);
        transform.localPosition = restingLocalPosition;
        isAnimating = false;
    }

    private void OnValidate()
    {
        if (action == ButtonAction.Digit &&
            (digit.Length != 1 || !char.IsDigit(digit[0])))
        {
            digit = "0";
        }
    }
}
