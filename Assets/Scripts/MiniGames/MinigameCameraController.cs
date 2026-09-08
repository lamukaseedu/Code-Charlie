using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class MinigameCameraController : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/7/2026
     */
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera minigameCamera;

    [Header("Player Control")]
    [SerializeField] private PlayerCamera playerCameraController;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionReference exitMinigameAction;

    [SerializeField] private string defaultActionMap = "Player";
    [SerializeField] private string minigameActionMap = "Minigame";

    [Header("Transition")]
    [Tooltip("Set this to the Cinemachine Brain blend duration.")]
    [SerializeField] private float transitionDuration = 0.5f;

    private bool minigameActive;
    private bool transitioning;

    private void OnEnable()
    {
        if (exitMinigameAction != null)
        {
            exitMinigameAction.action.performed += OnExitInput;
        }
    }

    private void OnDisable()
    {
        if (exitMinigameAction != null)
        {
            exitMinigameAction.action.performed -= OnExitInput;
        }
    }

    public void EnterMinigame()
    {
        if (minigameActive || transitioning)
        {
            return;
        }

        StartCoroutine(EnterRoutine());
    }

    public void ExitMinigame()
    {
        if (!minigameActive || transitioning)
        {
            return;
        }

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        transitioning = true;

        playerInput.currentActionMap.Disable();

        if (playerCameraController != null)
        {
            playerCameraController.enabled = false;
        }

        minigameCamera.Priority = 20;
        playerCamera.Priority = 10;

        yield return new WaitForSeconds(transitionDuration);

        playerInput.SwitchCurrentActionMap(minigameActionMap);

        minigameActive = true;
        transitioning = false;
    }

    private IEnumerator ExitRoutine()
    {
        transitioning = true;

        playerInput.currentActionMap.Disable();

        playerCamera.Priority = 20;
        minigameCamera.Priority = 10;

        yield return new WaitForSeconds(transitionDuration);

        playerInput.SwitchCurrentActionMap(defaultActionMap);

        if (playerCameraController != null)
        {
            playerCameraController.enabled = true;
        }

        minigameActive = false;
        transitioning = false;
    }

    private void OnExitInput(InputAction.CallbackContext context)
    {
        ExitMinigame();
    }
}