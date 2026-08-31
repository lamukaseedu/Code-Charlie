/*
 * Author: Shelton Joseph
 * Created: 8/30/2026
 */

using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonShooting : MonoBehaviour
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.5f;
    public Camera playerCamera;

    private float nextFireTime = 0f;

    private InputAction shootAction;

    private void Awake()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        shootAction = playerInput.actions["Shoot"];
    }

    private void Update()
    {
        if (shootAction.IsPressed())
        {
            Shoot();
        }
    }

    //Creates a ray facing forward from the first person camera. Any object that the ray hits that is damagable will take damage
    private void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireRate;

        Debug.Log("Bang");

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        Debug.DrawRay(
        ray.origin,
        ray.direction * range,
        Color.red,
        1f
        );

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}