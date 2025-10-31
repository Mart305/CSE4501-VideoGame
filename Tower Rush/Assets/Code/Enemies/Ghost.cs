using UnityEngine;

public class Ghost : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Ghost stats (1.5x faster, 2x health)
        moveSpeed = 5.25f; // 3.5f * 1.5
        health = 40f; // 20f * 2
        damage = 5f;
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