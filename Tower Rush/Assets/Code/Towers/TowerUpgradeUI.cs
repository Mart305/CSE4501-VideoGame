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
            closeButton.onClick.AddListener(CloseUpgradeMenu);
    }
    
    void Update()
    {
        CheckTowerSelection();
        
        // Unlock cursor when upgrade panel is open
        if (upgradePanel != null && upgradePanel.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
                // Fixed position for 1x1 canvas
                upgradePanel.transform.position = new Vector3(0, 2, 0);
                
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
        
        // Enable/disable buttons based on conditions
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        if (selectedTower == null) return;
        
        // Repair button - disable if at full health
        if (repairButton != null)
        {
            bool canRepair = selectedTower.GetCurrentHealth() < selectedTower.GetMaxHealth();
            repairButton.interactable = canRepair; // && HasEnoughGold(repairCost);
        }
        
        // Max health button - always available (could add level cap)
        if (maxHealthButton != null)
        {
            maxHealthButton.interactable = true; // && HasEnoughGold(maxHealthCost);
        }
        
        // Resistance button - disable if at max resistance
        if (damageResistanceButton != null)
        {
            bool canUpgrade = selectedTower.GetDamageResistance() < 0.8f;
            damageResistanceButton.interactable = canUpgrade; // && HasEnoughGold(resistanceCost);
        }
    }
    
    public void RepairTower()
    {
        if (selectedTower != null)
        {
            selectedTower.RepairTower();
            UpdateUI();
        }
    }
    
    public void UpgradeMaxHealth()
    {
        if (selectedTower != null)
        {
            selectedTower.UpgradeMaxHealth();
            UpdateUI();
        }
    }
    
    public void UpgradeDamageResistance()
    {
        if (selectedTower != null)
        {
            selectedTower.UpgradeDamageResistance(0.1f); // 10% increase
            UpdateUI();
        }
    }
    
    public void CloseUpgradeMenu()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
        selectedTower = null;
        
        // Re-lock cursor when closing UI
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // TODO: Implement gold/currency system
    // bool HasEnoughGold(int cost)
    // {
    //     return GameManager.Instance.GetGold() >= cost;
    // }
    
    // void SpendGold(int amount)
    // {
    //     GameManager.Instance.SpendGold(amount);
    // }
}
