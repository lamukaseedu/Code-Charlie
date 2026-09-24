/*
 * Author: Shelton Joseph
 * Created: 8/30/2026
 */
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Nailgun : MonoBehaviour, IUsable
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.5f;

    [SerializeField] private float spread = 2f;

    [SerializeField] private int magSize = 10;

    [SerializeField] private GameObject NailPrefab;

    [SerializeField] private Transform Muzzle;

    [SerializeField] private LineRenderer bulletTrailPrefab;
    private Camera playerCamera;

    [SerializeField] private Animator recoilAnimator;

    private float nextFireTime = 0f;

    private int nailsInMag = 10;

    private int totalNails = 100;

    private InputAction reloadGun;

    private void Awake()
    {
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        playerCamera = Camera.main;
        reloadGun = playerInput.actions["Reload"];
    }

    private void Update()
    {
        if (reloadGun.WasPressedThisFrame())
        {
            if (totalNails > magSize)
            {
                nailsInMag = magSize;
            }
            else nailsInMag = totalNails;
        }
    }

    //Creates a ray facing forward from the first person camera. Any object that the ray hits that is damagable will take damage.
    //Also creates a nail object embedded where ray hits object

    public void Use()
    {
        if (!isActiveAndEnabled || Time.time < nextFireTime)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            return;

        if (nailsInMag == 0)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

        Debug.Log("Bang");

        Vector3 direction = playerCamera.transform.forward;

        direction = Quaternion.Euler(
        Random.Range(-spread, spread),
        Random.Range(-spread, spread),
        0f
        ) * direction;

        Ray ray = new Ray(
            playerCamera.transform.position,
            direction
        );

        if (recoilAnimator != null)
        {
            recoilAnimator.SetTrigger("recoilTrigger");
        }

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
            Nail.transform.SetParent(hit.collider.transform, true);

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
        nailsInMag--;
        totalNails--;
        CreateBulletTrail(hitPoint);
    }


    private void CreateBulletTrail(Vector3 hitPoint)
    {
        LineRenderer trail = Instantiate(bulletTrailPrefab);

        trail.SetPosition(0, Muzzle.position);
        trail.SetPosition(1, hitPoint);

        Destroy(trail.gameObject, 0.05f);
    }
}
