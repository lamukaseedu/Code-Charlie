/*
 * Author: Savio Xavier
 * Created: 9/9/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GraphicRaycaster raycaster;
        
    private InputAction pauseAction;
    private InputActionMap playerMap;
    private bool isPaused;
    private WatchControl watchControl;

    private void Awake()
    {
        watchControl = playerInput.GetComponentInChildren<WatchControl>(true);
        playerMap = playerInput.actions.FindActionMap("Player");
        playerInput.actions.FindActionMap("UI").Enable();
        pauseAction = playerInput.actions["Pause"];
    }

    private void Start()
    {
        SetPaused(false);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
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
            raycaster.enabled = true;
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
        raycaster.enabled = false;
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
            CloseSettings();
            playerMap.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        playerMap.Enable();
        if (watchControl != null && watchControl.isActiveAndEnabled)
        {
            watchControl.RestorePlayerControls();
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
