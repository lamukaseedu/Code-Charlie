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

    [Header("Input")]
    [SerializeField] private InputActionReference buttonInteractable;

    private IInteractable currentInteractable;

    //Begins listening for user to press the key to activate interactable 
    private void OnEnable()
    {

        buttonInteractable.action.performed += OnInteract;
        buttonInteractable.action.Enable();
    }

    //Stops listening for user to press the key to activate interactable
    private void OnDisable()
    {
        buttonInteractable.action.performed -= OnInteract;
        buttonInteractable.action.Disable();
        ClearCurrentInteractable();
    }

    //Constantly finding objects that lie in the interactable layer
    private void Update()
    {
        FindInteractable();
    }

    private void FindInteractable()
    {
        IInteractable detectedInteractable = null;

        if (playerCamera != null &&
            Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                interactionDistance,
                interactionLayer))
        {
            detectedInteractable =
                hit.collider.GetComponentInParent<IInteractable>();
        }

        // The player is still looking at the same object.
        if (detectedInteractable == currentInteractable)
        {
            return;
        }

        // The player stopped looking at the previous object.
        currentInteractable?.Unhover();

        currentInteractable = detectedInteractable;

        // The player started looking at a new object.
        currentInteractable?.Hover();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        currentInteractable?.Interact();
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable?.Unhover();
        currentInteractable = null;
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