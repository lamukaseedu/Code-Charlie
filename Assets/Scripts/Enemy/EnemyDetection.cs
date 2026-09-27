using System;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    // Enemy can either do nothing, check a suspicious location, or actively follow the player.
    public enum DetectionState { Idle, Investigating, Chasing }

    [Header("Hearing")]
    public float hearingRadius = 10f;

    [Header("Vision")]
    public float visionRange = 12f;
    [Range(1, 360)] public float fieldOfView = 90f;

    // Only objects on these layers are considered when checking what blocks vision.
    public LayerMask obstacleMask;

    [Header("General")]
    public float eyeHeight = 1.2f;
    public bool drawGizmos = true;

    // Important! other scripts can read State, but only EnemyDetection is allowed to change it.
    public DetectionState State { get; private set; } = DetectionState.Idle;

    public Vector3 LastHeardPosition { get; private set; }
    public bool HasHeardSound { get; private set; }
    public Vector3 CurrentWaypoint { get; private set; }
    public bool HasWaypoint { get; private set; }

    public bool HasLineOfSightToPlayer { get; private set; }

    // Other scripts can subscribe to these events instead of constantly checking this script.
    public event Action<Vector3> OnHeardSound;
    public event Action<GameObject> OnSeeTarget;

    Transform playerTransform;

    void Awake()
    {
        // Finds the player once so we don't have to search for it every frame.
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        UpdateVision();
    }

    public void HearSound(Vector3 soundPosition)
    {
        float distanceToSound = Vector3.Distance(transform.position, soundPosition);

        if (distanceToSound <= hearingRadius)
        {
            LastHeardPosition = soundPosition;
            HasHeardSound = true;

            // The sound location becomes the place EnemyMovement will walk toward.
            CurrentWaypoint = soundPosition;
            HasWaypoint = true;

            // Hearing a sound won't interrupt the enemy if it is already chasing the player.
            if (State != DetectionState.Chasing) State = DetectionState.Investigating;

            Debug.Log(gameObject.name + " heard the sound at " + soundPosition);
            OnHeardSound?.Invoke(soundPosition);
        }
    }

    void UpdateVision()
    {
        // Reset this every frame, the checks below must prove that the player is visible.
        HasLineOfSightToPlayer = false;

        if (playerTransform == null) return;

        // Start vision from the enemy's eyes instead of from its feet.
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 dirToPlayer = playerTransform.position - eyePos;
        float distToPlayer = dirToPlayer.magnitude;

        if (distToPlayer <= visionRange)
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            // fieldOfView is split in half because vision extends to both sides of forward.
            if (angle <= fieldOfView * 0.5f)
            {
                // Raycast acts like an invisible line checking if something blocks the view.
                Ray ray = new Ray(eyePos, dirToPlayer.normalized);

                if (!Physics.Raycast(ray, out RaycastHit hit, visionRange, obstacleMask))
                {
                    HasLineOfSightToPlayer = true;
                }
                else
                {
                    // A child object counts too because the ray may hit part of the player's model/collider.
                    if (hit.collider != null && (hit.collider.transform == playerTransform || hit.collider.transform.IsChildOf(playerTransform)))
                        HasLineOfSightToPlayer = true;
                }
            }
        }

        if (HasLineOfSightToPlayer)
        {
            State = DetectionState.Chasing;

            // Unlike a sound waypoint, this updates every frame so the enemy follows the moving player.
            CurrentWaypoint = playerTransform.position;
            HasWaypoint = true;

            OnSeeTarget?.Invoke(playerTransform.gameObject);
        }
        else
        {
            if (State == DetectionState.Chasing)
            {
                // After losing sight, investigate the last waypoint instead of immediately going Idle.
                State = HasWaypoint ? DetectionState.Investigating : DetectionState.Idle;
            }
        }
    }

    public void ClearWaypoint()
    {
        HasWaypoint = false;
        CurrentWaypoint = Vector3.zero;

        // Reaching an investigation location finishes the investigation.
        if (State == DetectionState.Investigating) State = DetectionState.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        if (playerTransform != null)
        {
            // Red means LOS(line of sight) is clear, yellow means the player isn't currently visible.
            Gizmos.color = HasLineOfSightToPlayer ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, playerTransform.position);
        }
    }
}