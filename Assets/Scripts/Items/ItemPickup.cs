/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Item item;

    private InteractableHoverHighlight hoverHighlight;

    private void Awake()
    {
        hoverHighlight = GetComponentInChildren<InteractableHoverHighlight>();
    }

    public void Hover()
    {
        hoverHighlight?.SetHighlighted(true);
    }

    public void Unhover()
    {
        hoverHighlight?.SetHighlighted(false);
    }

    public void Interact()
    {
        if (item == null)
            return;

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null || !inventory.AddItem(item))
            return;

        Destroy(gameObject);
    }
}
