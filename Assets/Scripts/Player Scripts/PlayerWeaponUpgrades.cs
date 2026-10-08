/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using System;
using UnityEngine;

public enum WeaponUpgradeId
{
    PipeDamage,
    NailgunAccuracy
}

public class PlayerWeaponUpgrades : MonoBehaviour
{
    public event Action Changed;

    private bool pipeDamageUpgraded;
    private bool nailgunAccuracyUpgraded;

    public bool IsUpgraded(WeaponUpgradeId id)
    {
        return id switch
        {
            WeaponUpgradeId.PipeDamage => pipeDamageUpgraded,
            WeaponUpgradeId.NailgunAccuracy => nailgunAccuracyUpgraded,
            _ => false
        };
    }

    public bool TryApply(WeaponUpgradeId id)
    {
        if (IsUpgraded(id))
            return false;

        switch (id)
        {
            case WeaponUpgradeId.PipeDamage:
                pipeDamageUpgraded = true;
                break;
            case WeaponUpgradeId.NailgunAccuracy:
                nailgunAccuracyUpgraded = true;
                break;
            default:
                return false;
        }

        Changed?.Invoke();
        return true;
    }
}
