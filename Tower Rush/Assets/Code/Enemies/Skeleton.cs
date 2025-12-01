using UnityEngine;

public class Skeleton : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Skeleton stats - significantly harder
        moveSpeed = 10f;   // Much faster
        health = 40f;      // More health
        damage = 15f;      // More damage
        attackCooldown = 0.8f;
        attackRange = 1.5f; // Match other enemies to prevent going inside towers

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(40f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(40f);
        }

        base.Start();
        
        // Shorten death animation duration for skeletons
        deathEffectDuration = 0.8f; // Reduced from 2f to 0.8f for faster death animation
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

    // Skeletons are fast and aggressive - they target the closest tower
    // (This uses the default FindTargetTower behavior from Enemy base class)
}
