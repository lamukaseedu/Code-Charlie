/*
 * Author: Shelton Joseph
 * Created: 8/30/2026
 * Edited By: Savio Xavier
 * Edited: 10/20/2026
 */
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
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

    static private int nailsInMag = 0;
    [SerializeField] private float spreadAngle = 4f;
    [SerializeField] private float upgradedSpreadAngle = 0f;

    [SerializeField] private GameObject NailPrefab;

    [SerializeField] private Transform Muzzle;

    [SerializeField] private LineRenderer bulletTrailPrefab;
    private Camera playerCamera;
    private PlayerAmmo ammo;
    private PlayerWeaponUpgrades upgrades;

    [SerializeField] private Animator recoilAnimator;

    [Header("Audio")]
    [SerializeField] private AudioClip fireClip;

    private AudioSource audioSource;
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        Debug.Log("enabled", this);
        ammo = GetComponentInParent<PlayerAmmo>();
        upgrades = GetComponentInParent<PlayerWeaponUpgrades>();
    }

    private void Update()
    {
        if (playerInventory == null || playerInventory.IsOpen || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
            return;

        if (reloadGun != null && reloadGun.WasPressedThisFrame())
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
            nailsToLoad += nailsInMag;
            nailsInMag = 0;
            recoilAnimator.SetTrigger("reload");
            StartCoroutine(ReloadGun(nailsToLoad));
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

        nextFireTime = Time.time + fireRate;
        nailsInMag--;

        GameAudio.PlayOneShot(audioSource, fireClip);

        GameObject noiseSource = playerInventory != null ? playerInventory.gameObject : gameObject;
        NoiseEmitter.EmitAt(Muzzle != null ? Muzzle.position : transform.position,
            NoiseEmitter.NoiseLevel.Strong, noiseSource);

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
            if (hit.transform.parent != null)
            {
                Nail.transform.SetParent(hit.collider.transform.parent, true);
            }
            else
            {
                Nail.transform.SetParent(hit.collider.transform, true);
            }

            Destroy(Nail, 5.0f);

            Debug.Log("Hit: " + hit.collider.name);

            IImpactable impactable = hit.collider.GetComponentInParent<IImpactable>();

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            // Capture the receiver before damage, then notify after existing damage handling.
            if (impactable != null && (!(impactable is Object receiver) || receiver != null))
                impactable.OnImpact(new ImpactInfo(hit.point, hit.normal, ray.direction, noiseSource, hit.collider));
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

    private void OnDisable()
    {
        Debug.Log(
            $"Nailgun DISABLED | enabled={enabled} | " +
            $"activeSelf={gameObject.activeSelf} | " +
            $"activeInHierarchy={gameObject.activeInHierarchy}",
            this
        );

        Transform parent = transform.parent;
        while (parent != null)
        {
            Debug.Log($"Parent: {parent.name} | activeSelf={parent.gameObject.activeSelf}");
            parent = parent.parent;
        }
    }

    IEnumerator ReloadGun(int NailsToLoad)
    {
        yield return new WaitForSeconds(1f);
        nailsInMag += NailsToLoad;
        Debug.Log($"Reloaded! Magazine: {nailsInMag}/{magSize}");
    }

}
