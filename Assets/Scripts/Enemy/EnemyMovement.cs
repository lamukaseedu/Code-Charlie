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

    [Header("Waypoint Debug")]
    [SerializeField] private Vector3 debugWaypoint;
    [SerializeField] private bool debugHasWaypoint;
    [SerializeField] private EnemyDetection.DetectionState debugState;
    [SerializeField] private float debugDistance;
    [SerializeField] private float debugTurnAngle;

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
            StopAtWaypoint();
            return;
        }

        debugWaypoint = detection.CurrentWaypoint;
        debugHasWaypoint = detection.HasWaypoint;
        debugState = detection.State;

        Vector3 direction = debugWaypoint - rb.position;
        direction.y = 0f;

        debugDistance = direction.magnitude;

        debugTurnAngle = direction.sqrMagnitude > 0.000001f
            ? Vector3.SignedAngle(transform.forward, direction, Vector3.up)
            : 0f;

        Vector3 target = detection.CurrentWaypoint;
        target.y = rb.position.y;

        Vector3 moveDirection = target - rb.position;
        moveDirection.y = 0f;

        float distanceToTarget = moveDirection.magnitude;

        if (distanceToTarget <= stopDistance)
        {
            detection.ClearWaypoint();
            StopAtWaypoint();
            return;
        }

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

    private void StopAtWaypoint()
    {
        // Preserve vertical movement for gravity.
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);

        // Remove any remaining physical spin.
        rb.angularVelocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !debugHasWaypoint)
            return;

        Vector3 waypoint = debugWaypoint;
        waypoint.y = transform.position.y;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(waypoint, 0.2f);
        Gizmos.DrawLine(transform.position, waypoint);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
