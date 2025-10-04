using UnityEngine;

public class MutantZombie : Enemy
{
    private Health healthComponent;
    
    [Header("Mutant Zombie Special Abilities")]
    [SerializeField] private float rageThreshold = 0.3f; // Rage mode at 30% health
    [SerializeField] private float rageSpeedMultiplier = 1.5f; // 50% speed boost in rage
    [SerializeField] private float rageDamageMultiplier = 1.8f; // 80% damage boost in rage
    
    private bool isInRageMode = false;
    private float originalMoveSpeed;
    private float originalDamage;
    
    protected override void Start()
    {
        // Mutant Zombie stats - Strong and fast special enemy
        moveSpeed = 2.5f;        // Faster than normal Zombie (1.5f) but slower than Ghost (3.5f)
        health = 200f;           // Increased from 120f to 200f - very tanky special enemy
        damage = 25f;            // High damage - stronger than Zombie (15f)
        attackCooldown = 1.8f;   // Slower attack than normal enemies

        // Store original values for rage mode
        originalMoveSpeed = moveSpeed;
        originalDamage = damage;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component instead of the base Enemy health
        healthComponent.SetMaxHealth(200f);  // Increased from 120f to 200f

        // Ensure this enemy has the "Enemy" tag for tower targeting
        if (!gameObject.CompareTag("Enemy"))
        {
            gameObject.tag = "Enemy";
        }

        base.Start();
    }

    public override void TakeDamage(float amount)
    {
        // Use Health component instead of base Enemy health system
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(amount);
            
            // Check for rage mode activation
            float healthPercentage = healthComponent.GetHealth() / healthComponent.GetMaxHealth();
            if (healthPercentage <= rageThreshold && !isInRageMode)
            {
                EnterRageMode();
            }
        }
        else
        {
            base.TakeDamage(amount);
        }
    }

    private void EnterRageMode()
    {
        isInRageMode = true;
        moveSpeed = originalMoveSpeed * rageSpeedMultiplier;
        damage = originalDamage * rageDamageMultiplier;
        
        Debug.Log("Mutant Zombie enters RAGE MODE! Speed and damage increased!");
        
        // Visual effect could be added here (red glow, particle effects, etc.)
        // For now, we'll just change the color slightly
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
    }

    protected override void Die()
    {
        // Mutant Zombie death effect
        Debug.Log("Mutant Zombie defeated!");
        base.Die();
    }

    // Override to make Mutant Zombie target the strongest tower (opposite of Ghost)
    protected override void FindTargetTower()
    {
        Tower[] towers = FindObjectsOfType<Tower>();
        Tower strongest = null;
        float highestHealth = 0f;

        foreach (Tower t in towers)
        {
            if (t.IsDestroyed()) continue;
            if (t.GetCurrentHealth() > highestHealth)
            {
                highestHealth = t.GetCurrentHealth();
                strongest = t;
            }
        }

        targetTower = strongest;
    }
}
