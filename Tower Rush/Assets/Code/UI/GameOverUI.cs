using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    
    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button pauseMainMenuButton;
    
    // Public getter for GameStateManager
    public GameObject GetPausePanel() => pausePanel;
    
    void Start()
    {
        // Setup button listeners
            
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => GameStateManager.Instance?.ResumeGame());
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);
            
        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(() => GameStateManager.Instance?.ReturnToMainMenu());
        
        // Hide all panels initially
        if (pausePanel != null) pausePanel.SetActive(false);
    }
    
    void Update()
    {
        if (GameStateManager.Instance == null) return;
        
        // Update UI based on game state
        GameState currentState = GameStateManager.Instance.GetCurrentState();
        
        switch (currentState)
        {
            case GameState.Paused:
                ShowPauseUI();
                break;
            case GameState.Playing:
                HideAllUI();
                break;
        }
    }
    
    
    private void ShowPauseUI()
    {
        if (pausePanel != null && !pausePanel.activeInHierarchy)
        {
            pausePanel.SetActive(true);
        }
        
        // No other panels to hide
    }
    
    private void HideAllUI()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }
    
    public void OpenOptions()
    {
    }
}
