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
            
            // Apply platform fixes (works for all platforms including Unity Editor)
            ApplyWebGLFixes();
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
        
        // Continuously monitor and fix animator if it gets disabled (all platforms)
        if (currentState == GameState.Playing)
        {
            MonitorPlayerAnimator();
        }
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
        
        StartCoroutine(StartGameWithFade());
    }
    
    private IEnumerator StartGameWithFade()
    {
        // Fade to black
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut(0.3f));
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
        // Explicitly move SpawnEffectManager first to preserve references
        if (SpawnEffectManager.Instance != null)
        {
            DontDestroyOnLoad(SpawnEffectManager.Instance.gameObject);
        }
        
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
        
        // Ensure PlayerArmature components are active when game starts (all platforms)
        StartCoroutine(EnsurePlayerComponentsActiveOnGameStart());
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private IEnumerator SetupGameAfterSceneLoad()
    {
        // Wait for scene to load (WaveManager loads it asynchronously)
        // If scene is already loaded (returning from menu), this still gives time for setup
        yield return new WaitForSeconds(0.5f);
        
        // Fade in from black
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeIn(0.5f));
        }
        
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

		// ===== CRITICAL: Stop and reset AudioManager =====
		if (AudioManager.Instance != null) {
			AudioManager.Instance.StopAllMusic();
			// Play main menu music
			AudioManager.Instance.PlaySceneMusicByIndex(0, skipFade: true);
			Debug.Log("[GameStateManager] Audio reset to main menu music");
		}

		// ===== CRITICAL: Reset WaveManager completely =====
		if (WaveManager.Instance != null) {
			// Stop all coroutines first
			WaveManager.Instance.StopAllCoroutines();

			// Reset wave state to initial values (wave 1, scene 0, etc.)
			WaveManager.Instance.ResetWaveStateImmediate();
			Debug.Log("[GameStateManager] WaveManager reset");
		}

		// ===== CRITICAL: Reset CurrencyManager completely =====
		if (CurrencyManager.Instance != null) {
			CurrencyManager.Instance.ResetCurrency();
			Debug.Log("[GameStateManager] Currency reset to starting amount");
		}

		// ===== CRITICAL: Reset TowerPlacementManager completely =====
		if (TowerPlacementManager.Instance != null) {
			// Clear all placed towers
			TowerPlacementManager.Instance.ClearPlacedTowers();
			// Reset tower costs to base values
			TowerPlacementManager.Instance.ResetTowerCosts();
			Debug.Log("[GameStateManager] Tower costs reset to base values");
		}

		// ===== CRITICAL: Aggressively clear ALL enemies =====
		// Multiple passes to ensure complete cleanup
		for (int pass = 0; pass < 3; pass++) {
			// Method 1: By tag
			GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (GameObject enemy in enemies) {
				if (enemy != null) {
					Destroy(enemy);
				}
			}

			// Method 2: By component
			Enemy[] enemyComponents = FindObjectsOfType<Enemy>();
			foreach (Enemy enemy in enemyComponents) {
				if (enemy != null && enemy.gameObject != null) {
					Destroy(enemy.gameObject);
				}
			}
		}
		Debug.Log("[GameStateManager] Cleared all enemies");

		// ===== Hide GameHUD =====
		GameObject gameHUDCanvas = GameObject.Find("GameHUDCanvas");
		if (gameHUDCanvas != null) {
			// Stop all coroutines in GameHUD before hiding
			GameHUD gameHUD = gameHUDCanvas.GetComponent<GameHUD>();
			if (gameHUD != null) {
				gameHUD.StopAllCoroutines();
			}
			gameHUDCanvas.SetActive(false);
		}

		// ===== Hide all game panels =====
		if (pauseUI != null)
			pauseUI.SetActive(false);

		if (defeatPanel != null)
			defeatPanel.gameObject.SetActive(false);

		if (victoryPanel != null)
			victoryPanel.gameObject.SetActive(false);

		// ===== Show main menu =====
		if (mainMenuManager != null) {
			mainMenuManager.ShowMainMenu();
		}

		// ===== Reset cursor state =====
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		// ===== Freeze time for main menu =====
		Time.timeScale = 0f;

		Debug.Log("[GameStateManager] Game completely reset to main menu state");
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
    
    // ===== Platform Fixes (All Platforms) =====
    
    private IEnumerator EnsurePlayerComponentsActiveOnGameStart()
    {
        // Wait for scene to load and player to spawn
        yield return new WaitForSeconds(0.5f);
        
        // Try multiple times to find and activate player components
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Find PlayerArmature using multiple methods
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
                // Ensure ALL critical components are active
                ThirdPersonController tpc = playerArmature.GetComponent<ThirdPersonController>();
                if (tpc != null)
                {
                    tpc.enabled = true;
                }
                
                CharacterController cc = playerArmature.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = true;
                }
                
                StarterAssetsInputs inputs = playerArmature.GetComponent<StarterAssetsInputs>();
                if (inputs != null)
                {
                    inputs.enabled = true;
                }
                
                // Find ALL animators in hierarchy (parent and children)
                Animator[] allAnimators = playerArmature.GetComponentsInChildren<Animator>(true);
                
                foreach (Animator anim in allAnimators)
                {
                    anim.enabled = true;
                    FixAnimatorForWebGL(anim);
                }
                
                yield break; // Success, exit coroutine
            }
            
            yield return new WaitForSeconds(0.3f);
        }
    }
    
    private void ApplyWebGLFixes()
    {
        // Platform-specific fixes can be added here if needed
    }
    
    
    private GameObject lastPlayerArmature = null;
    private float lastAnimatorCheckTime = 0f;
    
    private void MonitorPlayerAnimator()
    {
        // Only check every 0.5 seconds to avoid performance issues
        if (Time.time - lastAnimatorCheckTime < 0.5f) return;
        lastAnimatorCheckTime = Time.time;
        
        // Find player if we don't have it cached
        if (lastPlayerArmature == null)
        {
            lastPlayerArmature = GameObject.FindGameObjectWithTag("Player");
            if (lastPlayerArmature == null)
            {
                lastPlayerArmature = GameObject.Find("PlayerArmature");
            }
            if (lastPlayerArmature == null)
            {
                ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
                if (controller != null)
                {
                    lastPlayerArmature = controller.gameObject;
                }
            }
        }
        
        if (lastPlayerArmature != null)
        {
            // Check ALL animators in hierarchy (not just on root)
            Animator[] allAnimators = lastPlayerArmature.GetComponentsInChildren<Animator>(true);
            
            foreach (Animator animator in allAnimators)
            {
                if (animator == null) continue;
                
                
                // Check if animator is disabled or has wrong settings
                bool needsFix = false;
                
                if (!animator.enabled)
                {
                    needsFix = true;
                }
                else if (animator.cullingMode != AnimatorCullingMode.CullUpdateTransforms)
                {
                    needsFix = true;
                }
                else if (animator.applyRootMotion != false)
                {
                    needsFix = true;
                }
                
                if (needsFix)
                {
                    FixAnimatorForWebGL(animator);
                }
            }
        }
    }
    
    private void FixAnimatorForWebGL(Animator animator)
    {
        if (animator == null) return;
        
        // Ensure the animator controller is assigned
        if (animator.runtimeAnimatorController == null)
        {
            return;
        }
        
        // Force animator settings BEFORE enabling
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; // Use CullUpdateTransforms for proper animation
        animator.updateMode = AnimatorUpdateMode.AnimatePhysics; // AnimatePhysics for better character controller sync
        animator.applyRootMotion = false; // CRITICAL: Disable root motion - ThirdPersonController handles movement
        
        // Enable animator
        animator.enabled = true;
        
        // Rebind animator to refresh all bindings (fixes state issues)
        animator.Rebind();
        
        // Force update to initialize state
        animator.Update(0f);
    }
    
    // ===== End Platform Fixes =====
    
    // ===== Terrain Basemap Fix - REMOVED =====
    // No terrain quality modifications per user request
    private void FixTerrainBasemaps()
    {
        // Intentionally empty - no terrain modifications
    }
}
