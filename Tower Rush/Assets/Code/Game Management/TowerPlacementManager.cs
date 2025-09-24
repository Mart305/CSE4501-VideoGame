using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class TowerData
{
    [Header("Tower Info")]
    public string towerName;
    public GameObject towerPrefab;
    public Sprite towerIcon;
    public int cost;
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description;
}

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }
    
    [Header("Tower Prefabs")]
    [SerializeField] private List<TowerData> availableTowers = new List<TowerData>();
    
    [Header("Placement Settings")]
    [SerializeField] private LayerMask groundLayer = 1; // What layer is considered "ground"
    [SerializeField] private float placementRange = 50f; // Max distance from camera to place towers
    [SerializeField] private Material previewMaterial; // Material for tower preview
    
    [Header("Placement Validation")]
    [SerializeField] private float minDistanceBetweenTowers = 3f;
    [SerializeField] private bool canPlaceOnEnemies = false;
    
    [Header("Events")]
    public UnityEvent<GameObject> OnTowerPlaced;
    public UnityEvent<TowerData> OnTowerSelected;
    public UnityEvent OnPlacementCancelled;
    
    private Camera playerCamera;
    private TowerData selectedTowerData;
    private GameObject previewTower;
    private bool isPlacingTower = false;
    private List<GameObject> placedTowers = new List<GameObject>();
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
            
        // Initialize events
        if (OnTowerPlaced == null)
            OnTowerPlaced = new UnityEvent<GameObject>();
        if (OnTowerSelected == null)
            OnTowerSelected = new UnityEvent<TowerData>();
        if (OnPlacementCancelled == null)
            OnPlacementCancelled = new UnityEvent();
    }
    
    void Update()
    {
        if (isPlacingTower)
        {
            HandleTowerPlacement();
        }
    }
    
    public void StartPlacingTower(int towerIndex)
    {
        if (towerIndex < 0 || towerIndex >= availableTowers.Count)
        {
            Debug.LogError($"Invalid tower index: {towerIndex}");
            return;
        }
        
        // Cancel any existing placement first
        if (isPlacingTower)
        {
            CancelPlacement();
        }
        
        TowerData towerData = availableTowers[towerIndex];
        
        // Check if player has enough currency
        if (CurrencyManager.Instance != null && !CurrencyManager.Instance.HasEnoughCurrency(towerData.cost))
        {
            Debug.Log($"Not enough currency to place {towerData.towerName}. Need {towerData.cost}, have {CurrencyManager.Instance.GetCurrentCurrency()}");
            return;
        }
        
        StartPlacingTower(towerData);
    }
    
    public void StartPlacingTower(TowerData towerData)
    {
        selectedTowerData = towerData;
        isPlacingTower = true;
        
        // Create preview tower
        CreatePreviewTower();
        
        // Notify listeners
        OnTowerSelected?.Invoke(towerData);
        
    }
    
    public void CancelPlacement()
    {
        if (previewTower != null)
        {
            Destroy(previewTower);
        }
        
        isPlacingTower = false;
        selectedTowerData = null;
        
        OnPlacementCancelled?.Invoke();
        
        Debug.Log("Tower placement cancelled");
    }
    
    private void HandleTowerPlacement()
    {
        // Cancel placement with ESC or right click
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }
        
        // Update preview position
        UpdatePreviewPosition();
        
        // Place tower with left click
        if (Input.GetMouseButtonDown(0))
        {
            // Don't place if clicking on UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            TryPlaceTower();
        }
    }
    
    private void CreatePreviewTower()
    {
        if (selectedTowerData?.towerPrefab != null)
        {
            // Instantiate and immediately deactivate to prevent any detection
            previewTower = Instantiate(selectedTowerData.towerPrefab);
            previewTower.SetActive(false);
            
            // IMMEDIATELY disable components before any enemy can detect it
            DisableAllGameplayComponentsImmediate(previewTower);
            
            // Make it semi-transparent
            MakePreviewTransparent(previewTower);
            
            // Change layer to prevent interactions
            SetLayerRecursively(previewTower, LayerMask.NameToLayer("Ignore Raycast"));
            
            // Reactivate only after all modifications are complete
            previewTower.SetActive(true);
            
        }
    }
    
    private void DisableAllGameplayComponentsImmediate(GameObject obj)
    {
        // FIRST: Remove tags immediately to prevent enemy detection
        RemoveAllTowerTags(obj);
        
        // SECOND: Disable colliders immediately
        DisableAllColliders(obj);
        
        // THIRD: Disable all gameplay components
        DisableAllGameplayComponents(obj);
        
    }
    
    private void RemoveAllTowerTags(GameObject obj)
    {
        // Remove Tower tag from main object
        if (obj.CompareTag("Tower"))
        {
            obj.tag = "Untagged";
        }
        
        // Remove Tower tag from all children recursively
        Transform[] allChildren = obj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.CompareTag("Tower"))
            {
                child.tag = "Untagged";
            }
        }
    }
    
    private void DisableAllColliders(GameObject obj)
    {
        // Disable all colliders immediately
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }
    
    private void DisableAllGameplayComponents(GameObject obj)
    {
        // COMPLETELY DESTROY the Tower component so FindObjectsOfType<Tower>() can't find it
        Tower towerComponent = obj.GetComponent<Tower>();
        if (towerComponent != null)
        {
            DestroyImmediate(towerComponent);
        }
        
        // Also destroy Tower components from all children
        Tower[] allTowerComponents = obj.GetComponentsInChildren<Tower>();
        foreach (Tower tower in allTowerComponents)
        {
            if (tower != null)
            {
                DestroyImmediate(tower);
            }
        }
        
        // Disable health component
        Health healthComponent = obj.GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.enabled = false;
        }
        
        // Disable all colliders
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Disable any other MonoBehaviour components that might interfere
        MonoBehaviour[] allBehaviours = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            // Skip this TowerPlacementManager
            if (behaviour == this) 
                continue;
                
            // Skip null components
            if (behaviour == null)
                continue;
                
            behaviour.enabled = false;
        }
        
        // Remove Tower tag to prevent enemy targeting
        if (obj.CompareTag("Tower"))
        {
            obj.tag = "Untagged";
        }
        
        // Apply to all children
        foreach (Transform child in obj.transform)
        {
            if (child.CompareTag("Tower"))
            {
                child.tag = "Untagged";
            }
        }
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    private void UpdatePreviewPosition()
    {
        if (previewTower == null || playerCamera == null) return;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, placementRange, groundLayer))
        {
            Vector3 targetPosition = hit.point;
            
            // Snap to grid if desired (optional)
            // targetPosition = SnapToGrid(targetPosition);
            
            previewTower.transform.position = targetPosition;
            
            // Change color based on whether placement is valid
            bool canPlace = IsValidPlacement(targetPosition);
            UpdatePreviewColor(canPlace);
        }
    }
    
    private void TryPlaceTower()
    {
        if (previewTower == null || !isPlacingTower || selectedTowerData == null) 
        {
            return;
        }
        
        Vector3 placementPosition = previewTower.transform.position;
        
        if (IsValidPlacement(placementPosition))
        {
            // Double-check currency before spending
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.HasEnoughCurrency(selectedTowerData.cost))
            {
                    CancelPlacement();
                return;
            }
            
            // Spend currency
            if (!CurrencyManager.Instance.SpendCurrency(selectedTowerData.cost))
            {
                CancelPlacement();
                return;
            }
            
            // Create actual tower
            GameObject newTower = Instantiate(selectedTowerData.towerPrefab, placementPosition, previewTower.transform.rotation);
            
            // Ensure the new tower has proper tag and components enabled
            newTower.tag = "Tower";
            
            // Add to placed towers list
            placedTowers.Add(newTower);
            
            // Notify listeners
            OnTowerPlaced?.Invoke(newTower);
            
            
            // Clean up and stop placement
            Destroy(previewTower);
            previewTower = null;
            isPlacingTower = false;
            selectedTowerData = null;
        }
        else
        {
        }
    }
    
    private bool IsValidPlacement(Vector3 position)
    {
        // Check minimum distance from other towers
        foreach (GameObject tower in placedTowers)
        {
            if (tower != null && Vector3.Distance(position, tower.transform.position) < minDistanceBetweenTowers)
            {
                return false;
            }
        }
        
        // Check if there are enemies at this position (if not allowed)
        if (!canPlaceOnEnemies)
        {
            Collider[] enemiesNearby = Physics.OverlapSphere(position, 1f);
            foreach (Collider col in enemiesNearby)
            {
                if (col.CompareTag("Enemy"))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private void MakePreviewTransparent(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (previewMaterial != null)
                {
                    materials[i] = previewMaterial;
                }
                else
                {
                    // Fallback: make existing material transparent
                    Material mat = new Material(materials[i]);
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.5f);
                    materials[i] = mat;
                }
            }
            renderer.materials = materials;
        }
    }
    
    private void UpdatePreviewColor(bool canPlace)
    {
        if (previewTower == null) return;
        
        Color targetColor = canPlace ? Color.green : Color.red;
        targetColor.a = 0.5f;
        
        Renderer[] renderers = previewTower.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].color = targetColor;
            }
            renderer.materials = materials;
        }
    }
    
    // Public getters
    public List<TowerData> GetAvailableTowers() => availableTowers;
    public bool IsCurrentlyPlacing() => isPlacingTower;
    public TowerData GetSelectedTowerData() => selectedTowerData;
}
