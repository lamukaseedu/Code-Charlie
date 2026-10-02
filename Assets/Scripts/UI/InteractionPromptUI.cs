using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/25/2026
     */
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private TMP_Text promptText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (promptText != null)
            promptText.text = message;

        if (promptContainer != null)
            promptContainer.SetActive(true);
    }

    public void Hide()
    {
        if (promptContainer != null)
            promptContainer.SetActive(false);
    }
}