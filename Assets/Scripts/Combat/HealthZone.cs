/*
 * Author: Savio Xavier
 * Created: 9/15/2026
 */

using UnityEngine;

public class HealthZone : MonoBehaviour
{
    public enum Mode
    {
        Damage,
        Heal
    }

    [SerializeField] private Mode mode = Mode.Damage;
    [SerializeField] private float amountPerTick = 10f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private bool playerOnly = true;

    private float nextTickTime;

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextTickTime)
        {
            return;
        }

        Health health = other.GetComponentInParent<Health>();
        if (health == null)
        {
            return;
        }

        if (playerOnly && other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        nextTickTime = Time.time + tickInterval;

        if (mode == Mode.Damage)
        {
            health.TakeDamage(amountPerTick);
        }
        else
        {
            health.Heal(amountPerTick);
        }
    }
}
