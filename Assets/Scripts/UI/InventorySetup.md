# Inventory setup

## Player and items

1. Add `PlayerInventory` to the same player object as `PlayerInput`. Disable/remove the old `PlayerWeaponSwitcher` component and old player weapon input scripts so they do not also switch or fire weapons.
2. Create an empty `EquipmentHolder` under the camera or camera rig. Assign it to **Equipment Holder**. Equipped prefabs retain their prefab-local position, rotation, and scale relative to this holder; adjust the holder/prefab to position the weapon in view.
3. Fill the inventory's eight **Slots** with `Item` assets for starting items. Empty entries are allowed. Entries 0–3 are hotbar positions 1–4; entries 4–7 are storage. Each entry holds one item, with no stacking.
4. Assign each Item's icon, name, and prefab. Put `Nailgun` (or another MonoBehaviour implementing `IUsable`) on the equipped prefab or an active child. Configure the nail prefab, muzzle, and trail on the nailgun. Tag the gameplay camera `MainCamera`.
5. Keep `WatchControl` on the animated watch under the player. It automatically finds `PlayerInventory` on a parent. Its existing `EnableWatch` and `DisableWatch` animation events now open/close the inventory and continue to own movement, look input, and cursor control. Do not disable WatchControl while using the inventory. The old Pause While Open list is no longer needed.

## Canvas

Use a Canvas with a Canvas Scaler and Graphic Raycaster. For inventory on the watch screen, use World Space, place/scale the Canvas on the watch, and assign the gameplay camera as its Event Camera. Screen Space - Overlay is also supported. Use this hierarchy:

```text
WatchCanvas (WatchTabs, CanvasGroup)
  TabBar
    InventoryButton (Button)
      SelectedHighlight (optional Image)
    MapButton (Button)
      SelectedHighlight (optional Image)
  InventoryPanel (InventoryUI)
    Hotbar (Horizontal Layout Group)
      Slot1 (Image, InventorySlotUI)
      Slot2 (Image, InventorySlotUI)
      Slot3 (Image, InventorySlotUI)
      Slot4 (Image, InventorySlotUI)
    StoragePanel (Horizontal Layout Group)
      Slot5 (Image, InventorySlotUI)
      Slot6 (Image, InventorySlotUI)
      Slot7 (Image, InventorySlotUI)
      Slot8 (Image, InventorySlotUI)
  MapPanel
    ExistingMinimapDisplay
```

