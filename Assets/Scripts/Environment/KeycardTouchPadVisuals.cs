using UnityEngine;
using UnityEngine.UI;

public class KeycardReaderVisuals : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Material requiresKeycardMaterial;
    [SerializeField] private Material acceptedKeycardMaterial;
    [SerializeField] private Light spotlight;

    private void Awake()
    {
        background.material = requiresKeycardMaterial;
        spotlight.color = Color.red;
    }

    public void ShowKeycardAccepted()
    {
        background.material = acceptedKeycardMaterial;
        spotlight.color = Color.green;
    }
}