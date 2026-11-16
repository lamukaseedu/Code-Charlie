using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MinigameInteractable : MonoBehaviour, IInteractable
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/13/2026
     */

    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineCamera interactableCamera;

    [Header("Transition")]
    [Tooltip("Set this to the Cinemachine Brain blend duration.")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private int activeCameraPriority = 20;
    [SerializeField] private int inactiveCameraPriority = 10;

    [Header("Minigame Interaction")]
    [SerializeField] private bool allowInteractionsWhileActive = false;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursorWhileActive = true;

    [Header("Hover Highlight")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);

    [Tooltip("Append the Hover Material while hovered instead of tinting the original materials.")]
    [SerializeField] private bool useHoverMaterial = false;

    [SerializeField] private Material hoverMaterial;

    [Header("Interaction Prompt")]
    [SerializeField] private string interactionPrompt;
    public string InteractionPrompt => interactionPrompt;

    [Header("Interaction Trigger")]
    [Tooltip("Optional interaction colliders to disable while active.")]
    [SerializeField] private Collider[] interactionTriggers;

    [Header("Interaction Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onUnhover;
    [SerializeField] private UnityEvent onInteractableEntered;
    [SerializeField] private UnityEvent onInteractableExited;


    public bool IsActive { get; private set; }
    public bool IsTransitioning { get; private set; }

    private PlayerMinigameContext player;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisibility;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private readonly Dictionary<Renderer, Material[]> originalMaterials =
    new Dictionary<Renderer, Material[]>();

    private Material hoverMaterialInstance;

    private bool[] originalTriggerStates;
    private bool exitSubscribed;

    private void Awake()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        InitializePlayer();
    }

    private bool InitializePlayer()
    {
        if (player == null)
            player = PlayerMinigameContext.Instance;

        if (player == null)
        {
            Debug.LogError(
                "PlayerMinigameContext not found!", this);
            return false;
        }

        if (interactableCamera == null ||
            player.PlayerCamera == null ||
            player.Input == null)
        {
            Debug.LogError(
                "Missing camera or player input reference!", this);
            return false;
        }

        return true;
    }

    private void SubscribeToExit()
    {
        if (exitSubscribed || player.ExitAction == null)
            return;

        player.ExitAction.action.performed += OnExitInput;
        exitSubscribed = true;
    }

    private void UnsubscribeFromExit()
    {
        if (!exitSubscribed || player == null)
            return;

        if (player.ExitAction != null)
            player.ExitAction.action.performed -= OnExitInput;

        exitSubscribed = false;
    }

    private void OnEnable()
    {
        if (IsActive)
            player?.SetMinigameActive(this, true);
    }

    private void OnDisable()
    {
        UnsubscribeFromExit();
        SetHighlighted(false);
        if (player != null)
            player.SetMinigameActive(this, false);
    }

    // Called when the player looks at this interactable.
    public void Hover()
    {
        if (IsActive || IsTransitioning)
            return;

        SetHighlighted(true);
        onHover?.Invoke();
    }

    // Called when the player stops looking at this interactable.
    public void Unhover()
    {
        if (IsActive)
            return;

        SetHighlighted(false);
        onUnhover?.Invoke();
    }

    public void SetInteractionPrompt(string newPrompt)
    {
        interactionPrompt = newPrompt;

        if (player == null)
            player = PlayerMinigameContext.Instance;

        player?.Interaction?.RefreshPrompt(this);
    }

    public void Interact()
    {
        EnterInteractable();
    }

    public void EnterInteractable()
    {
        if (IsActive || IsTransitioning)
            return;

        if (!InitializePlayer())
            return;

        StartCoroutine(EnterRoutine());
    }

    public void ExitInteractable()
    {
        if (!IsActive || IsTransitioning)
            return;

        StartCoroutine(ExitRoutine());
    }

    // Handles entering the minigame.
    private IEnumerator EnterRoutine()
    {
        IsTransitioning = true;
        if (!allowInteractionsWhileActive)
        {
            player.Interaction?.SetInteractionLocked(true);
        } else
        {
            player.Interaction?.ClearTarget();
        }
        SetHighlighted(false);
        onUnhover?.Invoke();

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;

        // Disable interaction colliders.
        SetInteractionTriggers(false);

        // Disable regular player controls.
        player.Input.currentActionMap?.Disable();

        if (player.CameraController != null)
            player.CameraController.enabled = false;

        // Hide the player model and held item.
        player.SetPlayerVisible(false);

        // Switch cameras.
        interactableCamera.Priority = activeCameraPriority;
        player.PlayerCamera.Priority = inactiveCameraPriority;

        yield return new WaitForSeconds(transitionDuration);

        // Enable minigame input.
        player.Input.SwitchCurrentActionMap(
            player.InteractableActionMap
        );

        SubscribeToExit();

        if (unlockCursorWhileActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        IsActive = true;
        IsTransitioning = false;
        player.SetMinigameActive(this, true);

        onInteractableEntered?.Invoke();
    }

    // Handles exiting the minigame.
    private IEnumerator ExitRoutine()
    {
        IsTransitioning = true;

        onInteractableExited?.Invoke();

        UnsubscribeFromExit();

        player.Input.currentActionMap?.Disable();

        // Switch back to the player camera.
        player.PlayerCamera.Priority = activeCameraPriority;
        interactableCamera.Priority = inactiveCameraPriority;

        yield return new WaitForSeconds(transitionDuration);

        // Restore player visuals.
        player.SetPlayerVisible(true);

        // Restore player controls.
        player.Input.SwitchCurrentActionMap(
            player.DefaultActionMap
        );

        if (player.CameraController != null)
            player.CameraController.enabled = true;

        // Restore cursor.
        if (unlockCursorWhileActive)
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisibility;
        }

        IsActive = false;
        player.SetMinigameActive(this, false);

        // Restore interaction colliders.
        SetInteractionTriggers(true);

        IsTransitioning = false;

        // Refresh the interaction prompt.
        if (!allowInteractionsWhileActive)
        {
            player.Interaction?.SetInteractionLocked(false);
        } else
        {
            player.Interaction?.RefreshTarget();
        }
    }

    private void OnExitInput(InputAction.CallbackContext context)
    {
        ExitInteractable();
    }

    // Disables and restores interaction colliders.
    private void SetInteractionTriggers(bool enabled)
    {
        if (interactionTriggers == null)
            return;

        if (!enabled)
            originalTriggerStates =
                new bool[interactionTriggers.Length];

        for (int i = 0; i < interactionTriggers.Length; i++)
        {
            Collider trigger = interactionTriggers[i];

            if (trigger == null)
                continue;

            if (!enabled)
            {
                originalTriggerStates[i] = trigger.enabled;
                trigger.enabled = false;
            }
            else if (originalTriggerStates != null &&
                     i < originalTriggerStates.Length)
            {
                trigger.enabled = originalTriggerStates[i];
            }
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightRenderers == null)
            return;

        if (useHoverMaterial && hoverMaterial != null)
        {
            if (highlighted)
                ApplyHoverMaterial();
            else
                RestoreOriginalMaterials();

            return;
        }

        // Also remove overlays if the optional mode was changed during play.
        RestoreOriginalMaterials();

        var block = new MaterialPropertyBlock();

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                int colorId = material.HasProperty(BaseColorId)
                    ? BaseColorId
                    : material.HasProperty(ColorId) ? ColorId : 0;

                if (colorId == 0)
                    continue;

                renderer.GetPropertyBlock(block, i);

                block.SetColor(
                    colorId,
                    highlighted ? highlightColor : material.GetColor(colorId)
                );

                if (material.HasProperty("_EmissionColor"))
                {
                    block.SetColor(
                        "_EmissionColor",
                        highlighted
                            ? highlightColor * 3f
                            : material.GetColor("_EmissionColor")
                    );
                }

                renderer.SetPropertyBlock(block, i);
            }
        }
    }

    private void ApplyHoverMaterial()
    {
        // Use a private copy so changing the highlight never edits the material asset.
        if (hoverMaterialInstance == null)
            hoverMaterialInstance = new Material(hoverMaterial);

        if (hoverMaterialInstance.HasProperty(BaseColorId))
            hoverMaterialInstance.SetColor(BaseColorId, highlightColor);
        else if (hoverMaterialInstance.HasProperty(ColorId))
            hoverMaterialInstance.SetColor(ColorId, highlightColor);

        if (hoverMaterialInstance.HasProperty("_EmissionColor"))
        {
            hoverMaterialInstance.SetColor(
                "_EmissionColor",
                highlightColor * 3f
            );
        }

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null || originalMaterials.ContainsKey(renderer))
                continue;

            Material[] materials = renderer.sharedMaterials;
            originalMaterials.Add(renderer, materials);

            Material[] highlightedMaterials =
                new Material[materials.Length + 1];

            System.Array.Copy(
                materials,
                highlightedMaterials,
                materials.Length
            );

            highlightedMaterials[materials.Length] = hoverMaterialInstance;
            renderer.sharedMaterials = highlightedMaterials;
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
        {
            if (entry.Key != null)
                entry.Key.sharedMaterials = entry.Value;
        }

        originalMaterials.Clear();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();

        if (hoverMaterialInstance != null)
            Destroy(hoverMaterialInstance);
    }
}

