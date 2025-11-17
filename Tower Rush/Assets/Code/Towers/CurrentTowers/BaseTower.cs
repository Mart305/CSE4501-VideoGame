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
    protected bool hasShot = false; // TEMP: Only shoot once for testing

    public virtual void Start()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        towerRenderer = GetComponent<Renderer>();
        
        // Initialize fire time to allow immediate first shot
        lastFireTime = -999f;
        
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
        
        // Find closest enemy to target
        FindTarget();
        
        // Shoot at target if we have one
        if (currentTarget != null && Time.time - lastFireTime >= 1f / fireRate)
        {
            PerformAttack();
            lastFireTime = Time.time;
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
        // Only find new target if we don't have one
        if (currentTarget != null) return;
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

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

        // Set new target (closest enemy)
        currentTarget = closestEnemy;
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
            
            // Clear target after shooting to retarget closest enemy next frame
            currentTarget = null;
        }
    }

    protected virtual void PerformAttack()
    {
        if (attackFXPrefab != null)
        {
            Vector3 attackPos = firePoint != null ? firePoint.position : transform.position;
            GameObject attackFXObj = Instantiate(attackFXPrefab);
            
            // Remove old scripts but add our simple collision script
            MonoBehaviour[] scripts = attackFXObj.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                Destroy(script);
            }
            
            // Remove any existing colliders and rigidbodies from prefab
            Collider[] existingColliders = attackFXObj.GetComponents<Collider>();
            foreach (Collider col in existingColliders)
            {
                Destroy(col);
            }
            
            Rigidbody[] existingRigidbodies = attackFXObj.GetComponents<Rigidbody>();
            foreach (Rigidbody rb in existingRigidbodies)
            {
                Destroy(rb);
            }
            
            // Add kinematic rigidbody (needed for trigger detection)
            Rigidbody projectileRb = attackFXObj.AddComponent<Rigidbody>();
            projectileRb.isKinematic = true;
            projectileRb.useGravity = false;
            projectileRb.interpolation = RigidbodyInterpolation.None;
            projectileRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            
            // Add sphere collider as trigger
            SphereCollider sphereCol = attackFXObj.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 1.5f;
            sphereCol.center = Vector3.zero;
            
            // Add simple projectile script for collision detection
            SimpleProjectile projectile = attackFXObj.AddComponent<SimpleProjectile>();
            projectile.Initialize(currentTarget, damage, explosionFX);
            projectile.towerPosition = transform.position; // Store tower position for warp ability
            
            // Set tower-specific abilities (override in subclasses)
            ConfigureProjectile(projectile);
            
            // Scale
            attackFXObj.transform.localScale = Vector3.one * 0.3f;
            attackFXObj.transform.position = attackPos;
            
            // Scale children
            foreach (Transform child in attackFXObj.transform)
            {
                child.localScale = Vector3.one * 0.3f;
            }

            // Aim at enemy if we have a target, otherwise shoot forward
            if (currentTarget != null)
            {
                Vector3 enemyCenter = currentTarget.transform.position + Vector3.up * 1f;
                attackFXObj.transform.LookAt(enemyCenter);
            }
            else
            {
                attackFXObj.transform.rotation = transform.rotation;
            }

            // Get particle system and configure it
            ParticleSystem ps = attackFXObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Set simulation space to Local so particles move with GameObject
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.stopAction = ParticleSystemStopAction.None;
                
                // Disable particle collision (we use GameObject collider instead)
                var collision = ps.collision;
                collision.enabled = false;
                
                // Disable any velocity modules that might affect direction
                var velocityOverLifetime = ps.velocityOverLifetime;
                velocityOverLifetime.enabled = false;
                
                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.enabled = false;
                
                var inheritVelocity = ps.inheritVelocity;
                inheritVelocity.enabled = false;
                
                ps.Play();
            }
        }
    }

    protected virtual void ConfigureProjectile(SimpleProjectile projectile)
    {
        // Base implementation does nothing
        // Override in subclasses to add tower-specific abilities
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
		// Calculate deduction amount based on current wave
		if (CurrencyManager.Instance != null) {
			// Get base deduction amount
			int baseDeductAmount = 150;

			// Calculate multiplier based on wave number
			int currentWave = 1;
			if (WaveManager.Instance != null) {
				currentWave = WaveManager.Instance.GetCurrentWave();
			}

			// Calculate which set of 5 waves we're in (0-based)
			int waveTier = (currentWave - 1) / 5;

			// Calculate multiplier: 1, 2, 4, 16, 32, etc. (2^tier)
			int multiplier = 1;
			for (int i = 0; i < waveTier; i++) {
				multiplier *= 1;
			}

			// Calculate final deduction amount
			int deductAmount = baseDeductAmount * multiplier;

			// Cap at player's current currency so it doesn't go negative
			int currentCurrency = CurrencyManager.Instance.GetCurrentCurrency();

			// Store the previous gold amount before deduction
			int previousGold = currentCurrency;

			deductAmount = Mathf.Min(currentCurrency, deductAmount);

			if (deductAmount > 0) {
				CurrencyManager.Instance.SpendCurrency(deductAmount);

				// Show visual feedback
				if (GameHUD.Instance != null) {
					GameHUD.Instance.ShowCurrencyChange(-deductAmount);
				}

				// Get the new gold amount after deduction
			}
		}

		// Rest of your existing OnTowerDestroyed code...
		StopAllCoroutines();
		if (towerFX != null) towerFX.Stop();
		if (attackFX != null) attackFX.Stop();
		if (explosionFX != null) explosionFX.Stop();
		currentTarget = null;
		CheckForDefeat();
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

		foreach (BaseTower tower in remainingTowers) {
			if (tower != this && tower.GetCurrentHealth() > 0f) {
				aliveTowers++;
			}
		}

		// If any tower is still alive, do nothing
		if (aliveTowers > 0) return;

		// No towers left: evaluate affordability immediately (even during wave)
		int currency = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrentCurrency() : 0;

		var tpm = TowerPlacementManager.Instance;
		if (tpm == null) {
			GameStateManager.Instance?.ShowDefeat();
			return;
		}

		var available = tpm.GetAvailableTowers();
		if (available == null || available.Count == 0) {
			GameStateManager.Instance?.ShowDefeat();
			return;
		}

		int minCost = int.MaxValue;
		foreach (var td in available) {
			if (td != null && td.cost >= 0 && td.cost < minCost) {
				minCost = td.cost;
			}
		}

		if (minCost == int.MaxValue || currency < minCost) {
			GameStateManager.Instance?.ShowDefeat();
		}
		// else: player can still afford at least one tower; allow them to place it
	}
}
