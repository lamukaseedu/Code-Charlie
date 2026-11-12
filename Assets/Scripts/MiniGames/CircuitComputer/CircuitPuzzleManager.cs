using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

public class CircuitPuzzleManager : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/7/2026
     */

    [Serializable]
    public struct PieceSprite
    {
        public CircuitPieceType type;
        public Sprite sprite;
    }

    [Header("Existing Canvas")]
    [SerializeField] private RectTransform dragLayer;
    [SerializeField] private CircuitInventoryUI inventory;
    [SerializeField] private RectTransform circuitGrid;
    [SerializeField, Min(1)] private int columns = 5;
    [SerializeField] private CircuitRayController rayController;

    [Header("Piece Sprites")]
    [SerializeField] private PieceSprite[] pieceSprites;

    [Header("Special Cell Sprites")]
    [SerializeField] private Sprite obstacleSprite;

    [Header("Start Sprites")]
    [SerializeField] private Sprite startUpSprite;
    [SerializeField] private Sprite startDownSprite;
    [SerializeField] private Sprite startLeftSprite;
    [SerializeField] private Sprite startRightSprite;

    [Header("End Sprites")]
    [SerializeField] private Sprite endUpSprite;
    [SerializeField] private Sprite endDownSprite;
    [SerializeField] private Sprite endLeftSprite;
    [SerializeField] private Sprite endRightSprite;

    [Header("Puzzle Completion")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UnityEvent onPuzzleCompleted;

    private CircuitCell[] cells;

    public RectTransform DragLayer => dragLayer;
    public CircuitInventoryUI Inventory => inventory;

    public bool IsSolved { get; private set; }
    public bool IsBusy { get; private set; }

    public int Columns => columns;
    public int CellCount => cells == null ? 0 : cells.Length;

    private void Start()
    {
        Debug.Log("CircuitPuzzleManager: Start called!", this);
        // Automatically find every circuit cell under the grid.
        if (circuitGrid != null)
        {
            cells = circuitGrid.GetComponentsInChildren<CircuitCell>(
                true
            );
        }

        if (inventory == null || rayController == null ||
            dragLayer == null || circuitGrid == null ||
            cells == null || cells.Length == 0)
        {
            Debug.LogError(
                "CircuitPuzzleManager: Missing required references.",
                this
            );

            enabled = false;
            return;
        }

        if (cells.Length % columns != 0)
        {
            Debug.LogWarning(
                "Circuit grid cell count is not divisible by Columns.",
                this
            );
        }

        // Initialize all cells.
        foreach (CircuitCell cell in cells)
        {
            if (cell != null)
                cell.Initialize(this);
        }

        inventory.Initialize(this);
        rayController.Initialize(this);

        Debug.Log(
        $"CircuitPuzzleManager: Initialized {cells.Length} cells.",
        this
        );

        IsSolved = false;
        IsBusy = false;

        SetStatus("ROUTE POWER THROUGH ALL RECEIVERS");

        // Start the beam automatically.
        StartCoroutine(InitializeBeam());
    }

    public CircuitCell GetCell(int index)
    {
        return index >= 0 && index < CellCount
            ? cells[index]
            : null;
    }

    public int IndexOf(CircuitCell cell)
    {
        return Array.IndexOf(cells, cell);
    }

    public Sprite GetSprite(CircuitPieceType type)
    {
        if (pieceSprites == null)
            return null;

        foreach (PieceSprite item in pieceSprites)
        {
            if (item.type == type)
                return item.sprite;
        }

        return null;
    }

    public Sprite GetSpecialSprite(
        CircuitCellKind kind,
        CircuitDirection direction)
    {
        switch (kind)
        {
            case CircuitCellKind.Obstacle:
                return obstacleSprite;

            case CircuitCellKind.Start:
                switch (direction)
                {
                    case CircuitDirection.Up:
                        return startUpSprite;

                    case CircuitDirection.Down:
                        return startDownSprite;

                    case CircuitDirection.Left:
                        return startLeftSprite;

                    case CircuitDirection.Right:
                        return startRightSprite;
                }
                break;

            case CircuitCellKind.End:
                switch (direction)
                {
                    case CircuitDirection.Up:
                        return endUpSprite;

                    case CircuitDirection.Down:
                        return endDownSprite;

                    case CircuitDirection.Left:
                        return endLeftSprite;

                    case CircuitDirection.Right:
                        return endRightSprite;
                }
                break;
        }

        return null;
    }

    public bool CanDropOnCell(
        CircuitDragHandler source,
        CircuitCell target)
    {
        if (IsBusy || IsSolved ||
            source == null || target == null ||
            !target.CanReceivePiece ||
            source.SourceCell == target)
        {
            return false;
        }

        if (source.SourceCell != null)
            return source.SourceCell.HasPlacedPiece;

        return inventory.HasAvailable(source.PieceType);
    }


    public void TryDropOnCell(
        CircuitDragHandler source,
        CircuitCell target)
    {
        if (!CanDropOnCell(source, target))
            return;

        CircuitPieceType newPiece = source.PieceType;

        // Determine whether the target already has a piece.
        bool targetOccupied = target.HasPlacedPiece;

        CircuitPieceType oldPiece = targetOccupied
            ? target.PlacedPiece
            : default;

        if (source.SourceCell != null)
        {
            // Dragging from another board cell.
            CircuitCell originalCell = source.SourceCell;

            if (targetOccupied)
            {
                // Swap the pieces between the two cells.
                originalCell.PlacePlayerPiece(oldPiece);
                target.PlacePlayerPiece(newPiece);
            }
            else
            {
                // Move the piece into an empty cell.
                originalCell.ClearPlayerPiece();
                target.PlacePlayerPiece(newPiece);
            }
        }
        else
        {
            // Dragging from the inventory.
            if (!inventory.Take(newPiece))
                return;

            if (targetOccupied)
            {
                // Return the replaced piece to inventory.
                inventory.Return(oldPiece);
            }

            target.PlacePlayerPiece(newPiece);
        }

        SetStatus("CIRCUIT UPDATED");

        // Automatically recalculate the beam.
        rayController.RefreshBeam();
    }


    public void TryReturnToInventory(CircuitCell source)
    {
        if (IsBusy || IsSolved ||
            source == null || !source.HasPlacedPiece)
        {
            return;
        }

        inventory.Return(source.PlacedPiece);
        source.ClearPlayerPiece();

        SetStatus("PIECE RETURNED");

        // Automatically recalculate the laser.
        rayController.RefreshBeam();
    }

    public void SimulationFinished(bool success)
    {
        if (!success || IsSolved)
            return;

        IsSolved = true;

        SetStatus("POWER RESTORED - DOOR UNLOCKED");

        onPuzzleCompleted?.Invoke();
    }

    // Optional reset method for debugging.
    // It is no longer connected to a button.
    public void ResetPuzzle()
    {
        if (IsBusy || IsSolved)
            return;

        foreach (CircuitCell cell in cells)
        {
            if (cell != null)
                cell.ClearPlayerPiece();
        }

        inventory.ResetQuantities();

        SetStatus("ROUTE POWER THROUGH ALL RECEIVERS");

        rayController.RefreshBeam();
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private IEnumerator InitializeBeam()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(circuitGrid);

        rayController.RefreshBeam();
    }

}
