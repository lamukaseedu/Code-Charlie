/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using UnityEngine;

public class WorkbenchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private WorkbenchUI workbenchUI;

    private InteractableHoverHighlight hoverHighlight;
    [SerializeField] private string interactionPrompt;
    public string InteractionPrompt => interactionPrompt;

    private void Awake()
    {
        if (workbenchUI == null)
            workbenchUI = GetComponent<WorkbenchUI>();
        hoverHighlight = GetComponentInChildren<InteractableHoverHighlight>();
    }

    public void Hover()
    {
        if (workbenchUI == null || !workbenchUI.IsOpen)
            hoverHighlight?.SetHighlighted(true);
    }

    public void Unhover()
    {
        hoverHighlight?.SetHighlighted(false);
    }

    public void Interact()
    {
        workbenchUI?.Open();
    }
}
