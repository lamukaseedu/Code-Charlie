using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/8/2026
     */

    [Header("Raycast")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private LayerMask blockRaycastLayers;

    [Header("Input")]
    [SerializeField] private InputActionReference[] interactActions;

    private Camera playerCamera;
    private InteractionPromptUI interactionPromptUI;
    private IInteractable currentInteractable;

    private bool interactionLocked;

    private void Awake()
    {
        // Automatically find the Main Camera and interaction prompt UI.
        playerCamera = Camera.main;

        interactionPromptUI =
            FindFirstObjectByType<InteractionPromptUI>(
                FindObjectsInactive.Include
            );

        if (playerCamera == null)
            Debug.LogWarning("Main Camera not found!", this);

        if (interactionPromptUI == null)
            Debug.LogWarning("InteractionPromptUI not found!", this);
    }

    // Begins listening for interaction inputs.
    private void OnEnable()
    {
        if (interactActions == null)
            return;

        foreach (InputActionReference actionReference in interactActions)
        {
            if (actionReference != null)
                actionReference.action.performed += OnInteract;
        }
    }

    // Stops listening for interaction inputs.
    private void OnDisable()
    {
        if (interactActions != null)
        {
            foreach (InputActionReference actionReference in interactActions)
            {
                if (actionReference != null)
                    actionReference.action.performed -= OnInteract;
            }
        }

        ClearCurrentInteractable();
    }

    private void Update()
    {
        if (interactionLocked ||
            Time.timeScale == 0f ||
            WorkbenchUI.AnyOpen)
        {
            ClearCurrentInteractable();
            return;
        }

        FindInteractable();
    }

    // Finds an interactable using a raycast from the player camera.
    private void FindInteractable()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
                return;
        }

        IInteractable detectedInteractable = null;

        int raycastLayers =
            interactionLayer.value | blockRaycastLayers.value;

        Ray ray;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            ray = new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );
        }
        else
        {
            if (Mouse.current == null)
                return;

            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            ray = playerCamera.ScreenPointToRay(mousePosition);
        }

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            raycastLayers,
            QueryTriggerInteraction.Collide))
        {
            int hitLayer = hit.collider.gameObject.layer;

            bool hitIsInteractable =
                (interactionLayer.value & (1 << hitLayer)) != 0;

            if (hitIsInteractable)
            {
                detectedInteractable =
                    hit.collider.GetComponentInParent<IInteractable>();
            }
        }

        // The player is still looking at the same interactable.
        if (detectedInteractable == currentInteractable)
            return;

        // Clear the previous interactable and its prompt.
        ClearCurrentInteractable();

        currentInteractable = detectedInteractable;

        if (currentInteractable != null)
        {
            currentInteractable.Hover();

            interactionPromptUI?.Show(
                currentInteractable.InteractionPrompt
            );
        }
    }

    // Called when the player presses an interaction button.
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (interactionLocked ||
            Time.timeScale == 0f ||
            WorkbenchUI.AnyOpen)
            return;

        currentInteractable?.Interact();
    }

    // Locks or unlocks interaction, mainly for minigames.
    public void SetInteractionLocked(bool locked)
    {
        interactionLocked = locked;

        if (locked)
        {
            ClearCurrentInteractable();
        }
        else
        {
            RefreshTarget();
        }
    }

    // Rechecks the current object and refreshes its prompt.
    public void RefreshTarget()
    {
        ClearCurrentInteractable();

        if (!interactionLocked &&
            Time.timeScale != 0f &&
            !WorkbenchUI.AnyOpen)
        {
            FindInteractable();
        }
    }

    // Updates the displayed prompt for the current interactable.
    public void RefreshPrompt(IInteractable interactable)
    {
        if (!interactionLocked &&
            currentInteractable == interactable)
        {
            interactionPromptUI?.Show(
                interactable.InteractionPrompt
            );
        }
    }

    // Clears the current interaction target.
    public void ClearTarget()
    {
        ClearCurrentInteractable();
    }

    private void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Unhover();
            currentInteractable = null;
        }

        interactionPromptUI?.Hide();
    }

    // Draws the interaction ray in the Scene view for debugging.
    private void OnDrawGizmosSelected()
    {
        Camera camera = playerCamera;

        if (camera == null)
            camera = Camera.main;

        if (camera == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawRay(
            camera.transform.position,
            camera.transform.forward * interactionDistance
        );
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

