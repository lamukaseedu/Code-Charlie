/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 * Edited By: Andres Rondon-Villarmosa
 * Edited:9/2/2026
 */

using UnityEngine.Events;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 10f;

    [Header("Health Events")]
    [SerializeField] private UnityEvent onDamaged;
    [SerializeField] private UnityEvent onDeath;

    private float currentHealth;
    private bool isDead;


    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Subtracts damage and destroys the object at 0 health
    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // Marks the object as dead and invokes events for death behavior. 
    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
