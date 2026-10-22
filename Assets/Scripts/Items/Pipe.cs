/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 * Edited By: Savio Xavier
 * Edited: 10/20/2026
 */

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class Pipe : MonoBehaviour, IUsable
{
    [SerializeField] float damage = 10f;
    [SerializeField] float upgradedDamage = 20f;
    [SerializeField] float range = 2f;
    [SerializeField] float radius = 0.6f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] private Animator swingAnimator;

    [Header("Audio")]
    [SerializeField] private AudioClip swingClip;
    [SerializeField] private AudioClip hitEnemyClip;
    [SerializeField] private AudioClip hitMetalClip;

    private PlayerWeaponUpgrades upgrades;
    private AudioSource audioSource;
    private bool swingType = true;

    private void Awake()
    {
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

    // Damages IDamageable targets in a sphere in front of the camera on click
    // Overlaps a sphere in look direction and applies damage, skipping the player
    public void Use()
    {
        Transform origin = Camera.main.transform;
        Vector3 center = origin.position + origin.forward * range;
        Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);
        float hitDamage = upgrades != null && upgrades.IsUpgraded(WeaponUpgradeId.PipeDamage)
            ? upgradedDamage
            : damage;

        if (swingAnimator != null)
        {
            swingAnimator.SetBool("swingAlternator", swingType);
            swingAnimator.SetTrigger("swingTrigger");
            swingType = !swingType;
        }


        GameAudio.PlayOneShot(audioSource, swingClip);

        var damagedTargets = new HashSet<IDamageable>();
        var knockedTargets = new HashSet<IKnockable>();
        bool hitWorld = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null ||
                hit.GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            hitWorld = true;

            // Resolve both before applying damage, which may destroy the enemy.
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            IKnockable knockable =
                hit.GetComponentInParent<IKnockable>();

            if (damageable != null && damagedTargets.Add(damageable))
            {
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

    // Draws the melee hit sphere in the Scene view when this object is selected
    private void OnDrawGizmosSelected()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Transform origin = camera.transform;
        Vector3 center = origin.position + origin.forward * range;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, radius);
    }
}