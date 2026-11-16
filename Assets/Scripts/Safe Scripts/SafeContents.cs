using System;
using System.Security.Cryptography;
using UnityEngine;

public class SafeContents : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/27/2026
     */

    [Serializable]
    private class SafeSlot
    {
        [Tooltip("Placement point inside the safe.")]
        public Transform placementPoint;

        [Tooltip("Pickup prefab with ItemHover on its root. Leave empty for no item.")]
        public ItemHover pickupPrefab;

        [Min(1)]
        public int quantity = 1;
    }

    [Header("Contents Slots")]
    [SerializeField] private SafeSlot[] slots = new SafeSlot[6];

    private bool hasSpawned;

    private void Start()
    {
        SpawnContents();
    }

    public void SpawnContents()
    {
        // Opening the safe again must not duplicate its contents.
        if (hasSpawned)
            return;

        hasSpawned = true;

        foreach (SafeSlot slot in slots)
        {
            // An unassigned prefab means this slot is intentionally empty.
            if (slot == null || slot.pickupPrefab == null)
                continue;

            if (slot.placementPoint == null)
            {
                Debug.LogWarning(
                    $"{name}: A safe pickup has no placement point assigned.",
                    this
                );

                continue;
            }

            ItemHover pickup = Instantiate(
                slot.pickupPrefab,
                slot.placementPoint.position,
                transform.rotation * slot.pickupPrefab.transform.localRotation,
                slot.placementPoint
            );

            pickup.SetQuantity(slot.quantity);
        }
    }
}