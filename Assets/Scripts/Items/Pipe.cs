/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 */

using UnityEngine;

public class Pipe : MonoBehaviour, IUsable
{
    [SerializeField] float damage = 1f;
    [SerializeField] float range = 2f;
    [SerializeField] float radius = 0.6f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] private Animator swingAnimator;
    private bool swingType = true;

    // Damages IDamageable targets in a sphere in front of the camera on click
    // Overlaps a sphere in look direction and applies damage, skipping the player
    public void Use()
    {
        Transform origin = Camera.main.transform;
        Vector3 center = origin.position + origin.forward * range;
        Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);

        if (swingAnimator != null)
        {
            swingAnimator.SetBool("swingAlternator", swingType);
            swingAnimator.SetTrigger("swingTrigger");
            swingType = !swingType;
        }


        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            IKnockable knockable = hits[i].GetComponentInParent<IKnockable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            if (knockable != null)
            {
                knockable.Execute(transform);
            }

        }


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