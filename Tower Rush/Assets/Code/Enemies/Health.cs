using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }
    
    [Header("Events")]
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent OnDeath;
    
    private bool isDead = false;
    
    void Awake()
    {
        // Initialize UnityEvents to prevent null reference exceptions
        if (OnHealthChanged == null)
            OnHealthChanged = new UnityEvent<float>();
        if (OnDamageTaken == null)
            OnDamageTaken = new UnityEvent<float>();
        if (OnDeath == null)
            OnDeath = new UnityEvent();
    }
    void Start()
    {
        // Only initialize if not already set (allows SetMaxHealth to override)
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Ensure health is initialized (in case Start hasn't run yet)
        if (currentHealth <= 0 && maxHealth > 0)
        {
            currentHealth = maxHealth;
        }
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(GetHealthPercentage());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        
        // Check if there's an Enemy component - if so, let it handle death (animations, cleanup, etc.)
        Enemy enemyComponent = GetComponent<Enemy>();
        
        // Award currency if this is an enemy
        if (gameObject.CompareTag("Enemy") && CurrencyManager.Instance != null)
        {
            string enemyType = "normal";
            
            if (enemyComponent != null)
            {
                // Get enemy type from the class name
                enemyType = enemyComponent.GetType().Name.ToLower();
            }
            
            CurrencyManager.Instance.AwardEnemyKill(enemyType);
        }
        
        // If there's an Enemy component, let it handle death (it will handle animations and destruction)
        if (enemyComponent != null)
        {
            // Call Enemy's Die() method - it will handle death animations, colliders, and destruction
            enemyComponent.Die();
            return; // Enemy.Die() will handle destruction, so we don't need to destroy here
        }
        
        // If no Enemy component, destroy the game object after a short delay
        Destroy(gameObject, 0.5f);
    }
    
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;
}