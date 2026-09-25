using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/8/2026
     */

    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private LayerMask blockRaycastLayers;

    [Header("Input")]
    [SerializeField] private InputActionReference[] interactActions;

    [Header("UI")]
    [SerializeField] private InteractionPromptUI interactionPromptUI;

    private IInteractable currentInteractable;

    //Begins listening for user to press the key to activate interactable 
    private void OnEnable()
    {
        foreach (InputActionReference actionReference in interactActions)
        {
            if (actionReference != null)
            {
                actionReference.action.performed += OnInteract;
            }
        }
    }

    //Stops listening for user to press the key to activate interactable
    private void OnDisable()
    {
        foreach (InputActionReference actionReference in interactActions)
        {
            if (actionReference != null)
            {
                actionReference.action.performed -= OnInteract;
            }
        }
        ClearCurrentInteractable();
    }

    //Constantly finding objects that lie in the interactable layer
    private void Update()
    {
        FindInteractable();
    }

    //Finds Interactable but filters out what raycast layers to ignore and what raycast layers contain the interactables. 
    private void FindInteractable()
    {
        IInteractable detectedInteractable = null;

        int raycastLayers =
        interactionLayer.value | blockRaycastLayers.value;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (playerCamera != null &&
            Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
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
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = playerCamera.ScreenPointToRay(mousePosition);

            if (playerCamera != null &&
            Physics.Raycast(
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
        }
        

        // The player is still looking at the same object.
        if (detectedInteractable == currentInteractable)
        {
            return;
        }

        // The player stopped looking at the previous object.
        currentInteractable?.Unhover();
        currentInteractable = detectedInteractable;

        if (currentInteractable != null)
        {
            currentInteractable.Hover();
            interactionPromptUI?.Show(
                currentInteractable.InteractionPrompt
            );
        }
        else
        {
            interactionPromptUI?.Hide();
        }
    }

    public void RefreshTarget()
    {
        ClearCurrentInteractable();
        FindInteractable();
    }

    public void ClearTarget()
    {
        ClearCurrentInteractable();
    }
    private void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log($"E received. Target: {currentInteractable}");
        currentInteractable?.Interact();
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable?.Unhover();
        currentInteractable = null;

        interactionPromptUI?.Hide();
    }

    //Helps for debugging raycast
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * interactionDistance
        );
    }

    //Unlocks cursor when needed
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    //Locks cursor when needed
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}