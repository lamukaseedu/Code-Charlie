using UnityEngine;

// EnemyMovement needs EnemyDetection to know WHERE and HOW to move.
[RequireComponent(typeof(EnemyDetection))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float investigateSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float stopDistance = 0.2f;

    [Header("Turning")]
    public float turnSpeed = 180f;

    private EnemyDetection detection;

    private void Awake()
    {
        detection = GetComponent<EnemyDetection>();
    }

    private void Update()
    {
        if (detection == null || !detection.HasWaypoint)
            return;

        Vector3 target = detection.CurrentWaypoint;
        target.y = transform.position.y;

        Vector3 moveDirection = target - transform.position;
        moveDirection.y = 0f;

        float distanceToTarget = moveDirection.magnitude;

        if (distanceToTarget > 0.0001f)
        {
            // Gradually turn toward the direction the enemy is moving.
            Quaternion targetRotation = Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        float speed = detection.State == EnemyDetection.DetectionState.Chasing
            ? chaseSpeed
            : investigateSpeed;

        if (distanceToTarget <= stopDistance)
        {
            detection.ClearWaypoint();
            return;
        }

        float step = speed * Time.deltaTime;
        float actualMove = Mathf.Min(step, distanceToTarget);

        transform.position += moveDirection.normalized * actualMove;
    }
}