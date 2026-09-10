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

    private void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        playerMovement = GetComponentInParent<PlayerMovement>();
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
        if (watchEnabled)
        {
            DisableWatch();
        }
    }

    private void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            watchAnimator.SetTrigger(ToggleTrigger);
        }
    }

    public void EnableWatch()
    {
        watchEnabled = true;
        playerMovement.SetMovementEnabled(false);
        lookAction.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisableWatch()
    {
        watchEnabled = false;
        playerMovement.SetMovementEnabled(true);
        lookAction.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
}
