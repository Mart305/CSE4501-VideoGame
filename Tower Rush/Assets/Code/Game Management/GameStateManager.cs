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
        // Check for pause input (only during gameplay)
        if (Input.GetKeyDown(KeyCode.P) && currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (Input.GetKeyDown(KeyCode.P) && currentState == GameState.Paused)
        {
            ResumeGame();
        }
        
        // ESC key functionality removed
        
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
        }
        else
        {
            Debug.LogError("GameHUDCanvas not found after scene load!");
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
        
        Debug.Log("Returned to main menu - game state cleared");
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
        else
        {
            Debug.LogError("VictoryPanel not assigned in GameStateManager!");
        }
    }
    
    public void ShowDefeat()
    {
        if (defeatPanel != null)
        {
            defeatPanel.ShowDefeat();
        }
        else
        {
            Debug.LogError("DefeatPanel not assigned in GameStateManager!");
        }
    }
    
    // Public getters
    public GameState GetCurrentState() => currentState;
    public bool IsGameActive() => currentState == GameState.Playing;
}
