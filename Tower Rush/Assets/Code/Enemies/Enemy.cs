using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float damage = 5f;
    public float health = 20f;

    protected Tower targetTower;
    private float lastAttackTime;

    protected virtual void Start()
    {
        FindTargetTower();
    }

    protected virtual void Update()
    {
        if (targetTower == null || targetTower.IsDestroyed())
        {
            FindTargetTower();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTower.transform.position);

        if (distance > attackRange)
        {
            MoveTowardsTower();
        }
        else
        {
            AttackTower();
        }
    }

    protected virtual void MoveTowardsTower()
    {
        Vector3 direction = (targetTower.transform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.LookAt(targetTower.transform);
    }

    protected virtual void AttackTower()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            targetTower.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }

    protected virtual void FindTargetTower()
    {
        Tower[] towers = FindObjectsOfType<Tower>();
        float closestDist = Mathf.Infinity;
        Tower nearest = null;

        foreach (Tower t in towers)
        {
            if (t.IsDestroyed()) continue;
            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = t;
            }
        }
        targetTower = nearest;
    }

    public virtual void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
    
    // Public methods for wave scaling
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    public void SetAttackCooldown(float newCooldown)
    {
        attackCooldown = newCooldown;
    }
    
    public void SetHealth(float newHealth)
    {
        health = newHealth;
    }
}
