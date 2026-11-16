/*
 * Author: Brian Delaney
 * Created: 10/22/2026
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SlidingDoor : MonoBehaviour, IInteractable
{
    [Header("Door Movement")]
    [SerializeField] private Transform door;
    [SerializeField] private float speed = 200f;
    [SerializeField] private float openHeight = 300f;

    [Header("Keycard Lock")]
    [Tooltip("0 allows players and enemies to open the door without a keycard. Otherwise, a held keycard must have an equal or higher ID; only players can open the door.")]
    [Min(0)]
    [SerializeField] private int id = 0;

    [Header("Interaction Prompt")]
    [SerializeField] private string interactionPrompt = "KEYCARD REQUIRED";

    [Header("Hover Highlight")]
    [Tooltip("Renderers highlighted while looking at a locked door. Defaults to the moving door's child renderers.")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);
    [Tooltip("Append the Hover Material while hovered instead of tinting the original materials.")]
    [SerializeField] private bool useHoverMaterial = false;
    [Tooltip("Optional transparent or additive highlight material. Extra material slots draw over the mesh's last submesh.")]
    [SerializeField] private Material hoverMaterial;

    [Header("Events")]
    [Tooltip("Invoked when a held keycard unlocks this door for one opening cycle.")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private bool unlocked;
    private bool openedSinceUnlock;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isHovered;
    private bool isHighlighted;
    private Material hoverMaterialInstance;
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = door != null
                ? door.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
    }

    public int Id => id;
    public bool IsUnlocked => id == 0 || unlocked;
    public string InteractionPrompt => IsUnlocked ? string.Empty : interactionPrompt;

    void Start()
    {
        closedPosition = door.localPosition;
        openPosition = closedPosition + new Vector3(0, openHeight, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || (id == 0 && other.CompareTag("Enemy")))
            occupants.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        occupants.Remove(other);
    }

    private void OnDisable()
    {
        Unhover();
        occupants.Clear();
        unlocked = false;
        openedSinceUnlock = false;
    }

    public void Hover()
    {
        isHovered = true;
        RefreshHighlight();
    }

    public void Unhover()
    {
        isHovered = false;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        bool highlighted = isHovered && isActiveAndEnabled && !IsUnlocked;
        if (highlighted == isHighlighted)
            return;

        isHighlighted = highlighted;
        RestoreOriginalMaterials();
        if (highlightRenderers == null)
            return;

        if (useHoverMaterial && hoverMaterial != null)
        {
            if (!highlighted)
                return;

            // Keep tint changes local to this door rather than editing the material asset.
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
            return;
        }

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
                    ? BaseColorId : material.HasProperty(ColorId) ? ColorId : 0;
                if (colorId == 0)
                    continue;
                renderer.GetPropertyBlock(block, i);
                block.SetColor(colorId, highlighted ? highlightColor : material.GetColor(colorId));
                if (material.HasProperty("_EmissionColor"))
                    block.SetColor("_EmissionColor", highlighted ? highlightColor * 3f : material.GetColor("_EmissionColor"));
                renderer.SetPropertyBlock(block, i);
            }
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

    private void OnDestroy()
    {
        RestoreOriginalMaterials();
        if (hoverMaterialInstance != null)
            Destroy(hoverMaterialInstance);
    }

    public void Interact()
    {
        if (IsUnlocked)
            return;

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null || inventory.EquippedObject == null)
            return;

        Keycard keycard = inventory.EquippedObject.GetComponentInChildren<Keycard>();
        if (keycard != null)
            keycard.Use();
    }

    public bool TryUnlock(Keycard keycard)
    {
        if (!isActiveAndEnabled || IsUnlocked || keycard == null ||
            !keycard.IsEquipped || keycard.Id < id)
            return false;

        // Block repeated or reentrant use until this opening cycle is complete.
        unlocked = true;
        openedSinceUnlock = false;
        RefreshHighlight();
        FindFirstObjectByType<PlayerInteraction>()?.RefreshPrompt(this);
        onUnlocked?.Invoke();
        return true;
    }

    void Update()
    {
        // Destroyed or disabled colliders may not send OnTriggerExit.
        occupants.RemoveWhere(occupant => occupant == null || !occupant.enabled ||
            !occupant.gameObject.activeInHierarchy);

        bool shouldOpen = IsUnlocked && occupants.Count > 0;
        if (shouldOpen && unlocked)
            openedSinceUnlock = true;

        Vector3 doorPosition = shouldOpen ? openPosition : closedPosition;
        door.localPosition = Vector3.MoveTowards(door.localPosition, doorPosition, speed * Time.deltaTime);

        // Consume keycard access after everyone leaves and the door finishes closing.
        if (unlocked && openedSinceUnlock && occupants.Count == 0 &&
            door.localPosition == closedPosition)
        {
            unlocked = false;
            openedSinceUnlock = false;
            FindFirstObjectByType<PlayerInteraction>()?.RefreshPrompt(this);
        }
        RefreshHighlight();
    }
}
