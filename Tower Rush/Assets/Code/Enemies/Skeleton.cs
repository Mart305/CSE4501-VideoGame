using UnityEngine;

public class Skeleton : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Skeleton stats - Fast but weak
        moveSpeed = 4.5f;        // Faster than Ghost (3.5f) and Zombie (1.5f)
        health = 40f;            // Increased from 15f to 40f - still weak but more survivable
        damage = 8f;             // Moderate damage
        attackCooldown = 1.2f;   // Slightly slower attack than Ghost

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component instead of the base Enemy health
        healthComponent.SetMaxHealth(40f);   // Increased from 15f to 40f

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
        }
        else
        {
            base.TakeDamage(amount);
        }
    }

    protected override void Die()
    {
        // Skeleton death effect - could add bone particles or sound here
        Debug.Log("Skeleton defeated!");
        base.Die();
    }
}
