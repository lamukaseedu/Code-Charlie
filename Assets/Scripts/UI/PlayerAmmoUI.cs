/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using TMPro;
using UnityEngine;

public class PlayerAmmoUI : MonoBehaviour
{
    [SerializeField] private PlayerAmmo ammo;
    [SerializeField] private TMP_Text ammoText;

    private void Awake()
    {
        if (ammo == null)
            ammo = GetComponent<PlayerAmmo>() ?? GetComponentInParent<PlayerAmmo>();
        if (ammo == null)
            ammo = FindFirstObjectByType<PlayerAmmo>();
    }

    private void OnEnable()
    {
        if (ammo != null)
            ammo.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (ammo != null)
            ammo.Changed -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (ammoText == null || ammo == null)
            return;

        ammoText.text = $"Nails {ammo.Current}/{ammo.Max}";
    }
}
