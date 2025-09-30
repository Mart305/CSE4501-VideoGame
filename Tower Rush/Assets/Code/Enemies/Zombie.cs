using UnityEngine;

public class Zombie : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Zombie stats
        moveSpeed = 1.5f;
        health = 50f;
        damage = 15f;
        attackCooldown = 2f;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component instead of the base Enemy health
        healthComponent.SetMaxHealth(50f);

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