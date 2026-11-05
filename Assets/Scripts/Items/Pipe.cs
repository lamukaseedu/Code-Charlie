/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 * Edited By: Savio Xavier
 * Edited: 10/22/2026
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Pipe : MonoBehaviour, IUsable
{
    [SerializeField] float damage = 10f;
    [SerializeField] float upgradedDamage = 20f;
    [Tooltip("Child collider defining the damage volume at the swing's hit time. Use an enabled trigger Box, Sphere, Capsule, or convex MeshCollider. If unassigned, a supported child collider is found automatically.")]
    [SerializeField] private Collider damageCollider;
    [SerializeField] private Animator swingAnimator;

    [Header("Audio")]
    [SerializeField] private AudioClip swingClip;
    [SerializeField] private AudioClip hitEnemyClip;
    [SerializeField] private AudioClip hitMetalClip;

    private PlayerWeaponUpgrades upgrades;
    private AudioSource audioSource;
    private bool swingType = true;

    private float nextAttackTime = 0f;
    private float attackCooldown = 0.75f;

    private void Awake()
    {
        FindDamageCollider();
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        upgrades = GetComponentInParent<PlayerWeaponUpgrades>();
    }

    // The child collider defines the hit volume; the swing delay defines when it deals damage.
    public void Use()
    {
        if (!isActiveAndEnabled) return;
        FindDamageCollider();
        if (damageCollider == null || !SupportsDamageVolume(damageCollider) ||
            !damageCollider.transform.IsChildOf(transform) || damageCollider.transform == transform)
        {
            Debug.LogWarning("Pipe needs a child Box, Sphere, Capsule, or convex MeshCollider assigned as its damage collider.", this);
            return;
        }
        if (Time.time > nextAttackTime)
        {
            if (swingAnimator != null)
            {
                swingAnimator.SetBool("swingAlternator", swingType);
                swingAnimator.SetTrigger("swingTrigger");
                swingType = !swingType;
            }
            GameAudio.PlayOneShot(audioSource, swingClip);
            StartCoroutine(Startup());
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    IEnumerator Startup()
    {
        yield return new WaitForSeconds(0.3f);
        if (damageCollider == null || !damageCollider.enabled || !damageCollider.gameObject.activeInHierarchy)
            yield break;
        Debug.Log("Swing!");
        float hitDamage = upgrades != null && upgrades.IsUpgraded(WeaponUpgradeId.PipeDamage)
            ? upgradedDamage
            : damage;
        // Synchronize animated child transforms before looking for overlapping world colliders.
        Physics.SyncTransforms();
        Bounds bounds = damageCollider.bounds;
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        var damagedTargets = new HashSet<IDamageable>();
        var knockedTargets = new HashSet<IKnockable>();
        bool hitWorld = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null || hit == damageCollider || hit.transform.IsChildOf(transform) ||
                hit.GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            // The bounds query is only a broad phase. Damage requires overlap with the collider's actual shape.
            if (!Physics.ComputePenetration(damageCollider, damageCollider.transform.position,
                damageCollider.transform.rotation, hit, hit.transform.position, hit.transform.rotation,
                out _, out _)) continue;

            hitWorld = true;

            // Resolve both before applying damage, which may destroy the enemy.
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            IKnockable knockable =
                hit.GetComponentInParent<IKnockable>();

            if (damageable != null && damagedTargets.Add(damageable))
            {
                if (damageable is Health health)
                    health.TakeDamage(hitDamage, gameObject);
                else
                    damageable.TakeDamage(hitDamage);
            }

            if (knockable != null && knockedTargets.Add(knockable))
            {
                knockable.Execute(transform);
            }
        }

        if (damagedTargets.Count > 0)
            GameAudio.PlayOneShot(audioSource, hitEnemyClip);
        else if (hitWorld)
            GameAudio.PlayOneShot(audioSource, hitMetalClip);

    }

    private void OnDisable()
    {
        // Unequipping the pipe cancels a swing that has not reached its hit time.
        StopAllCoroutines();
    }

    private void FindDamageCollider()
    {
        if (damageCollider != null) return;
        foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
        {
            if (candidate.transform == transform || !SupportsDamageVolume(candidate)) continue;
            damageCollider = candidate;
            break;
        }
    }

    private static bool SupportsDamageVolume(Collider volume)
    {
        return volume is BoxCollider || volume is SphereCollider || volume is CapsuleCollider ||
            (volume is MeshCollider mesh && mesh.convex);
    }
}
