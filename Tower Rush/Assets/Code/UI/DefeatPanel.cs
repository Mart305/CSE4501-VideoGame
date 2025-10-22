using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DefeatPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button backButton; // Back to Main Menu button

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(ReturnToMainMenu);
        
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowDefeat()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        
        // Pause game
        Time.timeScale = 0f;
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Play defeat sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDefeatSound();
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
