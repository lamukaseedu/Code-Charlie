using UnityEngine;

//Author Gabriel[gameplay design]
// Read-only diagnostics: awareness and movement retain all gameplay ownership.
[RequireComponent(typeof(EnemyAwareness))]
public class EnemyDebugVisualizer : MonoBehaviour
{
    [Tooltip("Segments used for hearing, proximity and vision outlines")]
    public int circleSegments = 48;
    public Color hearingColor = Color.cyan;
    public Color fovColor = Color.yellow;
    public Color losColor = Color.red;
    public float lineWidth = 0.05f;
    [Tooltip("Master switch for the panel and world drawings.")]
    public bool showRuntime = true;
    public Color shortFovColor = new Color(1f, 0.5f, 0f);
    public Color proximityColor = Color.magenta;
    [Header("Debugger display")]
    public bool showOverlay = true;
    public bool showWorldGeometry = true;
    [Tooltip("Normally diagnostics are visible only in the Editor and Development Builds.")]
    public bool showInReleaseBuild = false;
    [Tooltip("Offset inward from the upper-right screen corner. Increase Y to stack multiple enemy panels.")]
    public Vector2 overlayOffset = new Vector2(16f, 16f);
    [Range(0.6f, 2f)] public float overlayScale = 1f;
    public Color pathColor = new Color(0.3f, 1f, 0.45f);
    public Color destinationColor = Color.white;

    private EnemyAwareness awareness;
    private EnemyMovement movement;
    private LineRenderer hearingLR, fovLeftLR, fovRightLR;
    private LineRenderer shortLeftLR, shortRightLR, shortArcLR, proximityLR, losLR;
    private LineRenderer noiseLR, destinationLR, pathLR;
    private LineRenderer[] lines;
    private Material lineMat;
    private GUIStyle titleStyle, textStyle, smallStyle;
    private bool hasDisplayDestination;
    private Vector3 displayDestination;
    private string destinationLabel = "Destination: none";
    private string noiseLabel = "Last heard: none";
    private string navigationLabel = "Navigation: no movement component";
    private string stateLabel = "UNAWARE";
    private string sightLabel = "Sight: blocked / outside cone";
    private string proximityLabel = "Proximity: no nearby presence";
    private string lostSightLabel = "Lost sight: inactive";
    private float unawareMeter, alertMeter, recognitionMeter, lostSightMeter;
    private Color stateColor;
    private bool RuntimeVisible => showRuntime && (Application.isEditor || Debug.isDebugBuild || showInReleaseBuild)
        && awareness != null && awareness.isActiveAndEnabled;

