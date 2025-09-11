using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private Tower targetTower;
    public float speed = 3f;
    public float damage = 10f;
    public float attackRate = 1f; // seconds between attacks

    private float attackCooldown = 0f;

    void Start()
    {
        targetTower = FindNearestTower();
    }

    void Update()
    {
        if (targetTower == null || targetTower.IsDestroyed())
        {
            targetTower = FindNearestTower();
            if (targetTower == null) return; // no towers left
        }

        float distance = Vector3.Distance(transform.position, targetTower.transform.position);

        if (distance > 1.5f) // not in attack range yet
        {
            // Move toward the tower
            Vector3 direction = (targetTower.transform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            transform.LookAt(targetTower.transform);
        }
        else
        {
            // Attack if cooldown expired
            if (attackCooldown <= 0f)
            {
                AttackTower();
                attackCooldown = attackRate;
            }
        }
    }

    private void AttackTower()
    {
        if (targetTower != null && !targetTower.IsDestroyed())
        {
            targetTower.TakeDamage(damage);
        }
    }

    private Tower FindNearestTower()
    {
        Tower[] towers = FindObjectsOfType<Tower>();
        Tower nearest = null;
        float shortestDist = Mathf.Infinity;

        foreach (Tower tower in towers)
        {
            if (tower.IsDestroyed()) continue;
            float dist = Vector3.Distance(transform.position, tower.transform.position);
            if (dist < shortestDist)
            {
                shortestDist = dist;
                nearest = tower;
            }
        }

        return nearest;
    }
}
