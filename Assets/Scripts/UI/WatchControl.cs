/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class WatchControl : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private Animator watchAnimator;
    private static readonly int ToggleTrigger = Animator.StringToHash("Toggle");
    private InputAction toggleAction;
    private InputAction lookAction;
    private bool watchEnabled = false;
    private PlayerInventory playerInventory;
    private PlayerInteraction playerInteraction;
    private bool opening;
    private bool closing;
    private bool interactionsSuspended;
    private bool interactionWasEnabled;

    public bool IsOpenOrTransitioning => watchEnabled || opening || closing;
    public bool IsOpen => watchEnabled && !closing;

    private void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerInventory = GetComponentInParent<PlayerInventory>();
        playerInteraction = GetComponentInParent<PlayerInteraction>();
        watchAnimator = GetComponent<Animator>();
        toggleAction = playerInput.actions.FindAction("Watch/Toggle");
        lookAction = playerInput.actions["Look"];
    }

    private void OnEnable()
    {
        toggleAction?.Enable();
    }

    private void OnDisable()
    {
        toggleAction?.Disable();
        if (IsOpenOrTransitioning)
        {
            DisableWatch();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f || toggleAction == null || opening || closing)
            return;

        if (toggleAction.WasPressedThisFrame())
        {
            if (watchEnabled)
                RequestClose();
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                SuspendWorldInteractions();
                opening = true;
                watchAnimator.SetTrigger(ToggleTrigger);
            }
        }
    }

    public void RequestClose()
    {
        if (!watchEnabled || opening || closing || Time.timeScale == 0f)
            return;
        closing = true;
        watchAnimator.SetTrigger(ToggleTrigger);
    }

    public void EnableWatch()
    {
        opening = false;
        watchEnabled = true;
        playerMovement.SetMovementEnabled(false);
        lookAction.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playerInventory != null) playerInventory.SetOpen(true);
    }

    public void DisableWatch()
    {
        opening = false;
        closing = false;
        watchEnabled = false;
        if (playerInventory != null) playerInventory.SetOpen(false);
        if (Time.timeScale == 0f)
            return;
        RestorePlayerControls();
    }

    // PauseMenu enables the player map on resume; reapply the watch's restrictions.
    public void RestorePlayerControls()
    {
        if (Time.timeScale == 0f)
            return;

        RestoreWorldInteractions();
        playerMovement.SetMovementEnabled(!watchEnabled);
        if (watchEnabled)
            lookAction.Disable();
        else
            lookAction.Enable();
        Cursor.lockState = watchEnabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = watchEnabled;
    }

    private void SuspendWorldInteractions()
    {
        if (interactionsSuspended) return;

        interactionWasEnabled = playerInteraction != null && playerInteraction.enabled;
        interactionsSuspended = true;

        if (playerInteraction != null) playerInteraction.enabled = false;
    }

    private void RestoreWorldInteractions()
    {
        if (!interactionsSuspended) return;

        if (playerInteraction != null) playerInteraction.enabled = interactionWasEnabled;

        interactionsSuspended = false;
    }

}
