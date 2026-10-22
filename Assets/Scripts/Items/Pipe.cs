/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Cinemachine.IInputAxisOwner.AxisDescriptor;
using static UnityEngine.UI.Image;

public class Pipe : MonoBehaviour, IUsable
{
    [SerializeField] float damage = 10f;
    [SerializeField] float upgradedDamage = 20f;
    [SerializeField] float range = 2f;
    [SerializeField] float radius = 0.6f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] private Animator swingAnimator;

    private PlayerWeaponUpgrades upgrades;
    private bool swingType = true;

    private float nextAttackTime = 0f;

    private float attackCooldown = 0.75f;

    private void OnEnable()
    {
        upgrades = GetComponentInParent<PlayerWeaponUpgrades>();
        //swingAnimator.SetBool("Equip", true);
    }

    // Damages IDamageable targets in a sphere in front of the camera on click
    // Overlaps a sphere in look direction and applies damage, skipping the player
    public void Use()
    {
        if (Time.time > nextAttackTime)
        {
            if (swingAnimator != null)
            {
                swingAnimator.SetBool("swingAlternator", swingType);
                swingAnimator.SetTrigger("swingTrigger");
                swingType = !swingType;
            }
            StartCoroutine(Startup());
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    IEnumerator Startup()
    {
        yield return new WaitForSeconds(0.3f);
        Debug.Log("Swing!");
        float hitDamage = upgrades != null && upgrades.IsUpgraded(WeaponUpgradeId.PipeDamage)
            ? upgradedDamage
            : damage;
        Transform origin = Camera.main.transform;
        Vector3 center = origin.position + origin.forward * range;
        Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);
        var damagedTargets = new HashSet<IDamageable>();
        var knockedTargets = new HashSet<IKnockable>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null ||
                hit.GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

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
        yield return new WaitForSeconds(5f);
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