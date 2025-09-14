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
            targetTower = FindNearestTower();
            if (targetTower == null) return; // no towers left
        }

        float distance = Vector3.Distance(transform.position, targetTower.transform.position);

        if (distance > attackRange)
        {
            // Move toward the tower
            Vector3 direction = (targetTower.transform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.LookAt(targetTower.transform);
        }
        else
        {
            attackCooldown -= Time.deltaTime;

            if (attackCooldown <= 0f)
            {
                AttackTower();
                attackCooldown = attackRate; // reset cooldown
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
