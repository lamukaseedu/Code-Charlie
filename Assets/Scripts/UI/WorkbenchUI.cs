/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WorkbenchUI : MonoBehaviour
{
    public static bool AnyOpen { get; private set; }

    [Header("Items")]
    [SerializeField] private Item pipeItem;
    [SerializeField] private Item pipeKitItem;
    [SerializeField] private Item nailgunItem;
    [SerializeField] private Item nailgunKitItem;

    [Header("Menu")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button closeButton;

    [Header("Pipe Row")]
    [SerializeField] private TMP_Text pipeStatsText;
    [SerializeField] private TMP_Text pipeStatusText;
    [SerializeField] private Button pipeUpgradeButton;
    [SerializeField] private string pipeBaseStats = "Damage 10";
    [SerializeField] private string pipeUpgradedStats = "Damage 20";

    [Header("Nailgun Row")]
    [SerializeField] private TMP_Text nailgunStatsText;
    [SerializeField] private TMP_Text nailgunStatusText;
    [SerializeField] private Button nailgunUpgradeButton;
    [SerializeField] private string nailgunBaseStats = "Wide spread";
    [SerializeField] private string nailgunUpgradedStats = "Tight spread";

    public bool IsOpen { get; private set; }

    private PlayerInventory inventory;
    private PlayerWeaponUpgrades upgrades;
    private PlayerMovement playerMovement;
    private InputAction lookAction;
    private InputAction interactAction;
    private bool ignoreInteractUntilRelease;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (pipeUpgradeButton != null)
            pipeUpgradeButton.onClick.AddListener(UpgradePipe);
        if (nailgunUpgradeButton != null)
            nailgunUpgradeButton.onClick.AddListener(UpgradeNailgun);

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= Refresh;
        if (upgrades != null)
            upgrades.Changed -= Refresh;

        if (IsOpen)
            AnyOpen = false;

        IsOpen = false;
    }

    private void Update()
    {
        if (!IsOpen || Time.timeScale == 0f)
            return;

        if (ignoreInteractUntilRelease)
        {
            if (interactAction == null || !interactAction.IsPressed())
                ignoreInteractUntilRelease = false;
            return;
        }

        if (interactAction != null && interactAction.WasPressedThisFrame())
            Close();
    }

    public void Open()
    {
        if (IsOpen || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
            return;

        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
            return;

        upgrades = inventory.GetComponent<PlayerWeaponUpgrades>();
        playerMovement = inventory.GetComponent<PlayerMovement>();
        PlayerInput playerInput = inventory.GetComponent<PlayerInput>();
        lookAction = playerInput != null ? playerInput.actions["Look"] : null;
        interactAction = playerInput != null ? playerInput.actions["MinigameInteract"] : null;

        WatchControl watch = inventory.GetComponentInChildren<WatchControl>(true);
        watch?.RequestClose();

        IsOpen = true;
        AnyOpen = true;
        ignoreInteractUntilRelease = true;

        inventory.Changed += Refresh;
        if (upgrades != null)
            upgrades.Changed += Refresh;

        playerMovement?.SetMovementEnabled(false);
        lookAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
        SetVisible(true);
    }

    public void Close()
    {
        if (!IsOpen || Time.timeScale == 0f)
            return;

        if (inventory != null)
            inventory.Changed -= Refresh;
        if (upgrades != null)
            upgrades.Changed -= Refresh;

        IsOpen = false;
        AnyOpen = false;
        ignoreInteractUntilRelease = false;
        SetVisible(false);

        playerMovement?.SetMovementEnabled(true);
        lookAction?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UpgradePipe()
    {
        TryUpgrade(pipeItem, pipeKitItem, WeaponUpgradeId.PipeDamage);
    }

    public void UpgradeNailgun()
    {
        TryUpgrade(nailgunItem, nailgunKitItem, WeaponUpgradeId.NailgunAccuracy);
    }

    private void Refresh()
    {
        RefreshRow(
            pipeItem,
            pipeKitItem,
            WeaponUpgradeId.PipeDamage,
            pipeBaseStats,
            pipeUpgradedStats,
            pipeStatsText,
            pipeStatusText,
            pipeUpgradeButton);
        RefreshRow(
            nailgunItem,
            nailgunKitItem,
            WeaponUpgradeId.NailgunAccuracy,
            nailgunBaseStats,
            nailgunUpgradedStats,
            nailgunStatsText,
            nailgunStatusText,
            nailgunUpgradeButton);
    }

    private void RefreshRow(
        Item weaponItem,
        Item kitItem,
        WeaponUpgradeId upgradeId,
        string baseStats,
        string upgradedStats,
        TMP_Text statsText,
        TMP_Text statusText,
        Button upgradeButton)
    {
        bool hasWeapon = inventory != null && inventory.HasItem(weaponItem);
        bool upgraded = upgrades != null && upgrades.IsUpgraded(upgradeId);
        bool hasKit = inventory != null && inventory.HasItem(kitItem);
        bool canUpgrade = hasWeapon && !upgraded && hasKit;

        if (statsText != null)
            statsText.text = upgraded ? upgradedStats : baseStats;

        if (statusText != null)
        {
            if (!hasWeapon)
                statusText.text = "Weapon not in inventory";
            else if (upgraded)
                statusText.text = "Upgraded";
            else if (!hasKit)
                statusText.text = "Missing upgrade kit";
            else
                statusText.text = "Upgrade kit ready";
        }

        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade && Time.timeScale > 0f;
    }

    private void TryUpgrade(Item weaponItem, Item kitItem, WeaponUpgradeId upgradeId)
    {
        if (inventory == null || upgrades == null || Time.timeScale == 0f)
            return;
        if (!inventory.HasItem(weaponItem) || upgrades.IsUpgraded(upgradeId) || !inventory.HasItem(kitItem))
            return;
        if (!upgrades.TryApply(upgradeId))
            return;
        inventory.TryRemoveItem(kitItem);
    }

    private void SetVisible(bool visible)
    {
        if (menuRoot != null)
            menuRoot.SetActive(visible);
    }
}
