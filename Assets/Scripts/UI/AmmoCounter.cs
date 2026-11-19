/*
 * Author: Lam Nguyen
 * Created: 10/25/2026
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
public class AmmoCounter : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image nailIcon;

    private int lastMagazine = -1;
    private int lastCapacity = -1;
    private int lastReserve = -1;

    private void Awake()
    {
        if (ammoText == null)
            ammoText = GetComponent<TMP_Text>();

        if (nailIcon == null)
            nailIcon = GetComponentInChildren<Image>();

        ammoText.enabled = false;
        nailIcon.enabled = false;
    }

    private void LateUpdate()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        GameObject equipped = inventory != null && inventory.isActiveAndEnabled
            ? inventory.EquippedObject
            : null;
        Nailgun nailgun = equipped != null && equipped.activeInHierarchy
            ? equipped.GetComponentInChildren<Nailgun>()
            : null;

        bool holdingNailgun = nailgun != null && nailgun.isActiveAndEnabled;
        ammoText.enabled = holdingNailgun;
        nailIcon.enabled = holdingNailgun;
        if (!holdingNailgun)
            return;

        int magazine = nailgun.NailsInMagazine;
        int capacity = nailgun.MagazineSize;
        int reserve = nailgun.ReserveNails;
        if (magazine == lastMagazine && capacity == lastCapacity && reserve == lastReserve)
            return;

        ammoText.text = $"    {magazine}/{capacity} ({reserve})";
        lastMagazine = magazine;
        lastCapacity = capacity;
        lastReserve = reserve;
    }

    private void OnDisable()
    {
        if (ammoText != null)
            ammoText.enabled = false;
        if (nailIcon != null)
            nailIcon.enabled = false;
    }
}
