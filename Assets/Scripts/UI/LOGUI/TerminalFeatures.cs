using UnityEngine;

public class TerminalFeatures : MonoBehaviour
{
    [Header("Available Features")]
    [SerializeField] private bool hasCircuitMinigame = false;

    [Header("Circuit UI")]
    [SerializeField] private GameObject circuitButton;
    [SerializeField] private GameObject circuitsPanel;

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
    }

    public void CloseCircuitMinigame()
    {
        if (circuitsPanel != null)
            circuitsPanel.SetActive(false);
    }
}
