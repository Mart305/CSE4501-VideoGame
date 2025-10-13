using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TowerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image towerIcon;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI nameText;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color disabledColor = Color.gray;
    
    [Header("Drag Settings")]
    [SerializeField] private bool enableDragPlacement = true;
    
    private TowerData towerData;
    private int towerIndex;
    private bool isDragging = false;
    private GameObject dragPreview;
    private RectTransform dragPreviewRect;
    
    void Start()
    {
        // Ensure all components are enabled
        EnsureComponentsEnabled();
        
        // Setup button click listener
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        
        // Make sure tower icon doesn't block raycasts so drag events work
        if (towerIcon != null)
        {
            towerIcon.raycastTarget = false;
        }
        
        // Canvas will be found dynamically when needed
        
        // Hide tooltip initially
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
        
        // Subscribe to currency changes to update button state
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged.AddListener(UpdateButtonState);
        }
    }
    
    void Update()
    {
        // Continuously ensure components stay enabled
        EnsureComponentsEnabled();
    }
    
    void LateUpdate()
    {
        // Also check in LateUpdate to catch any late disabling
        EnsureComponentsEnabled();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged.RemoveListener(UpdateButtonState);
        }
    }
    

    public void Initialize(TowerData data, int index)
    {
        towerData = data;
        towerIndex = index;
        
        // Update UI elements
        if (towerIcon != null && data.towerIcon != null)
        {
            towerIcon.sprite = data.towerIcon;
        }
        
        if (costText != null)
        {
            costText.text = data.cost.ToString();
        }
        
        if (nameText != null)
        {
            nameText.text = data.towerName;
        }
        
        // Update tooltip
        if (tooltipText != null)
        {
            tooltipText.text = $"{data.towerName}\nCost: {data.cost}\n{data.description}";
        }
        
        // Update button state
        UpdateButtonState(CurrencyManager.Instance?.GetCurrentCurrency() ?? 0);
    }
    
    public void OnButtonClick()
    {
        if (towerData != null && TowerPlacementManager.Instance != null)
        {
            // Start placing tower
            TowerPlacementManager.Instance.StartPlacingTower(towerIndex);
        }
    }
    
    private void UpdateButtonState(int currentCurrency)
    {
        if (towerData == null || button == null) return;
        
        bool canAfford = currentCurrency >= towerData.cost;
        
        // Update button interactability
        button.interactable = canAfford;
        
        // Update visual appearance
        Color targetColor = canAfford ? normalColor : disabledColor;
        
        if (towerIcon != null)
        {
            towerIcon.color = targetColor;
        }
        
        if (costText != null)
        {
            costText.color = canAfford ? Color.white : Color.red;
        }
    }
    
    // Hover events
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip
        if (tooltip != null)
        {
            tooltip.SetActive(true);
        }
        
        // Change color if affordable
        if (button != null && button.interactable && towerIcon != null)
        {
            towerIcon.color = hoverColor;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide tooltip
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
        
        // Reset color
        if (towerIcon != null)
        {
            UpdateButtonState(CurrencyManager.Instance?.GetCurrentCurrency() ?? 0);
        }
    }
    
    // Drag events
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDragPlacement || towerData == null || !button.interactable) return;
        
        isDragging = true;
        
        // Start the 3D tower preview system like click-to-place
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.StartDragPlacement(towerIndex);
        }
        
        // Hide tooltip during drag
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // The 3D preview is automatically handled by TowerPlacementManager
        // No need to manually update position - it follows the mouse cursor automatically
    }
    
    private void UpdateDragPreviewPosition(PointerEventData eventData)
    {
        if (dragPreviewRect == null) return;
        
        // Convert screen position to local position in canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localPoint
            );
            dragPreviewRect.localPosition = localPoint;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // Check if we dropped over a valid area (not UI)
        if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // Try to place the tower at the current mouse position
            if (TowerPlacementManager.Instance != null)
            {
                // The TowerPlacementManager should handle the placement since it's already in drag mode
                // We just need to trigger the placement attempt
                TowerPlacementManager.Instance.TryPlaceCurrentTower();
            }
        }
        else
        {
            // Cancel placement if dropped over UI
            if (TowerPlacementManager.Instance != null)
            {
                TowerPlacementManager.Instance.CancelPlacement();
            }
        }
    }
    
    // No longer needed - using 3D preview from TowerPlacementManager
    
   
    public TowerData GetTowerData() => towerData;
    
   
    public int GetTowerIndex() => towerIndex;
    
    private void EnsureComponentsEnabled()
    {
        // Ensure this GameObject is active
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // Ensure this component is enabled
        if (!enabled)
        {
            enabled = true;
        }
        
        // Ensure button component is enabled
        if (button != null)
        {
            if (!button.enabled)
            {
                button.enabled = true;
            }
            
            if (!button.gameObject.activeInHierarchy)
            {
                button.gameObject.SetActive(true);
            }
        }
        
        // Ensure tower icon is enabled
        if (towerIcon != null)
        {
            if (!towerIcon.enabled)
            {
                towerIcon.enabled = true;
            }
            
            if (!towerIcon.gameObject.activeInHierarchy)
            {
                towerIcon.gameObject.SetActive(true);
            }
        }
        
        // Ensure cost text is enabled
        if (costText != null)
        {
            if (!costText.enabled)
            {
                costText.enabled = true;
            }
            
            if (!costText.gameObject.activeInHierarchy)
            {
                costText.gameObject.SetActive(true);
            }
        }
        
        // Ensure name text is enabled
        if (nameText != null)
        {
            if (!nameText.enabled)
            {
                nameText.enabled = true;
            }
            
            if (!nameText.gameObject.activeInHierarchy)
            {
                nameText.gameObject.SetActive(true);
            }
        }
        
        // Ensure tooltip text is enabled (but don't force tooltip GameObject active)
        if (tooltipText != null && !tooltipText.enabled)
        {
            tooltipText.enabled = true;
        }
        
        // Force all child components to be enabled
        ForceEnableAllChildComponents();
    }
    
    private void ForceEnableAllChildComponents()
    {
        // Get all components in children and force enable them
        Component[] allComponents = GetComponentsInChildren<Component>(true);
        
        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;
            
            // Skip Transform and RectTransform
            if (comp is Transform || comp is RectTransform) continue;
            
            // Enable the component if it has an enabled property
            if (comp is Behaviour behaviour && !behaviour.enabled)
            {
                behaviour.enabled = true;
            }
            
            // Ensure GameObject is active
            if (!comp.gameObject.activeInHierarchy && comp.gameObject != tooltip)
            {
                comp.gameObject.SetActive(true);
            }
        }
    }
}
