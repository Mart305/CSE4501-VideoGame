using UnityEngine;

public class Skeleton : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Skeleton stats (1.5x faster, 2x health) - Fast, low-health enemy
        moveSpeed = 7.5f; // 5f * 1.5 - Very fast
        health = 30f; // 15f * 2 - Low health but doubled
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
        healthComponent.SetMaxHealth(30f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(30f);
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
