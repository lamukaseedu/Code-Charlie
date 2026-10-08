/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WatchControl : MonoBehaviour
{
    [SerializeField, Tooltip("Objects to hide while the watch is open.")]
    private GameObject[] objectsToHideWhileOpen;

    private readonly Dictionary<GameObject, bool> hiddenObjectStates = new Dictionary<GameObject, bool>();
    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private Animator watchAnimator;
    private static readonly int ToggleTrigger = Animator.StringToHash("Toggle");
    private InputAction toggleAction;
    private InputAction lookAction;
    private bool watchEnabled = false;
    private PlayerInventory playerInventory;
    private bool opening;
    private bool closing;

    public bool IsOpenOrTransitioning => watchEnabled || opening || closing;
    public bool IsOpen => watchEnabled && !closing;

    private void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerInventory = GetComponentInParent<PlayerInventory>();
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
        HideConfiguredObjects();
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
        RestoreHiddenObjects();
        if (playerInventory != null) playerInventory.SetOpen(false);
        if (Time.timeScale == 0f)
            return;
        RestorePlayerControls();
    }

    private void HideConfiguredObjects()
    {
        if (objectsToHideWhileOpen == null)
            return;

        foreach (GameObject target in objectsToHideWhileOpen)
        {
            if (target == null || hiddenObjectStates.ContainsKey(target))
                continue;

            // Keep this controller active so it can close the watch and restore objects.
            if (transform.IsChildOf(target.transform))
                continue;

            hiddenObjectStates.Add(target, target.activeSelf);
            target.SetActive(false);
        }
    }

    private void RestoreHiddenObjects()
    {
        foreach (KeyValuePair<GameObject, bool> entry in hiddenObjectStates)
        {
            if (entry.Key != null)
                entry.Key.SetActive(entry.Value);
        }

        hiddenObjectStates.Clear();
    }

    // PauseMenu enables the player map on resume; reapply the watch's restrictions.
    public void RestorePlayerControls()
    {
        if (Time.timeScale == 0f)
            return;

        playerMovement.SetMovementEnabled(!watchEnabled);
        if (watchEnabled)
            lookAction.Disable();
        else
            lookAction.Enable();
        Cursor.lockState = watchEnabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = watchEnabled;
    }
    
}
