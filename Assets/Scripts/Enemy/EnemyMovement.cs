using UnityEngine;
using UnityEngine.AI;

// Awareness owns perception; this component owns the Rigidbody and root rotation.
[RequireComponent(typeof(EnemyAwareness), typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Speeds")]
    [Tooltip("Movement speed in metres per second while investigating a remembered sight or sound.")]
    public float investigateSpeed = 1.5f;
    [Tooltip("Movement speed in metres per second while actively chasing the player.")]
    public float chaseSpeed = 3f;
    [Tooltip("Horizontal distance in metres at which the enemy can stop at its destination.")]
    public float stopDistance = 0.2f;
    [Tooltip("Maximum horizontal acceleration and braking in metres per second squared.")]
    public float acceleration = 12f;
    [Header("Turning")]
    [Tooltip("Maximum rotation speed in degrees per second.")]
    public float turnSpeed = 180f;
    [Header("Patrol")]
    [Tooltip("Movement speed in metres per second while unaware and patrolling.")]
    [Min(0f)] public float patrolSpeed = 1.5f;
    [Tooltip("Maximum horizontal distance in metres from the enemy when choosing each random, reachable NavMesh destination.")]
    [Min(1f)] public float patrolRadius = 15f;
    [Tooltip("Maximum horizontal distance in metres from the original spawn position for unaware patrols. Chasing, investigating and alert searches may leave this area; unaware patrol then returns inside it.")]
    [Min(0.1f)] public float maxPatrolDistance = 25f;
    [Tooltip("Horizontal distance in metres that counts as reaching a patrol destination. A new destination is chosen without waiting to stop.")]
    [Min(0.02f)] public float patrolArrivalDistance = 1f;
    [Header("Alert Search")]
    [Tooltip("Radius in metres of the local NavMesh search around a reached investigation or last seen position while Alert. Keep larger than Patrol Arrival Distance. Uses Investigate Speed until awareness changes or a new destination is remembered.")]
    [Min(0.1f)] public float alertSearchRadius = 4f;
    [Header("Navigation")]
    [Tooltip("Must match the baked NavMesh surface's agent type.")]
    public int agentTypeID;
    [Tooltip("Bitmask of NavMesh areas this enemy may sample and travel through. -1 allows all areas.")]
    public int areaMask = NavMesh.AllAreas;
    [Tooltip("Seconds between path recalculations and retries when no patrol destination is available.")]
    [Min(0.05f)] public float repathInterval = 0.25f;
    [Tooltip("Search radius in metres when projecting the enemy or a candidate destination onto the NavMesh.")]
    [Min(0.05f)] public float sampleDistance = 3f;
    [Tooltip("Maximum horizontal adjustment of a destination onto the mesh.")]
    [Min(0.01f)] public float destinationProjectionLimit = 1f;
    [Tooltip("Minimum turning distance in metres used to control speed near intermediate path corners. Larger values carry more speed through turns.")]
    [Min(0.01f)] public float cornerTolerance = 0.1f;
    [Header("Waypoint Debug")]
    [Tooltip("Current movement destination, or the suspicion position the enemy is facing.")]
    [SerializeField] private Vector3 debugWaypoint;
    [Tooltip("Whether the enemy has a movement or facing destination this physics step.")]
    [SerializeField] private bool debugHasWaypoint;
    [Tooltip("Awareness state most recently read by the movement controller.")]
    [SerializeField] private EnemyAwareness.AwarenessState debugState;
    [Tooltip("Last measured horizontal distance in metres to the movement destination.")]
    [SerializeField] private float debugDistance;
    [Tooltip("Last measured signed angle in degrees from forward to the desired direction.")]
    [SerializeField] private float debugTurnAngle;
    [Tooltip("Current navigation activity or the reason movement cannot continue.")]
    [SerializeField] private string navigationStatus = "Idle";
    [Tooltip("Number of random patrol and alert search destinations reached during this play session.")]
    [SerializeField] private int patrolIndex;
    [Tooltip("The currently selected random NavMesh patrol destination.")]
    [SerializeField] private Vector3 patrolTarget;
    [Tooltip("Whether a random patrol destination is currently selected.")]
    [SerializeField] private bool hasPatrolTarget;
    public string NavigationStatus => navigationStatus;
    public int PatrolIndex => patrolIndex;
    public Vector3 PatrolTarget => patrolTarget;
    public bool HasPatrolTarget => hasPatrolTarget;
    public bool IsAlertSearching => alertSearching;
    public Vector3[] PathCorners => (Vector3[])corners.Clone();

    private Animator enemyAnimation;
    private static readonly int WalkParameter = Animator.StringToHash("Walk");

    private EnemyAwareness awareness;
    private Rigidbody rb;
    private Collider bodyCollider;
    private NavMeshPath path;
    private Vector3[] corners = new Vector3[0];
    private int cornerIndex;
    private float nextRepath;
    private float nextPatrolSelection;
    private Vector3 requestedTarget;
    private bool hasRequest;
    private bool requestIsPatrol;
    private string lastFailure;
    private Vector3 spawnPosition;
    private bool alertSearching;
    private Vector3 alertSearchCenter;
    private Vector3 PatrolAreaCenter => alertSearching ? alertSearchCenter : spawnPosition;
    private float PatrolAreaRadius => Mathf.Max(0.1f, alertSearching ? alertSearchRadius : maxPatrolDistance);

    private void Awake()
    {
        awareness = GetComponent<EnemyAwareness>();
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        spawnPosition = rb.position;
        if (bodyCollider != null && bodyCollider.enabled) spawnPosition.y = bodyCollider.bounds.min.y;
        path = new NavMeshPath();
        enemyAnimation = GetComponentInChildren<Animator>();
        patrolIndex = 0;
        hasPatrolTarget = false;
    }

    private void Update()
    {
        // Animate actual motion, including bounded braking and external impulses.
        UpdateWalkAnimation(Horizontal(rb.linearVelocity).magnitude);
    }

    private void OnDisable()
    {
        hasRequest = false;
        hasPatrolTarget = false;
        alertSearching = false;
        nextPatrolSelection = 0f;
        corners = new Vector3[0];
        UpdateWalkAnimation(0f);
    }

    // Also used by EnemyWalkDemo when this motor is disabled.
    public void UpdateWalkAnimation(float movementSpeed)
    {
        if (enemyAnimation == null)
            return;

        bool isWalking = movementSpeed > 0.01f;
        enemyAnimation.SetBool(WalkParameter, isWalking);

        // Keep idle animations and transitions playing when the enemy stops.
        enemyAnimation.speed = isWalking
            ? (chaseSpeed > 0f ? Mathf.Clamp01(movementSpeed / chaseSpeed) : 0f)
            : 1f;
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
        // New evidence interrupts the local search, even when it points at the same location.
        if (alertSearching && (awareness.State != EnemyAwareness.AwarenessState.Alert || awareness.HasWaypoint))
        {
            alertSearching = false;
            hasPatrolTarget = false;
            hasRequest = false;
            nextPatrolSelection = 0f;
        }
        if (awareness.State == EnemyAwareness.AwarenessState.Unaware && awareness.HasWaypoint)
        {
            // Suspicion-only proximity can turn Lester, but cannot start a pursuit.
            hasPatrolTarget = false;
            debugWaypoint = awareness.CurrentWaypoint;
            debugHasWaypoint = true;
            TurnTowards(debugWaypoint - rb.position);
            Idle("Turning toward suspicion");
            return;
        }

        bool patrol = awareness.State == EnemyAwareness.AwarenessState.Unaware || alertSearching;
        // Resume wandering within the spawn area after investigating or chasing.
        if (!patrol) hasPatrolTarget = false;
        Vector3 target;
        float speed;
        if (patrol)
        {
            if (!TryGetPatrolTarget(out target)) { Idle(navigationStatus); return; }
            speed = alertSearching ? investigateSpeed : patrolSpeed;
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
        if (patrol && debugDistance <= Mathf.Max(0.02f, patrolArrivalDistance))
        {
            hasPatrolTarget = false;
            hasRequest = false;
            nextPatrolSelection = 0f;
            patrolIndex++;
            corners = new Vector3[0];
            navigationStatus = alertSearching ? "Choosing next alert search destination" : "Choosing next patrol destination";
            StopAtWaypoint();
            return;
        }
        bool changed = !hasRequest || patrol != requestIsPatrol || (target - requestedTarget).sqrMagnitude > 0.01f;
        if (!hasRequest || patrol != requestIsPatrol || Time.fixedTime >= nextRepath)
        {
            // Recalculate periodically even for stationary targets: impulses and moved obstacles matter.
            requestedTarget = target;
            requestIsPatrol = patrol;
            hasRequest = true;
            nextRepath = Time.fixedTime + Mathf.Max(0.05f, repathInterval);
            BuildPath(target);
            if (patrol && corners.Length == 0)
            {
                // The mesh may have changed since selection. Retry with a new point.
                hasPatrolTarget = false;
                nextPatrolSelection = nextRepath;
            }
        }
        if (corners.Length == 0) { StopAtWaypoint(); return; }

        AdvancePathCorners();
        Vector3 delta = Horizontal(corners[cornerIndex] - rb.position);
        float distance = delta.magnitude;
        bool finalCorner = cornerIndex == corners.Length - 1;
        float radius = finalCorner ? Mathf.Max(0.02f, patrol ? patrolArrivalDistance : stopDistance) : Mathf.Max(0.01f, cornerTolerance);
        float horizontalSpeed = Horizontal(rb.linearVelocity).magnitude;
        if (finalCorner && distance <= radius && horizontalSpeed <= 0.05f && !changed && !patrol)
        {
            if (awareness.State == EnemyAwareness.AwarenessState.Alert)
            {
                // Anchor the search to the reached NavMesh position, rather than following the player.
                alertSearchCenter = corners[corners.Length - 1];
                alertSearching = true;
                hasPatrolTarget = false;
                hasRequest = false;
                nextPatrolSelection = 0f;
                awareness.ClearWaypoint();
            }
            // Active retains its destination even after reaching it.
            navigationStatus = alertSearching ? "Choosing next alert search destination" : "At remembered destination";
            StopAtWaypoint();
            return;
        }
        TurnTowards(delta);
        // Slow for tight turns, but retain a nonzero approach speed at intermediate corners.
        // Only the final destination uses a braking distance that falls all the way to zero.
        float brakingAcceleration = Mathf.Max(0f, acceleration);
        float stepVelocity = brakingAcceleration * Time.fixedDeltaTime;
        float brakingDistance = finalCorner
            ? Mathf.Max(0f, distance - radius * 0.5f)
            : Mathf.Max(distance, radius);
        float arrivalSpeed = Mathf.Sqrt(stepVelocity * stepVelocity + 2f * brakingAcceleration * brakingDistance) - stepVelocity;
        AccelerateTowards(delta.normalized * Mathf.Min(Mathf.Max(0f, speed), arrivalSpeed));
    }

    private void AdvancePathCorners()
    {
        if (cornerIndex >= corners.Length - 1) return;
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = areaMask };
        Vector3 feet = rb.position;
        if (bodyCollider != null && bodyCollider.enabled) feet.y = bodyCollider.bounds.min.y;
        if (!NavMesh.SamplePosition(feet, out NavMeshHit start, Mathf.Max(0.05f, sampleDistance), filter) ||
            Horizontal(start.position - feet).magnitude > 0.25f) return;

        // Re-run after repaths too, so nearby or redundant corners cannot reset progress.
        // Never skip a bend if the direct segment crosses a NavMesh boundary or excluded area.
        while (cornerIndex < corners.Length - 1 &&
            !NavMesh.Raycast(start.position, corners[cornerIndex + 1], out _, filter))
        {
            cornerIndex++;
        }
    }

    private bool TryGetPatrolTarget(out Vector3 target)
    {
        target = patrolTarget;
        Vector3 areaCenter = PatrolAreaCenter;
        float homeRadius = PatrolAreaRadius;
        if (hasPatrolTarget && Horizontal(patrolTarget - areaCenter).sqrMagnitude <= homeRadius * homeRadius)
            return true;
        hasPatrolTarget = false;
        if (Time.fixedTime < nextPatrolSelection) return false;
        nextPatrolSelection = Time.fixedTime + Mathf.Max(0.05f, repathInterval);

        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = areaMask };
        Vector3 feet = rb.position;
        if (bodyCollider != null && bodyCollider.enabled) feet.y = bodyCollider.bounds.min.y;
        float sampleRadius = Mathf.Max(0.05f, sampleDistance);
        if (!NavMesh.SamplePosition(feet, out NavMeshHit start, sampleRadius, filter) ||
            Horizontal(start.position - feet).magnitude > 0.25f)
        {
            Fail("Lester is not on the baked NavMesh");
            return false;
        }

        float radius = Mathf.Max(1f, patrolRadius);
        Vector3 selectionCenter = start.position;
        bool returningHome = Horizontal(rb.position - areaCenter).sqrMagnitude > homeRadius * homeRadius;
        // Sample near home when outside the area, even if a chase exceeded the per-leg radius.
        // Sampling the smaller circle also avoids wasting attempts when the home area is small.
        if (alertSearching || returningHome || homeRadius < radius)
        {
            selectionCenter = areaCenter;
            radius = homeRadius;
        }
        float minimumDistance = Mathf.Max(0.5f, patrolArrivalDistance);
        // Bound the work per attempt, and reject disconnected islands and points too close to arrival.
        for (int i = 0; i < 30; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = selectionCenter + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, filter)) continue;
            float distance = Horizontal(hit.position - rb.position).magnitude;
            if (distance <= minimumDistance || (!alertSearching && !returningHome && distance > Mathf.Max(1f, patrolRadius))) continue;
            // NavMesh projection can move a candidate beyond the patrol or search boundary.
            if (Horizontal(hit.position - areaCenter).sqrMagnitude > homeRadius * homeRadius) continue;
            if (!NavMesh.CalculatePath(start.position, hit.position, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete || path.corners.Length == 0) continue;
            if (!IsPatrolPathWithinRange(path.corners)) continue;

            patrolTarget = target = hit.position;
            hasPatrolTarget = true;
            // Keep the chosen destination until arrival; periodic repaths must not reroll it.
            hasRequest = false;
            return true;
        }
        Fail(alertSearching ? "No reachable alert search destination found" : "No reachable random patrol destination found");
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
        if (requestIsPatrol && !IsPatrolPathWithinRange(corners))
        {
            corners = new Vector3[0];
            Fail(alertSearching ? "Alert search path leaves the search area" : "Patrol path leaves the spawn area");
            return;
        }
        cornerIndex = corners.Length > 1 ? 1 : 0;
        lastFailure = null;
        navigationStatus = requestIsPatrol ? (alertSearching ? "Searching around last investigation position" : "Patrolling") : "Following remembered destination";
    }

    private void Fail(string reason)
    {
        navigationStatus = reason;
        if (lastFailure == reason) return;
        lastFailure = reason;
        Debug.LogWarning("Lester navigation: " + reason + ". Check the NavMesh surface, agent type, allowed areas and patrol distance settings; retrying automatically.", this);
    }

    private bool IsPatrolPathWithinRange(Vector3[] pathCorners)
    {
        Vector3 areaCenter = PatrolAreaCenter;
        float radius = PatrolAreaRadius;
        float radiusSquared = radius * radius;
        bool insideHome = Horizontal(rb.position - areaCenter).sqrMagnitude <= radiusSquared;
        foreach (Vector3 corner in pathCorners)
        {
            bool cornerInside = Horizontal(corner - areaCenter).sqrMagnitude <= radiusSquared;
            // An enemy returning from a chase may start outside, but cannot leave again after entering.
            if (insideHome && !cornerInside) return false;
            if (cornerInside) insideHome = true;
        }
        return insideHome;
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
