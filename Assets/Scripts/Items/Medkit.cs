using UnityEngine;

public class Medkit : MonoBehaviour, IUsable, IInventoryItem
{
    [Tooltip("Health restored when one medkit is consumed. Healing is capped at the player's maximum health.")]
    [SerializeField, Min(0f)] private float healAmount = 100f;

    private PlayerInventory inventory;
    private Health playerHealth;
    private bool isUsing;

    public void Initialize(PlayerInventory owner)
    {
        inventory = owner;
        playerHealth = owner != null ? owner.GetComponent<Health>() : null;
    }

    public void Use()
    {
        if (isUsing || !isActiveAndEnabled || inventory == null || !inventory.isActiveAndEnabled ||
            inventory.IsOpen || inventory.IsEquipCooldownActive || Time.timeScale == 0f ||
            Cursor.lockState != CursorLockMode.Locked)
            return;

        GameObject equipped = inventory.EquippedObject;
        if (equipped == null || !transform.IsChildOf(equipped.transform) || playerHealth == null ||
            playerHealth.IsDead || playerHealth.CurrentHealth >= playerHealth.MaxHealth || healAmount <= 0f)
            return;

        // Inventory changes and health events can invoke other game logic synchronously.
        isUsing = true;
        try
        {
            if (inventory.TryConsumeEquippedItem())
                playerHealth.Heal(healAmount);
        }
        finally
        {
            isUsing = false;
        }
    }
}
