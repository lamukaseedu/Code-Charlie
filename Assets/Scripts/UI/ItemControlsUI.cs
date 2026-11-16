/*
 * Author: Lam Nguyen
 * Created: 11/15/2026
 */

using TMPro;
using UnityEngine;

public class ItemControlsUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [Tooltip("Assign the five child text objects in display order. Unused entries are cleared.")]
    [SerializeField] private TMP_Text[] controlTexts = new TMP_Text[Item.MaxControls];

    private void OnValidate()
    {
        System.Array.Resize(ref controlTexts, Item.MaxControls);
    }

    private void OnEnable()
    {
        RefreshControls();
    }

    private void LateUpdate()
    {
        // Also handles the inventory or equipped object being disabled without a slot change.
        RefreshControls();
    }

    private void RefreshControls()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        PlayerMinigameContext minigameContext = PlayerMinigameContext.Instance;
        bool hideControls = WorkbenchUI.AnyOpen || (minigameContext != null && minigameContext.IsMinigameActive);
        GameObject equipped = !hideControls && inventory != null && inventory.isActiveAndEnabled
            ? inventory.EquippedObject : null;
        Item item = equipped != null && equipped.activeInHierarchy && inventory.GetQuantity(inventory.SelectedSlot) > 0
            ? inventory.GetItem(inventory.SelectedSlot) : null;

        for (int i = 0; controlTexts != null && i < controlTexts.Length; i++)
        {
            TMP_Text text = controlTexts[i];
            if (text == null) continue;
            string hint = item != null && i < Item.MaxControls && i < item.Controls.Count
                ? item.Controls[i] ?? string.Empty : string.Empty;
            if (text.text != hint) text.text = hint;
        }
    }

    private void OnDisable()
    {
        if (controlTexts == null) return;
        foreach (TMP_Text text in controlTexts)
        {
            if (text != null) text.text = string.Empty;
        }
    }
}
