/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/New Item")]
public class Item : ScriptableObject
{
    public const int MaxControls = 5;

    [SerializeField] private string itemName;
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField] private GameObject prefab;
    [SerializeField] private GameObject worldPickupPrefab;
    [SerializeField] private bool stackable = false;
    [Min(1)]
    [SerializeField] private int maxStackSize = 1;

    [Header("Controls")]
    [Tooltip("Up to five control hints, displayed in order while this item is equipped.")]
    [SerializeField] private string[] controls = new string[0];

    public string ItemName => itemName;
    public Sprite InventoryIcon => inventoryIcon;
    public GameObject Prefab => prefab;
    public GameObject WorldPickupPrefab => worldPickupPrefab;
    public IReadOnlyList<string> Controls => controls ?? System.Array.Empty<string>();


    public bool Stackable => stackable;
    public int MaxStackSize => stackable ? maxStackSize : 1;

    private void OnValidate()
    {
        if (controls != null && controls.Length > MaxControls)
            System.Array.Resize(ref controls, MaxControls);
    }
}
