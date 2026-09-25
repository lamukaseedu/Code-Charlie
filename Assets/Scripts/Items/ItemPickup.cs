using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ItemHover : MonoBehaviour, IInteractable
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/14/2026
     */
    [Header("Item")]
    [SerializeField] private Item item;

    [Min(1)]
    [SerializeField] private int quantity = 1;

    [Header("Player Inventory")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerInteraction playerInteraction;


    [Header("Hover Highlight")]
    [Tooltip("Renderer whose color changes while the player looks at this interactable.")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);

    [Header("InteractionPrompt")]
    [SerializeField] private string interactionPrompt;
    public string InteractionPrompt => $"{interactionPrompt} (x{quantity})";

    [Header("Interaction Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onUnhover;
    [SerializeField] private UnityEvent onItemPickedUp;


    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Color[] normalColors;
    private int colorPropertyId;

    private void Awake()
    {
        if (highlightRenderers == null)
            highlightRenderers = GetComponentsInChildren<Renderer>();

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
        PickUpItem();
    }

    public void PickUpItem()
    {
        if (playerInventory == null || item == null)
            return;

        int amountAdded = playerInventory.AddItem(item, quantity);

        // Nothing could fit.
        if (amountAdded <= 0)
        {
            Debug.Log("Inventory is full.");
            return;
        }

        // Remove however many were successfully picked up
        // from this world pickup.
        quantity -= amountAdded;

        Debug.Log(
            $"Picked up {amountAdded} {item.ItemName}. " +
            $"{quantity} remaining."
        );

        // Some items are still left in the world.
        if (quantity > 0)
        {
            playerInteraction?.RefreshTarget();
            return;
        }

        // Entire pickup has now been collected.
        playerInteraction?.ClearTarget();
        onItemPickedUp?.Invoke();
        Destroy(gameObject);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightRenderers == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        
        for (int i = 0; i< highlightRenderers.Length; i++)
        {
            if (highlightRenderers[i] == null)
                continue;
            highlightRenderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(
                colorPropertyId,
                highlighted ? highlightColor : normalColors[i]
            );
            highlightRenderers[i].SetPropertyBlock(propertyBlock);
        }
        
    }

    private void CacheMaterialColor()
    {
        if (highlightRenderers == null)
            return;

        normalColors = new Color[highlightRenderers.Length];
        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            Material material = highlightRenderers[i].sharedMaterial;

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

            normalColors[i] = material.GetColor(colorPropertyId);
        }
    }
}
