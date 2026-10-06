/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 * Edited By: Andres Rondon-Villarmosa
 * Edited: 9/10/2026
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
    [SerializeField] bool destroyOnDeath = true;

    [Header("Health Events")]
    [SerializeField] private UnityEvent onDamaged;
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent<float> onHealthChanged;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public UnityEvent<float> OnHealthChanged => onHealthChanged;

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
        onHealthChanged?.Invoke(HealthPercent);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(HealthPercent);
    }


    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    [ContextMenu("Debug Take Damage")]
    private void DebugTakeDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Debug Heal")]
    private void DebugHeal()
    {
        Heal(10f);
    }
}
