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
    
    public string ItemName => itemName;
    public Sprite InventoryIcon => inventoryIcon;
    public GameObject Prefab => prefab;
}
