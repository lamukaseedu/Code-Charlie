using UnityEngine;

// EnemyMovement needs EnemyDetection to know WHERE and HOW to move.
[RequireComponent(typeof(EnemyDetection))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float investigateSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float stopDistance = 0.2f;
    public float acceleration = 12f;

    [Header("Turning")]
    public float turnSpeed = 180f;

    private EnemyDetection detection;
    private Rigidbody rb;

    private void Awake()
    {
        detection = GetComponent<EnemyDetection>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (detection == null)
            return;

        if (!detection.HasWaypoint)
        {
            AccelerateTowards(Vector3.zero);
            return;
        }

        Vector3 target = detection.CurrentWaypoint;
        target.y = rb.position.y;

        Vector3 moveDirection = target - rb.position;
        moveDirection.y = 0f;

        float distanceToTarget = moveDirection.magnitude;

        if (distanceToTarget > 0.0001f)
        {
            // Gradually turn toward the direction the enemy is moving.
            Quaternion targetRotation = Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

            Quaternion nextRotation = Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );

            rb.MoveRotation(nextRotation);
        }

        float speed = detection.State == EnemyDetection.DetectionState.Chasing
            ? chaseSpeed
            : investigateSpeed;

        if (distanceToTarget <= stopDistance)
        {
            detection.ClearWaypoint();
            AccelerateTowards(Vector3.zero);
            return;
        }

        Vector3 desiredVelocity = moveDirection.normalized * speed;
        AccelerateTowards(desiredVelocity);
    }

    private void AccelerateTowards(Vector3 desiredVelocity)
    {
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(
            rb.linearVelocity,
            Vector3.up
        );

        Vector3 velocityChange = desiredVelocity - horizontalVelocity;
        float maxVelocityChange = acceleration * Time.fixedDeltaTime;
        velocityChange = Vector3.ClampMagnitude(
            velocityChange,
            maxVelocityChange
        );

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
}
