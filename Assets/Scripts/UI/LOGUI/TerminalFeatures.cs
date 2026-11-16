using UnityEngine;

public class TerminalFeatures : MonoBehaviour
{
    [Header("Available Features")]
    [SerializeField] private bool hasCircuitMinigame = false;

    [Header("Circuit UI")]
    [SerializeField] private GameObject circuitButton;
    [SerializeField] private GameObject circuitsPanel;

    [Header("Circuit Controller")]
    [SerializeField] private CircuitPuzzleManager circuitPuzzleManager;

    private void Start()
    {
        if (circuitButton != null)
            circuitButton.SetActive(hasCircuitMinigame);

        if (circuitsPanel != null)
            circuitsPanel.SetActive(false);
    }


    public void OpenCircuitMinigame()
    {
        if (!hasCircuitMinigame || circuitsPanel == null)
            return;

        circuitsPanel.SetActive(true);

        if (circuitPuzzleManager != null)
            circuitPuzzleManager.RefreshBeamOnOpen();
    }


    public void CloseCircuitMinigame()
    {
        if (circuitsPanel != null)
            circuitsPanel.SetActive(false);
    }
}
