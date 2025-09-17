using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private Tower targetTower;
    public float speed = 3f;
    public float damage = 10f;
    public float attackRate = 1f; // seconds between attacks

    private float attackCooldown = 0f;
    public float attackRange = 1.5f;
    
    private Health health;
    private Renderer enemyRenderer;
    private Color originalColor;

    void Start()
    {
        targetTower = FindNearestTower();
        
        // Add Health component if it doesn't exist
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
        
        // Subscribe to health events
        health.OnDamageTaken.AddListener(OnDamageTaken);
        health.OnDeath.AddListener(OnDeath);
        
        // Get renderer for visual feedback
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        
        // Add health bar if it doesn't exist
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar == null)
        {
            GameObject healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(transform);
            healthBarObj.transform.localPosition = Vector3.zero;
            healthBarObj.AddComponent<EnemyHealthBar>();
        }
    }

    void Update()
    {
        if (targetTower == null || targetTower.IsDestroyed())
        {
            Tower newTarget = FindNearestTower();
            if (newTarget != null)
            {
                targetTower = newTarget;
            }
            else
            {
                // No towers left - stop moving but don't reset rotation
                return;
            }
        }

        // Get positions
        Vector3 towerPos = targetTower.transform.position;
        Vector3 myPos = transform.position;
        
        // Calculate 2D distance (ignore Y)
        float distance = Vector2.Distance(
            new Vector2(myPos.x, myPos.z), 
            new Vector2(towerPos.x, towerPos.z)
        );

        if (distance > attackRange)
        {
            // Calculate direction (only X and Z)
            Vector3 direction = (towerPos - myPos);
            direction.y = 0; // Keep on ground
            direction.Normalize();
            
            // Move towards tower
            Vector3 newPos = myPos + direction * speed * Time.deltaTime;
            newPos.y = myPos.y; // Lock Y position
            transform.position = newPos;
            
            // Smoothly rotate to face tower
            Vector3 lookDirection = new Vector3(towerPos.x, myPos.y, towerPos.z) - myPos;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Attack when close enough
            attackCooldown -= Time.deltaTime;
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
    
    private void OnDamageTaken(float damage)
    {
        // Flash red when hit
        if (enemyRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
    }
    
    private void OnDeath()
    {
        // Play death animation or effects here if needed
        // For now, the Health component will destroy the GameObject
    }
    
    private System.Collections.IEnumerator FlashRed()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            enemyRenderer.material.color = originalColor;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (health != null)
        {
            health.OnDamageTaken.RemoveListener(OnDamageTaken);
            health.OnDeath.RemoveListener(OnDeath);
        }
    }
}
