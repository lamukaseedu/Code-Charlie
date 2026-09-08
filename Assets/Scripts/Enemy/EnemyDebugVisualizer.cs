using UnityEngine;

[RequireComponent(typeof(EnemyDetection))]
public class EnemyDebugVisualizer : MonoBehaviour
{
    [Tooltip("Number of segments used to draw the hearing circle")]
    public int circleSegments = 48;
    public Color hearingColor = Color.cyan;
    public Color fovColor = Color.yellow;
    public Color losColor = Color.red;
    public float lineWidth = 0.05f;
    public bool showRuntime = true;

    EnemyDetection detection;
    LineRenderer hearingLR;
    LineRenderer fovLeftLR;
    LineRenderer fovRightLR;
    LineRenderer losLR;
    Material lineMat;

    void Awake()
    {
        detection = GetComponent<EnemyDetection>();
        lineMat = new Material(Shader.Find("Sprites/Default"));
        CreateRenderers();
    }

    void CreateRenderers()
    {
        hearingLR = CreateLineRenderer("HearingLR", hearingColor, true, circleSegments + 1);
        fovLeftLR = CreateLineRenderer("FovLeftLR", fovColor, false, 2);
        fovRightLR = CreateLineRenderer("FovRightLR", fovColor, false, 2);
        losLR = CreateLineRenderer("LOS_LR", losColor, false, 2);
    }

    LineRenderer CreateLineRenderer(string name, Color color, bool loop, int positions)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = lineMat;
        lr.loop = loop;
        lr.positionCount = positions;
        lr.widthMultiplier = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        return lr;
    }

    void Update()
    {
        if (!showRuntime || detection == null) return;

        UpdateHearingCircle();
        UpdateFOVLines();
        UpdateLOSLine();
    }

    void UpdateHearingCircle()
    {
        if (hearingLR == null) return;

        float radius = detection.hearingRadius;
        int segments = Mathf.Max(4, circleSegments);
        hearingLR.positionCount = segments + 1;

        Vector3 center = transform.position;
        for (int i = 0; i <= segments; i++)
        {
            float theta = (float)i / segments * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(theta) * radius, 0f, Mathf.Sin(theta) * radius);
            hearingLR.SetPosition(i, pos);
        }
    }

    void UpdateFOVLines()
    {
        if (fovLeftLR == null || fovRightLR == null) return;

        Vector3 eyePos = transform.position + Vector3.up * detection.eyeHeight;
        float half = detection.fieldOfView * 0.5f;
        Vector3 leftDir = Quaternion.AngleAxis(-half, Vector3.up) * transform.forward;
        Vector3 rightDir = Quaternion.AngleAxis(half, Vector3.up) * transform.forward;

        fovLeftLR.SetPosition(0, eyePos);
        fovLeftLR.SetPosition(1, eyePos + leftDir.normalized * detection.visionRange);

        fovRightLR.SetPosition(0, eyePos);
        fovRightLR.SetPosition(1, eyePos + rightDir.normalized * detection.visionRange);
    }

    void UpdateLOSLine()
    {
        if (losLR == null) return;

        Vector3 eyePos = transform.position + Vector3.up * detection.eyeHeight;
        if (detection.HasLineOfSightToPlayer)
        {
            losLR.enabled = true;
            Vector3 target = detection.CurrentWaypoint;
            losLR.SetPosition(0, eyePos);
            losLR.SetPosition(1, target);
        }
        else
        {
            losLR.enabled = false;
        }
    }
}
