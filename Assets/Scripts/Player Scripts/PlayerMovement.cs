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
    
    private const float gravity = 10f;
    private bool isMoving = false;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction sprintAction;

    // Assign variables
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
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
        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;

        movement *= speed;

        // Move downwards always so player stays on the floor
        movement.y = -gravity;
        
        controller.Move(movement * Time.deltaTime);
    }
    
    // Return whether a movement key is being inputted.
    public bool CheckMovement()
    {
        return isMoving;
    }
}