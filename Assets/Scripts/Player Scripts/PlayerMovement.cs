/*
 * Author: Lam Nguyen
 * Created: 8/27/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float crouchSpeed = 0.75f;
    [SerializeField] float walkHeight = 2f;
    [SerializeField] float crouchHeight = 1.2f;
    
    private const float gravity = 10f;
    private bool isMoving = false;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    public bool IsSprinting => isActiveAndEnabled && isMoving && sprintAction != null && sprintAction.IsPressed();
    public bool IsWalking => isActiveAndEnabled && isMoving && !IsSprinting;

    // Assign variables
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        crouchAction = playerInput.actions["Crouch"];
    }

    // Reads input Player Controls and moves the player accordingly
    private void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;
        
        movement = Vector3.ClampMagnitude(movement, 1f);
        
        // Set a boolean that shows if the player is inputting a movement key
        if (moveInput.magnitude > 0f)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
        
        // If sprint button is pressed, increase speed
        float speed;
        float height;
        if (sprintAction.IsPressed())
        {
            speed = sprintSpeed;
            height = walkHeight;
        } else if (crouchAction.IsPressed())
        {
            speed = crouchSpeed;
            height = crouchHeight;
        } else
        {
            speed = walkSpeed;
            height = walkHeight;
        }

        movement *= speed;

        // Move downwards always so player stays on the floor
        movement.y = -gravity;
        
        controller.Move(movement * Time.deltaTime);
        controller.height = height;
    }
    
    public void SetMovementEnabled(bool enabled)
    {
        if (enabled)
        {
            moveAction.Enable();
            sprintAction.Enable();
            crouchAction.Enable();
        }
        else
        {
            moveAction.Disable();
            sprintAction.Disable();
            crouchAction.Disable();
            isMoving = false;
        }
    }

    // Return whether a movement key is being inputted.
    public bool CheckMovement()
    {
        return isMoving;
    }
}