    private void Awake()
    {
        awareness = GetComponent<EnemyAwareness>();
        movement = GetComponent<EnemyMovement>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("Enemy debug world lines need the Sprites/Default shader. The panel remains available.", this);
            return;
        }
        lineMat = new Material(shader);
        hearingLR = CreateLineRenderer("HearingLR", true);
        fovLeftLR = CreateLineRenderer("FovLeftLR", false);
        fovRightLR = CreateLineRenderer("FovRightLR", false);
        shortLeftLR = CreateLineRenderer("ShortFovLeftLR", false);
        shortRightLR = CreateLineRenderer("ShortFovRightLR", false);
        shortArcLR = CreateLineRenderer("ShortFovArcLR", false);
        proximityLR = CreateLineRenderer("ProximityLR", true);
        losLR = CreateLineRenderer("LOS_LR", false);
        noiseLR = CreateLineRenderer("LastHeardLR", true);
        destinationLR = CreateLineRenderer("DestinationLR", true);
        pathLR = CreateLineRenderer("NavigationPathLR", false);
        lines = new[] { hearingLR, fovLeftLR, fovRightLR, shortLeftLR,
            shortRightLR, shortArcLR, proximityLR, losLR, noiseLR, destinationLR, pathLR };
        SetVisible(false);
    }

    private LineRenderer CreateLineRenderer(string objectName, bool loop)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        var line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMat;
        line.loop = loop;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.enabled = false;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    private void LateUpdate()
    {
        bool visible = RuntimeVisible;
        SetVisible(visible && showWorldGeometry);
        if (!visible) return;
        ReadDiagnostics();
        if (!showWorldGeometry || lines == null) return;

        Vector3 center = transform.position;
        Vector3 eyes = center + Vector3.up * awareness.eyeHeight;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        DrawCircle(hearingLR, center, awareness.hearingRadius, hearingColor);
        DrawCircle(proximityLR, center, awareness.proximityRadius, proximityColor);
        DrawBoundary(fovLeftLR, eyes, forward, -awareness.fieldOfView * 0.5f, awareness.visionRange, fovColor);
        DrawBoundary(fovRightLR, eyes, forward, awareness.fieldOfView * 0.5f, awareness.visionRange, fovColor);
        DrawBoundary(shortLeftLR, eyes, forward, -awareness.shortFieldOfView * 0.5f, awareness.shortVisionRange, shortFovColor);
        DrawBoundary(shortRightLR, eyes, forward, awareness.shortFieldOfView * 0.5f, awareness.shortVisionRange, shortFovColor);
        DrawArc(shortArcLR, eyes, forward, awareness.shortFieldOfView, awareness.shortVisionRange, shortFovColor);
        bool shortEnabled = awareness.shortVisionRange > 0f;
        shortLeftLR.enabled = shortRightLR.enabled = shortArcLR.enabled = shortEnabled;
        proximityLR.enabled = awareness.proximityRadius > 0f;
        losLR.enabled = awareness.HasLineOfSightToPlayer;
        if (losLR.enabled)
        {
            Style(losLR, losColor);
            losLR.SetPosition(0, eyes);
            losLR.SetPosition(1, awareness.VisionTargetPosition);
        }
        noiseLR.enabled = awareness.HasHeardNoise;
        if (noiseLR.enabled) DrawCircle(noiseLR, awareness.LastHeardPosition + Vector3.up * 0.08f, 0.35f, hearingColor);
        destinationLR.enabled = hasDisplayDestination;
        if (destinationLR.enabled) DrawCircle(destinationLR, displayDestination + Vector3.up * 0.12f, 0.5f, destinationColor);
        Vector3[] corners = movement != null && movement.isActiveAndEnabled ? movement.PathCorners : null;
        pathLR.enabled = corners != null && corners.Length >= 2;
        if (pathLR.enabled)
        {
            Style(pathLR, pathColor);
            pathLR.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++) pathLR.SetPosition(i, corners[i] + Vector3.up * 0.1f);
        }
    }

    private void ReadDiagnostics()
    {
        var state = awareness.State;
        stateLabel = state.ToString().ToUpperInvariant();
        stateColor = state == EnemyAwareness.AwarenessState.Active ? new Color(1f, 0.35f, 0.3f) :
            state == EnemyAwareness.AwarenessState.Alert ? new Color(1f, 0.75f, 0.2f) : new Color(0.4f, 0.85f, 1f);
        unawareMeter = Mathf.Clamp01(awareness.UnawareSuspicion / 100f);
        alertMeter = Mathf.Clamp01(awareness.AlertSuspicion / 100f);
        recognitionMeter = Mathf.Clamp01(awareness.VisualRecognition);
        sightLabel = awareness.HasLineOfSightToPlayer ? "Sight: clear (recognition still required)" : "Sight: blocked / outside cone";
        proximityLabel = awareness.HasProximityAwareness ? "Proximity: nearby presence (suspicion only)" : "Proximity: no nearby presence";
        lostSightMeter = 0f;
        lostSightLabel = "Lost sight: inactive";
        if (state == EnemyAwareness.AwarenessState.Active)
        {
            float remaining = Mathf.Max(0f, awareness.lostSightTime - awareness.LostSightElapsed);
            lostSightMeter = awareness.lostSightTime > 0f ? Mathf.Clamp01(remaining / awareness.lostSightTime) : 0f;
            lostSightLabel = awareness.HasLineOfSightToPlayer ? "Lost sight: timer reset (player visible)" :
                "Lost sight: " + remaining.ToString("F1") + " s until Alert";
        }
        hasDisplayDestination = awareness.HasWaypoint;
        displayDestination = awareness.CurrentWaypoint;
        string kind = state == EnemyAwareness.AwarenessState.Active ? "Chase memory" :
            state == EnemyAwareness.AwarenessState.Alert ? "Investigation" : "Turn toward suspicion";
        if (!hasDisplayDestination && (state == EnemyAwareness.AwarenessState.Unaware || state == EnemyAwareness.AwarenessState.Alert) &&
            movement != null && movement.isActiveAndEnabled && movement.HasPatrolTarget)
        {
            hasDisplayDestination = true;
            displayDestination = movement.PatrolTarget;
            kind = movement.IsAlertSearching ? "Alert search" : "Random patrol " + (movement.PatrolIndex + 1);
        }
        destinationLabel = hasDisplayDestination ? kind + ": " + Position(displayDestination) : "Destination: none / waiting";
        navigationLabel = movement == null ? "Navigation: no movement component" :
            !movement.isActiveAndEnabled ? "Navigation: movement disabled" : "Navigation: " + movement.NavigationStatus;
        noiseLabel = "Last heard: none";
        if (awareness.HasHeardNoise)
        {
            string source = awareness.LastHeardSource != null ? awareness.LastHeardSource.name : "unknown / removed";
            if (source.Length > 28) source = source.Substring(0, 25) + "...";
            noiseLabel = "Last heard: " + awareness.LastHeardLevel + " | intensity " + awareness.LastHeardIntensity.ToString("F1") +
                "\nSource: " + source + "\nPosition: " + Position(awareness.LastHeardPosition) + " (remembered)";
        }
    }

    private static string Position(Vector3 p) { return "(" + p.x.ToString("F1") + ", " + p.y.ToString("F1") + ", " + p.z.ToString("F1") + ")"; }

    private void OnGUI()
    {
        // Labels and rectangles only: never consume gameplay mouse/keyboard input.
        if (!isActiveAndEnabled || !RuntimeVisible || !showOverlay || Event.current.type != EventType.Repaint) return;
        EnsureStyles();
        const float width = 370f, height = 540f;
        float scale = Mathf.Min(Mathf.Clamp(overlayScale, 0.6f, 2f),
            Mathf.Min(Screen.width / (width + 16f), Screen.height / (height + 16f)));
        if (scale <= 0f) return;
        float x = Mathf.Clamp(Screen.width - width * scale - Mathf.Max(0f, overlayOffset.x), 0f, Mathf.Max(0f, Screen.width - width * scale));
        float y = Mathf.Clamp(Mathf.Max(0f, overlayOffset.y), 0f, Mathf.Max(0f, Screen.height - height * scale));
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        try
        {
            GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.identity, Vector3.one * scale);
            Fill(new Rect(0, 0, width, height), new Color(0.035f, 0.045f, 0.065f, 0.94f));
            Fill(new Rect(0, 0, 5, height), stateColor);
            string enemyName = name.Length > 25 ? name.Substring(0, 22) + "..." : name;
            Label(new Rect(16, 12, 338, 26), enemyName + "  |  " + stateLabel, titleStyle, stateColor);
            Label(new Rect(16, 42, 338, 20), "AI DEBUG  /  read-only", smallStyle, Color.gray);
            Meter(72, "Unaware suspicion", unawareMeter, new Color(0.4f, 0.85f, 1f));
            Meter(119, "Alert suspicion", alertMeter, new Color(1f, 0.75f, 0.2f));
            Meter(166, "Visual recognition", recognitionMeter, new Color(1f, 0.4f, 0.35f));
            Label(new Rect(16, 215, 338, 22), lostSightLabel, textStyle, Color.white);
            Fill(new Rect(16, 241, 338, 5), new Color(0.16f, 0.19f, 0.23f));
            Fill(new Rect(16, 241, 338 * lostSightMeter, 5), stateColor);
            Label(new Rect(16, 257, 338, 20), sightLabel, textStyle, Color.white);
            Label(new Rect(16, 280, 338, 20), proximityLabel, smallStyle, Color.white);
            Label(new Rect(16, 310, 338, 60), noiseLabel, smallStyle, Color.white);
            Label(new Rect(16, 380, 338, 35), destinationLabel, textStyle, Color.white);
            Label(new Rect(16, 417, 338, 38), navigationLabel, smallStyle, Color.white);
            Label(new Rect(16, 464, 338, 18), "WORLD GUIDE", smallStyle, Color.gray);
            Legend(16, 486, "Hearing / noise", hearingColor);
            Legend(190, 486, "Vision", fovColor);
            Legend(16, 503, "Close vision", shortFovColor);
            Legend(190, 503, "Proximity", proximityColor);
            Legend(16, 520, "Path", pathColor);
            Legend(190, 520, "Destination", destinationColor);
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        smallStyle = new GUIStyle(textStyle) { fontSize = 12 };
    }
    private void Meter(float y, string caption, float value, Color color)
    {
        Label(new Rect(16, y, 338, 22), caption + "    " + (value * 100f).ToString("F0") + "%", textStyle, Color.white);
        Fill(new Rect(16, y + 25, 338, 10), new Color(0.16f, 0.19f, 0.23f));
        Fill(new Rect(16, y + 25, 338 * value, 10), color);
    }
    private void Legend(float x, float y, string caption, Color color)
    {
        Fill(new Rect(x, y + 4, 8, 8), color);
        Label(new Rect(x + 13, y, 155, 17), caption, smallStyle, Color.white);
    }
    private static void Fill(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
    private static void Label(Rect rect, string value, GUIStyle style, Color color)
    { GUI.color = color; GUI.Label(rect, value, style); }

    private void DrawBoundary(LineRenderer line, Vector3 origin, Vector3 forward, float angle, float radius, Color color)
    {
        Style(line, color);
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + Quaternion.AngleAxis(angle, Vector3.up) * forward * Mathf.Max(0f, radius));
    }
    private void DrawCircle(LineRenderer line, Vector3 center, float radius, Color color)
    {
        Style(line, color);
        int segments = Mathf.Clamp(circleSegments, 4, 256);
        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(0f, radius));
        }
    }
    private void DrawArc(LineRenderer line, Vector3 center, Vector3 forward, float fieldOfView, float radius, Color color)
    {
        Style(line, color);
        int segments = Mathf.Clamp(circleSegments, 4, 256);
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-fieldOfView * 0.5f, fieldOfView * 0.5f, (float)i / segments);
            line.SetPosition(i, center + Quaternion.AngleAxis(angle, Vector3.up) * forward * Mathf.Max(0f, radius));
        }
    }
    private void Style(LineRenderer line, Color color)
    {
        line.widthMultiplier = Mathf.Max(0.001f, lineWidth);
        line.startColor = line.endColor = color;
    }
    private void SetVisible(bool visible)
    {
        if (lines == null) return;
        foreach (LineRenderer line in lines) if (line != null) line.enabled = visible;
    }
    private void OnDisable() { SetVisible(false); }
    private void OnDestroy()
    {
        if (lines != null) foreach (LineRenderer line in lines) if (line != null) Destroy(line.gameObject);
        if (lineMat != null) Destroy(lineMat);
    }
}
