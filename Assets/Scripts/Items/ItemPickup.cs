using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

    private PlayerInventory playerInventory;
    private PlayerInteraction playerInteraction;


    [Header("Hover Highlight")]
    [Tooltip("Renderer whose color changes while the player looks at this interactable.")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);
    [Tooltip("Append the Hover Material while hovered and tint it with Highlight Color instead of tinting the original materials. Requires a Hover Material assignment.")]
    [SerializeField] private bool useHoverMaterial = false;
    [Tooltip("Optional transparent or additive highlight material. Extra material slots draw over the mesh's last submesh.")]
    [SerializeField] private Material hoverMaterial;

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
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Material hoverMaterialInstance;
    private bool isHighlighted;

    private void Awake()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();

        if (!useHoverMaterial || hoverMaterial == null)
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

    public void InitializeDrop(Item droppedItem, int droppedQuantity, PlayerInventory inventory, PlayerInteraction interaction)
    {
        item = droppedItem;
        quantity = droppedQuantity;
        playerInventory = inventory;
        playerInteraction = interaction;
    }

    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        if (highlightRenderers == null)
            return;

        if (useHoverMaterial && hoverMaterial != null)
        {
            if (highlighted)
                ApplyHoverMaterial();
            else
                RestoreOriginalMaterials();
            return;
        }

        // Also remove overlays if the optional mode was changed during play.
        RestoreOriginalMaterials();

        var block = new MaterialPropertyBlock();

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                int colorId = material.HasProperty(BaseColorId)
                    ? BaseColorId
                    : material.HasProperty(ColorId) ? ColorId : 0;

                if (colorId == 0)
                    continue;

                renderer.GetPropertyBlock(block, i);
                block.SetColor(
                    colorId,
                    highlighted ? highlightColor : material.GetColor(colorId)
                );

                if (material.HasProperty("_EmissionColor"))
                {
                    block.SetColor(
                        "_EmissionColor",
                        highlighted
                            ? highlightColor * 3f
                            : material.GetColor("_EmissionColor")
                    );
                }

                renderer.SetPropertyBlock(block, i);
            }
        }
    }

    // Used by containers to configure the amount in a spawned pickup.
    public void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Max(1, newQuantity);
    }

    private void ApplyHoverMaterial()
    {
        // Use a private copy so changing the highlight never edits the material asset.
        if (hoverMaterialInstance == null)
            hoverMaterialInstance = new Material(hoverMaterial);

        if (hoverMaterialInstance.HasProperty(BaseColorId))
            hoverMaterialInstance.SetColor(BaseColorId, highlightColor);
        else if (hoverMaterialInstance.HasProperty(ColorId))
            hoverMaterialInstance.SetColor(ColorId, highlightColor);

        if (hoverMaterialInstance.HasProperty("_EmissionColor"))
            hoverMaterialInstance.SetColor("_EmissionColor", highlightColor * 3f);

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null || originalMaterials.ContainsKey(renderer))
                continue;

            Material[] materials = renderer.sharedMaterials;
            originalMaterials.Add(renderer, materials);
            Material[] highlightedMaterials = new Material[materials.Length + 1];
            System.Array.Copy(materials, highlightedMaterials, materials.Length);
            highlightedMaterials[materials.Length] = hoverMaterialInstance;
            renderer.sharedMaterials = highlightedMaterials;
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
        {
            if (entry.Key != null)
                entry.Key.sharedMaterials = entry.Value;
        }
        originalMaterials.Clear();
    }

    private void OnDisable()
    {
        if (isHighlighted)
            SetHighlighted(false);
        RestoreOriginalMaterials();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();
        if (hoverMaterialInstance != null)
            Destroy(hoverMaterialInstance);
    }

    private void CacheMaterialColor()
    {
        if (highlightRenderers == null)
            return;

        normalColors = new Color[highlightRenderers.Length];
        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            if (highlightRenderers[i] == null || highlightRenderers[i].sharedMaterial == null)
                continue;

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
