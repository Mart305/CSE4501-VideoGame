using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button maxHealthButton;
    [SerializeField] private Button damageResistanceButton;
    [SerializeField] private Button closeButton;
    
    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI towerHealthText;
    [SerializeField] private TextMeshProUGUI repairButtonText;
    [SerializeField] private TextMeshProUGUI maxHealthButtonText;
    [SerializeField] private TextMeshProUGUI resistanceButtonText;
    [SerializeField] private TextMeshProUGUI currencyText;
    
    [Header("Upgrade Costs")]
    [SerializeField] private int repairCost = 50;
    [SerializeField] private int maxHealthCost = 100;
    [SerializeField] private int resistanceCost = 150;
    
    private Tower selectedTower;
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
            
        // Hide upgrade panel at start
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
            
        // Setup button listeners in code
        if (repairButton != null)
            repairButton.onClick.AddListener(RepairTower);
        if (maxHealthButton != null)
            maxHealthButton.onClick.AddListener(UpgradeMaxHealth);
        if (damageResistanceButton != null)
            damageResistanceButton.onClick.AddListener(UpgradeDamageResistance);
        if (closeButton != null)
        {
            // Clear any existing listeners and add our close method
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUpgradeMenu);
        }
            
        // Subscribe to currency changes
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged.AddListener(OnCurrencyChanged);
            
            // Initialize currency display
            UpdateCurrencyDisplay();
        }
    }
    
    void Update()
    {
        CheckTowerSelection();
        
        // Manage cursor state for web builds
        if (upgradePanel != null && upgradePanel.activeInHierarchy)
        {
            // For web builds, ensure cursor is always visible and unlocked when UI is open
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
        
        // Make panel continuously face camera while open
        if (upgradePanel != null && upgradePanel.activeInHierarchy && playerCamera != null)
        {
            Vector3 directionToCamera = playerCamera.transform.position - upgradePanel.transform.position;
            directionToCamera.y = 0; // Keep panel upright
            upgradePanel.transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }
    
    void CheckTowerSelection()
    {
        // Only check for tower selection on mouse click, not every frame
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Check if we clicked on UI first (don't close if clicking on UI)
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // Clicked on UI, don't do anything
                return;
            }
            
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Only check for towers, ignore enemies
                if (hit.collider.CompareTag("Tower"))
                {
                    Tower tower = hit.collider.GetComponent<Tower>();
                    if (tower != null)
                    {
                        SelectTower(tower);
                    }
                }
                else
                {
                    // Clicked on something else (not UI, not tower), close upgrade menu if open
                    if (upgradePanel != null && upgradePanel.activeInHierarchy)
                    {
                        CloseUpgradeMenu();
                    }
                }
            }
            else
            {
                // Clicked on empty space (not UI), close upgrade menu if open
                if (upgradePanel != null && upgradePanel.activeInHierarchy)
                {
                    CloseUpgradeMenu();
                }
            }
        }
        
        // Add keyboard shortcut to close menu (ESC key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (upgradePanel != null && upgradePanel.activeInHierarchy)
            {
                CloseUpgradeMenu();
            }
        }
    }
    
    void SelectTower(Tower tower)
    {
        selectedTower = tower;
        ShowUpgradeMenu();
        UpdateUI();
    }
    
    void ShowUpgradeMenu()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            
            // Position UI at fixed world position and make it face camera
            if (selectedTower != null)
            {   
                // Make panel face the camera
                Vector3 directionToCamera = playerCamera.transform.position - upgradePanel.transform.position;
                directionToCamera.y = 0; // Keep panel upright
                upgradePanel.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
    
    void UpdateUI()
    {
        if (selectedTower == null) return;
        
        // Update tower health display
        if (towerHealthText != null)
        {
            towerHealthText.text = $"Health: {selectedTower.GetCurrentHealth():F0}/{selectedTower.GetMaxHealth():F0}";
        }
        
        // Update button texts with costs
        if (repairButtonText != null)
        {
            repairButtonText.text = $"Repair ({repairCost} Gold)";
        }
        
        if (maxHealthButtonText != null)
        {
            int level = selectedTower.GetMaxHealthUpgradeLevel();
            maxHealthButtonText.text = $"Max Health Lv.{level + 1} ({maxHealthCost} Gold)";
        }
        
        if (resistanceButtonText != null)
        {
            float resistance = selectedTower.GetDamageResistance() * 100f;
            resistanceButtonText.text = $"Resistance {resistance:F0}% ({resistanceCost} Gold)";
        }
        
        // Update currency display
        UpdateCurrencyDisplay();
        
        // Enable/disable buttons based on conditions
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        if (selectedTower == null) return;
        
        // Repair button - disable if at full health or not enough currency
        if (repairButton != null)
        {
            bool canRepair = selectedTower.GetCurrentHealth() < selectedTower.GetMaxHealth();
            bool hasEnoughCurrency = HasEnoughCurrency(repairCost);
            repairButton.interactable = canRepair && hasEnoughCurrency;
        }
        
        // Max health button - disable if not enough currency
        if (maxHealthButton != null)
        {
            bool hasEnoughCurrency = HasEnoughCurrency(maxHealthCost);
            maxHealthButton.interactable = hasEnoughCurrency;
        }
        
        // Resistance button - disable if at max resistance or not enough currency
        if (damageResistanceButton != null)
        {
            bool canUpgrade = selectedTower.GetDamageResistance() < 0.8f;
            bool hasEnoughCurrency = HasEnoughCurrency(resistanceCost);
            damageResistanceButton.interactable = canUpgrade && hasEnoughCurrency;
        }
    }
    
    public void RepairTower()
    {
        if (selectedTower != null && SpendCurrency(repairCost))
        {
            selectedTower.RepairTower();
            UpdateUI();
        }
    }
    
    public void UpgradeMaxHealth()
    {
        if (selectedTower != null && SpendCurrency(maxHealthCost))
        {
            selectedTower.UpgradeMaxHealth();
            UpdateUI();
        }
    }
    
    public void UpgradeDamageResistance()
    {
        if (selectedTower != null && SpendCurrency(resistanceCost))
        {
            selectedTower.UpgradeDamageResistance(0.1f); // 10% increase
            UpdateUI();
        }
    }
    
    public void CloseUpgradeMenu()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
        selectedTower = null;
        
        // Only re-lock cursor if not in web build (web builds handle cursor differently)
        #if !UNITY_WEBGL
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        #else
        // For web builds, keep cursor visible but unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        #endif
    }
    
    // Alternative close method that can be called from UI buttons
    public void OnCloseButtonPressed()
    {
        CloseUpgradeMenu();
    }
    
    // Currency system methods
    bool HasEnoughCurrency(int cost)
    {
        return CurrencyManager.Instance != null && CurrencyManager.Instance.HasEnoughCurrency(cost);
    }
    
    bool SpendCurrency(int amount)
    {
        return CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCurrency(amount);
    }
    
    // Currency event handler
    private void OnCurrencyChanged(int newAmount)
    {
        // Update currency display immediately when currency changes
        UpdateCurrencyDisplay();
        
        // Update button states as well since currency affects what can be purchased
        if (selectedTower != null)
        {
            UpdateButtonStates();
        }
    }
    
    // Dedicated method to update currency display (like GameHUD)
    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null && CurrencyManager.Instance != null)
        {
            currencyText.text = $"Gold: {CurrencyManager.Instance.GetCurrentCurrency()}";
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged.RemoveListener(OnCurrencyChanged);
        }
    }
}
