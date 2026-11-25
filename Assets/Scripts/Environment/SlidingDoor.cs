/*
 * Author: Brian Delaney
 * Created: 10/22/2026
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class SlidingDoor : MonoBehaviour, IInteractable
{
    [Header("Door Movement")]
    [SerializeField] private Transform door;
    [SerializeField] private float speed = 200f;
    [SerializeField] private float openHeight = 300f;

    [Header("Enemy Navigation")]
    [Tooltip("Optional stationary obstacle covering the doorway. Enabled until the door is fully open. Exclude the moving door panel from the NavMesh bake.")]
    [SerializeField] private NavMeshObstacle doorwayObstacle;

    [Header("Keycard Lock")]
    [Tooltip("0 allows players and enemies to open the door without a keycard. Otherwise, only a player with a sufficient inventory keycard can unlock it. Enemies can keep an unlocked door open while passing through.")]
    [Min(0)]
    [SerializeField] private int id = 0;

    [Header("Interaction Prompt")]
    [SerializeField] private string interactionPrompt = "KEYCARD REQUIRED";

    [Header("Hover Highlight")]
    [Tooltip("Renderers highlighted while looking at a locked door. Defaults to the moving door's child renderers.")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color canUnlockColor = Color.green;
    [SerializeField] private Color cannotUnlockColor = Color.red;
    [Tooltip("Append the Hover Material while hovered instead of tinting the original materials.")]
    [SerializeField] private bool useHoverMaterial = false;
    [Tooltip("Optional transparent or additive highlight material. Extra material slots draw over the mesh's last submesh.")]
    [SerializeField] private Material hoverMaterial;

    [Header("Events")]
    [Tooltip("Invoked when a keycard in the player's inventory unlocks this door for one opening cycle.")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private bool unlocked;
    private bool openedSinceUnlock;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isHovered;
    private bool isHighlighted;
    private Color currentHighlightColor;
    private PlayerInventory playerInventory;
    private Material hoverMaterialInstance;
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (doorwayObstacle != null)
            doorwayObstacle.carving = true;
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
        RefreshDoorwayObstacle();
    }

    void OnTriggerEnter(Collider other)
    {
        // Track enemies even while locked: occupancy never grants access, but keeps an unlocked door open.
        GameObject actor = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (other.CompareTag("Player") || other.CompareTag("Enemy") ||
            actor.CompareTag("Player") || actor.CompareTag("Enemy"))
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
        Color highlightColor = cannotUnlockColor;
        if (highlighted)
        {
            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (playerInventory != null && playerInventory.HasKeycard(id))
                highlightColor = canUnlockColor;
        }
        if (highlighted == isHighlighted && (!highlighted || highlightColor == currentHighlightColor))
            return;

        isHighlighted = highlighted;
        currentHighlightColor = highlightColor;
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
        TryUnlock(inventory);
    }

    public bool TryUnlock(PlayerInventory inventory)
    {
        if (!isActiveAndEnabled || IsUnlocked || inventory == null ||
            !inventory.isActiveAndEnabled || inventory.IsOpen || !inventory.HasKeycard(id))
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
        RefreshDoorwayObstacle();

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

    private void RefreshDoorwayObstacle()
    {
        if (doorwayObstacle != null)
            doorwayObstacle.enabled = door.localPosition != openPosition;
    }
}
