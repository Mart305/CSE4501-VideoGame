using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("Tower Association")]
    [SerializeField] private BaseTower associatedTower; // The specific tower this UI belongs to
    
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
    
    [Header("UI Positioning")]
    [SerializeField] private float uiRadius = 10f; // Distance from tower center
    [SerializeField] private float uiHeightOffset = 10f; // Height offset from tower center
    
    private BaseTower selectedTower;
    private Camera playerCamera;
    
    void Start()
    {   
        // If no tower is assigned, try to find one on the same GameObject or parent
        if (associatedTower == null)
        {
            associatedTower = GetComponent<BaseTower>();
            if (associatedTower == null)
            {
                associatedTower = GetComponentInParent<BaseTower>();
            }
        }
        
        playerCamera = GetActiveCamera();
            
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
    
    Camera GetActiveCamera()
    {
        // Find all active cameras and return the one that's enabled
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                return cam;
            }
        }
        // Fallback to Camera.main
        return Camera.main;
    }
    
    void Update()
    {
		// Re-acquire camera if it was destroyed or disabled (scene changed or camera mode switched)
		if (playerCamera == null || !playerCamera.enabled)
			playerCamera = GetActiveCamera();

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
        
        // Position panel around tower and make it face camera while open
        if (upgradePanel != null && upgradePanel.activeInHierarchy && playerCamera != null && associatedTower != null)
        {
            PositionUIAroundTower();
        }
    }
    
    public void ShowUpgradePanel(BaseTower tower)
    {
        selectedTower = tower;
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
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
                    BaseTower clickedTower = hit.collider.GetComponent<BaseTower>();
                    
                    // Only respond if the clicked tower is THIS tower's associated tower
                    if (clickedTower != null && clickedTower == associatedTower)
                    {
                        SelectTower(clickedTower);
                    }
                    else if (upgradePanel != null && upgradePanel.activeInHierarchy)
                    {
                        // Different tower was clicked, close this UI
                        CloseUpgradeMenu();
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
    
    void SelectTower(BaseTower tower)
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
            
            // Position UI around tower based on player position
            if (selectedTower != null && playerCamera != null && associatedTower != null)
            {   
                PositionUIAroundTower();
            }
        }
    }
    
    void PositionUIAroundTower()
    {
        // Safety checks
        if (associatedTower == null || playerCamera == null || upgradePanel == null)
            return;
            
        // Get the canvas (parent of upgradePanel)
        Transform canvas = upgradePanel.transform.parent;
        if (canvas == null)
            return;
            
        // Get tower center position (world space)
        Vector3 towerCenter = associatedTower.transform.position;
        
        // Get direction from tower to player camera (on horizontal plane only)
        Vector3 directionToPlayer = playerCamera.transform.position - towerCenter;
        directionToPlayer.y = 0; // Flatten to horizontal plane (ignore height difference)
        
        // If camera is directly above, use forward direction
        if (directionToPlayer.magnitude < 0.1f)
        {
            directionToPlayer = Vector3.forward;
        }
        else
        {
            directionToPlayer.Normalize();
        }
        
        // Position the canvas (not the panel) relative to tower
        // Canvas is a child of tower, so use local position
        Vector3 localPosition = directionToPlayer * uiRadius;
        localPosition.y = uiHeightOffset;
        
        canvas.localPosition = localPosition;
        
        // Make canvas face the camera (world rotation)
        canvas.rotation = Quaternion.LookRotation(-directionToPlayer);
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
            
            // Visual feedback for insufficient funds
            if (repairButtonText != null)
            {
                if (!hasEnoughCurrency && canRepair)
                {
                    repairButtonText.color = Color.red;
                }
                else
                {
                    repairButtonText.color = Color.black;
                }
            }
        }
        
        // Max health button - disable if not enough currency
        if (maxHealthButton != null)
        {
            bool hasEnoughCurrency = HasEnoughCurrency(maxHealthCost);
            maxHealthButton.interactable = hasEnoughCurrency;
            
            // Visual feedback for insufficient funds
            if (maxHealthButtonText != null)
            {
                maxHealthButtonText.color = hasEnoughCurrency ? Color.black : Color.red;
            }
        }
        
        // Resistance button - disable if at max resistance or not enough currency
        if (damageResistanceButton != null)
        {
            bool canUpgrade = selectedTower.GetDamageResistance() < 0.8f;
            bool hasEnoughCurrency = HasEnoughCurrency(resistanceCost);
            damageResistanceButton.interactable = canUpgrade && hasEnoughCurrency;
            
            // Visual feedback for insufficient funds
            if (resistanceButtonText != null)
            {
                if (!hasEnoughCurrency && canUpgrade)
                {
                    resistanceButtonText.color = Color.red;
                }
                else if (!canUpgrade)
                {
                    resistanceButtonText.color = Color.gray;
                }
                else
                {
                    resistanceButtonText.color = Color.black;
                }
            }
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