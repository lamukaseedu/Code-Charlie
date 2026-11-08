using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircuitRayController : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/8/2026
     */
    [Header("Animated Beam")]
    [SerializeField] private RectTransform rayOverlay;
    [SerializeField] private Sprite beamSprite; 
    [SerializeField] private Color beamColor = new Color(0.5f, 1f, 0.6f, 1f);
    [SerializeField, Min(1f)] private float beamThickness = 4f;
    [SerializeField, Min(0f)] private float stepDelay = 0.12f;

    private CircuitPuzzleManager puzzle;
    private readonly List<GameObject> segments = new List<GameObject>();

    public void Initialize(CircuitPuzzleManager manager) => puzzle = manager;

    public void RunCircuit()
    {
        if (puzzle == null || rayOverlay == null)
        {
            Debug.LogError("CircuitRayController needs PuzzleManager and RayOverlay.", this);
            if (puzzle != null) puzzle.SimulationFinished(false);
            return;
        }
        StartCoroutine(Trace());
    }

    private IEnumerator Trace()
    {
        ClearVisuals();
        int source = -1;
        int requiredCount = 0;
        for (int i = 0; i < puzzle.CellCount; i++)
        {
            CircuitCell cell = puzzle.GetCell(i);
            if (cell == null) continue;
            if (cell.Kind == CircuitCellKind.Start) source = i;
            if (cell.RequiredReceiver) requiredCount++;
        }

        if (source < 0)
        {
            Debug.LogWarning("No START cell configured.", this);
            puzzle.SimulationFinished(false);
            yield break;
        }

        int current = source;
        CircuitDirection direction = puzzle.GetCell(source).StartDirection;
        int receiversReached = 0;
        var visited = new HashSet<int>();
        bool success = false;
        puzzle.GetCell(source).SetPowered(true);

        while (true)
        {
            int state = current * 4 + (int)direction;
            if (!visited.Add(state)) break;

            int next = NextIndex(current, direction);
            if (next < 0) break;
            CircuitCell nextCell = puzzle.GetCell(next);
            if (nextCell == null) break;

            DrawSegment(puzzle.GetCell(current).RectTransform, nextCell.RectTransform);
            if (stepDelay > 0f) yield return new WaitForSeconds(stepDelay);

            if (nextCell.Kind == CircuitCellKind.Obstacle || nextCell.Kind == CircuitCellKind.Start)
                break;

            if (nextCell.Kind == CircuitCellKind.End)
            {
                success = receiversReached == requiredCount;
                nextCell.SetPowered(success);
                break;
            }

            if (nextCell.TryGetPiece(out CircuitPieceType piece))
            {
                CircuitDirection enteringSide = CircuitPiece.Opposite(direction);
                if (!CircuitPiece.TryGetExit(piece, enteringSide, out CircuitDirection exit))
                    break;
                direction = exit;
            }
            // Otherwise the cell is empty and the beam keeps going straight.

            nextCell.SetPowered(true);
            if (nextCell.RequiredReceiver)
                receiversReached++;
            current = next;
        }
        puzzle.SimulationFinished(success);
    }

    private int NextIndex(int index, CircuitDirection direction)
    {
        int columns = puzzle.Columns;
        int row = index / columns;
        int col = index % columns;
        int nextRow = row;
        int nextCol = col;
        switch (direction)
        {
            case CircuitDirection.Up: nextRow--; break;
            case CircuitDirection.Right: nextCol++; break;
            case CircuitDirection.Down: nextRow++; break;
            case CircuitDirection.Left: nextCol--; break;
        }
        if (nextRow < 0 || nextCol < 0 || nextCol >= columns ||
            nextRow * columns + nextCol >= puzzle.CellCount) return -1;
        return nextRow * columns + nextCol;
    }

    private void DrawSegment(RectTransform from, RectTransform to)
    {
        if (rayOverlay == null) return;
        Canvas canvas = rayOverlay.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 screenA = RectTransformUtility.WorldToScreenPoint(eventCamera, from.TransformPoint(from.rect.center));
        Vector2 screenB = RectTransformUtility.WorldToScreenPoint(eventCamera, to.TransformPoint(to.rect.center));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rayOverlay, screenA, eventCamera, out Vector2 a);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rayOverlay, screenB, eventCamera, out Vector2 b);
        Vector2 delta = b - a;

        GameObject segment = new GameObject("BeamSegment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segment.transform.SetParent(rayOverlay, false);
        RectTransform rect = segment.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, beamThickness);
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Image image = segment.GetComponent<Image>();
        image.sprite = beamSprite;
        image.color = beamColor;
        image.raycastTarget = false;
        segments.Add(segment);
    }

    public void ClearVisuals()
    {
        foreach (GameObject obj in segments)
            if (obj != null) Destroy(obj);
        segments.Clear();
        if (puzzle == null) return;
        for (int i = 0; i < puzzle.CellCount; i++)
        {
            CircuitCell cell = puzzle.GetCell(i);
            if (cell != null) cell.SetPowered(false);
        }
    }
}
