using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircuitBeamGraphic : MaskableGraphic
{
    [Header("Beam Appearance")]
    [SerializeField, Min(0.1f)] private float coreWidth = 2.5f;
    [SerializeField, Min(0.1f)] private float glowWidth = 8f;
    [SerializeField]
    private Color coreColor =
        new Color(0.8f, 1f, 0.85f, 1f);
    [SerializeField]
    private Color glowColor =
        new Color(0.25f, 1f, 0.5f, 0.25f);

    [Header("Animation")]
    [SerializeField, Min(1f)] private float speed = 160f;

    private readonly List<Vector2> path = new List<Vector2>();
    private float visibleLength;
    private Coroutine animation;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void ClearBeam()
    {
        if (animation != null)
        {
            StopCoroutine(animation);
            animation = null;
        }

        path.Clear();
        visibleLength = 0f;
        SetVerticesDirty();
    }

    public void PlayBeam(
        IList<Vector2> points,
        System.Action onFinished = null)
    {
        ClearBeam();

        if (points == null || points.Count < 2)
        {
            onFinished?.Invoke();
            return;
        }

        path.AddRange(points);
        animation = StartCoroutine(
            AnimateBeam(onFinished)
        );
    }

    private IEnumerator AnimateBeam(System.Action onFinished)
    {
        float totalLength = 0f;

        for (int i = 1; i < path.Count; i++)
            totalLength += Vector2.Distance(path[i - 1], path[i]);

        while (visibleLength < totalLength)
        {
            visibleLength = Mathf.Min(
                visibleLength + speed * Time.deltaTime,
                totalLength
            );

            SetVerticesDirty();
            yield return null;
        }

        animation = null;
        onFinished?.Invoke();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (path.Count < 2)
            return;

        List<Vector2> visiblePoints = GetVisiblePath();

        if (visiblePoints.Count < 2)
            return;

        // Draw the wider glow first.
        DrawPath(vh, visiblePoints, glowWidth, glowColor);

        // Then the bright central core.
        DrawPath(vh, visiblePoints, coreWidth, coreColor);
    }

    private List<Vector2> GetVisiblePath()
    {
        List<Vector2> result = new List<Vector2>();

        result.Add(path[0]);

        float remaining = visibleLength;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 a = path[i - 1];
            Vector2 b = path[i];

            float distance = Vector2.Distance(a, b);

            if (distance <= 0.0001f)
                continue;

            if (remaining >= distance)
            {
                result.Add(b);
                remaining -= distance;
            }
            else
            {
                result.Add(Vector2.Lerp(
                    a, b, Mathf.Clamp01(remaining / distance)
                ));
                break;
            }
        }

        return result;
    }

    private void DrawPath(
        VertexHelper vh,
        List<Vector2> points,
        float width,
        Color color)
    {
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 a = points[i - 1];
            Vector2 b = points[i];

            Vector2 delta = b - a;

            if (delta.sqrMagnitude <= 0.000001f)
                continue;

            Vector2 normal = new Vector2(
                -delta.y, delta.x
            ).normalized * width * 0.5f;

            int index = vh.currentVertCount;

            AddVertex(vh, a - normal, color);
            AddVertex(vh, a + normal, color);
            AddVertex(vh, b + normal, color);
            AddVertex(vh, b - normal, color);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        // Circular joins fill gaps at turns and round the ends.
        foreach (Vector2 point in points)
            DrawCircle(vh, point, width * 0.5f, color);
    }

    private void DrawCircle(
        VertexHelper vh,
        Vector2 center,
        float radius,
        Color color)
    {
        const int segments = 12;

        int index = vh.currentVertCount;
        AddVertex(vh, center, color);

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector2 offset = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;

            AddVertex(vh, center + offset, color);
        }

        for (int i = 0; i < segments; i++)
            vh.AddTriangle(index, index + i + 1, index + i + 2);
    }

    private void AddVertex(
        VertexHelper vh,
        Vector2 position,
        Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vh.AddVert(vertex);
    }
}
