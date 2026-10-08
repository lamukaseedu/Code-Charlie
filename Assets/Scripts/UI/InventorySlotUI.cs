/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler,
    IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Sprite Overrides")]
    [Tooltip("Leave empty to use InventoryUI's default normal slot sprite.")]
    [SerializeField] private Sprite normalSlotSpriteOverride;
    [Tooltip("Leave empty to use InventoryUI's default hover slot sprite.")]
    [SerializeField] private Sprite hoverSlotSpriteOverride;
    [Tooltip("Leave empty to use InventoryUI's default selected slot sprite.")]
    [SerializeField] private Sprite selectedSlotSpriteOverride;

    private Image icon;
    private Image overlay;
    private Sprite normalSprite;
    private Sprite hoverSprite;
    private Sprite selectedSprite;
    private bool isHovered;
    private bool isSelected;

    [SerializeField] private TMP_Text quantityText;
    private InventoryUI owner;
    private int index;
    public Image Icon => icon;

    public void Initialize(InventoryUI inventoryUI, int slotIndex,
        Sprite normal, Sprite hover, Sprite selected)
    {
        owner = inventoryUI;
        index = slotIndex;
        normalSprite = normal;
        hoverSprite = hover;
        selectedSprite = selected;
        icon = GetComponent<Image>();
        icon.raycastTarget = true;

        if (overlay == null)
        {
            overlay = new GameObject("Slot Overlay", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            RectTransform rect = overlay.rectTransform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        overlay.raycastTarget = false;
        overlay.rectTransform.SetAsLastSibling();
        UpdateOverlay();
    }

    public void Display(Item item, int quantity, bool selected)
    {
        if (icon != null)
        {
            icon.sprite = item != null ? item.InventoryIcon : null;
            // Keep the image enabled so empty slots still accept drops.
            icon.enabled = true;
            icon.color = item != null ? Color.white : Color.clear;
            icon.preserveAspect = true;
        }
        isSelected = selected;
        UpdateOverlay();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateOverlay();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateOverlay();
    }

    private void OnDisable()
    {
        isHovered = false;
        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        if (overlay == null) return;
        Sprite sprite;
        if (isSelected)
            sprite = selectedSlotSpriteOverride != null ? selectedSlotSpriteOverride : selectedSprite;
        else if (isHovered)
            sprite = hoverSlotSpriteOverride != null ? hoverSlotSpriteOverride : hoverSprite;
        else
            sprite = normalSlotSpriteOverride != null ? normalSlotSpriteOverride : normalSprite;
        overlay.sprite = sprite;
        overlay.enabled = sprite != null;
        if (item != null && item.Stackable)
        {
            quantityText.text = "x" + quantity;
            quantityText.gameObject.SetActive(true);
        }
        else
        {
            quantityText.text = "";
            quantityText.gameObject.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            owner?.BeginDrag(index, eventData.position);
    }

    public void OnDrag(PointerEventData eventData) => owner?.MoveDrag(eventData.position);
    public void OnEndDrag(PointerEventData eventData) => owner?.EndDrag();

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<InventorySlotUI>() : null;
        if (source != null && source.owner == owner) owner?.DropOn(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
            owner?.SelectSlot(index);
    }
}
