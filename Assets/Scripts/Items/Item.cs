/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/New Item")]
public class Item : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool stackable = false;
    [Min(1)]
    [SerializeField] private int maxStackSize = 1;

    public string ItemName => itemName;
    public Sprite InventoryIcon => inventoryIcon;
    public GameObject Prefab => prefab;

    public bool Stackable => stackable;
    public int MaxStackSize => stackable ? maxStackSize : 1;
}
