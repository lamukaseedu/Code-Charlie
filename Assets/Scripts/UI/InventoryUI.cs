using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Tooltip("Exactly eight slots, in order: hotbar first, then storage.")]
    [SerializeField] private InventorySlotUI[] slotViews = new InventorySlotUI[PlayerInventory.SlotCount];
    [Tooltip("Panel containing only slots 5–8. Keep the hotbar outside this panel.")]
    [SerializeField] private GameObject storagePanel;
    private Image dragIcon;

    private PlayerInventory inventory;
    private int draggedSlot = -1;

    public bool IsOpen => inventory != null && inventory.IsOpen;
    private bool CanInteract => isActiveAndEnabled && IsOpen && Time.timeScale > 0f;

    private void Update()
    {
        if (!CanInteract && draggedSlot >= 0) EndDrag();
    }

    public void Initialize(PlayerInventory owner)
    {
        if (inventory != null) inventory.Changed -= Refresh;
        inventory = owner;
        inventory.Changed += Refresh;
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null) slotViews[i].Initialize(this, i);
        }
        SetOpen(inventory.IsOpen);
        Refresh();
    }

    public void SetOpen(bool open)
    {
        EndDrag();
        if (storagePanel != null) storagePanel.SetActive(open);
    }

    private void Refresh()
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
                slotViews[i].Display(inventory.GetItem(i), i == inventory.SelectedSlot);
        }
    }

    public void SelectSlot(int index)
    {
        if (CanInteract) inventory.SelectSlot(index);
    }

    public void BeginDrag(int index, Vector2 position)
    {
        if (!CanInteract || inventory.GetItem(index) == null) return;
        EndDrag();
        draggedSlot = index;
        Image source = index < slotViews.Length && slotViews[index] != null
            ? slotViews[index].Icon : null;
        if (source == null || source.sprite == null || source.canvas == null) return;
        
        dragIcon = new GameObject("Dragged Item Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        dragIcon.rectTransform.SetParent(source.rectTransform, false);
        dragIcon.rectTransform.sizeDelta = source.rectTransform.rect.size;
        dragIcon.sprite = source.sprite;
        dragIcon.color = source.color;
        dragIcon.material = source.material;
        dragIcon.type = source.type;
        dragIcon.preserveAspect = source.preserveAspect;
        dragIcon.fillMethod = source.fillMethod;
        dragIcon.fillOrigin = source.fillOrigin;
        dragIcon.fillClockwise = source.fillClockwise;
        dragIcon.fillAmount = source.fillAmount;
        dragIcon.rectTransform.SetParent(source.canvas.rootCanvas.transform, true);
        dragIcon.rectTransform.SetAsLastSibling();
        dragIcon.raycastTarget = false;
        dragIcon.gameObject.SetActive(true);
        MoveDrag(position);
    }

    public void MoveDrag(Vector2 position)
    {
        if (!CanInteract || draggedSlot < 0 || dragIcon == null) return;
        Canvas canvas = dragIcon.canvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && camera == null)
        {
            camera = Camera.main;
            if (camera == null) return;
        }
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragIcon.rectTransform.parent as RectTransform, position, camera, out Vector3 world))
            dragIcon.rectTransform.position = world;
    }

    public void DropOn(int index)
    {
        if (CanInteract && draggedSlot >= 0) inventory.SwapSlots(draggedSlot, index);
        EndDrag();
    }

    public void EndDrag()
    {
        draggedSlot = -1;
        if (dragIcon != null)
        {
            dragIcon.gameObject.SetActive(false);
            Destroy(dragIcon.gameObject);
            dragIcon = null;
        }
    }

    private void OnDisable() => EndDrag();

    private void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= Refresh;
    }
}
