using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CircuitDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/8/2026
     */
    [SerializeField] private Image sourcePieceImage;
    [SerializeField] private float dragVisualSize = 44f;

    public static CircuitDragHandler ActiveDrag { get; private set; }
    public CircuitCell SourceCell { get; private set; }
    public CircuitPieceType PieceType { get; private set; }
    public bool IsInventorySource => SourceCell == null;

    private CircuitPuzzleManager puzzle;
    private RectTransform ghost;

    public void ConfigureForInventory(CircuitPuzzleManager manager, CircuitPieceType piece)
    {
        puzzle = manager;
        SourceCell = null;
        PieceType = piece;
    }

    public void ConfigureForCell(CircuitPuzzleManager manager, CircuitCell cell)
    {
        puzzle = manager;
        SourceCell = cell;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || puzzle == null || puzzle.IsBusy)
            return;
        if (SourceCell != null)
        {
            if (!SourceCell.HasPlacedPiece) return;
            PieceType = SourceCell.PlacedPiece;
        }
        else if (!puzzle.Inventory.HasAvailable(PieceType)) return;

        if (ActiveDrag != null) return;
        ActiveDrag = this;

        GameObject graphic = new GameObject("CircuitDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ghost = graphic.GetComponent<RectTransform>();
        ghost.SetParent(puzzle.DragLayer, false);
        ghost.sizeDelta = new Vector2(dragVisualSize, dragVisualSize);
        Image image = graphic.GetComponent<Image>();
        image.sprite = puzzle.GetSprite(PieceType);
        image.color = new Color(1f, 1f, 1f, 0.9f);
        image.preserveAspect = true;
        image.raycastTarget = false;
        MoveGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ActiveDrag == this) MoveGhost(eventData);
    }

    private void MoveGhost(PointerEventData eventData)
    {
        if (ghost == null || puzzle == null || puzzle.DragLayer == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            puzzle.DragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local))
            ghost.anchoredPosition = local;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ActiveDrag != this) return;
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        ActiveDrag = null;
    }

    private void OnDisable()
    {
        if (ActiveDrag != this) return;
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        ActiveDrag = null;
    }
}
