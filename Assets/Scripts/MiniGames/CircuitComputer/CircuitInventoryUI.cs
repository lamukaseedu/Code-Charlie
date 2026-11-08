using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class CircuitInventoryUI : MonoBehaviour, IDropHandler
{
    [Serializable]
    public class InventorySlot
    {
        public CircuitPieceType pieceType;
        public int startingQuantity = 1;
        public CircuitDragHandler dragHandler;
        public Image pieceImage;
        public TMP_Text quantityText;
    }

    [SerializeField] private InventorySlot[] slots = new InventorySlot[6];
    private readonly Dictionary<CircuitPieceType, int> remaining = new Dictionary<CircuitPieceType, int>();
    private CircuitPuzzleManager puzzle;

    public void Initialize(CircuitPuzzleManager manager)
    {
       /*
        * Author: Andres Rondon-Villarmosa
        * Created: 11/8/2026
        */
        puzzle = manager;
        remaining.Clear();
        foreach (InventorySlot slot in slots)
        {
            if (slot == null) continue;
            remaining[slot.pieceType] = Mathf.Max(0, slot.startingQuantity);
            if (slot.dragHandler != null)
                slot.dragHandler.ConfigureForInventory(manager, slot.pieceType);
            if (slot.pieceImage != null)
            {
                slot.pieceImage.sprite = manager.GetSprite(slot.pieceType);
                slot.pieceImage.raycastTarget = false;
            }
        }
        Refresh();
    }

    public bool HasAvailable(CircuitPieceType piece) =>
        remaining.TryGetValue(piece, out int quantity) && quantity > 0;

    public bool Take(CircuitPieceType piece)
    {
        if (!HasAvailable(piece)) return false;
        remaining[piece]--;
        Refresh();
        return true;
    }

    public void Return(CircuitPieceType piece)
    {
        if (!remaining.ContainsKey(piece)) remaining[piece] = 0;
        remaining[piece]++;
        Refresh();
    }

    public void ResetQuantities()
    {
        remaining.Clear();
        foreach (InventorySlot slot in slots)
            if (slot != null)
                remaining[slot.pieceType] = Mathf.Max(0, slot.startingQuantity);
        Refresh();
    }

    private void Refresh()
    {
        foreach (InventorySlot slot in slots)
        {
            if (slot == null) continue;
            int value = remaining.TryGetValue(slot.pieceType, out int q) ? q : 0;
            if (slot.quantityText != null) slot.quantityText.text = "x" + value;
            if (slot.pieceImage != null)
                slot.pieceImage.color = value > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        CircuitDragHandler drag = CircuitDragHandler.ActiveDrag;
        if (drag != null && drag.SourceCell != null && puzzle != null)
            puzzle.TryReturnToInventory(drag.SourceCell);
    }
}
