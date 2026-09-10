/*
 * Author: Shelton Joseph
 * Created: 8/30/2026
 */
using Unity.Cinemachine;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.5f;

    [SerializeField] private GameObject NailPrefab;

    [SerializeField] private GameObject Gun;

    [SerializeField] private Transform Muzzle;

    [SerializeField] private LineRenderer bulletTrailPrefab;
    [SerializeField] private Camera playerCamera;

    private float nextFireTime = 0f;

    private InputAction shootAction;

    private void Awake()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        shootAction = playerInput.actions["Shoot"];
    }

    private void OnEnable()
    {
        Gun.SetActive(true);
        Debug.Log("Gun activated");
    }

    private void Update()
    {
        if (shootAction.IsPressed())
        {
            Shoot();
        }
    }

    //Creates a ray facing forward from the first person camera. Any object that the ray hits that is damagable will take damage.
    //Also creates a nail object embedded where ray hits object
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

        Vector3 hitPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            hitPoint = hit.point;

            GameObject Nail = Instantiate(NailPrefab, hit.point, Quaternion.LookRotation(playerCamera.transform.up));
            Nail.transform.SetParent(hit.collider.transform);

            Destroy(Nail, 5.0f);

            Debug.Log("Hit: " + hit.collider.name);

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
        else
        {
            hitPoint = ray.origin + ray.direction * range;
        }

        CreateBulletTrail(hitPoint);
    }

    private void CreateBulletTrail(Vector3 hitPoint)
    {
        LineRenderer trail = Instantiate(bulletTrailPrefab);

        trail.SetPosition(0, Muzzle.position);
        trail.SetPosition(1, hitPoint);

        Destroy(trail.gameObject, 0.05f);
    }

    private void OnDisable()
    {
        Gun.SetActive(false);
        Debug.Log("Gun unactivated");
    }
}