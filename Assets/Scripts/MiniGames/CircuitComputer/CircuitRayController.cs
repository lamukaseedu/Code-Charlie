using System.Collections.Generic;
using UnityEngine;

public class CircuitRayController : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/8/2026
     */

    [Header("Animated Beam")]
    [SerializeField] private CircuitBeamGraphic beamGraphic;

    private CircuitPuzzleManager puzzle;

    private readonly List<Vector2> path = new List<Vector2>();
    private readonly List<CircuitCell> poweredCells =
        new List<CircuitCell>();

    public void Initialize(CircuitPuzzleManager manager)
    {
        puzzle = manager;
    }

    // Automatically updates the beam whenever the circuit changes.

    public void RefreshBeam()
    {
        if (puzzle == null || beamGraphic == null)
        {
            Debug.LogError(
                "CircuitRayController needs PuzzleManager and CircuitBeamGraphic.",
                this
            );
            return;
        }

        ClearVisuals();

        bool success = TraceCircuit();

        // Always start with a red beam.
        beamGraphic.PlayBeam(path, () =>
        {
            foreach (CircuitCell cell in poweredCells)
            {
                if (cell != null)
                    cell.SetPowered(true);
            }

            if (success)
            {
                // Turn the completed beam green.
                beamGraphic.SetComplete(true);

                // Unlock the door.
                puzzle.SimulationFinished(true);
            }
        });
    }


    private bool TraceCircuit()
    {
        path.Clear();
        poweredCells.Clear();

        int source = -1;
        int requiredCount = 0;

        // Find START and count required receivers.
        for (int i = 0; i < puzzle.CellCount; i++)
        {
            CircuitCell cell = puzzle.GetCell(i);

            if (cell == null)
                continue;

            if (cell.Kind == CircuitCellKind.Start)
                source = i;

            if (cell.RequiredReceiver)
                requiredCount++;
        }

        if (source < 0)
        {
            Debug.LogWarning("No START cell configured.", this);
            return false;
        }

        CircuitCell startCell = puzzle.GetCell(source);

        int current = source;
        CircuitDirection direction = startCell.StartDirection;

        HashSet<int> receiversReached = new HashSet<int>();

        // Prevent infinite loops.
        HashSet<(int, CircuitDirection)> visited =
            new HashSet<(int, CircuitDirection)>();

        poweredCells.Add(startCell);

        // Start at the center of the START cell.
        path.Add(GetCellCenter(startCell));
        path.Add(GetCellEdge(startCell, direction));

        while (true)
        {
            if (!visited.Add((current, direction)))
                return false;

            int next = NextIndex(current, direction);

            if (next < 0)
            {
                // Extend the beam to the board boundary.
                path.Add(GetCellEdge(
                    puzzle.GetCell(current),
                    direction
                ));

                return false;
            }

            CircuitCell nextCell = puzzle.GetCell(next);

            if (nextCell == null)
                return false;

            CircuitDirection enteringSide =
                CircuitPiece.Opposite(direction);

            // Obstacles and START cells block the beam.
            if (nextCell.Kind == CircuitCellKind.Obstacle ||
                nextCell.Kind == CircuitCellKind.Start)
            {
                path.Add(GetCellEdge(nextCell, enteringSide));
                return false;
            }

            // Check END's incoming connector.
            if (nextCell.Kind == CircuitCellKind.End)
            {
                path.Add(GetCellEdge(nextCell, enteringSide));

                bool validEnd =
                    enteringSide == nextCell.ConnectionDirection;

                bool allReceiversReached =
                    receiversReached.Count == requiredCount;

                if (validEnd && allReceiversReached)
                    poweredCells.Add(nextCell);

                return validEnd && allReceiversReached;
            }

            // Circuit pieces must accept the incoming beam.
            if (nextCell.TryGetPiece(out CircuitPieceType piece))
            {
                if (!CircuitPiece.TryGetExit(
                    piece,
                    enteringSide,
                    out CircuitDirection exit))
                {
                    path.Add(GetCellEdge(nextCell, enteringSide));
                    return false;
                }

                direction = exit;
            }

            // Empty cells continue straight.
            path.Add(GetCellCenter(nextCell));

            poweredCells.Add(nextCell);

            if (nextCell.RequiredReceiver)
                receiversReached.Add(next);

            current = next;
        }
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
            case CircuitDirection.Up:
                nextRow--;
                break;

            case CircuitDirection.Right:
                nextCol++;
                break;

            case CircuitDirection.Down:
                nextRow++;
                break;

            case CircuitDirection.Left:
                nextCol--;
                break;

            default:
                return -1;
        }

        int rows = (puzzle.CellCount + columns - 1) / columns;

        if (nextRow < 0 || nextRow >= rows ||
            nextCol < 0 || nextCol >= columns)
            return -1;

        int nextIndex = nextRow * columns + nextCol;

        if (nextIndex >= puzzle.CellCount)
            return -1;

        return nextIndex;
    }

    private Vector2 GetCellCenter(CircuitCell cell)
    {
        RectTransform rect = cell.RectTransform;

        Vector3 worldCenter =
            rect.TransformPoint(rect.rect.center);

        RectTransform beamRect =
            (RectTransform)beamGraphic.transform;

        return beamRect.InverseTransformPoint(worldCenter);
    }

    private Vector2 GetCellEdge(
        CircuitCell cell,
        CircuitDirection direction)
    {
        RectTransform rect = cell.RectTransform;

        Vector2 edge = rect.rect.center;

        switch (direction)
        {
            case CircuitDirection.Up:
                edge.y += rect.rect.height * 0.5f;
                break;

            case CircuitDirection.Down:
                edge.y -= rect.rect.height * 0.5f;
                break;

            case CircuitDirection.Left:
                edge.x -= rect.rect.width * 0.5f;
                break;

            case CircuitDirection.Right:
                edge.x += rect.rect.width * 0.5f;
                break;
        }

        Vector3 worldPosition = rect.TransformPoint(edge);

        RectTransform beamRect =
            (RectTransform)beamGraphic.transform;

        return beamRect.InverseTransformPoint(worldPosition);
    }

    public void ClearVisuals()
    {
        if (beamGraphic != null)
            beamGraphic.ClearBeam();

        path.Clear();
        poweredCells.Clear();

        if (puzzle == null)
            return;

        for (int i = 0; i < puzzle.CellCount; i++)
        {
            CircuitCell cell = puzzle.GetCell(i);

            if (cell != null)
                cell.SetPowered(false);
        }
    }
}
