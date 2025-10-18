using UnityEngine;

public class MutantZombie : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Mutant Zombie stats (1.5x faster, 2x health) - Boss enemy
        moveSpeed = 4.5f; // 3f * 1.5 - Faster than normal zombie
        health = 300f; // 150f * 2 - Much higher health
        damage = 30f; // Higher damage than normal zombie (15f)
        attackCooldown = 1.5f; // Slightly faster attacks than normal zombie (2f)
        attackRange = 2f; // Slightly larger attack range

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(300f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(300f);
        }

        base.Start();
    }

    public override void TakeDamage(float amount)
    {
        // Use Health component instead of base Enemy health system
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(amount);
        }
        else
        {
            base.TakeDamage(amount);
        }
    }

    protected override void Die()
    {
        // Boss death effects could be added here
        // For now, just destroy the object
        base.Die();
    }

    // Override to make MutantZombie target the strongest tower instead of closest
    protected override void FindTargetTower()
    {
        BaseTower[] towers = FindObjectsOfType<BaseTower>();
        BaseTower strongest = null;
        float highestHealth = 0;

        foreach (BaseTower t in towers)
        {
            if (t.GetCurrentHealth() <= 0) continue;
            if (t.GetCurrentHealth() > highestHealth)
            {
                highestHealth = t.GetCurrentHealth();
                strongest = t;
            }
        }

        targetTower = strongest;
    }
}
