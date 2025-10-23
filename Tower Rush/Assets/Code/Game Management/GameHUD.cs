using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }
    [Header("Currency Display")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private string currencyPrefix = "Gold: ";
    
    [Header("Tower Placement UI")]
    [SerializeField] private GameObject towerButtonPrefab;
    [SerializeField] private Transform towerButtonContainer;
    [SerializeField] private GameObject placementInstructions;
    [SerializeField] public TextMeshProUGUI placementText;
    [SerializeField] private Vector2 towerButtonSize = new Vector2(150f, 60f); // Configurable button size
    [SerializeField] private float towerButtonSpacing = 10f; // Spacing between buttons
    [SerializeField] private float buttonRowOffsetX = -200f; // Horizontal offset to avoid overlap with other UI
    
    [Header("Wave Display")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private Slider waveProgressSlider;
    [SerializeField] private TextMeshProUGUI waveProgressText;
    
    [Header("Tower Health Display")]
    [SerializeField] private GameObject towerHealthPanel;
    [SerializeField] private TextMeshProUGUI towerHealthText;
    [SerializeField] private Slider towerHealthSlider;
    [SerializeField] private Image towerHealthFill;
    [SerializeField] private Gradient healthGradient;
    
    [Header("Currency Animation")]
    [SerializeField] private bool animateCurrencyChanges = true;
    [SerializeField] private Color earnColor = Color.green;
    [SerializeField] private Color spendColor = Color.red;
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private GameObject currencyChangePopup;
    [SerializeField] private float popupDuration = 1.5f;

	[Header("Menu UI")]
	[SerializeField] private GameObject menuUI; // Assign this in the Inspector to your GameHUDCanvas
	private bool isMenuVisible = true;

	private int displayedCurrency = 0;
    private Coroutine currencyAnimationCoroutine;
    
    [Header("Visibility Settings")]
    [SerializeField] private bool alwaysVisible = true;
    
    void Awake()
    {
        // Singleton pattern - prevent multiple instances
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Don't force active state - let GameStateManager control visibility
        // Keep parent canvas active but let child GameHUD be controlled by state
        if (transform.parent != null)
        {
            transform.parent.gameObject.SetActive(true);
        }
        
        // Handle persistence properly for Canvas children
        if (alwaysVisible)
        {
            // Find the root Canvas
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.transform.parent == null)
            {
                // Canvas is root - make the entire Canvas persistent
                rootCanvas.gameObject.SetActive(true);
                DontDestroyOnLoad(rootCanvas.gameObject);
            }
        }
    }
    
    void Start()
    {
        // Check game state before making HUD visible
        if (GameStateManager.Instance != null && GameStateManager.Instance.GetCurrentState() == GameState.MainMenu)
        {
            // Hide HUD during main menu
            gameObject.SetActive(false);
        }
        else
        {
            // Show HUD during gameplay
            gameObject.SetActive(true);
        }
        
        
        // Subscribe to currency events
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged.AddListener(OnCurrencyChanged);
            CurrencyManager.Instance.OnCurrencyEarned.AddListener(OnCurrencyEarned);
            CurrencyManager.Instance.OnCurrencySpent.AddListener(OnCurrencySpent);
            // Initialize display
            displayedCurrency = CurrencyManager.Instance.GetCurrentCurrency();
            UpdateCurrencyDisplay();
        }
        
        // Subscribe to wave events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted.AddListener(OnWaveStarted);
            WaveManager.Instance.OnWaveCompleted.AddListener(OnWaveCompleted);
        }
        
        // Initialize wave display
        if (WaveManager.Instance != null)
        {
            UpdateWaveDisplay(WaveManager.Instance.GetCurrentWave(), WaveManager.Instance.GetMaxWaves());
        }
        
        // Initialize tower buttons
        InitializeTowerButtons();
        
        // Show initial placement instructions to guide new players
        ShowInitialPlacementInstructions();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from wave events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted.RemoveListener(OnWaveStarted);
            WaveManager.Instance.OnWaveCompleted.RemoveListener(OnWaveCompleted);
        }
    }
    
    private void OnWaveStarted(int waveNumber)
    {
        // Check if GameObject is active before starting coroutines
        if (!gameObject.activeInHierarchy)
            return;
            
        // Show wave start notification
        if (placementText != null)
        {
            StartCoroutine(ShowTemporaryMessage($"Wave {waveNumber} Starting!", 3f));
        }
        
        // Update wave display
        UpdateWaveDisplay(waveNumber, WaveManager.Instance?.GetMaxWaves() ?? -1);
    }
    
    private void OnWaveCompleted(int waveNumber)
    {
        // Check if GameObject is active before starting coroutines
        if (!gameObject.activeInHierarchy)
            return;
            
        // Show wave completion notification
        if (placementText != null)
        {
            StartCoroutine(ShowTemporaryMessage($"Wave {waveNumber} Complete!", 2f));
        }
    }

    private bool IsAnyPanelOpen()
    {
        // Check if pause menu is open
        if (Time.timeScale == 0f)
            return true;
        
        // Check for any active UI panels
        VictoryPanel victoryPanel = FindObjectOfType<VictoryPanel>();
        if (victoryPanel != null && victoryPanel.gameObject.activeInHierarchy)
            return true;
        
        DefeatPanel defeatPanel = FindObjectOfType<DefeatPanel>();
        if (defeatPanel != null && defeatPanel.gameObject.activeInHierarchy)
            return true;
        
        OptionsPanel optionsPanel = FindObjectOfType<OptionsPanel>();
        if (optionsPanel != null && optionsPanel.gameObject.activeInHierarchy)
            return true;
        
        return false;
    }
    
    private void ToggleMenu()
{
    isMenuVisible = !isMenuVisible;
    if (menuUI != null)
        menuUI.SetActive(isMenuVisible);

    // Update cursor state
    if (isMenuVisible)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    else
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
    
    void Update()
    {
		// Don't allow E key when any UI panels are open
		if (Input.GetKeyDown(KeyCode.E) && !IsAnyPanelOpen()) {
			ToggleMenu();
		}

		// Ensure HUD stays visible if alwaysVisible is enabled
		if (alwaysVisible)
        {
            // Check every frame for critical components
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            
            // Check if currency text still exists
            if (currencyText == null)
            {
                currencyText = GameObject.Find("CurrencyTextDisplay")?.GetComponent<TextMeshProUGUI>();
            }
            
            // Only run component checks occasionally to avoid performance issues
            if (Time.frameCount % 30 == 0) // Every 30 frames (~0.5 seconds at 60fps)
            {
                EnsureUIComponentsEnabled();
                
                // Extra check: make sure we're still the singleton instance
                if (Instance != this)
                {
                    Instance = this;
                }
            }
            
        }
        
        // Update wave display with real-time progress
        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveActive())
        {
            UpdateWaveDisplay(WaveManager.Instance.GetCurrentWave(), WaveManager.Instance.GetMaxWaves());
        }
    }
    
    private void OnCurrencyChanged(int newAmount)
    {
        // Safety check: Don't start coroutines if GameObject is inactive
        if (!gameObject.activeInHierarchy)
        {
            displayedCurrency = newAmount;
            return;
        }
        
        if (animateCurrencyChanges && currencyText != null)
        {
            // Stop any existing animation
            if (currencyAnimationCoroutine != null)
            {
                StopCoroutine(currencyAnimationCoroutine);
            }
            
            // Start new animation
            currencyAnimationCoroutine = StartCoroutine(AnimateCurrencyChange(displayedCurrency, newAmount));
        }
        else
        {
            displayedCurrency = newAmount;
            UpdateCurrencyDisplay();
        }
    }
    
    private void OnCurrencyEarned(int amount)
    {
        // Optional: Show earning feedback
        if (currencyText != null && animateCurrencyChanges)
        {
            StartCoroutine(FlashCurrencyColor(earnColor));
        }
    }
    
    private void OnCurrencySpent(int amount)
    {
        // Optional: Show spending feedback
        if (currencyText != null && animateCurrencyChanges)
        {
            StartCoroutine(FlashCurrencyColor(spendColor));
        }
    }
    
    private System.Collections.IEnumerator AnimateCurrencyChange(int fromAmount, int toAmount)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            // Smooth interpolation
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            displayedCurrency = Mathf.RoundToInt(Mathf.Lerp(fromAmount, toAmount, progress));
            UpdateCurrencyDisplay();
            
            yield return null;
        }
        
        displayedCurrency = toAmount;
        UpdateCurrencyDisplay();
        currencyAnimationCoroutine = null;
    }
    
    private System.Collections.IEnumerator FlashCurrencyColor(Color flashColor)
    {
        if (currencyText == null) yield break;
        
        Color originalColor = Color.white; // Always return to white, not the current color
        float flashDuration = 0.3f;
        float elapsedTime = 0f;
        
        // Flash to color
        while (elapsedTime < flashDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (flashDuration / 2f);
            currencyText.color = Color.Lerp(originalColor, flashColor, progress);
            yield return null;
        }
        
        // Flash back to white
        elapsedTime = 0f;
        while (elapsedTime < flashDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (flashDuration / 2f);
            currencyText.color = Color.Lerp(flashColor, originalColor, progress);
            yield return null;
        }
        
        currencyText.color = originalColor; // Ensure it's white
    }
    
    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null)
        {
            currencyText.text = currencyPrefix + displayedCurrency.ToString();
        }
    }
    
    // Enhanced wave display with progress
    public void UpdateWaveDisplay(int currentWave, int totalWaves = -1)
    {
        // Scenes change every 5 waves, so show progress within current scene
        int wavesPerScene = 5;
        int waveInCurrentScene = ((currentWave - 1) % wavesPerScene) + 1; // 1-5 for each scene
        
        if (waveText != null)
        {
            waveText.text = $"Wave: {waveInCurrentScene}/{wavesPerScene}";
        }
        
        // Update wave progress slider based on scene progress (0-5 waves)
        if (waveProgressSlider != null)
        {
            // Ensure slider is configured correctly
            waveProgressSlider.minValue = 0f;
            waveProgressSlider.maxValue = 1f;
            
            float progress = (float)waveInCurrentScene / wavesPerScene;
            waveProgressSlider.value = progress;
        }
        
        // Update wave progress text to show scene progress
        if (waveProgressText != null)
        {
            waveProgressText.text = $"Wave: {waveInCurrentScene}/{wavesPerScene}";
        }
    }
    
    
    // Tower Placement Methods
    public void InitializeTowerButtons()
    {
        if (TowerPlacementManager.Instance == null || towerButtonContainer == null || towerButtonPrefab == null)
            return;
            
        var availableTowers = TowerPlacementManager.Instance.GetAvailableTowers();
        
        // Clear existing buttons
        foreach (Transform child in towerButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each tower type
        for (int i = 0; i < availableTowers.Count; i++)
        {
            GameObject buttonObj = Instantiate(towerButtonPrefab, towerButtonContainer);
            
            // Ensure the instantiated button and all its components are active/enabled
            buttonObj.SetActive(true);
            ForceEnableAllComponents(buttonObj);
            
            // Position buttons manually if no layout group is present
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            if (buttonRect != null && towerButtonContainer.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
            {
                // Get container width
                RectTransform containerRect = towerButtonContainer.GetComponent<RectTransform>();
                float containerWidth = containerRect != null ? containerRect.rect.width : 800f;
                
                // Calculate starting position with offset to avoid overlap
                float totalButtonsWidth = availableTowers.Count * towerButtonSize.x + (availableTowers.Count - 1) * towerButtonSpacing;
                float startX = (containerWidth / 2f) - totalButtonsWidth + (i * (towerButtonSize.x + towerButtonSpacing)) + buttonRowOffsetX;
                
                buttonRect.anchoredPosition = new Vector2(startX, 0);
                buttonRect.sizeDelta = towerButtonSize; // Use configurable size
            }
            
            TowerButton towerButton = buttonObj.GetComponent<TowerButton>();
            
            if (towerButton != null)
            {
                // Ensure TowerButton component is enabled
                towerButton.enabled = true;
                towerButton.Initialize(availableTowers[i], i);
            }
        }
    }
    
    private void OnTowerSelected(TowerData towerData)
    {
        // Show placement instructions
        if (placementInstructions != null)
        {
            placementInstructions.SetActive(true);
        }
        
        if (placementText != null)
        {
            placementText.text = $"Placing {towerData.towerName}\nLeft click to place, Right click or ESC to cancel";
        }
    }
    
    private void OnTowerPlaced(GameObject tower)
    {
        // Hide placement instructions
        if (placementInstructions != null)
        {
            placementInstructions.SetActive(false);
        }
        
        // Optional: Show placement success feedback
        if (placementText != null)
        {
            StartCoroutine(ShowTemporaryMessage("Tower placed successfully!", 2f));
        }
    }
    
    private void OnPlacementCancelled()
    {
        // Hide placement instructions
        if (placementInstructions != null)
        {
            placementInstructions.SetActive(false);
        }
    }
    
    private void ShowInitialPlacementInstructions()
    {
        // Show placement instructions immediately when game starts
        if (placementInstructions != null)
        {
            placementInstructions.SetActive(true);
        }
        
        if (placementText != null)
        {
            placementText.text = "Click a tower icon above, then click to place OR drag and drop to place";
        }
    }
    
    private System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        if (placementText != null)
        {
            string originalText = placementText.text;
            placementText.text = message;
            
            yield return new WaitForSeconds(duration);
            
            placementText.text = originalText;
        }
    }
    
    // Ensure all UI components stay enabled
    private void EnsureUIComponentsEnabled()
    {
        // Ensure currency text is enabled
        if (currencyText != null && !currencyText.enabled)
        {
            currencyText.enabled = true;
        }
        
        // Ensure currency text GameObject is active
        if (currencyText != null && !currencyText.gameObject.activeInHierarchy)
        {
            currencyText.gameObject.SetActive(true);
        }
        
        // Ensure tower button container is active
        if (towerButtonContainer != null && !towerButtonContainer.gameObject.activeInHierarchy)
        {
            towerButtonContainer.gameObject.SetActive(true);
        }
        
        // Ensure all tower buttons and their components are enabled
        if (towerButtonContainer != null)
        {
            foreach (Transform child in towerButtonContainer)
            {
                if (child != null && !child.gameObject.activeInHierarchy)
                {
                    child.gameObject.SetActive(true);
                }
                
                // Ensure TowerButton component is enabled
                TowerButton towerButton = child.GetComponent<TowerButton>();
                if (towerButton != null && !towerButton.enabled)
                {
                    towerButton.enabled = true;
                }
            }
        }
        
        // Ensure other UI elements stay enabled
        if (waveText != null && !waveText.enabled)
        {
            waveText.enabled = true;
        }
        
        if (towerHealthText != null && !towerHealthText.enabled)
        {
            towerHealthText.enabled = true;
        }
        
        if (towerHealthSlider != null && !towerHealthSlider.enabled)
        {
            towerHealthSlider.enabled = true;
        }
    }
    
    // Force enable all components in a GameObject and its children
    private void ForceEnableAllComponents(GameObject obj)
    {
        if (obj == null) return;
        
        // Ensure the main GameObject is active
        obj.SetActive(true);
        
        // Get all components in the GameObject and its children
        Component[] allComponents = obj.GetComponentsInChildren<Component>(true);
        
        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;
            
            // Skip Transform and RectTransform
            if (comp is Transform || comp is RectTransform) continue;
            
            // Enable the component if it has an enabled property
            if (comp is Behaviour behaviour)
            {
                behaviour.enabled = true;
            }
            
            // Ensure GameObject is active (except for tooltip which should be controlled)
            if (!comp.gameObject.activeInHierarchy && !comp.gameObject.name.ToLower().Contains("tooltip"))
            {
                comp.gameObject.SetActive(true);
            }
        }
        
    }

	// Public method to get available towers (for TowerButton initialization)
	public List<TowerData> GetAvailableTowers()
    {
        if (TowerPlacementManager.Instance != null)
        {
            return TowerPlacementManager.Instance.GetAvailableTowers();
        }
        return new List<TowerData>();
    }
    
    // Update wave progress display
    public void UpdateWaveProgress(int currentEnemies, int totalEnemies)
    {
        if (waveProgressSlider != null)
        {
            float progress = totalEnemies > 0 ? (float)(totalEnemies - currentEnemies) / totalEnemies : 0f;
            waveProgressSlider.value = progress;
        }
        
        if (waveProgressText != null)
        {
            waveProgressText.text = $"{totalEnemies - currentEnemies}/{totalEnemies}";
        }
    }
    
    // Update tower health display
    public void UpdateTowerHealth(float currentHealth, float maxHealth, string towerName = "Tower")
    {
        if (towerHealthPanel != null && !towerHealthPanel.activeSelf)
        {
            towerHealthPanel.SetActive(true);
        }
        
        if (towerHealthSlider != null)
        {
            towerHealthSlider.value = currentHealth / maxHealth;
        }
        
        if (towerHealthFill != null && healthGradient != null)
        {
            towerHealthFill.color = healthGradient.Evaluate(currentHealth / maxHealth);
        }
        
        if (towerHealthText != null)
        {
            towerHealthText.text = $"{towerName}: {currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    // Show currency change popup
    public void ShowCurrencyChange(int amount)
    {
        if (currencyChangePopup != null)
        {
            GameObject popup = Instantiate(currencyChangePopup, transform);
            TextMeshProUGUI popupText = popup.GetComponentInChildren<TextMeshProUGUI>();
            
            if (popupText != null)
            {
                string prefix = amount > 0 ? "+" : "";
                popupText.text = $"{prefix}{amount}";
                popupText.color = amount > 0 ? earnColor : spendColor;
            }
            
            StartCoroutine(AnimatePopup(popup));
        }
    }
    
    private IEnumerator AnimatePopup(GameObject popup)
    {
        float elapsed = 0f;
        Vector3 startPos = popup.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 50f;
        
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.AddComponent<CanvasGroup>();
        }
        
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;
            
            popup.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
        Destroy(popup);
    }
    
    // Public method to get tower button container (for TowerButton drag functionality)
    public Transform GetTowerButtonContainer()
    {
        return towerButtonContainer;
    }
}