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
        // Find UI components in the scene
        if (mainMenuManager == null)
            mainMenuManager = FindObjectOfType<MainMenuManager>();
            
        GameOverUI gameOverUI = FindObjectOfType<GameOverUI>();
        if (gameOverUI != null)
        {
            pauseUI = gameOverUI.GetPausePanel();
        }
        
        // Hide GameHUD in manager scene
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
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        // Hide main menu before moving to DontDestroyOnLoad
        if (mainMenuManager != null)
        {
            mainMenuManager.HideMainMenu();
        }
        
        // Move manager objects to DontDestroyOnLoad before scene transition
        MoveManagersToDontDestroyOnLoad();
        
        // Start the gameplay through WaveManager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StartGameplay();
        }
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        Time.timeScale = 0f;
        
        // Clear game state
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        
        // Hide GameHUD when returning to main menu
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
        
        // Show main menu
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }
        
        // Hide pause UI
        if (pauseUI != null) pauseUI.SetActive(false);
    }
    
    public void QuitGame()
    {
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // Public getters
    public GameState GetCurrentState() => currentState;
    public bool IsGameActive() => currentState == GameState.Playing;
}
