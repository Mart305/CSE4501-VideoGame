using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTower : MonoBehaviour
{
    [Header("Tower Health")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [Header("Visual Feedback")]
    [SerializeField] protected Color healthyColor = Color.white;
    [SerializeField] protected Color damagedColor = Color.yellow;
    [SerializeField] protected Color criticalColor = Color.red;

    [Header("Particle Effects")]
    [SerializeField] protected GameObject towerFXPrefab; // Ambient tower effect prefab
    [SerializeField] protected GameObject attackFXPrefab; // Attack projectile effect prefab  
    [SerializeField] protected GameObject explosionFXPrefab; // Impact explosion effect prefab
    
    // Runtime particle system instances
    protected ParticleSystem towerFX;
    protected ParticleSystem attackFX;
    protected ParticleSystem explosionFX; // Optional
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float range = 10f;
    [SerializeField] protected float damage = 25f;
    [SerializeField] protected float fireRate = 2f; // shots per second
    [SerializeField] protected float attackDuration = 0.5f; // How long attack particles play

    [Header("Upgrade System")]
    [SerializeField] protected float damageResistance = 0f; // 0-1 (0% to 100% resistance)
    [SerializeField] protected int maxHealthUpgrades = 0;
    [SerializeField] protected float maxHealthUpgradeAmount = 25f;
    [SerializeField] protected float repairAmount = 50f;

    protected Renderer towerRenderer;
    protected Color originalColor;
    protected HealthBar healthBar;
    protected GameObject currentTarget;
    protected float lastFireTime;
    protected float baseMaxHealth; // Store original max health

    public virtual void Start()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        towerRenderer = GetComponent<Renderer>();
        
        // Initialize visual feedback
        if (towerRenderer != null)
        {
            originalColor = towerRenderer.material.color;
        }
        
        // Initialize health bar (optional component)
        healthBar = GetComponentInChildren<HealthBar>();
        if (healthBar != null)
        {
            // Use the correct HealthBar methods
            healthBar.Initialize(maxHealth);
        }

        // Initialize particle systems
        InitializeParticleSystems();
        
        // Adjust tower stats for scale if needed
        AdjustStatsForScale();
    }

    protected virtual void InitializeParticleSystems()
    {
        float scaleFactor = transform.localScale.x;
        
        // Only instantiate towerFX and explosionFX at start
        if (towerFXPrefab != null)
        {
            GameObject towerFXObj = Instantiate(towerFXPrefab, transform);
            towerFX = towerFXObj.GetComponent<ParticleSystem>();
            
            // Scale particle system for 0.1x towers
            towerFXObj.transform.localScale = Vector3.one * scaleFactor;
            
            // Position towerFX at firePoint if available, otherwise at tower center
            if (firePoint != null)
            {
                towerFXObj.transform.position = firePoint.position;
            }
            // Tower FX stays active for ambient effects (like flames, energy, etc.)
            if (towerFX != null && !towerFX.isPlaying)
                towerFX.Play();
        }

        if (explosionFXPrefab != null)
        {
            GameObject explosionFXObj = Instantiate(explosionFXPrefab, transform);
            explosionFX = explosionFXObj.GetComponent<ParticleSystem>();
            
            // Scale particle system for 0.1x towers
            explosionFXObj.transform.localScale = Vector3.one * scaleFactor;
            
            // ExplosionFX will be positioned at impact points
            if (explosionFX != null)
                explosionFX.Stop();
        }

        // Don't instantiate attackFX here - create it only when attacking
    }


    protected virtual void AdjustStatsForScale()
    {
        float scaleFactor = transform.localScale.x;
        
        // Only adjust if significantly scaled down (less than 0.5x)
        if (scaleFactor < 0.5f)
        {
            // Don't scale range - keep full range for 0.1x towers
            // Don't scale damage - keep full damage for 0.1x towers
            // Towers should maintain their effectiveness regardless of visual scale
        }
    }

    protected virtual void Update()
    {
        // Don't do anything if tower is destroyed
        if (currentHealth <= 0) return;
        
        if (currentTarget != null)
        {
            AttackTarget();
        }
        else
        {
            FindTarget();
        }
    } // Handle mouse clicks for upgrade UI
    void OnMouseDown()
    {
        // Find and show the upgrade UI
        TowerUpgradeUI upgradeUI = GetComponentInChildren<TowerUpgradeUI>();
        if (upgradeUI != null)
        {
            upgradeUI.ShowUpgradePanel(this);
        }
    }

    protected virtual void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closestEnemy = null;
        float closestDistance = range;

        // Check if enemies are found
        if (enemies.Length == 0)
        {
            // Try alternative method - look for Enemy components
            Enemy[] enemyComponents = FindObjectsOfType<Enemy>();
            
            foreach (Enemy enemyComp in enemyComponents)
            {
                GameObject enemy = enemyComp.gameObject;
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        else
        {
            // Use tag-based search
            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        // Clear target if it's out of range or destroyed
        if (currentTarget != null)
        {
            float targetDistance = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (targetDistance > range || currentTarget == null)
            {
                currentTarget = null;
            }
        }

        // Set new target
        if (closestEnemy != null && currentTarget == null)
        {
            currentTarget = closestEnemy;
        }
    }

    protected virtual void AttackTarget()
    {
        if (currentTarget == null) return;
        if (currentHealth <= 0) return; // Don't attack if destroyed

        // Check fire rate
        if (Time.time - lastFireTime >= 1f / fireRate)
        {
            PerformAttack();
            lastFireTime = Time.time;
        }
    }

    protected virtual void PerformAttack()
    {
        if (currentTarget == null) return;

        // Play tower shooting sound
        if (AudioManager.Instance != null)
        {
            string towerType = this.GetType().Name;
            Vector3 soundPosition = firePoint != null ? firePoint.position : transform.position;
            AudioManager.Instance.PlayTowerShootSound(towerType, soundPosition);
        }

        // Create attackFX only when attacking
        if (attackFXPrefab != null)
        {
            Vector3 attackPos = firePoint != null ? firePoint.position : transform.position;
            GameObject attackFXObj = Instantiate(attackFXPrefab);
            attackFX = attackFXObj.GetComponent<ParticleSystem>();

            // Scale and position attack effect
            float scaleFactor = transform.localScale.x;
            attackFXObj.transform.localScale = Vector3.one * scaleFactor;
            attackFXObj.transform.position = attackPos;

            // Aim at enemy center (add height offset to target their center instead of feet)
            Vector3 enemyCenter = currentTarget.transform.position + Vector3.up * 1f;
            attackFXObj.transform.LookAt(enemyCenter);

            if (attackFX != null)
            {
                attackFX.Play();

                // Destroy attack effect after duration
                StartCoroutine(DestroyAttackFXAfterDelay(attackFXObj));
            }
        }

        // Deal damage immediately
        Health enemyHealth = currentTarget.GetComponent<Health>();
        Enemy enemyComponent = currentTarget.GetComponent<Enemy>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        else if (enemyComponent != null)
        {
            enemyComponent.TakeDamage(damage);
        }

        // Play explosion effect at target center
        if (explosionFX != null)
        {
            Vector3 enemyCenter = currentTarget.transform.position + Vector3.up * 1f;
            explosionFX.transform.position = enemyCenter;
            explosionFX.Play();
            StartCoroutine(StopExplosionAfterDelay());
        }
    }

    protected virtual IEnumerator StopExplosionAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (explosionFX != null)
        {
            explosionFX.Stop();
        }
    }

    protected virtual IEnumerator DestroyAttackFXAfterDelay(GameObject attackFXObj)
    {
        yield return new WaitForSeconds(attackDuration);
        if (attackFXObj != null)
        {
            Destroy(attackFXObj);
        }
    }

    protected virtual IEnumerator PlayExplosionAtTarget()
    {
        if (currentTarget == null || explosionFX == null) yield break;

        // Move explosion to target position
        Vector3 targetPosition = currentTarget.transform.position;
        explosionFX.transform.position = targetPosition;
        
        // Play explosion
        explosionFX.Play();
        
        // Stop explosion after a short time
        yield return new WaitForSeconds(1f);
        explosionFX.Stop();
    }


    protected virtual void UpdateHealthVisuals()
    {
        // Update health bar if it exists
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        // Update tower color based on health
        Renderer towerRenderer = GetComponent<Renderer>();
        if (towerRenderer != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            Color targetColor;

            if (healthPercentage > 0.6f)
                targetColor = healthyColor;
            else if (healthPercentage > 0.3f)
                targetColor = damagedColor;
            else
                targetColor = criticalColor;

            towerRenderer.material.color = Color.Lerp(towerRenderer.material.color, targetColor, Time.deltaTime * 2f);
        }
    }

    public virtual void TakeDamage(float amount)
    {
        // Apply damage resistance
        float actualDamage = amount * (1f - damageResistance);
        currentHealth -= actualDamage;
        
        // Update visual feedback based on health
        UpdateHealthVisuals();
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnTowerDestroyed();
        }
    }

    protected virtual void OnTowerDestroyed()
    {
        // Stop all coroutines to prevent continued shooting
        StopAllCoroutines();
        
        // Stop all particle effects
        if (towerFX != null) towerFX.Stop();
        if (attackFX != null) attackFX.Stop();
        if (explosionFX != null) explosionFX.Stop();
        
        // Clear target
        currentTarget = null;
        
        // Check if all towers are destroyed (defeat condition)
        CheckForDefeat();
        
        // Actually destroy the GameObject after a short delay
        Destroy(gameObject, 0.5f);
    }

    // Upgrade System Methods
    public virtual void RepairTower()
    {
        currentHealth = Mathf.Min(currentHealth + repairAmount, maxHealth);
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }

    public virtual void UpgradeMaxHealth()
    {
        maxHealthUpgrades++;
        maxHealth += maxHealthUpgradeAmount;
        currentHealth += maxHealthUpgradeAmount; // Also heal when upgrading
        
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }

    public virtual void UpgradeDamageResistance(float resistanceIncrease)
    {
        damageResistance = Mathf.Clamp01(damageResistance + resistanceIncrease);
    }

    // Getter methods for UI and other systems
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }
    public float GetDamageResistance() { return damageResistance; }
    public int GetMaxHealthUpgradeLevel() { return maxHealthUpgrades; }
    public float GetRange() { return range; }

    // Gizmo for showing range in editor
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
    
    private void CheckForDefeat()
    {
        // Count remaining towers after this one is destroyed
        BaseTower[] remainingTowers = FindObjectsOfType<BaseTower>();
        int aliveTowers = 0;
        
        foreach (BaseTower tower in remainingTowers)
        {
            if (tower != this && tower.currentHealth > 0)
            {
                aliveTowers++;
            }
        }
        
        // If no towers left, trigger defeat
        if (aliveTowers == 0)
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ShowDefeat();
            }
        }
    }
}
