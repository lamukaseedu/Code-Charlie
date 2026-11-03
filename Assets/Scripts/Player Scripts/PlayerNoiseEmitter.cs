using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerMovement), typeof(NoiseEmitter))]
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Min(0.01f)] public float walkingInterval = 0.5f;
    [Min(0.01f)] public float runningInterval = 0.3f;
    [Min(0f)] public float minimumTravelSpeed = 0.1f;
    [Min(0.1f)] public float teleportSpeedThreshold = 30f;
    private CharacterController controller;
    private PlayerMovement movement;
    private NoiseEmitter emitter;
    private Vector3 previousPosition;
    private float walkingTimer;
    private float runningTimer;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        emitter = GetComponent<NoiseEmitter>();
    }
    private void OnEnable()
    {
        previousPosition = transform.position;
        walkingTimer = 0f;
        runningTimer = 0f;
    }
    private void LateUpdate()
    {
        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;
        displacement.y = 0f;
        if (Time.deltaTime <= 0f) return;
        float speed = displacement.magnitude / Time.deltaTime;
        bool travelling = controller.enabled && controller.isGrounded &&
            speed >= minimumTravelSpeed && speed < teleportSpeedThreshold;
        if (!travelling || !movement.isActiveAndEnabled)
        {
            walkingTimer = 0f;
            runningTimer = 0f;
            return;
        }
        if (movement.IsSprinting)
        {
            walkingTimer = 0f;
            runningTimer += Time.deltaTime;
            if (runningTimer >= Mathf.Max(0.01f, runningInterval))
            {
                runningTimer %= Mathf.Max(0.01f, runningInterval);
                emitter.EmitMedium();
            }
        }
        else if (movement.IsWalking)
        {
            runningTimer = 0f;
            walkingTimer += Time.deltaTime;
            if (walkingTimer >= Mathf.Max(0.01f, walkingInterval))
            {
                walkingTimer %= Mathf.Max(0.01f, walkingInterval);
                emitter.EmitSoft();
            }
        }
        else { walkingTimer = 0f; runningTimer = 0f; }
    }
}
