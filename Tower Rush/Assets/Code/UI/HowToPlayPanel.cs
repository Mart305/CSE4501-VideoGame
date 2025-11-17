using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HowToPlayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI instructionsText;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
            
        // Auto-fill instructions text
        SetInstructionsText();
    }
    
    private void SetInstructionsText()
    {
        if (instructionsText == null) return;
        
        instructionsText.text = @"<b>OBJECTIVE:</b> Defend your base from waves of enemies by placing towers strategically!

<b>CONTROLS:</b> WASD - Move  |  Mouse - Aim/Shoot  |  E - Lock Camera  |  V - RTS Mode  |  ESC - Pause

<b>RTS MODE:</b> Press V to toggle RTS camera view  |  Arrow Keys - Move camera  |  Mouse Wheel - Zoom in/out  |  Z/C - Rotate camera  |  Perfect for tower placement and strategy

<b>TOWER PLACEMENT:</b>
<b>Click:</b> 1. Click tower button  2. Click ground  3. Right-click/ESC to cancel
<b>Drag & Drop:</b> 1. Hold tower button  2. Drag to location  3. Release to place

<b>TOWER TYPES:</b> Fire - Area damage  |  Ice - Slows enemies  |  Lightning - Chain attacks  |  Void - Special abilities

<b>WAVES & PROGRESSION:</b> 5 waves per scene  |  Enemies get stronger each wave  |  New scene unlocks every 5 waves  |  Wave progress shown at top

<b>GOLD SYSTEM:</b> Earn gold by defeating enemies  |  Spend gold to place towers  |  Save for expensive towers  |  Balance offense and defense

<b>TIPS:</b> Place towers near spawn points  |  Mix tower types for synergy  |  Use RTS mode for better tower placement  |  Help towers by shooting  |  Focus tough enemies first";
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }
}
