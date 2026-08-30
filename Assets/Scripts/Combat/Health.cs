/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 */

using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 10f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Subtracts damage and destroys the object at 0 health
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
