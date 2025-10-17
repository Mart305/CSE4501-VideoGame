using UnityEngine;

public class Zombie : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Zombie stats (1.5x faster, 2x health)
        moveSpeed = 2.25f; // 1.5f * 1.5
        health = 100f; // 50f * 2
        damage = 15f;
        attackCooldown = 2f;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component instead of the base Enemy health
        healthComponent.SetMaxHealth(100f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(100f);
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
}