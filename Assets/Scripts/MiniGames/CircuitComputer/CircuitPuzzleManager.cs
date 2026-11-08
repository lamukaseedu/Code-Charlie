using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private PieceSprite[] pieceSprites;

    [Header("Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button resetButton;
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
        // Automatically find all circuit cells under the grid.
        if (circuitGrid != null)
            cells = circuitGrid.GetComponentsInChildren<CircuitCell>(true);

        if (inventory == null || rayController == null ||
            dragLayer == null || circuitGrid == null ||
            cells == null || cells.Length == 0)
        {
            Debug.LogError(
                "CircuitPuzzleManager: assign Inventory, Ray Controller, DragLayer and CircuitGrid.",
                this);

            enabled = false;
            return;
        }

        if (cells.Length % columns != 0)
        {
            Debug.LogWarning(
                "Circuit grid cell count is not divisible by Columns.",
                this);
        }

        foreach (CircuitCell cell in cells)
        {
            if (cell != null)
                cell.Initialize(this);
        }

        inventory.Initialize(this);
        rayController.Initialize(this);

        if (startButton != null)
            startButton.onClick.AddListener(StartCircuit);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetPuzzle);

        SetStatus("DRAG PIECES TO ROUTE POWER");
    }

    public CircuitCell GetCell(int index) =>
        index >= 0 && index < CellCount ? cells[index] : null;

    public int IndexOf(CircuitCell cell) =>
        Array.IndexOf(cells, cell);

    public Sprite GetSprite(CircuitPieceType type)
    {
        foreach (PieceSprite item in pieceSprites)
        {
            if (item.type == type)
                return item.sprite;
        }

        return null;
    }

    public bool CanDropOnCell(CircuitDragHandler source, CircuitCell target) =>
        !IsBusy && !IsSolved && source != null && target != null &&
        target.CanReceivePiece && source.SourceCell != target &&
        (source.SourceCell != null && source.SourceCell.HasPlacedPiece ||
         source.SourceCell == null && inventory.HasAvailable(source.PieceType));

    public void TryDropOnCell(CircuitDragHandler source, CircuitCell target)
    {
        if (!CanDropOnCell(source, target))
            return;

        if (source.SourceCell != null)
        {
            source.SourceCell.ClearPlayerPiece();
        }
        else if (!inventory.Take(source.PieceType))
        {
            return;
        }

        target.PlacePlayerPiece(source.PieceType);

        rayController.ClearVisuals();
        SetStatus("READY TO TEST");
    }

    public void TryReturnToInventory(CircuitCell source)
    {
        if (IsBusy || IsSolved || source == null || !source.HasPlacedPiece)
            return;

        inventory.Return(source.PlacedPiece);
        source.ClearPlayerPiece();

        rayController.ClearVisuals();
        SetStatus("PIECE RETURNED");
    }

    public void StartCircuit()
    {
        if (IsBusy || IsSolved)
            return;

        IsBusy = true;

        if (startButton != null)
            startButton.interactable = false;

        if (resetButton != null)
            resetButton.interactable = false;

        SetStatus("POWER FLOW IN PROGRESS...");
        rayController.RunCircuit();
    }

    public void SimulationFinished(bool success)
    {
        IsBusy = false;

        if (resetButton != null)
            resetButton.interactable = true;

        if (success)
        {
            IsSolved = true;
            SetStatus("POWER RESTORED - DOOR UNLOCKED");
            onPuzzleCompleted?.Invoke();
        }
        else
        {
            SetStatus("CIRCUIT INCOMPLETE - TRY AGAIN");

            if (startButton != null)
                startButton.interactable = true;
        }
    }

    public void ResetPuzzle()
    {
        if (IsBusy || IsSolved)
            return;

        rayController.ClearVisuals();

        foreach (CircuitCell cell in cells)
        {
            if (cell != null)
                cell.ClearPlayerPiece();
        }

        inventory.ResetQuantities();
        SetStatus("CIRCUIT RESET");

        if (startButton != null)
            startButton.interactable = true;
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartCircuit);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetPuzzle);
    }
}
