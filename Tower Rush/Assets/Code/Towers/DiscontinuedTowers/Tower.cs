using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Tower Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] private Color healthyColor = Color.white;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Laser Combat System")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float range = 10f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float fireRate = 2f; // shots per second
    [SerializeField] private float laserDuration = 0.15f;

    [Header("Upgrade System")]
    [SerializeField] private float damageResistance = 0f; // 0-1 (0% to 100% resistance)
    [SerializeField] private int maxHealthUpgrades = 0;
    [SerializeField] private float maxHealthUpgradeAmount = 25f;
    [SerializeField] private float repairAmount = 50f;

    private Renderer towerRenderer;
    private Color originalColor;
    private HealthBar healthBar;
    private GameObject currentTarget;
    private float lastFireTime;
    private float laserTimer;
    private float baseMaxHealth; // Store original max health

    public void Start()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        towerRenderer = GetComponent<Renderer>();
        
        // Initialize visual feedback
        if (towerRenderer != null)
        {
            originalColor = towerRenderer.material.color;
        }
        
        // Initialize health bar
        healthBar = GetComponentInChildren<HealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
        }

        // Set fire point to tower center if not assigned
        if (firePoint == null)
        {
            firePoint = transform;
        }
        
        // Ensure laser starts disabled
        if (laserLine != null)
        {
            laserLine.enabled = false;
        }
    }

    void Update()
    {
        // Update laser visibility timer
        if (laserTimer > 0)
        {
            laserTimer -= Time.deltaTime;
            if (laserTimer <= 0 && laserLine != null)
            {
                laserLine.enabled = false;
            }
        }
        
        // Find and target enemies
        FindTarget();
        
        // Fire at target if available, otherwise deactivate laser
        if (currentTarget != null && CanFire())
        {
            FireLaser();
        }
        else if (currentTarget == null && laserLine != null)
        {
            // No enemies nearby - deactivate laser
            laserLine.enabled = false;
        }
    }

    void FindTarget()
    {
        // Clear target if it's destroyed or out of range
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            Health enemyHealth = currentTarget.GetComponent<Health>();
            if (enemyHealth == null || enemyHealth.GetHealth() <= 0 || distance > range)
            {
                currentTarget = null;
            }
        }
        
        // Find new target if we don't have one
        if (currentTarget == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float closestDistance = range;
            GameObject closestEnemy = null;
            
            foreach (GameObject enemy in enemies)
            {
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null && enemyHealth.GetHealth() > 0)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance <= range && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            }
            
            currentTarget = closestEnemy;
        }
    }

    bool CanFire()
    {
        return Time.time >= lastFireTime + (1f / fireRate);
    }

    void FireLaser()
    {
        if (currentTarget == null || laserLine == null) return;
        
        // Update fire time
        lastFireTime = Time.time;
        
        // Calculate distance to enemy for laser scaling
        Vector3 enemyCenter = currentTarget.transform.position + Vector3.up * 1f;
        float distanceToEnemy = Vector3.Distance(transform.position, enemyCenter);
        Vector3 direction = transform.InverseTransformDirection((enemyCenter - transform.position).normalized);
        
        // Scale the LineRenderer to match distance
        float laserLength = distanceToEnemy - 0.3f;
        laserLine.transform.localScale = new Vector3(1f, 1f, laserLength);
        
        // Set fixed positions - line extends in Z direction
        laserLine.SetPosition(0, Vector3.zero);
        laserLine.SetPosition(1, Vector3.forward);
        
        // Rotate line to point at enemy
        laserLine.transform.LookAt(enemyCenter);
        
        laserLine.enabled = true;
        laserTimer = laserDuration;
        
        // Deal damage to target
        Health enemyHealth = currentTarget.GetComponent<Health>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;
        
        // Apply damage resistance
        float actualDamage = damage * (1f - damageResistance);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Update visual feedback
        UpdateVisualFeedback();
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        if (currentHealth <= 0)
        {
            DestroyTower();
        }
    }

    private void UpdateVisualFeedback()
    {
        if (towerRenderer == null) return;
        
        float healthPercent = currentHealth / maxHealth;
        Color targetColor;
        
        if (healthPercent > 0.6f)
        {
            targetColor = Color.Lerp(originalColor, healthyColor, 0.3f);
        }
        else if (healthPercent > 0.3f)
        {
            targetColor = Color.Lerp(originalColor, damagedColor, 0.5f);
        }
        else
        {
            targetColor = Color.Lerp(originalColor, criticalColor, 0.7f);
        }
        
        towerRenderer.material.color = targetColor;
    }

    private void DestroyTower()
    {
        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw tower range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }

    // Upgrade Methods
    public void RepairTower()
    {
        currentHealth = Mathf.Min(currentHealth + repairAmount, maxHealth);
        
        // Update visual feedback
        UpdateVisualFeedback();
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
    }
    
    public void UpgradeMaxHealth()
    {
        maxHealthUpgrades++;
        maxHealth = baseMaxHealth + (maxHealthUpgrades * maxHealthUpgradeAmount);
        
        // Also heal the tower when upgrading max health
        currentHealth = Mathf.Min(currentHealth + maxHealthUpgradeAmount, maxHealth);
        
        // Update health bar with new max health
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
    }
    
    public void UpgradeDamageResistance(float resistanceIncrease)
    {
        damageResistance = Mathf.Min(damageResistance + resistanceIncrease, 0.8f); // Cap at 80%
        
    }

    // Public accessors
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsDestroyed() => currentHealth <= 0;
    public float GetDamageResistance() => damageResistance;
    public int GetMaxHealthUpgradeLevel() => maxHealthUpgrades;
}