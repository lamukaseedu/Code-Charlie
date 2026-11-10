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

    [Header("Events")]
    [Tooltip("Invoked when a held keycard unlocks this door for one opening cycle.")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private bool unlocked;
    private bool openedSinceUnlock;
    private Vector3 closedPosition;
    private Vector3 openPosition;

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
        occupants.Clear();
        unlocked = false;
        openedSinceUnlock = false;
    }

    // Hover visuals are handled independently by InteractableHoverHighlight.
    public void Hover() { }
    public void Unhover() { }

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
    }
}
