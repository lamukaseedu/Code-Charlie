using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ItemHover : MonoBehaviour, IInteractable
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/14/2026
     */

    [Header("Player Control")]
    [SerializeField] private PlayerCamera playerCameraController;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;


    [Header("Hover Highlight")]
    [Tooltip("Renderer whose color changes while the player looks at this interactable.")]
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);

    [Header("Interaction Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onUnhover;
    [SerializeField] private UnityEvent onInteractableEntered;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Color normalColor = Color.white;
    private int colorPropertyId;

    private void Awake()
    {
        if (highlightRenderer == null)
            highlightRenderer = GetComponentInChildren<Renderer>();

        CacheMaterialColor();
    }

    // Called by PlayerInteraction when its raycast begins targeting this object.
    public void Hover()
    {
         SetHighlighted(true);
         onHover?.Invoke();
    }

    // Called by PlayerInteraction when its raycast stops targeting this object.
    public void Unhover()
    {
        SetHighlighted(false);
        onUnhover?.Invoke();
    }

    // Called by PlayerInteraction when the player presses the interact button.
    public void Interact()
    {
        EnterInteractable();
    }

    public void EnterInteractable()
    {
        SetHighlighted(false);
        onUnhover?.Invoke();
        onInteractableEntered?.Invoke();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightRenderer == null || colorPropertyId == 0)
            return;

        propertyBlock ??= new MaterialPropertyBlock();

        highlightRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(
            colorPropertyId,
            highlighted ? highlightColor : normalColor
        );
        highlightRenderer.SetPropertyBlock(propertyBlock);
    }

    private void CacheMaterialColor()
    {
        if (highlightRenderer == null || highlightRenderer.sharedMaterial == null)
            return;

        Material material = highlightRenderer.sharedMaterial;

        if (material.HasProperty(BaseColorId))
            colorPropertyId = BaseColorId;
        else if (material.HasProperty(ColorId))
            colorPropertyId = ColorId;
        else
        {
            Debug.LogWarning(
                "The interactable material has no _BaseColor or _Color property.",
                this
            );
            return;
        }

        normalColor = material.GetColor(colorPropertyId);
    }
}
