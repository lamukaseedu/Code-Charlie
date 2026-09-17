/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 8;
    public const int HotbarSlotCount = 4;

    [SerializeField] private Item[] slots = new Item[SlotCount];
    [SerializeField] private Transform equipmentHolder;
    [SerializeField] private InventoryUI inventoryUI;

    private readonly GameObject[] instances = new GameObject[SlotCount];
    private IUsable equippedUse;

    private InputAction useAction;
    private InputAction equipAction;
    private InputAction scrollAction;
    public event Action Changed;
    public int SelectedSlot { get; private set; }
    public bool IsOpen { get; private set; }
    public GameObject EquippedObject => instances[SelectedSlot];

    private void OnValidate()
    {
        Array.Resize(ref slots, SlotCount);
    }

    private void Awake()
    {
        Array.Resize(ref slots, SlotCount);
        PlayerInput playerInput = GetComponent<PlayerInput>();
        useAction = playerInput.actions["Use"];
        equipAction = playerInput.actions["Equip"];
        scrollAction = playerInput.actions["Scroll"];
    }

    private void Start()
    {
        if (inventoryUI != null)
            inventoryUI.Initialize(this);
        RefreshEquipment();
        Changed?.Invoke();
    }

    private void OnEnable()
    {
        if (instances[SelectedSlot] != null)
            instances[SelectedSlot].SetActive(true);
    }

    private void Update()
    {
        if (IsOpen)
            return;

        // Respect pause menus that stop time or release the gameplay cursor.
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
            return;

        if (equipAction.WasPerformedThisFrame())
        {
            int slot = Mathf.RoundToInt(equipAction.ReadValue<float>());
            if (slot >= 1 && slot <= HotbarSlotCount)
                SelectSlot(slot - 1);
        }

        float scroll = scrollAction.ReadValue<float>();
        if (scroll != 0f)
            SelectSlot(FindOccupiedHotbarSlot(scroll > 0f ? -1 : 1));

        if (useAction.WasPressedThisFrame())
            UseEquippedItem();
    }

    public Item GetItem(int index) => IsValidSlot(index) ? slots[index] : null;

    public bool AddItem(Item item)
    {
        if (item == null)
            return false;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null)
                continue;
            slots[i] = item;
            if (i == SelectedSlot || slots[SelectedSlot] == null) RefreshEquipment();
            Changed?.Invoke();
            return true;
        }
        return false;
    }

    public Item RemoveItem(int index)
    {
        if (!IsValidSlot(index) || slots[index] == null)
            return null;

        Item removed = slots[index];
        slots[index] = null;
        if (instances[index] != null)
        {
            instances[index].SetActive(false);
            Destroy(instances[index]);
            instances[index] = null;
        }
        if (index == SelectedSlot) RefreshEquipment();
        Changed?.Invoke();
        return removed;
    }

    public void SwapSlots(int first, int second)
    {
        if (!IsValidSlot(first) || !IsValidSlot(second) || first == second)
            return;

        (slots[first], slots[second]) = (slots[second], slots[first]);
        (instances[first], instances[second]) = (instances[second], instances[first]);
        if (first == SelectedSlot || second == SelectedSlot || slots[SelectedSlot] == null)
            RefreshEquipment();
        Changed?.Invoke();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= HotbarSlotCount || index == SelectedSlot || slots[index] == null)
            return;
        SelectedSlot = index;
        RefreshEquipment();
        Changed?.Invoke();
    }

    public void UseEquippedItem()
    {
        if (!isActiveAndEnabled || IsOpen || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked || EquippedObject == null)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (equippedUse is Behaviour behaviour && behaviour != null && behaviour.isActiveAndEnabled)
            equippedUse.Use();
    }

    private void RefreshEquipment()
    {
        if (slots[SelectedSlot] == null)
            SelectedSlot = FindOccupiedHotbarSlot(1);

        equippedUse = null;
        for (int i = 0; i < SlotCount; i++)
        {
            if (instances[i] != null)
                instances[i].SetActive(false);
        }

        Item item = slots[SelectedSlot];
        if (item == null || item.Prefab == null || equipmentHolder == null)
            return;

        if (instances[SelectedSlot] == null)
            instances[SelectedSlot] = Instantiate(item.Prefab, equipmentHolder, false);

        GameObject equipped = instances[SelectedSlot];
        equipped.SetActive(isActiveAndEnabled);
        equippedUse = equipped.GetComponentInChildren<IUsable>();
    }

    private int FindOccupiedHotbarSlot(int direction)
    {
        for (int offset = 1; offset <= HotbarSlotCount; offset++)
        {
            int index = (SelectedSlot + direction * offset + HotbarSlotCount) % HotbarSlotCount;
            if (slots[index] != null)
                return index;
        }

        // Keep slot 1 selected when the entire hotbar is empty.
        return 0;
    }
    
    public void SetOpen(bool open)
    {
        if (open == IsOpen)
            return;

        IsOpen = open;
        if (inventoryUI != null) inventoryUI.SetOpen(open);
    }
    private void OnDisable()
    {
        SetOpen(false);
        if (instances[SelectedSlot] != null)
            instances[SelectedSlot].SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (GameObject instance in instances)
        {
            if (instance != null) Destroy(instance);
        }
    }

    private static bool IsValidSlot(int index) => index >= 0 && index < SlotCount;
}
