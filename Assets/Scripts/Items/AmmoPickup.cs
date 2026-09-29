/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using UnityEngine;

public class AmmoPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int amount = 12;

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
        if (amount <= 0)
            return;

        PlayerAmmo ammo = FindFirstObjectByType<PlayerAmmo>();
        if (ammo == null || ammo.Add(amount) <= 0)
            return;

        Destroy(gameObject);
    }
}
