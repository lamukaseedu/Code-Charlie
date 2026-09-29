/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using System;
using UnityEngine;

public class PlayerAmmo : MonoBehaviour
{
    [SerializeField] private int maxAmmo = 48;
    [SerializeField] private int startingAmmo = 24;

    public event Action Changed;

    public int Current { get; private set; }
    public int Max => maxAmmo;

    private void Awake()
    {
        Current = Mathf.Clamp(startingAmmo, 0, Mathf.Max(0, maxAmmo));
    }

    public bool TryConsume(int amount)
    {
        if (amount <= 0 || Current < amount)
            return false;

        Current -= amount;
        Changed?.Invoke();
        return true;
    }

    public int Add(int amount)
    {
        if (amount <= 0 || maxAmmo <= 0 || Current >= maxAmmo)
            return 0;

        int added = Mathf.Min(amount, maxAmmo - Current);
        Current += added;
        Changed?.Invoke();
        return added;
    }
}
