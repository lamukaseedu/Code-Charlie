using UnityEngine;
using UnityEngine.Events;

public class ComputerTerminal : MonoBehaviour, IInteractable
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/8/2026
     */

    [Header("Minigame")]
    [SerializeField] private MinigameCameraController cameraController;

    //The set of functions for when you hover over the terminal, stop hovering over the terminal and when you interact with the terminal
    [Header("Interaction Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onUnhover;
    [SerializeField] private UnityEvent onInteracted;

    //Defines the function from IInteractable and invokes the events for hovering over the computer terminal
    public void Hover()
    {
        onHover?.Invoke();
    }

    //Defines the function from IInteractable and invokes the events for not hovering over the comptuer terminal
    public void Unhover()
    {
        onUnhover?.Invoke();
    }

    //Defines the function from IInteractable and invokes the event when user presses button to activate interactable
    public void Interact()
    {
        onInteracted?.Invoke();
    }
}