- Keep WatchCanvas and TabBar active. WatchTabs controls the two page panels; put InventoryUI on InventoryPanel so leaving that page cancels dragging. Assign **Storage Panel** to StoragePanel; do not assign InventoryPanel, WatchCanvas, or Hotbar to that field.
- Each slot needs only an Image and InventorySlotUI on the same GameObject. The script automatically uses that Image, reads its sprite from the Item asset, and tints it green when selected or white otherwise. Empty slots remain visible and accept drops. A Layout Element with preferred width/height of 80 works well with the layout groups.
- Remove old child Icon, SelectionHighlight, item-name, and hotbar-number objects if you created them. No child graphics, text fields, or Image assignment are needed. Item names, icons, and prefabs stay on the Item ScriptableObject; slots do not display names for now.
- Assign **Slot Views** on InventoryUI to exactly eight slot components in order, 1 through 8. No duplicate entries.
- The drag preview is automatically copied from the dragged slot's Icon Image, preserving its appearance and size. No separate DragIcon object or assignment is needed; delete any manually created DragIcon from the previous setup. The preview does not block drops and is removed when dragging ends.
- Assign InventoryPanel's InventoryUI component to **Inventory UI** on PlayerInventory, even if the panel starts inactive.
- Ensure there is one EventSystem with **InputSystemUIInputModule**, with its UI actions assigned (the module's default actions are sufficient). Remove any legacy StandaloneInputModule. Avoid full-screen decorative Images with Raycast Target enabled: they can block both UI drops and gameplay clicks.

## Controls and API

### Watch tabs

1. Add `WatchTabs` to WatchCanvas. A CanvasGroup is added automatically. Assign the existing `WatchControl` to **Watch Control** (it also auto-finds it on a parent).
2. Assign **Inventory Panel** and **Map Panel** to the sibling page objects above. Do not assign WatchCanvas or TabBar: both tabs must remain available when pages change.
3. Assign **Inventory Button** and **Map Button**. The script connects their click handlers automatically; no Inspector On Click entries are needed. Optional highlight objects show the selected tab; disable Raycast Target on these decorative Images.
4. Choose **Starting Tab**. The selected tab is remembered when lowering/reopening the watch and when pausing/resuming.
5. Put your existing minimap display under MapPanel. For a RenderTexture minimap, move only its RawImage display here and keep its camera/render setup. For a UI-based minimap, place its existing display hierarchy here. Keep `MinimapLevel` on a parent of both floor displays or another active object, not on a floor display that it disables. It continues switching floors as before; no replacement minimap is needed.
6. Switching away from InventoryPanel automatically cancels dragging and removes the preview. The hotbar in this example is part of the watch's inventory page; number keys and scrolling still work during gameplay regardless of which tab was last viewed.

Tab buttons work only while the watch is open and gameplay is not paused. Pause remains a separate menu; neither pausing nor changing pages lowers the watch. Hidden inventory pages cannot accept clicks or drops. To cancel a drag and change page, release outside the slots, then click the other tab.

- **1–4:** select the corresponding hotbar slot, including empty slots.
- **Mouse wheel:** cycle through four hotbar slots, wrapping at the ends.
- **Left click during gameplay:** call the equipped component's `IUsable.Use()` once. Items without IUsable do nothing. Holding the button does not repeatedly use it.
- **Watch/Toggle (currently Tab):** raise/lower the watch. Inventory opens/closes at the existing watch animation events. PlayerInventory no longer listens to Tab separately. Menus and paused gameplay block opening.
- **Escape:** opens the separate pause menu, including while the watch is open. Resuming preserves the watch and inventory state, including movement/look restrictions and the free cursor. Inventory clicks and dragging are blocked while paused; an active drag is cancelled. Use the watch toggle to lower the watch.
- **Drag with left mouse while open:** drop on another slot to swap, or move into an empty slot. Dropping outside the slots cancels. Click a hotbar slot while open to select it.
- `TryAddItem(item)` inserts into the first empty slot and returns false when full. Pickups should only disappear if it returns true.
- `RemoveItem(index)` returns the removed Item, or null for an empty/invalid slot. It does not spawn a world drop.
- `GetItem(index)`, `SelectSlot(index)`, `SwapSlots(a, b)`, and `Changed` support future inventory integrations. API indices are zero-based.

Only the selected hotbar prefab is active. Instances are created on first equip and cached per slot; swapping slots carries their instance/state with them. Moving the equipped item into storage unequips it and equips the new contents of the selected hotbar slot. Inventory contents are runtime-only; saving and stacking are not implemented.

## Play Mode checks

1. Start with a nailgun in slot 1, another item in slot 2, and empty slots elsewhere. Verify only the selected item is visible; numbers/wheel select and wrap correctly.
2. Click with the nailgun equipped; verify damage/trail and cooldown. Select an empty slot or a prefab without IUsable; clicking should do nothing.
3. Raise the watch with Tab. Verify inventory opens at the EnableWatch animation event, the cursor is free, camera/movement stop, and UI clicks never fire the weapon.
4. Drag the selected item into storage; verify it unequips. Swap occupied slots, move to an empty slot, and drop outside the UI to cancel. Verify icons come from the Item assets and only the selected slot is green, including when empty.
5. With the watch open, press Escape to pause. Verify the watch stays open and inventory dragging/clicking are blocked. Resume and verify the cursor remains free, movement/look stay disabled, and inventory interaction resumes. Lower the watch with Tab and verify normal gameplay controls return. Also test pausing during watch opening/closing animations and during a drag.
6. Click Map and Inventory and verify only the selected page is visible. Verify the minimap still displays the correct floor. Close/reopen the watch and pause/resume from each tab; the selected page should remain unchanged and tab buttons must not respond while paused.
