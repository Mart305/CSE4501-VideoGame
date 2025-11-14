using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using StarterAssets;

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
            
            // WebGL: Ensure PlayerArmature components are active immediately
            #if UNITY_WEBGL && !UNITY_EDITOR
            EnsurePlayerComponentsActive();
            #endif
            
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
    
    private void EnsurePlayerComponentsActive()
    {
        // Find PlayerArmature immediately
        GameObject playerArmature = GameObject.FindGameObjectWithTag("Player");
        if (playerArmature == null)
        {
            playerArmature = GameObject.Find("PlayerArmature");
        }
        if (playerArmature == null)
        {
            ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
            if (controller != null)
            {
                playerArmature = controller.gameObject;
            }
        }
        
        if (playerArmature != null)
        {
            // Ensure all components are active
            Animator animator = playerArmature.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                Debug.Log("[GameStateManager] Awake: Enabled Animator on PlayerArmature");
            }
            
            ThirdPersonController tpc = playerArmature.GetComponent<ThirdPersonController>();
            if (tpc != null)
            {
                tpc.enabled = true;
                Debug.Log("[GameStateManager] Awake: Enabled ThirdPersonController");
            }
            
            CharacterController cc = playerArmature.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = true;
                Debug.Log("[GameStateManager] Awake: Enabled CharacterController");
            }
        }
        else
        {
            Debug.LogWarning("[GameStateManager] Awake: Could not find PlayerArmature immediately");
        }
    }
    
    private void ApplyWebGLFixes()
    {
        // Log current quality level
        int currentQuality = QualitySettings.GetQualityLevel();
        Debug.Log($"[GameStateManager] WebGL Quality Level: {currentQuality}");
    }
    
    private IEnumerator InitializeWebGLPlayerAnimator()
    {
        // Try immediately first, then retry if needed
        yield return null; // Wait one frame for scene to load
        
        // Try multiple times to find and fix the player animator
        for (int attempt = 0; attempt < 10; attempt++) // Increased from 5 to 10 attempts
        {
            // Find player GameObject using multiple methods
            GameObject playerArmature = GameObject.FindGameObjectWithTag("Player");
            if (playerArmature == null)
            {
                playerArmature = GameObject.Find("PlayerArmature");
            }
            if (playerArmature == null)
            {
                // Try finding by component
                ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
                if (controller != null)
                {
                    playerArmature = controller.gameObject;
                }
            }
            
            if (playerArmature != null)
            {
                // Ensure all components are enabled
                Animator playerAnimator = playerArmature.GetComponent<Animator>();
                if (playerAnimator != null) playerAnimator.enabled = true;
                
                ThirdPersonController tpc = playerArmature.GetComponent<ThirdPersonController>();
                if (tpc != null) tpc.enabled = true;
                
                CharacterController cc = playerArmature.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
                
                if (playerAnimator != null)
                {
                    // Check if CameraModeManager exists and ensure we're in third person mode
                    CameraModeManager cameraManager = FindObjectOfType<CameraModeManager>();
                    if (cameraManager != null)
                    {
                        Debug.Log("[GameStateManager] Coroutine: Found CameraModeManager - ensuring third person mode for animator");
                    }
                    
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
                    
                    // Keep monitoring and re-fixing for the first few seconds
                    StartCoroutine(ContinuousAnimatorMonitoring(playerAnimator));
                    
                    Debug.Log("[GameStateManager] Coroutine: Successfully enabled all PlayerArmature components");
                    yield break; // Success, exit coroutine
                }
                else
                {
                    Debug.LogWarning($"[GameStateManager] Coroutine: Player Animator not found on attempt {attempt + 1}");
                }
            }
            else
            {
                Debug.LogWarning($"[GameStateManager] PlayerArmature not found on attempt {attempt + 1}");
            }
            
            yield return new WaitForSeconds(0.3f);
        }
        
        Debug.LogError("[GameStateManager] Failed to find PlayerArmature after 10 attempts!");
    }
    
    private IEnumerator ContinuousAnimatorMonitoring(Animator animator)
    {
        // Monitor continuously throughout the entire game
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check every half second
            
            if (animator != null && animator.gameObject.activeInHierarchy)
            {
                // Check if we're in RTS mode - if so, don't re-enable components
                CameraModeManager cameraManager = FindObjectOfType<CameraModeManager>();
                bool isRTSMode = cameraManager != null && cameraManager.IsRTSMode();
                
                // Only re-enable components if NOT in RTS mode
                if (!isRTSMode)
                {
                    // Ensure all PlayerArmature components stay active
                    GameObject playerArmature = animator.gameObject;
                    
                    // Check and re-enable Animator
                    if (!animator.enabled)
                    {
                        animator.enabled = true;
                        Debug.LogWarning("[GameStateManager] Re-enabled Animator - it was disabled!");
                    }
                    
                    // Check and re-enable ThirdPersonController
                    ThirdPersonController tpc = playerArmature.GetComponent<ThirdPersonController>();
                    if (tpc != null && !tpc.enabled)
                    {
                        tpc.enabled = true;
                        Debug.LogWarning("[GameStateManager] Re-enabled ThirdPersonController - it was disabled!");
                    }
                    
                    // Check and re-enable CharacterController
                    CharacterController cc = playerArmature.GetComponent<CharacterController>();
                    if (cc != null && !cc.enabled)
                    {
                        cc.enabled = true;
                        Debug.LogWarning("[GameStateManager] Re-enabled CharacterController - it was disabled!");
                    }
                }
            }
            else
            {
                // Player was destroyed or deactivated, stop monitoring
                Debug.LogWarning("[GameStateManager] Player no longer active, stopping monitoring");
                yield break;
            }
        }
    }
    
    private void FixAnimatorForWebGL(Animator animator)
    {
        if (animator == null) return;
        
        Debug.Log($"[GameStateManager] Applying animator fixes for WebGL on {animator.gameObject.name} (currently enabled: {animator.enabled})");
        
        // CRITICAL: Force enable first - something may be disabling it
        animator.enabled = true;
        
        // Wait a frame to ensure it's actually enabled
        UnityEngine.Object.DontDestroyOnLoad(animator.gameObject);
        
        // Disable first to reset state
        animator.enabled = false;
        
        // Force animator to always animate mode (prevents culling issues in WebGL)
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        
        // Set update mode to normal (not AnimatePhysics which can cause issues in WebGL)
        animator.updateMode = AnimatorUpdateMode.Normal;
        
        // Ensure the animator controller is assigned
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"[GameStateManager] Animator on {animator.gameObject.name} has no RuntimeAnimatorController!");
        }
        
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
        
        Debug.Log($"[GameStateManager] Animator fixed: {animator.gameObject.name} - enabled: {animator.enabled}, culling=AlwaysAnimate, updateMode=Normal, rebound");
    }
    
    #endif
    // ===== End WebGL-Specific Fixes =====
    
    // ===== Terrain Basemap Fix - REMOVED =====
    // No terrain quality modifications per user request
    private void FixTerrainBasemaps()
    {
        // Intentionally empty - no terrain modifications
    }
}
