using UnityEngine;
using UnityEngine.AI;
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
    [SerializeField] private float obstacleCheckRadius = 2f; // Radius to check for obstacles
    [SerializeField] private LayerMask obstacleLayers = ~8; // All layers except Ground (layer 8)
    [SerializeField] private bool requireNavMeshAccess = true; // Ensure enemies can reach the tower
    
    [Header("Events")]
    public UnityEvent<GameObject> OnTowerPlaced;
    public UnityEvent<TowerData> OnTowerSelected;
    public UnityEvent OnPlacementCancelled;

	[Header("Tower Cost Scaling")]
	private List<int> baseTowerCosts = new List<int>(); // Not serialized - captured once in Awake
	[SerializeField] private bool costScalingEnabled = true;
	private int currentSceneTier = 0;

	private Camera playerCamera;
    private TowerData selectedTowerData;
    private GameObject previewTower;
    private bool isPlacingTower = false;
    private List<GameObject> placedTowers = new List<GameObject>();
    
    // Drag-based placement variables
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private TowerData dragTowerData;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // CRITICAL: Capture base costs immediately in Awake before anything can modify them
            // This happens only once when the manager is first created
            baseTowerCosts.Clear();
            foreach (var tower in availableTowers) {
                baseTowerCosts.Add(tower.cost);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

	void Start()
	{
		playerCamera = GetActiveCamera();

		// Initialize events
		if (OnTowerPlaced == null)
			OnTowerPlaced = new UnityEvent<GameObject>();
		if (OnTowerSelected == null)
			OnTowerSelected = new UnityEvent<TowerData>();
		if (OnPlacementCancelled == null)
			OnPlacementCancelled = new UnityEvent();

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

	// Add this method to apply the multiplier
	public void UpdateTowerCostsByWave(int currentWave)
	{
		if (!costScalingEnabled) return;

		// Calculate which tier we're in (0-based)
		int tier = (currentWave - 1) / 5;

		// If tier hasn't changed, don't update costs
		if (tier == currentSceneTier) return;

		// Store the new tier
		currentSceneTier = tier;

		// Ensure base costs were captured (should have been done in Awake)
		if (baseTowerCosts.Count == 0) {
			return;
		}

		// Calculate multiplier: 2^tier
		int multiplier = 1;
		for (int i = 0; i < tier; i++) {
			multiplier *= 2;
		}

		// Apply to all towers
		for (int i = 0; i < availableTowers.Count && i < baseTowerCosts.Count; i++) {
			int newCost = baseTowerCosts[i] * multiplier;
			availableTowers[i].cost = newCost;
		}

		// Update UI if GameHUD exists
		if (GameHUD.Instance != null) {
			GameHUD.Instance.InitializeTowerButtons();
		}
	}

	// Reset tower costs to base values (for new game)
	public void ResetTowerCosts()
	{
		// Reset tier to 0
		currentSceneTier = 0;

		// Restore all tower costs to their base values (captured in Awake)
		if (baseTowerCosts.Count > 0) {
			for (int i = 0; i < availableTowers.Count && i < baseTowerCosts.Count; i++) {
				availableTowers[i].cost = baseTowerCosts[i];
			}
		}

		// Update UI if GameHUD exists
		if (GameHUD.Instance != null) {
			GameHUD.Instance.InitializeTowerButtons();
		}
	}

	void Update()
    {
		// Re-acquire camera if it was destroyed or disabled (scene changed or camera mode switched)
		if (playerCamera == null || !playerCamera.enabled)
			playerCamera = GetActiveCamera();

		// Handle drag-based placement
		HandleDragPlacement();

		// Handle click-based placement
		if (isPlacingTower)
        {
            HandleTowerPlacement();
        }
    }
    
    public void StartPlacingTower(int towerIndex)
    {
        if (towerIndex < 0 || towerIndex >= availableTowers.Count)
        {
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
    
    // New method for drag-based placement from UI buttons
    public void StartDragPlacement(TowerData towerData)
    {
        // Check if player has enough currency
        if (CurrencyManager.Instance != null && !CurrencyManager.Instance.HasEnoughCurrency(towerData.cost))
        {
            return;
        }
        
        dragTowerData = towerData;
        isDragging = true;
        dragStartPosition = Input.mousePosition;
        
        // Create preview tower for dragging
        CreatePreviewTower(towerData);
        
        // Notify listeners
        OnTowerSelected?.Invoke(towerData);
    }
    
    // Public method for UI buttons to call with tower index
    public void StartDragPlacement(int towerIndex)
    {
        if (towerIndex < 0 || towerIndex >= availableTowers.Count)
        {
            return;
        }
        
        StartDragPlacement(availableTowers[towerIndex]);
    }
    
    public void CancelPlacement()
    {
        if (previewTower != null)
        {
            Destroy(previewTower);
        }
        
        isPlacingTower = false;
        isDragging = false;
        selectedTowerData = null;
        dragTowerData = null;
        
        OnPlacementCancelled?.Invoke();
    }
    
    public void ClearPlacedTowers()
    {
        foreach (var tower in placedTowers) {
            if (tower != null)
                Destroy(tower);
        }
        placedTowers.Clear();
    }
    
    private void HandleDragPlacement()
    {
        if (!isDragging) return;
        
        // Check for mouse button release to place tower
        if (Input.GetMouseButtonUp(0))
        {
            // Try to place the tower at current position
            if (TryPlaceTowerAtMousePosition(dragTowerData))
            {
                isDragging = false;
                dragTowerData = null;
            }
            else
            {
                // Invalid placement - cancel drag
                CancelPlacement();
            }
            return;
        }
        
        // Check for right click or escape to cancel
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }
        
        // Update preview position while dragging
        if (previewTower != null)
        {
            UpdatePreviewPosition();
        }
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
            CreatePreviewTower(selectedTowerData);
        }
    }
    
    private void CreatePreviewTower(TowerData towerData)
    {
        if (towerData?.towerPrefab != null)
        {
            // Instantiate and immediately deactivate to prevent any detection
            previewTower = Instantiate(towerData.towerPrefab);
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
    
    private bool TryPlaceTowerAtMousePosition(TowerData towerData)
    {
        if (playerCamera == null || towerData == null) return false;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, placementRange, groundLayer))
        {
            Vector3 targetPosition = hit.point;
            
            // Check if placement is valid
            if (IsValidPlacement(targetPosition))
            {
                // Place the tower
                return PlaceTowerAtPosition(towerData, targetPosition);
            }
        }
        
        return false;
    }
    
    private bool PlaceTowerAtPosition(TowerData towerData, Vector3 position)
    {
        // Double-check currency before spending
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.HasEnoughCurrency(towerData.cost))
        {
            return false;
        }
        
        // Spend currency
        if (!CurrencyManager.Instance.SpendCurrency(towerData.cost))
        {
            return false;
        }
        
        // Create actual tower
        GameObject newTower = Instantiate(towerData.towerPrefab, position, Quaternion.identity);
        
        // Ensure the new tower has proper tag and components enabled
        newTower.tag = "Tower";
        
        // Add to placed towers list
        placedTowers.Add(newTower);
        
        // Clean up preview
        if (previewTower != null)
        {
            Destroy(previewTower);
        }
        
        // Reset placement state
        isPlacingTower = false;
        isDragging = false;
        selectedTowerData = null;
        dragTowerData = null;
        
        // Notify listeners
        OnTowerPlaced?.Invoke(newTower);
        
        return true;
    }
    
    public void TryPlaceCurrentTower()
    {
        if (isDragging && dragTowerData != null)
        {
            // Try to place the tower at current mouse position
            if (TryPlaceTowerAtMousePosition(dragTowerData))
            {
                isDragging = false;
                dragTowerData = null;
            }
        }
        else if (isPlacingTower && selectedTowerData != null)
        {
            // Handle click-based placement
            TryPlaceTower();
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
        
        // Disable particle systems (they look weird in preview)
        ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            ps.gameObject.SetActive(false);
        }
        
        // Disable audio sources (no sounds in preview)
        AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in audioSources)
        {
            audio.enabled = false;
        }
        
        // Disable light components (can be distracting in preview)
        Light[] lights = obj.GetComponentsInChildren<Light>();
        foreach (Light light in lights)
        {
            light.enabled = false;
        }
        
        // Disable line renderers (laser beams shouldn't show in preview)
        LineRenderer[] lineRenderers = obj.GetComponentsInChildren<LineRenderer>();
        foreach (LineRenderer lr in lineRenderers)
        {
            lr.enabled = false;
        }
        
        // Disable trail renderers
        TrailRenderer[] trailRenderers = obj.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer tr in trailRenderers)
        {
            tr.enabled = false;
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
                
            // Skip components we want to keep for visual purposes
            // (Transform is not a MonoBehaviour, so no need to check)
                
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
            
            // Restore original materials to the new tower (in case preview materials were applied)
            RestoreOriginalMaterials(newTower);
            
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
        
        // Check for obstacles overlapping with placement position
        Collider[] obstacles = Physics.OverlapSphere(position, obstacleCheckRadius, obstacleLayers);
        foreach (Collider col in obstacles)
        {
            // Ignore towers, enemies, and ground
            if (col.CompareTag("Tower") || col.CompareTag("Enemy"))
                continue;
                
            // Check if this collider's layer is NOT the ground layer
            if (col.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                // This is an obstacle - placement is invalid
                return false;
            }
        }
        
        // Ensure the position is on/near the NavMesh so enemies can reach it
        if (requireNavMeshAccess)
        {
            NavMeshHit hit;
            // Sample the NavMesh near the placement position
            // This checks if there's valid NavMesh within 2 units (adjust as needed)
            if (!NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
            {
                // Position is not accessible via NavMesh - invalid placement
                return false;
            }
            
            // Additional check: Verify that at least one spawn point can pathfind to this position
            // This prevents placing towers in isolated areas
            if (!CanReachFromSpawnPoints(position))
            {
                return false;
            }
        }
        
        // Check if placement would be blocked by existing NavMeshObstacles
        // This prevents placing inside areas that are carved out by obstacles
        if (IsBlockedByNavMeshObstacles(position))
        {
            return false;
        }
        
        return true;
    }
    
    // Helper method to check if enemies can pathfind to this position from spawn points
    private bool CanReachFromSpawnPoints(Vector3 targetPosition)
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner == null || spawner.spawnPoints == null || spawner.spawnPoints.Length == 0)
        {
            // If no spawner found, just check NavMesh accessibility
            return true;
        }
        
        // Check if at least one spawn point can pathfind to the target
        foreach (Transform spawnPoint in spawner.spawnPoints)
        {
            if (spawnPoint == null) continue;
            
            NavMeshPath path = new NavMeshPath();
            NavMeshHit spawnHit, targetHit;
            
            // Sample NavMesh at spawn point
            if (!NavMesh.SamplePosition(spawnPoint.position, out spawnHit, 2f, NavMesh.AllAreas))
                continue;
                
            // Sample NavMesh at target position
            if (!NavMesh.SamplePosition(targetPosition, out targetHit, 2f, NavMesh.AllAreas))
                continue;
            
            // Calculate path from spawn to target
            if (NavMesh.CalculatePath(spawnHit.position, targetHit.position, NavMesh.AllAreas, path))
            {
                // Path found - position is reachable
                return true;
            }
        }
        
        // No valid path from any spawn point - position is unreachable
        return false;
    }

    // Check if position is blocked by NavMeshObstacle components
    private bool IsBlockedByNavMeshObstacles(Vector3 position)
    {
        // Check for NavMeshObstacles in the area
        NavMeshObstacle[] obstacles = FindObjectsOfType<NavMeshObstacle>();
        
        foreach (NavMeshObstacle obstacle in obstacles)
        {
            if (obstacle == null || !obstacle.gameObject.activeInHierarchy)
                continue;
                
            // Skip towers (they create obstacles but we want to check existing ones)
            if (obstacle.CompareTag("Tower"))
                continue;
            
            // Calculate distance from obstacle
            float distance = Vector3.Distance(position, obstacle.transform.position);
            
            // Get obstacle bounds
            Bounds bounds = GetObstacleBounds(obstacle);
            
            // Check if position is within obstacle bounds
            Vector3 localPos = obstacle.transform.InverseTransformPoint(position);
            if (bounds.Contains(localPos))
            {
                // Position is inside an obstacle - invalid
                return true;
            }
            
            // Also check if too close to obstacle edge (within carve radius)
            float minDistance = Mathf.Max(bounds.extents.x, bounds.extents.z);
            if (distance < minDistance + 1f) // 1f buffer
            {
                // Too close to obstacle - might cause pathfinding issues
                // (Optional: you might want to remove this check or make it less strict)
                // return true;
            }
        }
        
        return false;
    }

    // Helper to get bounds of a NavMeshObstacle
    private Bounds GetObstacleBounds(NavMeshObstacle obstacle)
    {
        Bounds bounds = new Bounds();
        
        if (obstacle.shape == NavMeshObstacleShape.Box)
        {
            bounds = new Bounds(obstacle.center, obstacle.size);
        }
        else if (obstacle.shape == NavMeshObstacleShape.Capsule)
        {
            float radius = obstacle.radius;
            float height = obstacle.height;
            bounds = new Bounds(obstacle.center, new Vector3(radius * 2, height, radius * 2));
        }
        
        return bounds;
    }
    
    // Store original materials for restoration
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    private void MakePreviewTransparent(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Store original materials
            originalMaterials[renderer] = renderer.materials;
            
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (previewMaterial != null)
                {
                    // Use assigned preview material
                    materials[i] = previewMaterial;
                }
                else
                {
                    // Create a simple transparent material for preview
                    Material previewMat = new Material(Shader.Find("Standard"));
                    previewMat.color = new Color(1f, 1f, 1f, 0.5f);
                    previewMat.SetFloat("_Mode", 3); // Transparent mode
                    previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    previewMat.SetInt("_ZWrite", 0);
                    previewMat.DisableKeyword("_ALPHATEST_ON");
                    previewMat.EnableKeyword("_ALPHABLEND_ON");
                    previewMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    previewMat.renderQueue = 3000;
                    
                    materials[i] = previewMat;
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
                Material mat = materials[i];
                
                // Try different common color property names for custom shaders
                if (mat.HasProperty("_Color"))
                {
                    mat.color = targetColor;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", targetColor);
                }
                else if (mat.HasProperty("_MainColor"))
                {
                    mat.SetColor("_MainColor", targetColor);
                }
                else if (mat.HasProperty("_Tint"))
                {
                    mat.SetColor("_Tint", targetColor);
                }
                else
                {
                    // If no color property found, replace with a simple colored material
                    Material simpleMat = new Material(Shader.Find("Standard"));
                    simpleMat.color = targetColor;
                    simpleMat.SetFloat("_Mode", 3); // Transparent mode
                    simpleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    simpleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    simpleMat.SetInt("_ZWrite", 0);
                    simpleMat.EnableKeyword("_ALPHABLEND_ON");
                    simpleMat.renderQueue = 3000;
                    
                    materials[i] = simpleMat;
                }
            }
            renderer.materials = materials;
        }
    }
    
    private void RestoreOriginalMaterials(GameObject obj)
    {
        // This ensures the placed tower uses its original materials, not preview materials
        // The new tower is instantiated fresh from prefab, so it should already have correct materials
        // This method is here for safety and future extensibility
    }
    
    // Public getters
    public List<TowerData> GetAvailableTowers() => availableTowers;
    public bool IsCurrentlyPlacing() => isPlacingTower;
    public TowerData GetSelectedTowerData() => selectedTowerData;
}
