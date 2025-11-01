using UnityEngine;

public class MutantZombie : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Mutant Zombie stats - significantly harder (boss enemy)
        moveSpeed = 4.5f;  // Much faster
        health = 200f;     // Much more health (boss)
        damage = 50f;      // Much more damage
        attackCooldown = 1.8f;
        attackRange = 2f;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(200f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(200f);
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
