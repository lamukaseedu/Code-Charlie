using UnityEngine;
using UnityEngine.AI;

// Awareness owns perception; this component owns the Rigidbody and root rotation.
[RequireComponent(typeof(EnemyAwareness), typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float investigateSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float stopDistance = 0.2f;
    public float acceleration = 12f;
    [Header("Turning")]
    public float turnSpeed = 180f;
    [Header("Patrol")]
    public Transform[] patrolPoints = new Transform[0];
    [Min(0f)] public float patrolSpeed = 1.5f;
    [Min(0f)] public float patrolWaitTime = 2f;
    [Header("Navigation")]
    [Tooltip("Must match the baked NavMesh surface's agent type.")]
    public int agentTypeID;
    public int areaMask = NavMesh.AllAreas;
    [Min(0.05f)] public float repathInterval = 0.25f;
    [Min(0.05f)] public float sampleDistance = 3f;
    [Tooltip("Maximum horizontal adjustment of a destination onto the mesh.")]
    [Min(0.01f)] public float destinationProjectionLimit = 1f;
    [Min(0.01f)] public float cornerTolerance = 0.1f;
    [Header("Waypoint Debug")]
    [SerializeField] private Vector3 debugWaypoint;
    [SerializeField] private bool debugHasWaypoint;
    [SerializeField] private EnemyAwareness.AwarenessState debugState;
    [SerializeField] private float debugDistance;
    [SerializeField] private float debugTurnAngle;
    [SerializeField] private string navigationStatus = "Idle";
    [SerializeField] private int patrolIndex;
    public string NavigationStatus => navigationStatus;
    public int PatrolIndex => patrolIndex;
    public Vector3[] PathCorners => (Vector3[])corners.Clone();

    private EnemyAwareness awareness;
    private Rigidbody rb;
    private Collider bodyCollider;
    private NavMeshPath path;
    private Vector3[] corners = new Vector3[0];
    private int cornerIndex;
    private float nextRepath;
    private float waitRemaining;
    private Vector3 requestedTarget;
    private bool hasRequest;
    private bool requestIsPatrol;
    private bool patrolWaiting;
    private string lastFailure;

    private void Awake()
    {
        awareness = GetComponent<EnemyAwareness>();
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        path = new NavMeshPath();
    }

    private void OnDisable()
    {
        hasRequest = false;
        corners = new Vector3[0];
    }

    private void FixedUpdate()
    {
        debugHasWaypoint = false;
        if (awareness == null || !awareness.isActiveAndEnabled)
        {
            Idle("Awareness disabled");
            return;
        }
        debugState = awareness.State;
        if (awareness.State == EnemyAwareness.AwarenessState.Unaware && awareness.HasWaypoint)
        {
            // Suspicion-only proximity can turn Lester, but cannot start a pursuit.
            patrolWaiting = false;
            debugWaypoint = awareness.CurrentWaypoint;
            debugHasWaypoint = true;
            TurnTowards(debugWaypoint - rb.position);
            Idle("Turning toward suspicion");
            return;
        }

        bool patrol = awareness.State == EnemyAwareness.AwarenessState.Unaware;
        // After an interruption, return to the same point before starting its wait again.
        if (!patrol) patrolWaiting = false;
        Vector3 target;
        float speed;
        if (patrol)
        {
            if (!TryGetPatrolTarget(out target)) { Idle("No patrol points"); return; }
            if (patrolWaiting)
            {
                waitRemaining -= Time.fixedDeltaTime;
                Idle("Waiting at patrol point");
                if (waitRemaining <= 0f)
                {
                    patrolWaiting = false;
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                }
                return;
            }
            speed = patrolSpeed;
        }
        else
        {
            if (!awareness.HasWaypoint) { Idle("Waiting for awareness"); return; }
            target = awareness.CurrentWaypoint;
            speed = awareness.State == EnemyAwareness.AwarenessState.Active ? chaseSpeed : investigateSpeed;
        }
        debugWaypoint = target;
        debugHasWaypoint = true;
        debugDistance = Horizontal(target - rb.position).magnitude;
        bool changed = !hasRequest || patrol != requestIsPatrol || (target - requestedTarget).sqrMagnitude > 0.01f;
        if (!hasRequest || patrol != requestIsPatrol || Time.fixedTime >= nextRepath)
        {
            // Recalculate periodically even for stationary targets: impulses and moved obstacles matter.
            requestedTarget = target;
            requestIsPatrol = patrol;
            hasRequest = true;
            nextRepath = Time.fixedTime + Mathf.Max(0.05f, repathInterval);
            BuildPath(target);
        }
        if (corners.Length == 0) { StopAtWaypoint(); return; }

        Vector3 delta = Horizontal(corners[cornerIndex] - rb.position);
        float distance = delta.magnitude;
        bool finalCorner = cornerIndex == corners.Length - 1;
        float radius = finalCorner ? Mathf.Max(0.02f, stopDistance) : Mathf.Max(0.01f, cornerTolerance);
        float horizontalSpeed = Horizontal(rb.linearVelocity).magnitude;
        if (distance <= radius && horizontalSpeed <= 0.05f)
        {
            if (!finalCorner) cornerIndex++;
            else if (!changed)
            {
                if (patrol)
                {
                    patrolWaiting = true;
                    waitRemaining = Mathf.Max(0f, patrolWaitTime);
                }
                else if (awareness.State == EnemyAwareness.AwarenessState.Alert) awareness.ClearWaypoint();
                // Active retains its destination even after reaching it.
                navigationStatus = patrol ? "Waiting at patrol point" : "At remembered destination";
                StopAtWaypoint();
                return;
            }
            delta = Horizontal(corners[cornerIndex] - rb.position);
            distance = delta.magnitude;
        }
        TurnTowards(delta);
        // Brake at corners as well as the destination; no high-speed cutting across walls.
        float brakingAcceleration = Mathf.Max(0f, acceleration);
        float stepVelocity = brakingAcceleration * Time.fixedDeltaTime;
        float brakingDistance = Mathf.Max(0f, distance - radius * 0.5f);
        float arrivalSpeed = Mathf.Sqrt(stepVelocity * stepVelocity + 2f * brakingAcceleration * brakingDistance) - stepVelocity;
        AccelerateTowards(delta.normalized * Mathf.Min(Mathf.Max(0f, speed), arrivalSpeed));
    }

    private bool TryGetPatrolTarget(out Vector3 target)
    {
        target = Vector3.zero;
        if (patrolPoints == null || patrolPoints.Length == 0) return false;
        patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[patrolIndex] != null) { target = patrolPoints[patrolIndex].position; return true; }
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
        return false;
    }

    private void BuildPath(Vector3 target)
    {
        corners = new Vector3[0];
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = areaMask };
        Vector3 feet = rb.position;
        if (bodyCollider != null && bodyCollider.enabled) feet.y = bodyCollider.bounds.min.y;
        float sampleRadius = Mathf.Max(0.05f, sampleDistance);
        if (!NavMesh.SamplePosition(feet, out NavMeshHit start, sampleRadius, filter) ||
            Horizontal(start.position - feet).magnitude > 0.25f)
        {
            Fail("Lester is not on the baked NavMesh"); return;
        }
        if (!NavMesh.SamplePosition(target, out NavMeshHit end, sampleRadius, filter) ||
            Horizontal(end.position - target).magnitude > Mathf.Max(0.01f, destinationProjectionLimit))
        {
            Fail("Destination is outside the baked NavMesh"); return;
        }
        if (!NavMesh.CalculatePath(start.position, end.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            Fail("No complete path to the destination"); return;
        }
        corners = path.corners;
        if (corners.Length == 0) { Fail("Navigation returned an empty path"); return; }
        cornerIndex = corners.Length > 1 ? 1 : 0;
        lastFailure = null;
        navigationStatus = requestIsPatrol ? "Patrolling" : "Following remembered destination";
    }

    private void Fail(string reason)
    {
        navigationStatus = reason;
        if (lastFailure == reason) return;
        lastFailure = reason;
        Debug.LogWarning("Lester navigation: " + reason + ". Check the NavMesh surface, agent type and patrol points; retrying automatically.", this);
    }

    private static Vector3 Horizontal(Vector3 value) { value.y = 0f; return value; }

    private void TurnTowards(Vector3 direction)
    {
        direction = Horizontal(direction);
        debugTurnAngle = direction.sqrMagnitude > 0.000001f ? Vector3.SignedAngle(transform.forward, direction, Vector3.up) : 0f;
        if (direction.sqrMagnitude <= 0.000001f) return;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(direction, Vector3.up), Mathf.Max(0f, turnSpeed) * Time.fixedDeltaTime));
    }

    private void AccelerateTowards(Vector3 desiredVelocity)
    {
        Vector3 velocityChange = desiredVelocity - Horizontal(rb.linearVelocity);
        rb.AddForce(Vector3.ClampMagnitude(velocityChange, Mathf.Max(0f, acceleration) * Time.fixedDeltaTime), ForceMode.VelocityChange);
    }

    private void StopAtWaypoint()
    {
        AccelerateTowards(Vector3.zero);
        rb.angularVelocity = Vector3.zero;
    }

    private void Idle(string status)
    {
        navigationStatus = status;
        hasRequest = false;
        corners = new Vector3[0];
        StopAtWaypoint();
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !debugHasWaypoint) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(debugWaypoint, 0.2f);
        Gizmos.color = Color.green;
        for (int i = 1; i < corners.Length; i++) Gizmos.DrawLine(corners[i - 1] + Vector3.up * 0.1f, corners[i] + Vector3.up * 0.1f);
    }
}
