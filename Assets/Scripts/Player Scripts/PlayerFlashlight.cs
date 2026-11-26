/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFlashlight : MonoBehaviour
{
    [SerializeField] Light flashlight;

    private InputAction flashlightAction;
    private bool minigameLocked;



    private void Awake()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        flashlightAction = playerInput.actions["Flashlight"];
    }

    private void Start()
    {
        flashlight.enabled = false;
    }

    public void SetMinigameLocked(bool locked)
    {
        minigameLocked = locked;

        if (locked)
            flashlight.enabled = false;
    }

    private void Update()
    {
        if (!minigameLocked && flashlightAction.WasPressedThisFrame())
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }

}
