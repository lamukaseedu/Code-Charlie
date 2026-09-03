/*
 * Author: Savio Xavier
 * Created: 9/2/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponSwitcher : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] weapons;

    private int currentIndex;
    private InputAction nextWeaponAction;
    private InputAction previousWeaponAction;
    private InputAction weapon1Action;
    private InputAction weapon2Action;

    // Caches input actions and equips the first weapon before other scripts update
    private void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        nextWeaponAction = playerInput.actions["NextWeapon"];
        previousWeaponAction = playerInput.actions["PreviousWeapon"];
        weapon1Action = playerInput.actions["Weapon1"];
        weapon2Action = playerInput.actions["Weapon2"];

        SelectWeapon(0);
    }

    // Checks for weapon switch inputs each frame (modify if perf issue)
    private void Update()
    {
        if (nextWeaponAction.WasPressedThisFrame())
        {
            CycleWeapon(1);
        }

        if (previousWeaponAction.WasPressedThisFrame())
        {
            CycleWeapon(-1);
        }

        if (weapon1Action.WasPressedThisFrame())
        {
            SelectWeapon(0);
        }

        if (weapon2Action.WasPressedThisFrame())
        {
            SelectWeapon(1);
        }
    }

    // Steps to the next weapon slot, wrapping at the ends
    private void CycleWeapon(int direction)
    {
        if (weapons == null || weapons.Length == 0)
        {
            return;
        }

        int nextIndex = (currentIndex + direction + weapons.Length) % weapons.Length;
        SelectWeapon(nextIndex);
    }

    // Activates the weapon at the specified index
    private void SelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length)
        {
            return;
        }

        currentIndex = index;
        SetAllWeaponScriptsEnabled(false);
        weapons[currentIndex].enabled = true;

        Debug.Log("Active weapon: " + weapons[currentIndex].GetType().Name);
    }

    // Enables or disables every melee and gun script on the player, including duplicates
    private void SetAllWeaponScriptsEnabled(bool enabled)
    {
        PlayerMelee melee = GetComponent<PlayerMelee>();
        if (melee != null)
        {
            melee.enabled = enabled;
        }

        PlayerGun[] guns = GetComponentsInChildren<PlayerGun>(true);
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].enabled = enabled;
        }
    }
}
