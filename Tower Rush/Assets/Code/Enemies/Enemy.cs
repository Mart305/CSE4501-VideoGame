using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float damage = 5f;
    public float health = 20f;

    protected BaseTower targetTower;
    private float lastAttackTime;
    private SlowEffect slowEffect;

    protected virtual void Start()
    {
        FindTargetTower();
        
        // Get or add SlowEffect component
        slowEffect = GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = gameObject.AddComponent<SlowEffect>();
        }
    }

    protected virtual void Update()
    {
        if (targetTower == null || targetTower.GetCurrentHealth() <= 0)
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
        
        // Apply slow effect if present
        float effectiveMoveSpeed = moveSpeed;
        if (slowEffect != null)
        {
            effectiveMoveSpeed *= slowEffect.GetSpeedMultiplier();
        }
        
        transform.position += direction * effectiveMoveSpeed * Time.deltaTime;
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
        BaseTower[] towers = FindObjectsOfType<BaseTower>();
        float closestDist = Mathf.Infinity;
        BaseTower nearest = null;

        foreach (BaseTower t in towers)
        {
            if (t.GetCurrentHealth() <= 0) continue;
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
