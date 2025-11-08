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
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthPercentage());
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
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
        
        // Award currency if this is an enemy
        if (gameObject.CompareTag("Enemy") && CurrencyManager.Instance != null)
        {
            // Try to get enemy type from component
            Enemy enemyComponent = GetComponent<Enemy>();
            string enemyType = "normal";
            
            if (enemyComponent != null)
            {
                // Get enemy type from the class name
                enemyType = enemyComponent.GetType().Name.ToLower();
            }
            
            CurrencyManager.Instance.AwardEnemyKill(enemyType);
        }
        
        // If this has an Enemy component, let Enemy.Die() handle destruction and animation
        // Otherwise, destroy after a short delay (for non-enemy objects)
        Enemy enemy = GetComponent<Enemy>();
        if (enemy == null)
        {
            // Destroy the game object after a short delay (for non-enemy objects)
            Destroy(gameObject, 0.5f);
        }
        // Enemy.Die() will handle destruction for enemy objects
    }
    
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;
}