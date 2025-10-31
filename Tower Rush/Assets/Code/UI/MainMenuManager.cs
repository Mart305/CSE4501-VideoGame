using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private HowToPlayPanel howToPlayPanel;
    
    [Header("Game Settings")]
    [SerializeField] private bool showCursor = true;
    
    void Start()
    {
        // Setup button listeners (remove first to prevent duplicates)
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }
            
        if (howToPlayButton != null)
        {
            howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
            howToPlayButton.onClick.AddListener(OpenHowToPlay);
        }
        
        // Initialize UI
        ShowMainMenu();
        
        // Setup cursor for menu
        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Instructions text will be populated later
    }
    
    void Update()
    {
        // ESC key navigation can be added later
    }
    
    public void StartGame()
    {
        
        // Start game in current scene through GameStateManager
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameFromMenu();
        }
        else
        {
            // Fallback if no GameStateManager
            HideMainMenu();
            Time.timeScale = 1f;
        }
    }
    
    public void OpenHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.OpenPanel();
        }
    }
    
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
            
        // Ensure cursor is visible for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause the game if it's running
        Time.timeScale = 0f;
    }
    
    public void HideMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
            
        // Resume game time
        Time.timeScale = 1f;
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        // Keep cursor visible in main menu
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
