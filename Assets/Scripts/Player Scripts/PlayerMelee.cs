/*
 * Author: Savio Xavier
 * Created: 8/30/2026
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMelee : MonoBehaviour
{
    [SerializeField] float damage = 1f;
    [SerializeField] float range = 2f;
    [SerializeField] float radius = 0.6f;
    [SerializeField] LayerMask hitMask = ~0;

    private InputAction attackAction;

    // Caches the Attack action from the player's Input System asset
    private void Awake()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        attackAction = playerInput.actions["Attack"];
    }

    // Damages IDamageable targets in a sphere in front of the camera on click
    private void Update()
    {
        if (attackAction.WasPressedThisFrame())
        {
            TryAttack();
        }
    }

    // Overlaps a sphere in look direction and applies damage, skipping the player
    private void TryAttack()
    {
        Transform origin = Camera.main.transform;
        Vector3 center = origin.position + origin.forward * range;
        Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
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
