using UnityEngine;

public class Ghost : Enemy
{
    protected override void Start()
    {
        // Ghost stats
        moveSpeed = 3.5f;
        health = 20f;
        damage = 5f;
        attackCooldown = 1f;

        base.Start();
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
