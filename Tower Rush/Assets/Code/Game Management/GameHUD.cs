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
    [SerializeField] private TextMeshProUGUI placementText;
    
    [Header("Optional HUD Elements")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthSlider;
    
    [Header("Currency Animation")]
    [SerializeField] private bool animateCurrencyChanges = true;
    [SerializeField] private Color earnColor = Color.green;
    [SerializeField] private Color spendColor = Color.red;
    [SerializeField] private float animationDuration = 1f;
    
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
        
        // Ensure cursor is properly set for UI interaction
        InitializeCursorState();
        
        // Ensure all UI components are enabled
        EnsureUIComponentsEnabled();
        
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
        // Show wave completion notification
        if (placementText != null)
        {
            StartCoroutine(ShowTemporaryMessage($"Wave {waveNumber} Complete!", 2f));
        }
    }
    
    void Update()
    {
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
            
            // Ensure cursor stays available for UI interaction
            EnsureCursorAvailable();
        }
        
        // Update wave display with real-time progress
        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveActive())
        {
            UpdateWaveDisplay(WaveManager.Instance.GetCurrentWave(), WaveManager.Instance.GetMaxWaves());
        }
    }
    
    private void OnCurrencyChanged(int newAmount)
    {
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
        if (waveText != null)
        {
            string waveTextContent;
            
            if (WaveManager.Instance != null && WaveManager.Instance.IsWaveActive())
            {
                int enemiesRemaining = WaveManager.Instance.GetEnemiesRemaining();
                int totalEnemies = WaveManager.Instance.GetTotalEnemiesThisWave();
                int enemiesKilled = totalEnemies - enemiesRemaining;
                
                // Calculate percentage of enemies killed
                float killedPercentage = totalEnemies > 0 ? (float)enemiesKilled / totalEnemies * 100f : 0f;
                
                if (totalWaves > 0)
                {
                    waveTextContent = $"Wave: {currentWave}/{totalWaves}\n{enemiesRemaining} remaining, {killedPercentage:F0}% killed";
                }
                else
                {
                    waveTextContent = $"Wave: {currentWave}\n{enemiesRemaining} remaining, {killedPercentage:F0}% killed";
                }
            }
            else
            {
                if (totalWaves > 0)
                {
                    waveTextContent = $"Wave: {currentWave}/{totalWaves}";
                }
                else
                {
                    waveTextContent = $"Wave: {currentWave}";
                }
            }
            
            waveText.text = waveTextContent;
        }
    }
    
    public void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
        }
        
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }
    
    // Tower Placement Methods
    private void InitializeTowerButtons()
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
                // Position buttons to the right side with spacing
                float buttonWidth = 100f;
                float spacing = 10f;
                
                // Get container width
                RectTransform containerRect = towerButtonContainer.GetComponent<RectTransform>();
                float containerWidth = containerRect != null ? containerRect.rect.width : 800f;
                
                // Calculate starting position from the right edge
                float totalButtonsWidth = availableTowers.Count * buttonWidth + (availableTowers.Count - 1) * spacing;
                float startX = (containerWidth / 2f) - totalButtonsWidth + (i * (buttonWidth + spacing));
                
                buttonRect.anchoredPosition = new Vector2(startX, 0);
                buttonRect.sizeDelta = new Vector2(buttonWidth, 100f);
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
        
        if (healthText != null && !healthText.enabled)
        {
            healthText.enabled = true;
        }
        
        if (healthSlider != null && !healthSlider.enabled)
        {
            healthSlider.enabled = true;
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
    
    // Initialize cursor state for proper UI interaction
    private void InitializeCursorState()
    {
        // Ensure cursor is visible and unlocked for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Ensure EventSystem is active
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.enabled = true;
        }
    }
    
    // Ensure cursor stays available for UI interaction
    private void EnsureCursorAvailable()
    {
        // Only ensure cursor is available if no tower upgrade UI is open
        bool upgradeUIOpen = false;
        
        // Check if any tower upgrade UI is currently open
        TowerUpgradeUI[] upgradeUIs = FindObjectsOfType<TowerUpgradeUI>();
        foreach (TowerUpgradeUI ui in upgradeUIs)
        {
            if (ui != null && ui.gameObject.activeInHierarchy)
            {
                // Check if the upgrade panel is actually visible
                Transform upgradePanel = ui.transform.Find("UpgradePanel");
                if (upgradePanel != null && upgradePanel.gameObject.activeInHierarchy)
                {
                    upgradeUIOpen = true;
                    break;
                }
            }
        }
        
        // If no upgrade UI is open, ensure cursor is available for HUD interaction
        if (!upgradeUIOpen)
        {
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
            
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
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
    
    // Public method to get tower button container (for TowerButton drag functionality)
    public Transform GetTowerButtonContainer()
    {
        return towerButtonContainer;
    }
}