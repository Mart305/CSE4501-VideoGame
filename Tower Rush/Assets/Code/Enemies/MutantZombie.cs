using UnityEngine;

public class MutantZombie : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Mutant Zombie stats (balanced for mid-game boss) - Reduced from overpowered values
        moveSpeed = 2.8f; // Reduced from 4.5f - Still faster but not overwhelming
        health = 80f; // Reduced from 300f - Strong but manageable
        damage = 12f; // Reduced from 30f - Dangerous but not instant kill
        attackCooldown = 1.8f; // Slightly slower attacks for balance
        attackRange = 2f; // Slightly larger attack range

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(80f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(80f);
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
