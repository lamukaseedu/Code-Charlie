/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Graphic Raycaster on the pause Canvas, not the watch Canvas. Auto-found from Pause Menu Root when empty.")]
    [SerializeField] private GraphicRaycaster pauseRaycaster;

    private InputAction pauseAction;
    private InputActionMap uiMap;
    private InputActionMap actionMapBeforePause;
    private readonly List<InputAction> enabledActionsBeforePause = new List<InputAction>();

    private CursorLockMode cursorLockBeforePause;
    private bool cursorVisibilityBeforePause;
    private bool isPaused;

    private void Awake()
    {
        uiMap = playerInput.actions.FindActionMap("UI");
        uiMap.Enable();
        pauseAction = playerInput.actions["Pause"];
        if (pauseRaycaster == null)
        {
            Canvas pauseCanvas = pauseMenuRoot != null
                ? pauseMenuRoot.GetComponentInParent<Canvas>(true)
                : GetComponentInParent<Canvas>(true);
            if (pauseCanvas != null)
                pauseRaycaster = pauseCanvas.GetComponent<GraphicRaycaster>();
        }
        if (pauseRaycaster != null) pauseRaycaster.enabled = false;
    }

    private void Start()
    {
        SetPaused(false);
    }

    private void OnDisable()
    {
        if (isPaused) SetPaused(false);
        if (pauseRaycaster != null) pauseRaycaster.enabled = false;
    }

    private void Update()
    {
        if (!pauseAction.WasPressedThisFrame())
        {
            return;
        }

        if (!isPaused)
        {
            SetPaused(true);
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        Resume();
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void OpenSettings()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    private void SetPaused(bool paused)
    {
        if (paused && isPaused) return;
        bool wasPaused = isPaused;
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (pauseRaycaster != null) pauseRaycaster.enabled = paused;

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(paused);
        }

        if (paused)
        {
            //Remember the actionMap that the player was using before paused
            actionMapBeforePause = playerInput.currentActionMap;
            enabledActionsBeforePause.Clear();
            if (actionMapBeforePause != null)
            {
                foreach (InputAction action in actionMapBeforePause.actions)
                {
                    if (action.enabled) enabledActionsBeforePause.Add(action);
                }
            }

            //Also remember the cursor state that the player was using before paused
            cursorLockBeforePause = Cursor.lockState;
            cursorVisibilityBeforePause = Cursor.visible;


            CloseSettings();

            if (actionMapBeforePause != null)
                actionMapBeforePause.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            return;
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Startup has no saved input/cursor state to restore.
        if (!wasPaused) return;

        // Restore only the actions that were enabled in the player or minigame map.
        // The watch may have deliberately disabled movement and look before pausing.
        if (actionMapBeforePause != null)
        {
            foreach (InputAction action in enabledActionsBeforePause)
                action.Enable();
            actionMapBeforePause = null;
        }
        enabledActionsBeforePause.Clear();

        //Restore the correct cursor state for that map.
        Cursor.lockState = cursorLockBeforePause;
        Cursor.visible = cursorVisibilityBeforePause;
    }
}
