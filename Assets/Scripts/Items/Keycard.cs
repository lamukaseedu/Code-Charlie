/*
 * Author: Lam Nguyen
 * Created: 10/25/2026
 */

using UnityEngine;

public class Keycard : MonoBehaviour, IUsable, IInventoryItem
{
    [Min(0)]
    [SerializeField] private int id = 0;
    [Min(0f)]
    [SerializeField] private float useDistance = 3f;
    [Tooltip("Include locks and any geometry that should block use, such as walls.")]
    [SerializeField] private LayerMask useLayers = Physics.DefaultRaycastLayers;

    private PlayerInventory inventory;
    private Camera playerCamera;

    public int Id => id;
    public bool IsEquipped => isActiveAndEnabled && inventory != null &&
        inventory.isActiveAndEnabled && inventory.EquippedObject != null &&
        transform.IsChildOf(inventory.EquippedObject.transform);

    public void Initialize(PlayerInventory playerInventory)
    {
        inventory = playerInventory;
    }

    private void OnEnable()
    {
        if (inventory == null)
            inventory = GetComponentInParent<PlayerInventory>();
    }

    public void Use()
    {
        if (!IsEquipped || inventory.IsOpen || Time.timeScale == 0f ||
            Cursor.lockState != CursorLockMode.Locked)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, useDistance, useLayers, QueryTriggerInteraction.Collide))
        {
            DoorLock doorLock = hit.collider.GetComponentInParent<DoorLock>();
            if (doorLock != null)
                doorLock.TryUnlock(this);
        }
    }
}
