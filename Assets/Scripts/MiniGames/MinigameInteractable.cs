using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class MinigameInteractable : MonoBehaviour, IInteractable
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/13/2026
     */

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera interactableCamera;

    [Header("Player Control")]
    [SerializeField] private PlayerCamera playerCameraController;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionReference exitInteractableAction;
    [SerializeField] private string defaultActionMap = "Player";
    [SerializeField] private string interactableActionMap = "Minigame";

    [Header("Transition")]
    [Tooltip("Set this to the Cinemachine Brain blend duration.")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private int activeCameraPriority = 20;
    [SerializeField] private int inactiveCameraPriority = 10;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursorWhileActive = true;

    [Header("Hover Highlight")]
    [Tooltip("Renderer whose color changes while the player looks at this interactable.")]
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);

    [Header("InteractionPrompt")]
    [SerializeField] private string interactionPrompt;
    public string InteractionPrompt => interactionPrompt;

    [Header("Interaction Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onUnhover;
    [SerializeField] private UnityEvent onInteractableEntered;
    [SerializeField] private UnityEvent onInteractableExited;

    [Header("Player Visuals")]
    [Tooltip("Only assign the renderers for the player's visible body/model.")]
    [SerializeField] private Renderer[] playerRenderers;

    public bool IsActive { get; private set; }
    public bool IsTransitioning { get; private set; }

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisibility;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Color normalColor = Color.white;
    private int colorPropertyId;

    private void Awake()
    {
        if (highlightRenderer == null)
            highlightRenderer = GetComponentInChildren<Renderer>();

        CacheMaterialColor();
    }

    private void OnEnable()
    {
        if (exitInteractableAction != null)
            exitInteractableAction.action.performed += OnExitInput;
    }

    private void OnDisable()
    {
        if (exitInteractableAction != null)
            exitInteractableAction.action.performed -= OnExitInput;

        SetHighlighted(false);
    }

    // Called by PlayerInteraction when its raycast begins targeting this object.
    public void Hover()
    {
        if (!IsActive && !IsTransitioning)
        {
            SetHighlighted(true);
            onHover?.Invoke();
        }
    }

    // Called by PlayerInteraction when its raycast stops targeting this object.
    public void Unhover()
    {
        if (!IsActive)
        {
            SetHighlighted(false);
            onUnhover?.Invoke();
        }
    }

    // Called by PlayerInteraction when the player presses the interact button.
    public void Interact()
    {
        EnterInteractable();
    }

    public void EnterInteractable()
    {
        Debug.Log($"Enter requested. Active: {IsActive}, Transitioning: {IsTransitioning}");

        if (IsActive || IsTransitioning)
            return;

        StartCoroutine(EnterRoutine());
    }

    public void ExitInteractable()
    {
        if (!IsActive || IsTransitioning)
            return;

        StartCoroutine(ExitRoutine());
    }

    //Run this routine for entering the minigame which does a couple things such as disabling the highlighted interactable object, disable the default player map, and activate the minigame action map.
    private IEnumerator EnterRoutine()
    {
        IsTransitioning = true;
        SetHighlighted(false);
        onUnhover?.Invoke();

        playerInput.currentActionMap.Disable();

        if (playerCameraController != null)
            playerCameraController.enabled = false;

        SetPlayerVisible(false);

        interactableCamera.Priority = activeCameraPriority;
        playerCamera.Priority = inactiveCameraPriority;

        yield return new WaitForSeconds(transitionDuration);

        playerInput.SwitchCurrentActionMap(interactableActionMap);

        if (unlockCursorWhileActive)
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisibility = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        IsActive = true;
        IsTransitioning = false;
        onInteractableEntered?.Invoke();
    }

    //Run this routine when exiting the minigame which disables the minigame action map, reenables the default player action map, locks the cursor and switches the cameras. 
    private IEnumerator ExitRoutine()
    {
        IsTransitioning = true;
        onInteractableExited?.Invoke();

        playerInput.currentActionMap.Disable();

        playerCamera.Priority = activeCameraPriority;
        interactableCamera.Priority = inactiveCameraPriority;

        yield return new WaitForSeconds(transitionDuration);

        SetPlayerVisible(true);

        playerInput.SwitchCurrentActionMap(defaultActionMap);

        if (playerCameraController != null)
            playerCameraController.enabled = true;

        if (unlockCursorWhileActive)
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisibility;
        }

        IsActive = false;
        IsTransitioning = false;
        playerInteraction?.RefreshTarget();
    }

    private void OnExitInput(InputAction.CallbackContext context)
    {
        ExitInteractable();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightRenderer == null || colorPropertyId == 0)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        highlightRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(
            colorPropertyId,
            highlighted ? highlightColor : normalColor
        );
        highlightRenderer.SetPropertyBlock(propertyBlock);
    }

    //Shows or hides the player's model without disabling the actual player.
    private void SetPlayerVisible(bool visible)
    {
        foreach (Renderer playerRenderer in playerRenderers)
        {
            if (playerRenderer != null)
                playerRenderer.enabled = visible;
        }
    }

    private void CacheMaterialColor()
    {
        if (highlightRenderer == null || highlightRenderer.sharedMaterial == null)
            return;

        Material material = highlightRenderer.sharedMaterial;

        if (material.HasProperty(BaseColorId))
            colorPropertyId = BaseColorId;
        else if (material.HasProperty(ColorId))
            colorPropertyId = ColorId;
        else
        {
            Debug.LogWarning(
                "The interactable material has no _BaseColor or _Color property.",
                this
            );
            return;
        }

        normalColor = material.GetColor(colorPropertyId);
    }
}
