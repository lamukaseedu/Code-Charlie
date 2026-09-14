/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    private InputAction pauseAction;
    private InputActionMap uiMap;
    private InputActionMap actionMapBeforePause;

    private CursorLockMode cursorLockBeforePause;
    private bool cursorVisibilityBeforePause;
    private bool isPaused;

    private void Awake()
    {
        uiMap = playerInput.actions.FindActionMap("UI");
        uiMap.Enable();
        pauseAction = playerInput.actions["Pause"];
    }

    private void Start()
    {
        SetPaused(false);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;

        if (isPaused && actionMapBeforePause != null)
            actionMapBeforePause.Enable();
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
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(paused);
        }

        if (paused)
        {
            //Remember the actionMap that the player was using before paused
            actionMapBeforePause = playerInput.currentActionMap;

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

        //Restore whichever map was active before pausing.
        if (actionMapBeforePause != null)
        {
            actionMapBeforePause.Enable();
            actionMapBeforePause = null;
        }

        //Restore the correct cursor state for that map.
        Cursor.lockState = cursorLockBeforePause;
        Cursor.visible = cursorVisibilityBeforePause;
    }
}
