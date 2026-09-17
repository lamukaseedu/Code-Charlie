/*
 * Author: Lam Nguyen
 * Created: 9/17/2026
 */

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class WatchTabs : MonoBehaviour
{
    public enum Tab { Inventory, Map }
    
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Tab startingTab = Tab.Inventory;

    private WatchControl watchControl;
    private CanvasGroup canvasGroup;
    public Tab SelectedTab { get; private set; }
    private bool CanInteract => isActiveAndEnabled && watchControl != null
        && watchControl.IsOpen && Time.timeScale > 0f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        watchControl = GetComponentInParent<WatchControl>();
        if (inventoryButton != null) inventoryButton.onClick.AddListener(ShowInventory);
        if (mapButton != null) mapButton.onClick.AddListener(ShowMap);
        SelectedTab = startingTab;
        RefreshPanels();
        UpdateInteraction();
    }

    private void Update() => UpdateInteraction();

    public void ShowInventory()
    {
        if (!CanInteract) return;
        SelectedTab = Tab.Inventory;
        RefreshPanels();
    }

    public void ShowMap()
    {
        if (!CanInteract) return;
        SelectedTab = Tab.Map;
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        bool inventorySelected = SelectedTab == Tab.Inventory;
        if (inventoryPanel != null) inventoryPanel.SetActive(inventorySelected);
        if (mapPanel != null) mapPanel.SetActive(!inventorySelected);
    }

    private void UpdateInteraction()
    {
        canvasGroup.interactable = CanInteract;
        canvasGroup.blocksRaycasts = CanInteract;
    }

    private void OnDisable()
    {
        if (canvasGroup == null) return;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (inventoryButton != null) inventoryButton.onClick.RemoveListener(ShowInventory);
        if (mapButton != null) mapButton.onClick.RemoveListener(ShowMap);
    }
}
