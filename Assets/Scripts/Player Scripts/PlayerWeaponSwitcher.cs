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

    private void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        nextWeaponAction = playerInput.actions["NextWeapon"];
        previousWeaponAction = playerInput.actions["PreviousWeapon"];
        weapon1Action = playerInput.actions["Weapon1"];
        weapon2Action = playerInput.actions["Weapon2"];
    }

    // Starts with the first assigned weapon
    private void Start()
    {
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

    private void CycleWeapon(int direction)
    {
        int count = GetWeaponCount();
        if (count == 0)
        {
            return;
        }

        int nextIndex = (currentIndex + direction) % count;
        if (nextIndex < 0)
        {
            nextIndex += count;
        }

        SelectWeapon(nextIndex);
    }

    // Activates the weapon at the specified index
    private void SelectWeapon(int index)
    {
        if (index < 0 || index >= GetWeaponCount())
        {
            return;
        }

        currentIndex = index;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                continue;
            }

            weapons[i].enabled = i == currentIndex;
        }

        Debug.Log("Active weapon: " + weapons[currentIndex].GetType().Name);
    }

    // Counts assigned weapon components, skips empty
    private int GetWeaponCount()
    {
        if (weapons == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                count++;
            }
        }

        return count;
    }
}
