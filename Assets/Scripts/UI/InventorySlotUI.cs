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
    IEndDragHandler, IDropHandler, IPointerClickHandler
{
    private Image icon;
    [SerializeField] private TMP_Text quantityText;
    private InventoryUI owner;
    private int index;
    public Image Icon => icon;

    public void Initialize(InventoryUI inventoryUI, int slotIndex)
    {
        owner = inventoryUI;
        index = slotIndex;
        icon = GetComponent<Image>();
        icon.raycastTarget = true;
    }

    public void Display(Item item, int quantity, bool selected)
    {
        if (icon != null)
        {
            icon.sprite = item != null ? item.InventoryIcon : null;
            // Empty slots stay visible and accept drops.
            icon.enabled = true;
            icon.color = selected ? Color.green : Color.white;
            icon.preserveAspect = true;
        }
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
