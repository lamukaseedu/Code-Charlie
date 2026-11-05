using Unity.Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMinigameContext : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/24/2026 
     */
    
    public static PlayerMinigameContext Instance { get; private set;}
    [Header("Camera")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private PlayerCamera playerCameraController;

    [Header("Interaction")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerInput playerInput;

    [Header("Input")]
    [SerializeField] InputActionReference exitInteractableAction;
    [SerializeField] private string defaultActionMap = "Player";
    [SerializeField] private string interactableActionMap = "Minigame";

    [Header("Minigame UI")]
    [Tooltip("Shown while the player is inside any minigame interactable.")]
    [SerializeField] private GameObject exitText;
    private readonly HashSet<MinigameInteractable> activeMinigames = new HashSet<MinigameInteractable>();

    [Header("Player Visuals")]
    [SerializeField] private GameObject playerItemHolder;
    [SerializeField] private Renderer[] playerRenderers;

    public CinemachineCamera PlayerCamera => playerCamera;
    public PlayerCamera CameraController => playerCameraController;
    public PlayerInteraction Interaction => playerInteraction;
    public PlayerInput Input => playerInput;
    public InputActionReference ExitAction => exitInteractableAction;
    public string DefaultActionMap => defaultActionMap;
    public string InteractableActionMap => interactableActionMap;

    private void Awake()
    {
        if (Instance!= null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
        RefreshExitText();
    }

    private void OnEnable()
    {
        RefreshExitText();
    }

    private void OnDisable()
    {
        if (exitText != null)
            exitText.SetActive(false);
    }

    public void SetMinigameActive(MinigameInteractable minigame, bool active)
    {
        if (active)
            activeMinigames.Add(minigame);
        else
            activeMinigames.Remove(minigame);

        RefreshExitText();
    }

    private void RefreshExitText()
    {
        if (exitText != null)
            exitText.SetActive(isActiveAndEnabled && activeMinigames.Count > 0);
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    public void SetPlayerVisible (bool visible)
    {
        if (playerItemHolder != null)
            playerItemHolder.SetActive(visible);
        if (playerRenderers == null)
            return;
        foreach (Renderer playerRenderer in playerRenderers)
        {
            if (playerRenderer != null)
                playerRenderer.enabled = visible;
        }
    }

}
