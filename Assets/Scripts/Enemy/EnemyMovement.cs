using System.Collections.Generic;
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
    [Tooltip("Seconds without progress near another enemy before choosing a different patrol destination. Also applies to alert searches.")]
    [Min(0.5f)] public float patrolBlockedTimeout = 3f;
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
    [Header("NavMesh Recovery")]
    [Tooltip("Walking speed toward the nearest NavMesh point when outside the walkable area.")]
    [Min(0f)] public float recoverySpeed = 1.5f;
    [Tooltip("Seconds continuously outside the NavMesh before teleporting to a clear NavMesh position.")]
    [Min(0f)] public float recoveryTeleportDelay = 5f;
    [Tooltip("Maximum search distance for recovery. Searches expand from nearby points and retry periodically if no mesh is found.")]
    [Min(1f)] public float recoverySearchDistance = 100f;
    [Header("Enemy Avoidance")]
    public bool enableAvoidance = true;
    [Tooltip("Extra space between enemy body colliders, in metres.")]
    [Min(0f)] public float separationDistance = 0.2f;
    [Tooltip("How far ahead, in seconds, to predict contact with another enemy.")]
    [Min(0.1f)] public float avoidanceLookAhead = 0.8f;
    [Tooltip("Minimum distance at which to consider neighbouring enemies. Faster movement also extends this distance.")]
    [Min(0.5f)] public float neighbourDistance = 3f;
    [Tooltip("Weight of keeping space between enemies when choosing a direction.")]
    [Min(0f)] public float separationStrength = 2f;
    [Tooltip("Lower numbers have right of way. Equal priorities use a stable instance order. Enemies still brake to avoid contact.")]
    public int avoidancePriority = 50;
    [Tooltip("Spread enemies pursuing the visible player around a circle of this radius. Set to 0 to disable. Keep below EnemyAttack's Attack Range.")]
    [Min(0f)] public float chaseSpacingRadius = 1.2f;
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
    public bool IsRecoveringNavMesh => recoveringNavMesh;
    public Vector3[] PathCorners => (Vector3[])corners.Clone();

    private Animator enemyAnimation;
    private EnemyAttack attack;
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
    private float patrolProgressDeadline;
    private float closestPatrolDistance;
    private static readonly List<EnemyMovement> ActiveEnemies = new List<EnemyMovement>();
    private readonly List<EnemyMovement> neighbours = new List<EnemyMovement>();
    private readonly RaycastHit[] avoidanceHits = new RaycastHit[32];
    private Vector3 preferredVelocity;
    private bool recoveringNavMesh;
    private float timeOutsideNavMesh;
    private float nextRecoverySearch;
    private bool hasRecoveryTarget;
    private Vector3 recoveryTarget;
    private readonly Collider[] recoveryOverlaps = new Collider[32];
    private Vector3 alertSearchCenter;
    private Vector3 PatrolAreaCenter => alertSearching ? alertSearchCenter : spawnPosition;
    private float PatrolAreaRadius => Mathf.Max(0.1f, alertSearching ? alertSearchRadius : maxPatrolDistance);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEnemies() => ActiveEnemies.Clear();

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
    }

    private void Awake()
    {
        awareness = GetComponent<EnemyAwareness>();
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        spawnPosition = rb.position;
        if (bodyCollider != null && bodyCollider.enabled) spawnPosition.y = bodyCollider.bounds.min.y;
        path = new NavMeshPath();
        enemyAnimation = GetComponentInChildren<Animator>();
        attack = GetComponent<EnemyAttack>();
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
        ActiveEnemies.Remove(this);
        preferredVelocity = Vector3.zero;
        recoveringNavMesh = false;
        timeOutsideNavMesh = 0f;
        hasRecoveryTarget = false;
        nextRecoverySearch = 0f;
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

        if (attack != null && attack.IsAttacking)
        {
            enemyAnimation.SetBool(WalkParameter, false);
            enemyAnimation.speed = 1f;
            return;
        }

        bool isWalking = movementSpeed > 0.01f;
        enemyAnimation.SetBool(WalkParameter, isWalking);

        // Keep idle animations and transitions playing when the enemy stops.
        enemyAnimation.speed = isWalking
            ? (chaseSpeed > 0f ? Mathf.Clamp01(movementSpeed / chaseSpeed) : 0f)
            : 1f;
    }

    private void FixedUpdate()
    {
        preferredVelocity = Vector3.zero;
        debugHasWaypoint = false;
        if (rb.isKinematic) return;
        if (RecoverNavMesh()) return;
        if (attack != null && attack.ShouldPauseMovement)
        {
            navigationStatus = "Attacking";
            if (awareness != null && awareness.isActiveAndEnabled && awareness.HasWaypoint)
                TurnTowards(awareness.CurrentWaypoint - rb.position);
            return;
        }
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
            if (enableAvoidance && awareness.State == EnemyAwareness.AwarenessState.Active && awareness.HasLineOfSightToPlayer)
                target = GetSpacedChaseTarget(target);
            speed = awareness.State == EnemyAwareness.AwarenessState.Active ? chaseSpeed : investigateSpeed;
        }
        debugWaypoint = target;
        debugHasWaypoint = true;
        debugDistance = Horizontal(target - rb.position).magnitude;
        if (patrol && enableAvoidance && debugDistance > Mathf.Max(0.02f, patrolArrivalDistance))
        {
            if (debugDistance < closestPatrolDistance - 0.1f)
            {
                closestPatrolDistance = debugDistance;
                patrolProgressDeadline = Time.fixedTime + Mathf.Max(0.5f, patrolBlockedTimeout);
            }
            if (Time.fixedTime >= patrolProgressDeadline && HasNearbyEnemy())
            {
                // Keep avoidance active on moving patrols, but give up a destination that causes a lasting queue.
                hasPatrolTarget = false;
                hasRequest = false;
                nextPatrolSelection = 0f;
                corners = new Vector3[0];
                navigationStatus = "Choosing a less crowded patrol destination";
                StopAtWaypoint();
                return;
            }
        }
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
            if (awareness.State == EnemyAwareness.AwarenessState.Active)
                TurnTowards(awareness.CurrentWaypoint - rb.position);
            StopAtWaypoint();
            return;
        }
        // Slow for tight turns, but retain a nonzero approach speed at intermediate corners.
        // Only the final destination uses a braking distance that falls all the way to zero.
        float brakingAcceleration = Mathf.Max(0f, acceleration);
        float stepVelocity = brakingAcceleration * Time.fixedDeltaTime;
        float brakingDistance = finalCorner
            ? Mathf.Max(0f, distance - radius * 0.5f)
            : Mathf.Max(distance, radius);
        float arrivalSpeed = Mathf.Sqrt(stepVelocity * stepVelocity + 2f * brakingAcceleration * brakingDistance) - stepVelocity;
        preferredVelocity = delta.normalized * Mathf.Min(Mathf.Max(0f, speed), arrivalSpeed);
        Vector3 velocity = enableAvoidance ? AvoidEnemies(preferredVelocity, Mathf.Max(0f, speed)) : preferredVelocity;
        bool facePlayer = awareness.State == EnemyAwareness.AwarenessState.Active && awareness.HasLineOfSightToPlayer &&
            Horizontal(awareness.CurrentWaypoint - rb.position).magnitude <= chaseSpacingRadius + stopDistance + 0.25f;
        TurnTowards(facePlayer ? awareness.CurrentWaypoint - rb.position : velocity);
        AccelerateTowards(velocity);
    }

    private bool RecoverNavMesh()
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = areaMask };
        Vector3 feet = FeetPosition;
        if (NavMesh.SamplePosition(feet, out NavMeshHit nearby, 0.5f, filter) &&
            Horizontal(nearby.position - feet).sqrMagnitude <= 0.15f * 0.15f &&
            Mathf.Abs(nearby.position.y - feet.y) <= 0.5f)
        {
            recoveringNavMesh = false;
            timeOutsideNavMesh = 0f;
            hasRecoveryTarget = false;
            nextRecoverySearch = 0f;
            return false;
        }

        if (!recoveringNavMesh)
        {
            recoveringNavMesh = true;
            timeOutsideNavMesh = 0f;
            nextRecoverySearch = 0f;
            hasRequest = hasPatrolTarget = false;
            corners = new Vector3[0];
            // Recovery takes over from an attack that carried the enemy off the mesh.
            if (attack != null && attack.IsAttacking) attack.CancelAttack();
        }
        timeOutsideNavMesh += Time.fixedDeltaTime;

        if (Time.fixedTime >= nextRecoverySearch)
        {
            nextRecoverySearch = Time.fixedTime + Mathf.Max(0.1f, repathInterval);
            hasRecoveryTarget = TryFindRecoveryTarget(feet, filter, out recoveryTarget);
            if (hasRecoveryTarget && timeOutsideNavMesh >= Mathf.Max(0f, recoveryTeleportDelay) &&
                TryFindClearRecoverySpot(feet, recoveryTarget, filter, out Vector3 landing))
            {
                // Keep the collider's feet on the surface even when the Rigidbody pivot is at its center.
                Vector3 pivotOffset = rb.position - feet;
                rb.position = landing + pivotOffset + Vector3.up * 0.03f;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                recoveringNavMesh = false;
                timeOutsideNavMesh = 0f;
                hasRecoveryTarget = false;
                hasRequest = hasPatrolTarget = false;
                nextRepath = nextPatrolSelection = 0f;
                lastFailure = null;
                navigationStatus = "Returned to NavMesh";
                return true;
            }
        }

        if (!hasRecoveryTarget)
        {
            navigationStatus = "Outside NavMesh: searching for a recovery position";
            StopAtWaypoint();
            return true;
        }
        navigationStatus = "Returning to nearest NavMesh position";
        debugWaypoint = recoveryTarget;
        debugHasWaypoint = true;
        Vector3 delta = Horizontal(recoveryTarget - feet);
        debugDistance = delta.magnitude;
        float speed = Mathf.Min(Mathf.Max(0f, recoverySpeed),
            Mathf.Sqrt(2f * Mathf.Max(0f, acceleration) * delta.magnitude));
        preferredVelocity = delta.normalized * speed;
        TurnTowards(delta);
        // Normal avoidance needs a valid NavMesh starting point, so use the physics motor directly here.
        AccelerateTowards(preferredVelocity);
        return true;
    }

    private bool TryFindRecoveryTarget(Vector3 feet, NavMeshQueryFilter filter, out Vector3 target)
    {
        float maximum = Mathf.Max(1f, recoverySearchDistance);
        float radius = Mathf.Min(maximum, Mathf.Max(1f, sampleDistance));
        while (true)
        {
            if (NavMesh.SamplePosition(feet, out NavMeshHit hit, radius, filter))
            {
                target = hit.position;
                return true;
            }
            if (radius >= maximum) break;
            radius = Mathf.Min(maximum, radius * 2f);
        }
        target = default;
        return false;
    }

    private bool TryFindClearRecoverySpot(Vector3 feet, Vector3 nearest, NavMeshQueryFilter filter, out Vector3 landing)
    {
        landing = nearest;
        if (IsRecoverySpotClear(nearest)) return true;
        bool found = false;
        float bestDistance = float.PositiveInfinity;
        float step = Mathf.Max(0.5f, BodyRadius * 2f + separationDistance);
        // If the closest point is occupied, check nearby points and choose the closest clear candidate.
        for (int ring = 1; ring <= 4; ring++)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                Vector3 candidate = nearest + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * step);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.5f, filter)) continue;
                float distance = (hit.position - feet).sqrMagnitude;
                if (distance >= bestDistance || !IsRecoverySpotClear(hit.position)) continue;
                landing = hit.position;
                bestDistance = distance;
                found = true;
            }
        }
        return found;
    }

    private bool IsRecoverySpotClear(Vector3 feet)
    {
        float radius = Mathf.Max(0.01f, BodyRadius - 0.02f);
        float height = bodyCollider != null && bodyCollider.enabled ? bodyCollider.bounds.size.y : 2f;
        Vector3 bottom = feet + Vector3.up * (radius + 0.03f);
        Vector3 top = feet + Vector3.up * (Mathf.Max(height, radius * 2f) - radius + 0.01f);
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, recoveryOverlaps,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == recoveryOverlaps.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Collider other = recoveryOverlaps[i];
            if (other != null && other.attachedRigidbody != rb && !other.transform.IsChildOf(transform))
                return false;
        }
        return true;
    }

    private float BodyRadius => bodyCollider != null && bodyCollider.enabled
        ? Mathf.Max(0.05f, Mathf.Max(bodyCollider.bounds.extents.x, bodyCollider.bounds.extents.z)) : 0.35f;

    private Vector3 FeetPosition => bodyCollider != null && bodyCollider.enabled
        ? new Vector3(rb.position.x, bodyCollider.bounds.min.y, rb.position.z) : rb.position;

    private bool HasRightOfWay(EnemyMovement other) => avoidancePriority < other.avoidancePriority ||
        (avoidancePriority == other.avoidancePriority && GetInstanceID() < other.GetInstanceID());

    private Vector3 GetSpacedChaseTarget(Vector3 target)
    {
        if (chaseSpacingRadius <= 0f) return target;
        int count = 0;
        int rank = 0;
        foreach (EnemyMovement other in ActiveEnemies)
        {
            if (other == null || other.awareness == null || !other.enableAvoidance || other.chaseSpacingRadius <= 0f ||
                other.awareness.State != EnemyAwareness.AwarenessState.Active || !other.awareness.HasLineOfSightToPlayer ||
                (other.awareness.CurrentWaypoint - target).sqrMagnitude > 1f) continue;
            count++;
            if (other.GetInstanceID() < GetInstanceID()) rank++;
        }
        if (count < 2) return target;
        float angle = rank * (Mathf.PI * 2f / count);
        Vector3 candidate = target + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * chaseSpacingRadius;
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = areaMask };
        // Reject positions projected across walls or onto disconnected parts of the mesh.
        if (NavMesh.SamplePosition(target, out NavMeshHit center, Mathf.Max(0.05f, sampleDistance), filter) &&
            NavMesh.SamplePosition(candidate, out NavMeshHit end, 0.5f, filter) &&
            Horizontal(end.position - candidate).sqrMagnitude <= 0.25f &&
            !NavMesh.Raycast(center.position, end.position, out _, filter))
            return end.position;
        return target;
    }

    private Vector3 AvoidEnemies(Vector3 desired, float speed)
    {
        if (desired.sqrMagnitude < 0.000001f || speed <= 0f) return desired;
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = areaMask };
        if (!NavMesh.SamplePosition(FeetPosition, out NavMeshHit start, 0.3f, filter)) return Vector3.zero;
        float horizon = Mathf.Max(0.1f, avoidanceLookAhead);
        float searchDistance = Mathf.Max(neighbourDistance, BodyRadius * 2f + speed * horizon * 2f);
        neighbours.Clear();
        Vector3 forward = desired.normalized;
        bool yielding = false;
        foreach (EnemyMovement other in ActiveEnemies)
        {
            if (other == null || other == this || other.rb == null || !other.isActiveAndEnabled) continue;
            Vector3 offset = other.rb.position - rb.position;
            float height = bodyCollider != null ? bodyCollider.bounds.size.y : 2f;
            if (Mathf.Abs(offset.y) > height || Horizontal(offset).sqrMagnitude > searchDistance * searchDistance) continue;
            // Enemies on the other side of a NavMesh boundary cannot block this corridor.
            if (NavMesh.Raycast(start.position, other.FeetPosition, out _, filter)) continue;
            neighbours.Add(other);
            float clearance = BodyRadius + other.BodyRadius + Mathf.Max(0f, separationDistance);
            float ahead = Vector3.Dot(Horizontal(offset), forward);
            Vector3 otherIntent = other.preferredVelocity;
            if (otherIntent.sqrMagnitude < 0.001f) otherIntent = Horizontal(other.rb.linearVelocity);
            if (ahead > 0f && ahead < clearance + speed * horizon &&
                Vector3.Cross(forward, Horizontal(offset)).magnitude < clearance &&
                Vector3.Dot(forward, otherIntent) < -0.1f && !HasRightOfWay(other))
                yielding = true;
        }
        if (neighbours.Count == 0) return desired;

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 best = Vector3.zero;
        float bestScore = float.PositiveInfinity;
        // Sample a full turn, including reversing, so a yielding enemy can back out of a narrow passage.
        for (int i = -1; i < 32; i++)
        {
            float speedFraction = i < 16 ? 1f : 0.5f;
            Vector3 candidate = i < 0 ? Vector3.zero :
                Quaternion.AngleAxis((i % 16) * 22.5f, Vector3.up) * forward * desired.magnitude * speedFraction;
            if (!IsAvoidanceDirectionClear(candidate, start.position, filter, horizon)) continue;
            float score = (candidate - desired).sqrMagnitude / Mathf.Max(0.01f, speed * speed);
            // Waiting is a fallback: prefer a clear side route over queuing forever in open space.
            if (candidate.sqrMagnitude < 0.000001f) score += 0.75f;
            // Both approaching enemies prefer their own right, avoiding mirrored side-to-side decisions.
            score -= Vector3.Dot(candidate, right) / speed * 0.15f;
            if (yielding)
                score += Vector3.Dot(candidate, forward) / speed * 3f - Vector3.Dot(candidate, right) / speed;
            foreach (EnemyMovement other in neighbours)
            {
                Vector3 offset = Horizontal(other.rb.position - rb.position);
                Vector3 relativeVelocity = candidate - Horizontal(other.rb.linearVelocity);
                float clearance = BodyRadius + other.BodyRadius + Mathf.Max(0f, separationDistance);
                float collisionTime = relativeVelocity.sqrMagnitude > 0.0001f
                    ? Mathf.Clamp(Vector3.Dot(offset, relativeVelocity) / relativeVelocity.sqrMagnitude, 0f, horizon) : 0f;
                float closestDistance = (offset - relativeVelocity * collisionTime).magnitude;
                float currentDistance = offset.magnitude;
                if (closestDistance < clearance)
                {
                    float penetration = 1f - closestDistance / clearance;
                    score += 20f * penetration * penetration;
                }
                float spacing = clearance + Mathf.Max(0f, separationDistance);
                float futureDistance = (offset - relativeVelocity * horizon).magnitude;
                score += Mathf.Max(0f, separationStrength) * Mathf.Clamp01(1f - futureDistance / spacing);
                // When already touching, prefer separating rather than pressing into the collider.
                if (currentDistance < clearance && Vector3.Dot(offset, relativeVelocity) > 0f)
                    score += 20f * Vector3.Dot(offset.normalized, relativeVelocity) / Mathf.Max(0.1f, speed);
            }
            if (score < bestScore) { bestScore = score; best = candidate; }
        }
        if (yielding) navigationStatus = "Giving way to another enemy";
        else if ((best - desired).sqrMagnitude > 0.01f) navigationStatus = "Avoiding another enemy";
        return best;
    }

    private bool IsAvoidanceDirectionClear(Vector3 velocity, Vector3 start, NavMeshQueryFilter filter, float horizon)
    {
        if (velocity.sqrMagnitude < 0.000001f) return true;
        Vector3 travel = velocity * Mathf.Max(0.25f, horizon);
        if (NavMesh.Raycast(start, start + travel, out _, filter)) return false;
        Vector3 center = bodyCollider != null ? bodyCollider.bounds.center : rb.position + Vector3.up;
        int count = Physics.SphereCastNonAlloc(center, BodyRadius * 0.9f, travel.normalized, avoidanceHits,
            travel.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == avoidanceHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Collider hit = avoidanceHits[i].collider;
            if (hit == null || hit.attachedRigidbody == rb) continue;
            EnemyMovement other = hit.GetComponentInParent<EnemyMovement>();
            if (other != null && other.isActiveAndEnabled) continue;
            return false;
        }
        return true;
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
            if (enableAvoidance && !IsPatrolDestinationClear(hit.position)) continue;
            if (!NavMesh.CalculatePath(start.position, hit.position, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete || path.corners.Length == 0) continue;
            if (!IsPatrolPathWithinRange(path.corners)) continue;

            patrolTarget = target = hit.position;
            hasPatrolTarget = true;
            closestPatrolDistance = distance;
            patrolProgressDeadline = Time.fixedTime + Mathf.Max(0.5f, patrolBlockedTimeout);
            // Keep the chosen destination until arrival; periodic repaths must not reroll it.
            hasRequest = false;
            return true;
        }
        Fail(alertSearching ? "No reachable alert search destination found" : "No reachable random patrol destination found");
        return false;
    }

    private bool HasNearbyEnemy()
    {
        foreach (EnemyMovement other in ActiveEnemies)
        {
            if (other == null || other == this || other.rb == null) continue;
            float range = Mathf.Max(neighbourDistance, BodyRadius + other.BodyRadius + separationDistance);
            if ((other.rb.position - rb.position).sqrMagnitude < range * range) return true;
        }
        return false;
    }

    private bool IsPatrolDestinationClear(Vector3 destination)
    {
        foreach (EnemyMovement other in ActiveEnemies)
        {
            if (other == null || other == this || other.rb == null) continue;
            float clearance = BodyRadius + other.BodyRadius + Mathf.Max(0f, separationDistance);
            // Different floors may overlap horizontally without sharing space.
            float height = bodyCollider != null ? bodyCollider.bounds.size.y : 2f;
            if (Mathf.Abs(destination.y - other.FeetPosition.y) < height &&
                Horizontal(destination - other.rb.position).sqrMagnitude < clearance * clearance)
                return false;
            // Reserve enough room for both enemies to arrive at their independently selected destinations.
            float reservedDistance = clearance + Mathf.Max(patrolArrivalDistance, other.patrolArrivalDistance);
            if (other.hasPatrolTarget && Mathf.Abs(destination.y - other.patrolTarget.y) < height &&
                Horizontal(destination - other.patrolTarget).sqrMagnitude < reservedDistance * reservedDistance)
                return false;
        }
        return true;
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
        if (attack != null && attack.ShouldPauseTurning)
            return;
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
