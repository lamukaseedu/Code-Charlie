/*
 * Author: Lam Nguyen
 * Created: 10/25/2026
 */

using UnityEngine;
using UnityEngine.Events;

public class DoorLock : MonoBehaviour, IInteractable
{
    [Tooltip("A held keycard must have an ID greater than or equal to this ID.")]
    [Min(0)]
    [SerializeField] private int id = 0;

    [Header("Interaction Prompt")]
    [SerializeField] private string interactionPrompt = "";

    [Header("Events")]
    [Tooltip("Invoked once when a held keycard successfully unlocks this lock.")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    public int Id => id;
    public bool IsUnlocked { get; private set; }
    public string InteractionPrompt => IsUnlocked ? string.Empty : interactionPrompt;

    // Hover visuals are handled independently by InteractableHoverHighlight.
    public void Hover() { }
    public void Unhover() { }

    public void Interact()
    {
        // Also support the normal interaction input, but only with a held card.
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

        // Set before invoking events so repeated or reentrant use cannot unlock twice.
        IsUnlocked = true;
        FindFirstObjectByType<PlayerInteraction>()?.RefreshPrompt(this);
        onUnlocked?.Invoke();
        return true;
    }
}
