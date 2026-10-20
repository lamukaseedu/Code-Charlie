using UnityEngine;

//Author Gabriel[gameplay design]

[RequireComponent(typeof(EnemyAwareness))]
public class EnemyDebugVisualizer : MonoBehaviour
{
    [Tooltip("Segments used for hearing, proximity and short-range vision outlines")]
    public int circleSegments = 48;
    public Color hearingColor = Color.cyan;
    public Color fovColor = Color.yellow;
    public Color losColor = Color.red;
    public float lineWidth = 0.05f;
    public bool showRuntime = true;
    public Color shortFovColor = new Color(1f, 0.5f, 0f);
    public Color proximityColor = Color.magenta;

    private EnemyAwareness awareness;
    private LineRenderer hearingLR, fovLeftLR, fovRightLR;
    private LineRenderer shortLeftLR, shortRightLR, shortArcLR, proximityLR, losLR;
    private LineRenderer[] lines;
    private Material lineMat;

    private void Awake()
    {
        awareness = GetComponent<EnemyAwareness>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("Enemy debug lines need the Sprites/Default shader.", this);
            enabled = false;
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
        lines = new[] { hearingLR, fovLeftLR, fovRightLR, shortLeftLR,
            shortRightLR, shortArcLR, proximityLR, losLR };
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
        return line;
    }

    private void LateUpdate()
    {
        bool visible = showRuntime && awareness != null && awareness.isActiveAndEnabled;
        SetVisible(visible);
        if (!visible || lines == null) return;

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
    }

    private void DrawBoundary(LineRenderer line, Vector3 origin, Vector3 forward,
        float angle, float radius, Color color)
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

    private void DrawArc(LineRenderer line, Vector3 center, Vector3 forward,
        float fieldOfView, float radius, Color color)
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
        foreach (LineRenderer line in lines)
            if (line != null) line.enabled = visible;
    }

    private void OnDisable() { SetVisible(false); }

    private void OnDestroy()
    {
        if (lines != null)
            foreach (LineRenderer line in lines)
                if (line != null) Destroy(line.gameObject);
        if (lineMat != null) Destroy(lineMat);
    }
}
