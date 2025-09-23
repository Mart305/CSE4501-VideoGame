using UnityEngine;
using UnityEngine.Events;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    
    [Header("Currency Settings")]
    [SerializeField] private int startingCurrency = 200;
    [SerializeField] private int currentCurrency;
    
    [Header("Earning Settings")]
    [SerializeField] private int baseEnemyReward = 10;
    [SerializeField] private int bossEnemyReward = 50;
    
    [Header("Events")]
    public UnityEvent<int> OnCurrencyChanged;
    public UnityEvent<int> OnCurrencyEarned;
    public UnityEvent<int> OnCurrencySpent;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize events
            if (OnCurrencyChanged == null)
                OnCurrencyChanged = new UnityEvent<int>();
            if (OnCurrencyEarned == null)
                OnCurrencyEarned = new UnityEvent<int>();
            if (OnCurrencySpent == null)
                OnCurrencySpent = new UnityEvent<int>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        currentCurrency = startingCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
    /// <summary>
    /// Add currency to the player's balance
    /// </summary>
    /// <param name="amount">Amount to add</param>
    public void EarnCurrency(int amount)
    {
        if (amount <= 0) return;
        
        currentCurrency += amount;
        OnCurrencyEarned?.Invoke(amount);
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
    /// <summary>
    /// Spend currency if player has enough
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if purchase was successful</returns>
    public bool SpendCurrency(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            OnCurrencySpent?.Invoke(amount);
            OnCurrencyChanged?.Invoke(currentCurrency);
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if player has enough currency for a purchase
    /// </summary>
    /// <param name="amount">Amount to check</param>
    /// <returns>True if player has enough currency</returns>
    public bool HasEnoughCurrency(int amount)
    {
        return currentCurrency >= amount;
    }
    
    /// <summary>
    /// Get current currency amount
    /// </summary>
    /// <returns>Current currency balance</returns>
    public int GetCurrentCurrency()
    {
        return currentCurrency;
    }
    
    /// <summary>
    /// Award currency for killing an enemy
    /// </summary>
    /// <param name="enemyType">Type of enemy killed (for different rewards)</param>
    public void AwardEnemyKill(string enemyType = "normal")
    {
        int reward = baseEnemyReward;
        
        // Different rewards based on enemy type
        switch (enemyType.ToLower())
        {
            case "boss":
            case "ghost":
                reward = bossEnemyReward;
                break;
            case "zombie":
            case "normal":
            default:
                reward = baseEnemyReward;
                break;
        }
        
        EarnCurrency(reward);
    }
    
    /// <summary>
    /// Set currency to a specific amount (for testing/cheats)
    /// </summary>
    /// <param name="amount">Amount to set</param>
    public void SetCurrency(int amount)
    {
        currentCurrency = Mathf.Max(0, amount);
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
    /// <summary>
    /// Reset currency to starting amount
    /// </summary>
    public void ResetCurrency()
    {
        currentCurrency = startingCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
}
