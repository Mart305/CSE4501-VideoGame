using UnityEngine;

public class Zombie : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Zombie stats - 10% faster and 15% stronger
        moveSpeed = 2.5f; // Increased from 2.25f
        health = 100f;
        damage = 17.5f; // Increased from 15f
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