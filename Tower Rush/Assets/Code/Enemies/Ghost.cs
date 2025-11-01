using UnityEngine;

public class Ghost : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Ghost stats - 10% faster and 15% stronger
        moveSpeed = 5.75f; // Increased from 5.25f
        health = 40f;
        damage = 5.75f; // Increased from 5f
        attackCooldown = 1f;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component instead of the base Enemy health
        healthComponent.SetMaxHealth(40f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(40f);
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

    protected override void FindTargetTower()
    {
        BaseTower[] towers = FindObjectsOfType<BaseTower>();
        BaseTower weakest = null;
        float lowestHealth = Mathf.Infinity;

        foreach (BaseTower t in towers)
        {
            if (t.GetCurrentHealth() <= 0) continue;
            if (t.GetCurrentHealth() < lowestHealth)
            {
                lowestHealth = t.GetCurrentHealth();
                weakest = t;
            }
        }

        targetTower = weakest;
    }
}