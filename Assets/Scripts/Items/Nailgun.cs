/*
 * Author: Shelton Joseph
 * Created: 8/30/2026
 */
using UnityEngine;
using UnityEngine.InputSystem;

public class Nailgun : MonoBehaviour, IUsable, IInventoryItem
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask layersToIgnore;
    [SerializeField] private float fireRate = 0.5f;

    [Header("Ammo")]
    [SerializeField] private Item nailAmmo;
    [Min(1)]
    [SerializeField] private int magSize = 10;
    [SerializeField] private float spread = 2f;

    private int nailsInMag = 0;
    [SerializeField] private float spreadAngle = 4f;
    [SerializeField] private float upgradedSpreadAngle = 0f;

    [SerializeField] private GameObject NailPrefab;

    [SerializeField] private Transform Muzzle;

    [SerializeField] private LineRenderer bulletTrailPrefab;
    private Camera playerCamera;
    private PlayerAmmo ammo;
    private PlayerWeaponUpgrades upgrades;

    [SerializeField] private Animator recoilAnimator;

    private float nextFireTime = 0f;

    private InputAction reloadGun;

    private PlayerInventory playerInventory;

    public void Initialize(PlayerInventory inventory)
    {
        playerInventory = inventory;
    }

    private void Awake()
    {
        playerCamera = Camera.main;
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            reloadGun = playerInput.actions["Reload"];
    }

    private void OnEnable()
    {
        ammo = GetComponentInParent<PlayerAmmo>();
        upgrades = GetComponentInParent<PlayerWeaponUpgrades>();
    }

    private void Update()
    {
        if (playerInventory == null || playerInventory.IsOpen || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
            return;

        if (reloadGun.WasPressedThisFrame())
        {
            Reload();
        }
    }

    //Reloads mag by first checking amount needed and if the inventory contains at least 1 to the amount needed insert that ammo from the inventory into the nailgun using PlayerInventory. 
    private void Reload()
    {
        if (playerInventory == null || nailAmmo == null)
            return;

        int nailsNeeded = magSize - nailsInMag;

        if (nailsNeeded <= 0)
            return;

        int reserveNails = playerInventory.GetItemCount(nailAmmo);
        int nailsToLoad = Mathf.Min(nailsNeeded, reserveNails);

        if (nailsToLoad <= 0)
        {
            Debug.Log("No reserve nails!");
            return;
        }

        // Transfer only the nails needed to top up the magazine.
        if (playerInventory.TryConsumeItem(nailAmmo, nailsToLoad))
        {
            nailsInMag += nailsToLoad;
            Debug.Log($"Reloaded! Magazine: {nailsInMag}/{magSize}");
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

        if (nailsInMag <= 0)
        {
            Debug.Log("Magazine empty! Reload!");
            return;
        }

        if (playerInventory == null ||
        !playerInventory.TryConsumeItem(nailAmmo, 1))
        {
            Debug.Log("Out of nails!");
            return;
        }

        nextFireTime = Time.time + fireRate;
        nailsInMag--;

        Debug.Log("Bang");

        Ray ray = new Ray(
            playerCamera.transform.position,
            GetShotDirection()
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

        if (Physics.Raycast(ray, out RaycastHit hit, range, ~layersToIgnore.value))
        {
            hitPoint = hit.point;

            GameObject Nail = Instantiate(NailPrefab, hit.point, Quaternion.LookRotation(playerCamera.transform.up));
            Nail.transform.SetParent(hit.collider.transform, true);

            Destroy(Nail, 5.0f);

            Debug.Log("Hit: " + hit.collider.name);

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

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

    private Vector3 GetShotDirection()
    {
        Vector3 forward = playerCamera.transform.forward;
        float spread = upgrades != null && upgrades.IsUpgraded(WeaponUpgradeId.NailgunAccuracy)
            ? upgradedSpreadAngle
            : spreadAngle;
        if (spread <= 0f)
            return forward;

        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
        return (forward + playerCamera.transform.right * offset.x + playerCamera.transform.up * offset.y).normalized;
    }
}
