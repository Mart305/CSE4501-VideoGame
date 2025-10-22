using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private OptionsPanel optionsPanel;
    
    private bool isPaused = false;
    private EventSystem eventSystem;

    void Start()
    {
        // Make sure pause menu is hidden at start
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        // Find EventSystem in scene
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("No EventSystem found! UI buttons won't work. Add one via: GameObject -> UI -> Event System");
        }
    }

    void Update()
    {
        // Only allow pause menu during gameplay (not in main menu)
        if (GameStateManager.Instance != null)
        {
            GameState currentState = GameStateManager.Instance.GetCurrentState();
            
            // Only respond to ESC if we're in Playing or Paused state (not MainMenu)
            if (currentState == GameState.Playing || currentState == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (isPaused)
                    {
                        ResumeGame();
                    }
                    else
                    {
                        PauseGame();
                    }
                }
            }
        }
    }

    public void PauseGame()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
        }
        
        Time.timeScale = 0f; // Freeze game
        isPaused = true;
        
        // Disable StarterAssetsInputs to prevent cursor lock conflict
        var starterInputs = FindObjectOfType<StarterAssets.StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.enabled = false;
        }
        
        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    public void ResumeGame()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        Time.timeScale = 1f; // Unfreeze game
        isPaused = false;
        
        // Re-enable StarterAssetsInputs
        var starterInputs = FindObjectOfType<StarterAssets.StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.enabled = true;
        }
        
        // Lock and hide cursor (for FPS gameplay)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    public void OpenOptions()
    {
        // Open the options panel using the inspector reference
        if (optionsPanel != null)
        {
            optionsPanel.OpenPanel();
        }
        else
        {
            Debug.LogError("OptionsPanel reference not assigned in PauseMenuManager!");
        }
        
        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    public void ReturnToMainMenu()
    {
        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
        
        // CRITICAL: Reset game state
        isPaused = false;
        
        // Hide pause menu
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        // Re-enable StarterAssetsInputs
        var starterInputs = FindObjectOfType<StarterAssets.StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.enabled = true;
        }
        
        // Use GameStateManager to properly return to main menu
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ReturnToMainMenu();
        }
        else
        {
            // Fallback: Load ManagerScene directly
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopAllSounds();
            }
            
            SceneManager.LoadScene("ManagerScene");
        }
    }

    public void QuitGame()
    {
        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
        
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Public getter for other scripts to check pause state
    public bool IsPaused()
    {
        return isPaused;
    }
}
