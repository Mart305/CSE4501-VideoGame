using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class VictoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button continueButton; // Continue to Main Menu button
    
    [Header("Stats Display - Dynamically Created")]
    [SerializeField] private TextMeshProUGUI enemiesDefeatedText;

    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ReturnToMainMenu);
        
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowVictory(int enemiesDefeated)
    {
        // Update stats display
        if (enemiesDefeatedText != null)
            enemiesDefeatedText.text = $"Enemies Defeated: {enemiesDefeated}";
        
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        
        // Pause game
        Time.timeScale = 0f;
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Play victory sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictorySound();
        }
    }

    private void ReturnToMainMenu()
    {
        // Resume time
        Time.timeScale = 1f;
        
        // Hide panel first
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Play sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
        
        // Use GameStateManager to properly transition
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ReturnToMainMenu();
        }
    }
}
