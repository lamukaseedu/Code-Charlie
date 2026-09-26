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
    [SerializeField] private int[] quantities = new int[SlotCount];
    [SerializeField] private Transform equipmentHolder;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private Transform dropSpawnPoint;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private InputActionReference dropAction;

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
        Array.Resize(ref quantities, SlotCount);
    }

    private void Awake()
    {
        Array.Resize(ref slots, SlotCount);
        Array.Resize(ref quantities, SlotCount);
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

        if (dropAction != null && dropAction.action.WasPressedThisFrame())
            DropEquippedItem();
    }

    public Item GetItem(int index) => IsValidSlot(index) ? slots[index] : null;
    public int GetQuantity(int index) => IsValidSlot(index) ? quantities[index] : 0;

    public int AddItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return 0;

        int remaining = amount;

        // First fill existing stacks.
        if (item.Stackable)
        {
            for (int i = 0; i < SlotCount && remaining > 0; i++)
            {
                if (slots[i] != item)
                    continue;

                if (quantities[i] >= item.MaxStackSize)
                    continue;

                int availableSpace =
                    item.MaxStackSize - quantities[i];

                int amountToAdd =
                    Mathf.Min(availableSpace, remaining);

                quantities[i] += amountToAdd;
                remaining -= amountToAdd;
            }
        }

        // Then create new stacks in empty slots.
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (slots[i] != null)
                continue;

            int amountToAdd = item.Stackable
                ? Mathf.Min(item.MaxStackSize, remaining)
                : 1;

            slots[i] = item;
            quantities[i] = amountToAdd;

            remaining -= amountToAdd;

            if (i == SelectedSlot || slots[SelectedSlot] == null)
                RefreshEquipment();
        }

        
        int amountAdded = amount - remaining;

        if (amountAdded > 0)
            Changed?.Invoke();

        return amountAdded;
    }

    //THIS FUNCTION IS FOR WHETHER WE WANT THE PICKUP SYSTEM TO PICK UP THE ENTIRE AMMO AMOUNT OR NONE OF IT
    /*
    public bool CanAddItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int availableSpace = 0;

        for (int i = 0; i < SlotCount; i++)
        {
            // Empty slot.
            if (slots[i] == null)
            {
                availableSpace += item.MaxStackSize;
            }
            // Existing stack of this same item.
            else if (item.Stackable && slots[i] == item)
            {
                availableSpace +=
                    item.MaxStackSize - quantities[i];
            }

            if (availableSpace >= amount)
                return true;
        }

        return false;
    }
    */
    public int GetItemCount(Item item)
    {
        if (item == null)
            return 0;

        int total = 0;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == item)
                total += quantities[i];
        }

        return total;
    }

    public Item RemoveItem(int index)
    {
        if (!IsValidSlot(index) || slots[index] == null)
            return null;

        Item removed = slots[index];
        slots[index] = null;
        quantities[index] = 0;
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

    // Called by the hotbar drop input while the player is holding an item.
    public bool DropEquippedItem()
    {
        if (!isActiveAndEnabled || IsOpen || Time.timeScale == 0f ||
            Cursor.lockState != CursorLockMode.Locked || EquippedObject == null)
            return false;

        return DropSlot(SelectedSlot);
    }

    // Connect each inventory slot's Drop button to this method with that slot's index.
    public bool DropSlot(int index)
    {
        if (!isActiveAndEnabled || Time.timeScale == 0f || !IsValidSlot(index))
            return false;

        Item item = slots[index];
        int amount = quantities[index];
        if (item == null || amount <= 0 || item.WorldPickupPrefab == null || dropSpawnPoint == null)
            return false;

        GameObject pickup = Instantiate(item.WorldPickupPrefab,
            dropSpawnPoint.position, dropSpawnPoint.rotation);
        if (pickup == null)
            return false;

        ItemHover itemHover = pickup.GetComponentInChildren<ItemHover>();
        if (itemHover == null)
        {
            Debug.LogError($"World pickup prefab for {item.ItemName} needs ItemHover.", item);
            Destroy(pickup);
            return false;
        }

        itemHover.InitializeDrop(item, amount, this, playerInteraction);
        RemoveItem(index);
        return true;
    }


    public bool TryConsumeItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (GetItemCount(item) < amount)
            return false;

        int remaining = amount;

        // Work backwards so smaller/newer stacks
        // tend to disappear first.
        for (int i = SlotCount - 1; i >= 0; i--)
        {
            if (slots[i] != item)
                continue;

            int amountToRemove =
                Mathf.Min(quantities[i], remaining);

            quantities[i] -= amountToRemove;
            remaining -= amountToRemove;

            if (quantities[i] <= 0)
            {
                quantities[i] = 0;
                slots[i] = null;

                if (instances[i] != null)
                {
                    instances[i].SetActive(false);
                    Destroy(instances[i]);
                    instances[i] = null;
                }
            }

            if (remaining <= 0)
                break;
        }

        RefreshEquipment();
        Changed?.Invoke();

        return true;
    }

    public void SwapSlots(int first, int second)
    {
        if (!IsValidSlot(first) || !IsValidSlot(second) || first == second)
            return;

        (slots[first], slots[second]) = (slots[second], slots[first]);
        (quantities[first], quantities[second]) = (quantities[second], quantities[first]);
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
        {
            instances[SelectedSlot] = Instantiate(item.Prefab, equipmentHolder, false);

            IInventoryItem inventoryItem = instances[SelectedSlot].GetComponentInChildren<IInventoryItem>();

            inventoryItem?.Initialize(this);

            ItemHover itemHover = instances[SelectedSlot].GetComponentInChildren<ItemHover>(true);
            itemHover?.InitializeDrop(item, quantities[SelectedSlot], this, playerInteraction);
        }
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
