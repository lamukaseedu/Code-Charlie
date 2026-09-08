using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerTriggerEvents : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If disabled, this trigger can only be activated once.")]
    [SerializeField] private bool repeatable = true;

    [Tooltip("Prevents On Triggered from activating multiple times during one visit.")]
    [SerializeField] private bool activatedOnce = true;

    [Tooltip("This is not required for the script to work, just gives user the ability to trigger a second set of events through a button press")]
    [Header("Input Settings.")]
    [SerializeField] private InputActionReference buttonInteractble;

    [Header("Events")]
    [Tooltip("Runs when the player presses the interact button while inside the zone.")]
    [SerializeField] private UnityEvent onButtonTriggered;

    [Tooltip("Runs when the player first enters the zone.")]
    [SerializeField] private UnityEvent onPlayerEntered;

    [Tooltip("Runs when the player completely exits the zone.")]
    [SerializeField] private UnityEvent onPlayerExited;

    private readonly HashSet<Collider> playerColliders = new();

    private bool activatedThisVisit;
    private bool hasEverActivated;
    private bool enabledInputHere;

    private bool PlayerIsInside => playerColliders.Count > 0;

    // Begins listening for the interact button.
    private void OnEnable()
    {
        if (buttonInteractble == null)
        {
            return;
        }

        buttonInteractble.action.performed += OnInteractPerformed;

        if (!buttonInteractble.action.enabled)
        {
            buttonInteractble.action.Enable();
            enabledInputHere = true;
        }
    }

    // Stops listening for the interact button.
    private void OnDisable()
    {
        if (buttonInteractble == null)
        {
            return;
        }

        buttonInteractble.action.performed -= OnInteractPerformed;

        if (enabledInputHere)
        {
            buttonInteractble.action.Disable();
            enabledInputHere = false;
        }
    }

    // Runs when the player first enters the zone.
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        bool wasAlreadyInside = PlayerIsInside;
        playerColliders.Add(other);

        
        if (wasAlreadyInside)
        {
            return;
        }

        activatedThisVisit = false;
        onPlayerEntered?.Invoke();
    }

    // Runs once the player has completely left the zone.
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerColliders.Remove(other);

        
        if (PlayerIsInside)
        {
            return;
        }

        activatedThisVisit = false;
        onPlayerExited?.Invoke();
    }

    // Runs On Triggered when the player presses the interaction button
    // while inside the zone.
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!PlayerIsInside)
        {
            return;
        }

        if (!repeatable && hasEverActivated)
        {
            return;
        }

        if (activatedOnce && activatedThisVisit)
        {
            return;
        }

        activatedThisVisit = true;
        hasEverActivated = true;

        onButtonTriggered?.Invoke();
    }

    // Checks whether the collider belongs to the player.
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag);
    }
}