using UnityEngine;

public class Skeleton : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Skeleton stats (Fast, low-health enemy)
        moveSpeed = 5f; // Very fast - faster than Ghost (3.5f)
        health = 15f; // Low health - less than Ghost (20f)
        damage = 8f; // Moderate damage
        attackCooldown = 0.8f; // Fast attacks
        attackRange = 1.2f; // Slightly smaller attack range

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(15f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(15f);
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

    // Skeletons are fast and aggressive - they target the closest tower
    // (This uses the default FindTargetTower behavior from Enemy base class)
}
