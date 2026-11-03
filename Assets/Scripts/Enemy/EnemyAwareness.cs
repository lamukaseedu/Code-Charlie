using System;
using UnityEngine;

// Sensing and memory only. EnemyMovement remains the sole physical motor.
public class EnemyAwareness : MonoBehaviour
{
    public enum AwarenessState { Unaware, Alert, Active }
    [SerializeField] private Transform player;
    [Header("Hearing")]
    [Min(0.01f)] public float hearingRadius = 12f;
    [Min(0f)] public float alertHearingMultiplier = 1.5f;
    [Min(0f)] public float noisePriorityWindow = 0.15f;
    [Min(0f)] public float targetPriorityDecayPerSecond = 10f;
    [Header("Suspicion")]
    [Range(0f, 100f)] public float initialAlertSuspicion = 50f;
    [Min(0f)] public float unawareDecayDelay = 2f;
    [Min(0f)] public float alertDecayDelay = 5f;
    [Min(0f)] public float suspicionDecayPerSecond = 5f;
    [Header("Vision")]
    [Min(0.01f)] public float visionRange = 12f;
    [Range(1f, 360f)] public float fieldOfView = 90f;
    [Min(0f)] public float shortVisionRange = 2.5f;
    [Range(1f, 360f)] public float shortFieldOfView = 240f;
    public LayerMask obstacleMask = Physics.DefaultRaycastLayers;
    public float eyeHeight = 1.2f;
    public float targetHeight = 1f;
    [Min(0.01f)] public float unawareRecognitionTime = 1f;
    [Min(0.01f)] public float alertRecognitionTime = 0.4f;
    [Range(0.01f, 1f)] public float glimpseThreshold = 0.2f;
    [Range(0f, 1f)] public float farRecognitionMultiplier = 0.4f;
    [Range(0f, 1f)] public float edgeRecognitionMultiplier = 0.5f;
    [Min(0f)] public float recognitionDecayPerSecond = 1f;
    [Min(0f)] public float lostSightTime = 3f;
    [Header("Proximity (suspicion, never visual recognition)")]
    [Min(0f)] public float proximityRadius = 2.5f;
    [Min(0f)] public float unawareProximityPerSecond = 40f;
    [Min(0f)] public float alertProximityPerSecond = 60f;
    [Header("Runtime debug (read only during play)")]
    [SerializeField] private AwarenessState state;
    [SerializeField] private float unawareSuspicion;
    [SerializeField] private float alertSuspicion;
    [SerializeField] private float visualRecognition;
    [SerializeField] private float lostSightElapsed;
    [SerializeField] private bool hasLineOfSightToPlayer;
    [SerializeField] private bool hasProximityAwareness;
    [SerializeField] private bool hasWaypoint;
    [SerializeField] private Vector3 currentWaypoint;
    [SerializeField] private Vector3 lastKnownPlayerPosition;
    [SerializeField] private bool hasHeardNoise;
    [SerializeField] private Vector3 lastHeardPosition;
    [SerializeField] private GameObject lastHeardSource;
    [SerializeField] private float lastHeardIntensity;
    [SerializeField] private NoiseEmitter.NoiseLevel lastHeardLevel;
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

    private void Awake()
    {
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
    private void OnEnable() { NoiseEmitter.NoiseProduced += HearNoise; }
    private void OnDisable()
    {
        NoiseEmitter.NoiseProduced -= HearNoise;
        hasLineOfSightToPlayer = hasProximityAwareness = false;
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
            float duration = state == AwarenessState.Unaware ? unawareRecognitionTime : alertRecognitionTime;
            visualRecognition = Mathf.Clamp01(visualRecognition + dt / Mathf.Max(0.01f, duration) * recognitionWeight);
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
