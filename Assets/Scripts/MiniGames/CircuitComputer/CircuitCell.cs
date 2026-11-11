using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class CircuitCell : MonoBehaviour, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 11/7/2026
     */

    [Header("Cell Configuration")]
    [SerializeField] private CircuitCellKind cellKind = CircuitCellKind.Empty;
    [SerializeField] private CircuitPieceType fixedPiece;
    [SerializeField] private bool requiredReceiver;

    [FormerlySerializedAs("startDirection")]
    [SerializeField] private CircuitDirection connectionDirection = CircuitDirection.Right;

    [Header("Visuals")]
    [SerializeField] private Image cellImage;

    [Header("Cell Colors")]
    [SerializeField] private Color normalPieceColor = Color.white;
    [SerializeField] private Color poweredPieceColor = new Color(0.5f, 1f, 0.7f);
    [SerializeField] private Color idleReceiverColor = new Color(1f, 0.75f, 0.25f);
    [SerializeField] private Color poweredReceiverColor = Color.cyan;
    [SerializeField] private Color validDropColor = new Color(0.25f, 1f, 0.35f, 0.3f);
    [SerializeField] private Color invalidDropColor = new Color(1f, 0.2f, 0.2f, 0.3f);

    [Header("Debug Visuals")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private bool showDebugText = true;

    private CircuitPuzzleManager puzzle;
    private CircuitDragHandler dragHandler;
    private Image backgroundImage;
    private Color originalBackgroundColor;

    private bool hasPlacedPiece;
    private CircuitPieceType placedPiece;
    private bool powered;

    public CircuitCellKind Kind => cellKind;

    // START uses this as its outgoing direction.
    // END uses this as the side its connector faces.
    public CircuitDirection ConnectionDirection => connectionDirection;

    // Preserve compatibility with the existing ray controller.
    public CircuitDirection StartDirection => connectionDirection;

    public bool RequiredReceiver =>
        cellKind == CircuitCellKind.FixedPiece && requiredReceiver;

    public bool HasPlacedPiece => hasPlacedPiece;
    public bool IsEmpty => cellKind == CircuitCellKind.Empty && !hasPlacedPiece;
    public bool CanReceivePiece => IsEmpty;
    public CircuitPieceType PlacedPiece => placedPiece;
    public RectTransform RectTransform => (RectTransform)transform;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        originalBackgroundColor = backgroundImage.color;

        backgroundImage.raycastTarget = true;
    }

    public void Initialize(CircuitPuzzleManager manager)
    {
        puzzle = manager;

        dragHandler = GetComponent<CircuitDragHandler>();

        if (dragHandler != null)
            dragHandler.ConfigureForCell(puzzle, this);

        hasPlacedPiece = false;
        powered = false;

        RefreshVisuals();
    }

    public bool TryGetPiece(out CircuitPieceType piece)
    {
        piece = fixedPiece;

        if (cellKind == CircuitCellKind.FixedPiece)
            return true;

        if (cellKind == CircuitCellKind.Empty && hasPlacedPiece)
        {
            piece = placedPiece;
            return true;
        }

        return false;
    }

    public void PlacePlayerPiece(CircuitPieceType piece)
    {
        if (!CanReceivePiece)
            return;

        placedPiece = piece;
        hasPlacedPiece = true;
        powered = false;

        RefreshVisuals();
    }

    public void ClearPlayerPiece()
    {
        hasPlacedPiece = false;
        powered = false;

        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        Sprite sprite = null;

        switch (cellKind)
        {
            case CircuitCellKind.Obstacle:
            case CircuitCellKind.Start:
            case CircuitCellKind.End:
                if (puzzle != null)
                {
                    sprite = puzzle.GetSpecialSprite(
                        cellKind,
                        connectionDirection
                    );
                }
                break;

            case CircuitCellKind.FixedPiece:
                if (puzzle != null)
                    sprite = puzzle.GetSprite(fixedPiece);
                break;

            case CircuitCellKind.Empty:
                if (hasPlacedPiece && puzzle != null)
                    sprite = puzzle.GetSprite(placedPiece);
                break;
        }

        if (cellImage != null)
        {
            cellImage.sprite = sprite;

            cellImage.gameObject.SetActive(
                !showDebugText && sprite != null
            );

            cellImage.raycastTarget = false;
        }

        UpdateDebugText();
    }

    public void SetPowered(bool value)
    {
        powered = value;
        UpdatePowerColor();
    }

    private void UpdatePowerColor()
    {
        Color color;

        if (RequiredReceiver)
        {
            color = powered
                ? poweredReceiverColor
                : idleReceiverColor;
        }
        else
        {
            color = powered
                ? poweredPieceColor
                : normalPieceColor;
        }

        if (cellImage != null)
            cellImage.color = color;

        if (debugText != null)
            debugText.color = color;
    }

    private void UpdateDebugText()
    {
        if (debugText == null)
        {
            UpdatePowerColor();
            return;
        }

        debugText.gameObject.SetActive(showDebugText);

        if (!showDebugText)
        {
            UpdatePowerColor();
            return;
        }

        switch (cellKind)
        {
            case CircuitCellKind.Empty:
                debugText.text = hasPlacedPiece
                    ? GetPieceSymbol(placedPiece)
                    : "";
                break;

            case CircuitCellKind.Obstacle:
                debugText.text = "X";
                break;

            case CircuitCellKind.Start:
                debugText.text = "S";
                break;

            case CircuitCellKind.End:
                debugText.text = "E";
                break;

            case CircuitCellKind.FixedPiece:
                string symbol = GetPieceSymbol(fixedPiece);

                debugText.text = requiredReceiver
                    ? "R" + symbol
                    : symbol;
                break;
        }

        UpdatePowerColor();
    }

    private string GetPieceSymbol(CircuitPieceType piece)
    {
        switch (piece)
        {
            case CircuitPieceType.UpRight:
                return "UpRight";

            case CircuitPieceType.RightDown:
                return "RightDown";

            case CircuitPieceType.DownLeft:
                return "DownLeft";

            case CircuitPieceType.LeftUp:
                return "LeftUp";

            case CircuitPieceType.Horizontal:
                return "Horizontal";

            case CircuitPieceType.Vertical:
                return "Vertical";

            default:
                return "?";
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (puzzle != null && CircuitDragHandler.ActiveDrag != null)
        {
            puzzle.TryDropOnCell(
                CircuitDragHandler.ActiveDrag,
                this
            );
        }

        ShowDropHighlight(false, false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (puzzle != null && CircuitDragHandler.ActiveDrag != null)
        {
            bool valid = puzzle.CanDropOnCell(
                CircuitDragHandler.ActiveDrag,
                this
            );

            ShowDropHighlight(true, valid);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowDropHighlight(false, false);
    }

    private void ShowDropHighlight(bool visible, bool valid)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = visible
            ? (valid ? validDropColor : invalidDropColor)
            : originalBackgroundColor;
    }
}
