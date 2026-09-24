/*
 * Author: Lam Nguyen
 * Created: 8/27/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    // Sens will be configurable in the main menu
    private float horizontalSens;
    private float verticalSens;

    private float pitch;

    private Transform playerBody;
    private InputAction lookAction;

    // Obtain variable references
    private void Awake()
    {
        playerBody = transform.parent;
        PlayerInput playerInput = playerBody.GetComponent<PlayerInput>();
        lookAction = playerInput.actions["Look"];
    }

    // Set default sensitivity and lock cursor
    private void Start()
    {
        SetSensitivity(GameSettings.GetSensitivity());

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetSensitivity(float value)
    {
        horizontalSens = value;
        verticalSens = value;
    }

    // On update, check for changes in mouse movement and either rotate the player horizontally or rotate the camera holder vertically
    private void Update()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * horizontalSens;
        float mouseY = lookInput.y * verticalSens;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}