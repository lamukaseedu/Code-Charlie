/*
 * Author: Lam Nguyen
 * Created: 11/15/2026
 */

using TMPro;
using UnityEngine;

public class ItemControlsUI : MonoBehaviour
{
    [System.Serializable]
    private class PersistentControl
    {
        public bool show = true;
        public TMP_Text text;
    }

    [SerializeField] private PlayerInventory inventory;

    [Tooltip("Assign the five item control text objects in display order. Unused entries are cleared.")]
    [SerializeField] private TMP_Text[] controlTexts = new TMP_Text[Item.MaxControls];

    [Tooltip("Assign separate text objects for hints that can show with or without an equipped item.")]
    [SerializeField]
    private PersistentControl[] persistentControls =
        System.Array.Empty<PersistentControl>();

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
        bool hideControls = WorkbenchUI.AnyOpen ||
            (minigameContext != null && minigameContext.IsMinigameActive);

        GameObject equipped = !hideControls && inventory != null && inventory.isActiveAndEnabled
            ? inventory.EquippedObject
            : null;

        Item item = equipped != null && equipped.activeInHierarchy &&
            inventory.GetQuantity(inventory.SelectedSlot) > 0
            ? inventory.GetItem(inventory.SelectedSlot)
            : null;

        for (int i = 0; controlTexts != null && i < controlTexts.Length; i++)
        {
            string hint = item != null && i < Item.MaxControls && i < item.Controls.Count
                ? item.Controls[i] ?? string.Empty
                : string.Empty;

            SetText(controlTexts[i], hint);
        }

        RefreshPersistentControls(!hideControls);
    }

    private void RefreshPersistentControls(bool visible)
    {
        foreach (PersistentControl control in persistentControls)
        {
            if (control?.text == null)
                continue;

            control.text.enabled = visible && control.show;
        }
    }

    private static void SetText(TMP_Text text, string hint)
    {
        if (text != null && text.text != hint)
            text.text = hint;
    }

    private void OnDisable()
    {
        if (controlTexts != null)
        {
            foreach (TMP_Text text in controlTexts)
                SetText(text, string.Empty);
        }

        RefreshPersistentControls(false);
    }
}