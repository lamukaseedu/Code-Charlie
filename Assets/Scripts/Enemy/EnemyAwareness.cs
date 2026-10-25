using System;
using UnityEngine;

// Sensing and memory only. EnemyMovement remains the sole physical motor.
public class EnemyAwareness : MonoBehaviour
{
    public enum AwarenessState { Unaware, Alert, Active }
    [Tooltip("Player to sense. If unassigned, finds the object tagged Player on Awake.")]
    [SerializeField] private Transform player;
    [Header("Hearing")]
    [Tooltip("Maximum hearing distance in world units. Noise strength decreases with distance.")]
    [Min(0.01f)] public float hearingRadius = 12f;
    [Tooltip("Multiplier for suspicion gained from sounds while Alert; does not affect target priority.")]
    [Min(0f)] public float alertHearingMultiplier = 1.5f;
    [Tooltip("Time window in seconds during which only sounds at least as strong as the strongest recent sound can replace the investigation target.")]
    [Min(0f)] public float noisePriorityWindow = 0.15f;
    [Tooltip("Priority points lost per second by the remembered target, allowing weaker new stimuli to replace it.")]
    [Min(0f)] public float targetPriorityDecayPerSecond = 10f;
    [Header("Suspicion")]
    [Tooltip("Starting Alert suspicion (0-100) when suspicion or a visual glimpse first makes the enemy Alert.")]
    [Range(0f, 100f)] public float initialAlertSuspicion = 50f;
    [Tooltip("Seconds without a stimulus before suspicion starts decaying while Unaware.")]
    [Min(0f)] public float unawareDecayDelay = 2f;
    [Tooltip("Seconds without a stimulus before suspicion starts decaying while Alert.")]
    [Min(0f)] public float alertDecayDelay = 5f;
    [Tooltip("Suspicion points lost per second after the decay delay. An Alert enemy returns to Unaware at zero.")]
    [Min(0f)] public float suspicionDecayPerSecond = 5f;
    [Header("Vision")]
    [Tooltip("Maximum distance of the main vision cone in world units.")]
    [Min(0.01f)] public float visionRange = 12f;
    [Tooltip("Full horizontal angle of the main vision cone in degrees, centered on the enemy's forward direction.")]
    [Range(1f, 360f)] public float fieldOfView = 90f;
    [Tooltip("Maximum distance of the additional close-range vision cone in world units. Set to zero to disable it.")]
    [Min(0f)] public float shortVisionRange = 2.5f;
    [Tooltip("Full horizontal angle of the close-range vision cone in degrees.")]
    [Range(1f, 360f)] public float shortFieldOfView = 240f;
    [Tooltip("Layers that block vision and proximity awareness. Trigger colliders and the enemy's and player's own colliders are ignored.")]
    public LayerMask obstacleMask = Physics.DefaultRaycastLayers;
    [Tooltip("World-space vertical offset above the enemy's position where sight rays start.")]
    public float eyeHeight = 1.2f;
    [Tooltip("World-space vertical offset above the player's position where sight rays aim.")]
    public float targetHeight = 1f;
    [Tooltip("Seconds of visibility needed for full recognition while Unaware, before distance and angle slow recognition.")]
    [Min(0.01f)] public float unawareRecognitionTime = 1f;
    [Tooltip("Seconds of visibility needed for full recognition while Alert, before distance and angle slow recognition.")]
    [Min(0.01f)] public float alertRecognitionTime = 0.4f;
    [Tooltip("Visual recognition speed while the player is crouching, in both Unaware and Alert states. 0.5 takes twice as long, 1 uses normal speed, and 0 prevents recognition from increasing. Combines with distance and angle multipliers.")]
    [Range(0f, 1f)] public float crouchingRecognitionMultiplier = 0.5f;
    [Tooltip("Recognition fraction (0-1) that triggers Alert and investigation of the player's position. Full recognition triggers Active.")]
    [Range(0.01f, 1f)] public float glimpseThreshold = 0.2f;
    [Tooltip("Recognition speed multiplier at the maximum range of each vision cone, interpolated from 1 at zero distance.")]
    [Range(0f, 1f)] public float farRecognitionMultiplier = 0.4f;
    [Tooltip("Recognition speed multiplier at the edge of each vision cone, interpolated from 1 at its center. Combines with the distance multiplier.")]
    [Range(0f, 1f)] public float edgeRecognitionMultiplier = 0.5f;
    [Tooltip("Recognition fraction lost per second when the player is outside clear line of sight.")]
    [Min(0f)] public float recognitionDecayPerSecond = 1f;
    [Tooltip("Seconds an Active enemy can lose sight of the player before returning to Alert and investigating the last known position.")]
    [Min(0f)] public float lostSightTime = 3f;
    [Header("Proximity (suspicion, never visual recognition)")]
    [Tooltip("Distance in world units within which a player with an unobstructed sight ray raises suspicion, regardless of vision cone angles.")]
    [Min(0f)] public float proximityRadius = 2.5f;
    [Tooltip("Suspicion points gained per second from player proximity while Unaware.")]
    [Min(0f)] public float unawareProximityPerSecond = 40f;
    [Tooltip("Suspicion points gained per second from player proximity while Alert. Proximity alone does not trigger Active.")]
    [Min(0f)] public float alertProximityPerSecond = 60f;
    [Header("Runtime debug (read only during play)")]
    [Tooltip("Runtime awareness state: Unaware, Alert (investigating), or Active (confirmed player).")]
    [SerializeField] private AwarenessState state;
    [Tooltip("Runtime suspicion while Unaware (0-100). Reaching 100 enters Alert.")]
    [SerializeField] private float unawareSuspicion;
    [Tooltip("Runtime suspicion while Alert (0-100). Decaying to zero returns the enemy to Unaware.")]
    [SerializeField] private float alertSuspicion;
    [Tooltip("Runtime visual recognition (0-1). Reaching 1 confirms the player and enters Active.")]
    [SerializeField] private float visualRecognition;
    [Tooltip("Runtime seconds since an Active enemy last saw the player. Resets when sight is regained or Alert is entered.")]
    [SerializeField] private float lostSightElapsed;
    [Tooltip("Whether the player is currently inside either vision cone with an unobstructed sight ray.")]
    [SerializeField] private bool hasLineOfSightToPlayer;
    [Tooltip("Whether the player is currently within the proximity radius with an unobstructed sight ray.")]
    [SerializeField] private bool hasProximityAwareness;
    [Tooltip("Whether an investigation or pursuit waypoint is currently assigned.")]
    [SerializeField] private bool hasWaypoint;
    [Tooltip("Runtime world-space destination remembered for investigation or pursuit.")]
    [SerializeField] private Vector3 currentWaypoint;
    [Tooltip("Last world-space player position confirmed by full visual recognition, Active tracking, or player damage.")]
    [SerializeField] private Vector3 lastKnownPlayerPosition;
    [Tooltip("Whether any valid external noise has been heard since Awake; clearing a waypoint does not reset this flag.")]
    [SerializeField] private bool hasHeardNoise;
    [Tooltip("World-space origin of the most recently heard valid noise, even if it did not replace the current target.")]
    [SerializeField] private Vector3 lastHeardPosition;
    [Tooltip("Object that emitted the most recently heard valid noise. May be null.")]
    [SerializeField] private GameObject lastHeardSource;
    [Tooltip("Original intensity of the most recently heard valid noise, before distance attenuation or Alert sensitivity.")]
    [SerializeField] private float lastHeardIntensity;
    [Tooltip("Noise level category of the most recently heard valid noise.")]
    [SerializeField] private NoiseEmitter.NoiseLevel lastHeardLevel;
    [Tooltip("Remaining priority of the remembered target after decay. New sounds must meet or exceed this priority to replace it.")]
    [SerializeField] private float currentTargetSignificance;
    public AwarenessState State => state;
    public float UnawareSuspicion => unawareSuspicion;
    public float AlertSuspicion => alertSuspicion;
    public float VisualRecognition => visualRecognition;
    public float LostSightElapsed => lostSightElapsed;
    public bool HasLineOfSightToPlayer => hasLineOfSightToPlayer;
    // Debug ray endpoint only; movement consumes remembered CurrentWaypoint.
    public Vector3 VisionTargetPosition => player != null ? player.position + Vector3.up * targetHeight : lastKnownPlayerPosition;
    public bool HasProximityAwareness => hasProximityAwareness;
    public bool HasWaypoint => hasWaypoint;
    public Vector3 CurrentWaypoint => currentWaypoint;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    public bool HasHeardNoise => hasHeardNoise;
    public Vector3 LastHeardPosition => lastHeardPosition;
    public GameObject LastHeardSource => lastHeardSource;
    public float LastHeardIntensity => lastHeardIntensity;
    public NoiseEmitter.NoiseLevel LastHeardLevel => lastHeardLevel;
    public event Action<AwarenessState> StateChanged;
    public event Action<Vector3> DestinationChanged;
    public event Action<NoiseEmitter.NoiseEvent> HeardNoise;
    private float lastStimulusTime = float.NegativeInfinity;
    private float targetTime = float.NegativeInfinity;
    private float targetStrength;
    private bool hasCandidate;
    private Vector3 candidatePosition;
    private float recentNoiseStrength;
    private float lastNoiseWindowTime = float.NegativeInfinity;
    private Health health;
    private Transform cachedMovementPlayer;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        health = GetComponent<Health>();
        // Runtime state must never be restored from an accidental Inspector edit.
        state = AwarenessState.Unaware;
        unawareSuspicion = alertSuspicion = visualRecognition = lostSightElapsed = 0f;
        hasWaypoint = hasHeardNoise = false;
        if (player == null)
        {
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            if (target != null) player = target.transform;
        }
    }
    private void OnEnable()
    {
        NoiseEmitter.NoiseProduced += HearNoise;
        if (health != null) health.DamagedBy += OnDamagedBy;
    }
    private void OnDisable()
    {
        NoiseEmitter.NoiseProduced -= HearNoise;
        if (health != null) health.DamagedBy -= OnDamagedBy;
        hasLineOfSightToPlayer = hasProximityAwareness = false;
    }

    private void OnDamagedBy(GameObject source)
    {
        if (!isActiveAndEnabled || source == null) return;
        Transform attacker = source.transform;
        if (player != null && (attacker == player || attacker.IsChildOf(player)))
            attacker = player;
        else
        {
            var playerMovement = source.GetComponentInParent<PlayerMovement>();
            if (playerMovement == null) return;
            attacker = playerMovement.transform;
        }

        // A player hit reveals an exact position without requiring vision or hearing.
        player = attacker;
        lastKnownPlayerPosition = attacker.position;
        lostSightElapsed = 0f;
        visualRecognition = 1f;
        unawareSuspicion = 0f;
        alertSuspicion = 100f;
        lastStimulusTime = Time.time;
        RememberTarget(lastKnownPlayerPosition, 100f);
        SetWaypoint(lastKnownPlayerPosition);
        ChangeState(AwarenessState.Active);
    }
    private void Update()
    {
        float dt = Time.deltaTime;
        currentTargetSignificance = GetTargetSignificance();
        hasLineOfSightToPlayer = false;
        hasProximityAwareness = false;
        float distance = 0f;
        float angle = 0f;
        float recognitionWeight = 0f;
        if (player != null)
        {
            Vector3 delta = player.position - transform.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(delta, Vector3.up);
            distance = delta.magnitude;
            angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), horizontal);
            bool inLongCone = distance <= visionRange && angle <= fieldOfView * 0.5f;
            bool inShortCone = shortVisionRange > 0f && distance <= shortVisionRange && angle <= shortFieldOfView * 0.5f;
            bool inCone = inLongCone || inShortCone;
            if (inLongCone) recognitionWeight = GetRecognitionWeight(distance, angle, visionRange, fieldOfView);
            if (inShortCone) recognitionWeight = Mathf.Max(recognitionWeight, GetRecognitionWeight(distance, angle, shortVisionRange, shortFieldOfView));
            bool close = delta.sqrMagnitude <= proximityRadius * proximityRadius;
            bool clear = (inCone || close) && HasClearView();
            hasLineOfSightToPlayer = inCone && clear;
            hasProximityAwareness = close && clear;
        }
        if (state == AwarenessState.Active)
        {
            if (hasLineOfSightToPlayer)
            {
                lostSightElapsed = 0f;
                visualRecognition = 1f;
                lastKnownPlayerPosition = player.position;
                SetWaypoint(lastKnownPlayerPosition);
            }
            else
            {
                visualRecognition = Mathf.MoveTowards(visualRecognition, 0f, recognitionDecayPerSecond * dt);
                lostSightElapsed += dt;
                if (lostSightElapsed >= lostSightTime)
                {
                    EnterAlert(100f);
                    RememberTarget(lastKnownPlayerPosition, 100f);
                    SetWaypoint(lastKnownPlayerPosition);
                }
            }
            return;
        }
        if (hasLineOfSightToPlayer)
        {
            lastStimulusTime = Time.time;
            if (cachedMovementPlayer != player)
            {
                cachedMovementPlayer = player;
                playerMovement = player.GetComponentInParent<PlayerMovement>();
            }
            float crouchMultiplier = playerMovement != null && playerMovement.IsCrouching
                ? Mathf.Clamp01(crouchingRecognitionMultiplier) : 1f;
            float duration = state == AwarenessState.Unaware ? unawareRecognitionTime : alertRecognitionTime;
            visualRecognition = Mathf.Clamp01(visualRecognition + dt / Mathf.Max(0.01f, duration) * recognitionWeight * crouchMultiplier);
            if (visualRecognition >= 1f)
            {
                lastKnownPlayerPosition = player.position;
                lostSightElapsed = 0f;
                ChangeState(AwarenessState.Active);
                SetWaypoint(lastKnownPlayerPosition);
                return;
            }
            if (visualRecognition >= glimpseThreshold)
            {
                if (state == AwarenessState.Unaware) EnterAlert(initialAlertSuspicion);
                RememberTarget(player.position, 100f);
                SetWaypoint(player.position);
            }
        }
        else visualRecognition = Mathf.MoveTowards(visualRecognition, 0f, recognitionDecayPerSecond * dt);

        if (hasProximityAwareness)
        {
            lastStimulusTime = Time.time;
            AddSuspicion((state == AwarenessState.Unaware ? unawareProximityPerSecond : alertProximityPerSecond) * dt);
            // A nearby body supplies an investigation/turn intention, not confirmed chase memory.
            if (!hasLineOfSightToPlayer && (GetTargetSignificance() <= 60f || !hasWaypoint))
            {
                RememberTarget(player.position, 60f);
                SetWaypoint(player.position);
            }
        }
        float delay = state == AwarenessState.Unaware ? unawareDecayDelay : alertDecayDelay;
        if (Time.time - lastStimulusTime >= delay)
        {
            if (state == AwarenessState.Unaware)
            {
                unawareSuspicion = Mathf.MoveTowards(unawareSuspicion, 0f, suspicionDecayPerSecond * dt);
                if (unawareSuspicion <= 0f && visualRecognition <= 0f)
                    ClearWaypoint();
            }
            else
            {
                alertSuspicion = Mathf.MoveTowards(alertSuspicion, 0f, suspicionDecayPerSecond * dt);
                if (alertSuspicion <= 0f)
                {
                    unawareSuspicion = visualRecognition = 0f;
                    ClearWaypoint();
                    ChangeState(AwarenessState.Unaware);
                }
            }
        }
    }
    public void HearNoise(NoiseEmitter.NoiseEvent noise)
    {
        if (!isActiveAndEnabled || state == AwarenessState.Active) return;
        if (noise.Source != null && (noise.Source.transform == transform ||
            noise.Source.transform.IsChildOf(transform))) return;
        float distance = Vector3.Distance(transform.position, noise.Position);
        if (distance >= hearingRadius || float.IsNaN(noise.Intensity) || float.IsInfinity(noise.Intensity)) return;
        float strength = noise.Intensity * (1f - distance / Mathf.Max(0.01f, hearingRadius));
        // Priority describes the sound itself; Alert sensitivity changes meter gain only.
        float suspicionGain = strength * (state == AwarenessState.Alert ? alertHearingMultiplier : 1f);
        if (strength <= 0f) return;
        hasHeardNoise = true;
        lastHeardPosition = noise.Position;
        lastHeardSource = noise.Source;
        lastHeardIntensity = noise.Intensity;
        lastHeardLevel = noise.Level;
        lastStimulusTime = Time.time;
        bool withinWindow = Time.time - lastNoiseWindowTime <= noisePriorityWindow;
        bool winsWindow = !withinWindow || strength >= recentNoiseStrength;
        if (!withinWindow) { recentNoiseStrength = strength; lastNoiseWindowTime = Time.time; }
        else recentNoiseStrength = Mathf.Max(recentNoiseStrength, strength);
        if (winsWindow && (!hasCandidate || strength >= GetTargetSignificance()))
        {
            RememberTarget(noise.Position, strength);
            if (state == AwarenessState.Alert) SetWaypoint(candidatePosition);
        }
        bool wasUnaware = state == AwarenessState.Unaware;
        AddSuspicion(suspicionGain);
        if (wasUnaware && state == AwarenessState.Alert && hasCandidate) SetWaypoint(candidatePosition);
        HeardNoise?.Invoke(noise);
    }
    private void AddSuspicion(float amount)
    {
        if (state == AwarenessState.Unaware)
        {
            unawareSuspicion = Mathf.Clamp(unawareSuspicion + amount, 0f, 100f);
            if (unawareSuspicion >= 100f) EnterAlert(initialAlertSuspicion);
        }
        else if (state == AwarenessState.Alert) alertSuspicion = Mathf.Clamp(alertSuspicion + amount, 0f, 100f);
    }
    private void EnterAlert(float initial)
    {
        alertSuspicion = Mathf.Clamp(initial, 0f, 100f);
        unawareSuspicion = 0f;
        lostSightElapsed = 0f;
        lastStimulusTime = Time.time;
        ChangeState(AwarenessState.Alert);
    }
    private float GetRecognitionWeight(float distance, float angle, float range, float coneAngle)
    {
        float distanceWeight = Mathf.Lerp(1f, farRecognitionMultiplier, Mathf.Clamp01(distance / Mathf.Max(0.01f, range)));
        float angleWeight = Mathf.Lerp(1f, edgeRecognitionMultiplier, Mathf.Clamp01(angle / Mathf.Max(0.01f, coneAngle * 0.5f)));
        return distanceWeight * angleWeight;
    }
    private bool HasClearView()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 delta = player.position + Vector3.up * targetHeight - origin;
        float distance = delta.magnitude;
        if (distance <= 0.001f) return true;
        // Ignore self colliders even if environment layers overlap the enemy layer.
        foreach (RaycastHit hit in Physics.RaycastAll(origin, delta / distance, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.transform == player || hit.transform.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }
    private float GetTargetSignificance()
    {
        return hasCandidate ? Mathf.Max(0f, targetStrength - Mathf.Max(0f, Time.time - targetTime) * targetPriorityDecayPerSecond) : 0f;
    }
    private void RememberTarget(Vector3 position, float strength)
    {
        candidatePosition = position;
        hasCandidate = true;
        targetStrength = strength;
        targetTime = Time.time;
        currentTargetSignificance = strength;
    }
    public void ClearWaypoint()
    {
        hasWaypoint = hasCandidate = false;
        currentTargetSignificance = targetStrength = recentNoiseStrength = 0f;
        targetTime = lastNoiseWindowTime = float.NegativeInfinity;
        candidatePosition = currentWaypoint = Vector3.zero;
    }
    private void SetWaypoint(Vector3 position)
    {
        bool changed = !hasWaypoint || (currentWaypoint - position).sqrMagnitude > 0.000001f;
        currentWaypoint = position;
        hasWaypoint = true;
        if (changed) DestinationChanged?.Invoke(position);
    }
    private void ChangeState(AwarenessState next)
    {
        if (state == next) return;
        state = next;
        StateChanged?.Invoke(next);
    }
}
