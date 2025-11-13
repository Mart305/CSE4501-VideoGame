using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState
{
    MainMenu,
    Playing,
    Paused
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    
    [Header("UI References")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private MainMenuManager mainMenuManager;
    [SerializeField] private VictoryPanel victoryPanel;
    [SerializeField] private DefeatPanel defeatPanel;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Make sure other key managers persist too
            if (WaveManager.Instance != null)
                DontDestroyOnLoad(WaveManager.Instance.gameObject);
            if (CurrencyManager.Instance != null)
                DontDestroyOnLoad(CurrencyManager.Instance.gameObject);
            
            // Apply WebGL-specific fixes
            #if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[GameStateManager] WebGL platform detected - applying fixes");
            ApplyWebGLFixes();
            #endif
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // This should only run once since we use DontDestroyOnLoad
        
        // Find UI components in the scene
        if (mainMenuManager == null)
            mainMenuManager = FindObjectOfType<MainMenuManager>();
        
        // Initialize WebGL player animator fixes
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(InitializeWebGLPlayerAnimator());
        #endif
        
        // Fix terrain basemaps on all platforms (fixes white spots at distance)
        FixTerrainBasemaps();
            
        GameOverUI gameOverUI = FindObjectOfType<GameOverUI>();
        if (gameOverUI != null)
        {
            pauseUI = gameOverUI.GetPausePanel();
        }
        
        // Hide GameHUD ONLY if we're in main menu (not during gameplay)
        if (currentState == GameState.MainMenu)
        {
            GameObject gameHUDCanvas = GameObject.Find("GameHUDCanvas");
            if (gameHUDCanvas != null)
            {
                gameHUDCanvas.SetActive(false);
            }
            else
            {
                // Fallback to finding by component
                GameHUD gameHUD = FindObjectOfType<GameHUD>();
                if (gameHUD != null)
                {
                    gameHUD.gameObject.SetActive(false);
                }
            }
        }
        
        // Initialize UI state
        if (pauseUI != null) pauseUI.SetActive(false);
        
        // Show main menu if starting in main menu state
        if (currentState == GameState.MainMenu && mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }
        
    }
    
    void Update()
    {
        // Pause is now handled by PauseMenuManager using ESC key only
        // Game over input handling can be added later if needed
    }
    
    
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        
        if (pauseUI != null)
        {
            pauseUI.SetActive(true);
        }
        
        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }
        
        // Re-lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void StartGameFromMenu()
    {
        // Prevent double execution
        if (currentState == GameState.Playing)
        {
            return;
        }
        
        // CRITICAL: Reset all game state for fresh start
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        // Reset tower costs to base values
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.ResetTowerCosts();
        }
        
        // Force WaveManager to store these reset costs as base costs
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ForceStoreBaseTowerCosts();
        }
        
        // Hide main menu before moving to DontDestroyOnLoad
        if (mainMenuManager != null)
        {
            mainMenuManager.HideMainMenu();
        }
        
        // Clear any existing enemies from previous game
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        
        // Re-enable GameHUD if it was hidden
        GameObject gameHUDCanvas = GameObject.Find("GameHUDCanvas");
        if (gameHUDCanvas != null)
        {
            gameHUDCanvas.SetActive(true);
        }
        
        // Move manager objects to DontDestroyOnLoad before scene transition
        MoveManagersToDontDestroyOnLoad();
        
        // Start the gameplay through WaveManager (this loads the gameplay scene)
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StartGameplay();
        }
        
        // IMMEDIATELY ensure there's an active AudioListener (before scene fully loads)
        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
        bool hasActiveListener = false;
        foreach (AudioListener listener in listeners)
        {
            if (listener.enabled && listener.gameObject.activeInHierarchy)
            {
                hasActiveListener = true;
                break;
            }
        }
        
        if (!hasActiveListener && listeners.Length > 0)
        {
            // Enable the first AudioListener we find
            listeners[0].enabled = true;
            if (!listeners[0].gameObject.activeInHierarchy)
            {
                listeners[0].gameObject.SetActive(true);
            }
        }
        
        // Start coroutine to setup game after scene loads
        StartCoroutine(SetupGameAfterSceneLoad());
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private IEnumerator SetupGameAfterSceneLoad()
    {
        // Wait for scene to load (WaveManager loads it asynchronously)
        // If scene is already loaded (returning from menu), this still gives time for setup
        yield return new WaitForSeconds(0.5f);
        
        // Find GameHUD (including inactive objects since it might be hidden)
        GameObject gameHUDCanvas = null;
        GameHUD gameHUD = FindObjectOfType<GameHUD>(true); // true = include inactive
        
        if (gameHUD != null)
        {
            gameHUDCanvas = gameHUD.gameObject;
        }
        else
        {
            // Fallback to GameObject.Find (only finds active objects)
            gameHUDCanvas = GameObject.Find("GameHUDCanvas");
        }
        
        if (gameHUDCanvas != null)
        {
            if (!gameHUDCanvas.activeSelf)
            {
                gameHUDCanvas.SetActive(true);
            }
            
            // Wait one frame for GameHUD to initialize
            yield return null;
            
            // Reset currency AFTER GameHUD is active
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.ResetCurrency();
            }
            
            // Force UI update to show correct tower costs
            if (GameHUD.Instance != null && TowerPlacementManager.Instance != null)
            {
                GameHUD.Instance.InitializeTowerButtons();
                
                // Wait another frame and force update again to ensure costs are correct
                yield return null;
                yield return null; // Extra frame for safety
                
                if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.InitializeTowerButtons();
                }
            }
        }
    }
    
    private void MoveManagersToDontDestroyOnLoad()
    {
        // Get all root objects in the ManagerScene
        Scene managerScene = SceneManager.GetSceneByName("ManagerScene");
        if (managerScene.IsValid())
        {
            GameObject[] rootObjects = managerScene.GetRootGameObjects();
            int movedCount = 0;
            
            foreach (GameObject obj in rootObjects)
            {
                // Skip DirectionalLight - let the gameplay scene handle lighting
                Light light = obj.GetComponent<Light>();
                if (light != null && light.type == LightType.Directional)
                {
                    continue;
                }
                
                // Skip Camera - let the gameplay scene handle cameras
                Camera camera = obj.GetComponent<Camera>();
                if (camera != null)
                {
                    continue;
                }
                
                // Skip AudioListener - let the gameplay scene handle audio
                AudioListener audioListener = obj.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    continue;
                }
                
                // Skip EnemySpawner - let each scene have its own spawn points
                EnemySpawner enemySpawner = obj.GetComponent<EnemySpawner>();
                if (enemySpawner != null)
                {
                    continue;
                }
                
                // Skip Terrain - let each scene have its own terrain
                Terrain terrain = obj.GetComponent<Terrain>();
                if (terrain != null)
                {
                    continue;
                }
                
                // Move other objects to DontDestroyOnLoad
                DontDestroyOnLoad(obj);
                movedCount++;
            }
            
        }
    }
    
    public void ReturnToMainMenu()
    {
        currentState = GameState.MainMenu;
        Time.timeScale = 1f; // Unfreeze time temporarily
        
        // Stop wave system
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StopAllCoroutines();
        }
        
        // Clear all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        
        // Clear all placed towers
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.ClearPlacedTowers();
        }
        
        // Hide GameHUD when returning to main menu
        GameObject gameHUDCanvas = GameObject.Find("GameHUDCanvas");
        if (gameHUDCanvas != null)
        {
            // Stop all coroutines in GameHUD before hiding
            GameHUD gameHUD = gameHUDCanvas.GetComponent<GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.StopAllCoroutines();
            }
            gameHUDCanvas.SetActive(false);
        }
        
        // Show main menu
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }
        
        // Hide pause UI
        if (pauseUI != null) pauseUI.SetActive(false);
        
        // Freeze time for main menu
        Time.timeScale = 0f;
    }
    
    public void QuitGame()
    {
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // Victory and Defeat handling
    public void ShowVictory(int enemiesDefeated)
    {
        if (victoryPanel != null)
        {
            victoryPanel.ShowVictory(enemiesDefeated);
        }
    }
    
    public void ShowDefeat()
    {
        if (defeatPanel != null)
        {
            defeatPanel.ShowDefeat();
        }
    }
    
    // Public getters
    public GameState GetCurrentState() => currentState;
    public bool IsGameActive() => currentState == GameState.Playing;
    
    // ===== WebGL-Specific Fixes =====
    #if UNITY_WEBGL && !UNITY_EDITOR
    
    private void ApplyWebGLFixes()
    {
        // Fix terrain quality settings for WebGL
        AdjustTerrainQualityForWebGL();
        
        // Log current quality level
        int currentQuality = QualitySettings.GetQualityLevel();
        Debug.Log($"[GameStateManager] WebGL Quality Level: {currentQuality}");
    }
    
    private IEnumerator InitializeWebGLPlayerAnimator()
    {
        // Wait for scene to fully load
        yield return new WaitForSeconds(0.5f);
        
        // Try multiple times to find and fix the player animator
        for (int attempt = 0; attempt < 5; attempt++)
        {
            // Find player GameObject
            GameObject playerArmature = GameObject.FindGameObjectWithTag("Player");
            if (playerArmature == null)
            {
                playerArmature = GameObject.Find("PlayerArmature");
            }
            
            if (playerArmature != null)
            {
                Animator playerAnimator = playerArmature.GetComponent<Animator>();
                
                if (playerAnimator != null)
                {
                    FixAnimatorForWebGL(playerAnimator);
                    
                    // Also check for child animators
                    Animator[] childAnimators = playerArmature.GetComponentsInChildren<Animator>();
                    foreach (Animator childAnim in childAnimators)
                    {
                        if (childAnim != playerAnimator)
                        {
                            FixAnimatorForWebGL(childAnim);
                        }
                    }
                    
                    yield break; // Success, exit coroutine
                }
                else
                {
                    Debug.LogWarning($"[GameStateManager] Player Animator not found on attempt {attempt + 1}");
                }
            }
            else
            {
                Debug.LogWarning($"[GameStateManager] PlayerArmature not found on attempt {attempt + 1}");
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        Debug.LogError("[GameStateManager] Failed to find PlayerArmature after 5 attempts!");
    }
    
    private void FixAnimatorForWebGL(Animator animator)
    {
        if (animator == null) return;
        
        Debug.Log($"[GameStateManager] Applying animator fixes for WebGL on {animator.gameObject.name}");
        
        // Disable first to reset state
        animator.enabled = false;
        
        // Force animator to always animate mode (prevents culling issues in WebGL)
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        
        // Set update mode to normal (not AnimatePhysics which can cause issues in WebGL)
        animator.updateMode = AnimatorUpdateMode.Normal;
        
        // Re-enable animator
        animator.enabled = true;
        
        // Force animator to update immediately
        animator.Update(0f);
        
        // Rebind animator to refresh all bindings (fixes WebGL state issues)
        animator.Rebind();
        
        // Force play the default state
        if (animator.runtimeAnimatorController != null && animator.layerCount > 0)
        {
            // Get the default state from layer 0
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(stateInfo.fullPathHash, 0, 0f);
        }
        
        Debug.Log($"[GameStateManager] Animator fixed: {animator.gameObject.name} - culling=AlwaysAnimate, updateMode=Normal, rebound");
    }
    
    private void AdjustTerrainQualityForWebGL()
    {
        // Find all terrains in the scene
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        
        if (terrains.Length == 0)
        {
            Debug.Log("[GameStateManager] No terrains found in scene");
            return;
        }
        
        foreach (Terrain terrain in terrains)
        {
            if (terrain != null)
            {
                // Adjust terrain settings for better WebGL performance and rendering
                terrain.heightmapPixelError = 5; // Slightly higher for WebGL (default is 1)
                terrain.basemapDistance = 500; // Reduce from 1000 for WebGL
                terrain.detailObjectDistance = 60; // Reduce from 80 for WebGL
                terrain.treeDistance = 2000; // Reduce from 5000 for WebGL
                terrain.treeBillboardDistance = 50;
                terrain.treeCrossFadeLength = 5;
                terrain.treeMaximumFullLODCount = 50;
                
                // Force terrain to refresh its material
                if (terrain.materialTemplate != null)
                {
                    Material terrainMat = terrain.materialTemplate;
                    
                    // Enable keywords that might be disabled in WebGL
                    if (terrainMat.HasProperty("_MainTex"))
                    {
                        terrainMat.EnableKeyword("_NORMALMAP");
                    }
                    
                    // Force shader to refresh
                    terrain.materialTemplate = terrainMat;
                    terrain.Flush();
                }
                
                Debug.Log($"[GameStateManager] Adjusted terrain quality for WebGL: {terrain.name}");
            }
        }
        
        // Also adjust global terrain quality settings
        QualitySettings.terrainPixelError = 5;
        QualitySettings.terrainDetailDensityScale = 0.8f;
        QualitySettings.terrainBasemapDistance = 500;
        QualitySettings.terrainDetailDistance = 60;
        QualitySettings.terrainTreeDistance = 2000;
        
        Debug.Log("[GameStateManager] Applied global terrain quality settings for WebGL");
    }
    
    
    #endif
    // ===== End WebGL-Specific Fixes =====
    
    // ===== Terrain Basemap Fix (All Platforms) =====
    // This fixes white spots on terrain at distance in both editor and builds
    private void FixTerrainBasemaps()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        
        foreach (Terrain terrain in terrains)
        {
            if (terrain != null && terrain.terrainData != null)
            {
                // Increase basemap distance to prevent white spots and compression artifacts
                terrain.basemapDistance = 2000f; // Increased from 1000 to reduce compression
                
                // Set lower pixel error for better quality (lower = higher quality)
                terrain.heightmapPixelError = 1f;
                
                // Enable draw instanced for better performance
                terrain.drawInstanced = true;
                
                // Increase heightmap resolution if it looks too compressed
                // This doesn't change the actual heightmap, just how it's rendered
                terrain.heightmapMaximumLOD = 0; // 0 = highest quality, no LOD reduction
                
                // Set detail settings for better quality
                terrain.detailObjectDensity = 1.0f; // Maximum detail density
                terrain.detailObjectDistance = 80f; // How far to render details
                
                // Force terrain to regenerate basemap with higher quality
                terrain.terrainData.SetBaseMapDirty();
                terrain.Flush();
                
                Debug.Log($"[GameStateManager] Fixed terrain basemap for: {terrain.name} (basemapDistance: 2000, heightmapMaxLOD: 0)");
            }
        }
        
        // Adjust global quality settings to reduce compression
        QualitySettings.terrainBasemapDistance = 2000f;
        QualitySettings.terrainPixelError = 1f;
        QualitySettings.terrainDetailDensityScale = 1.0f;
        
        // Disable texture mipmap limiting which can cause compression artifacts
        QualitySettings.globalTextureMipmapLimit = 0; // 0 = full resolution, no mipmap reduction
        
        // Enable anisotropic filtering for better terrain texture quality at angles
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        
        Debug.Log("[GameStateManager] Applied high-quality terrain settings to reduce compression (mipmap limit: 0, anisotropic: enabled)");
    }
}
