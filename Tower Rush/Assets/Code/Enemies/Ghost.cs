using UnityEngine;

public class Ghost : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Ghost stats
        moveSpeed = 3.5f;
        health = 20f;
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
        Tower[] towers = FindObjectsOfType<Tower>();
        Tower weakest = null;
        float lowestHealth = Mathf.Infinity;

        foreach (Tower t in towers)
        {
            if (t.IsDestroyed()) continue;
            if (t.GetCurrentHealth() < lowestHealth)
            {
                lowestHealth = t.GetCurrentHealth();
                weakest = t;
            }
        }

        targetTower = weakest;
    }
}