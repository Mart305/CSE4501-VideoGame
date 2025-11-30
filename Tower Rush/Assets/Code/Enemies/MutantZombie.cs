using UnityEngine;
using System.Collections;

public class MutantZombie : Enemy
{
    private Health healthComponent;
    private bool isRaging = false;
    private bool hasEnteredRage = false; // Track if rage was triggered
    
    [Header("Rage Animation")]
    [SerializeField] private string rageTriggerName = "rage";
    
    [Header("Rage Mode")]
    [SerializeField] private float rageHealthThreshold = 0.5f; // 50% health
    [SerializeField] private float rageDamageMultiplier = 1.5f; // 1.5x damage in rage
    [SerializeField] private float rageAttackSpeedMultiplier = 0.8f; // 0.8x cooldown = faster attacks
    private float baseDamage;
    private float baseAttackCooldown;
    
    [Header("Ground Slam")]
    [SerializeField] private int normalAttacksBeforeSlam = 4; // Every 4 normal attacks
    [SerializeField] private int rageAttacksBeforeSlam = 2; // Every 2 attacks in rage
    [SerializeField] private float groundSlamRadius = 5f;
    [SerializeField] private float groundSlamDamage = 75f; // Higher than normal attack
    [SerializeField] private float groundSlamCooldown = 3f; // Cooldown after slam
    private int attackCount = 0;
    private float lastSlamTime = 0f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject rageAuraPrefab; // Optional: red aura particle effect
    [SerializeField] private GameObject groundSlamEffectPrefab; // Optional: shockwave effect
    private GameObject rageAuraInstance;
    
    protected override void Start()
    {
        // Mutant Zombie stats - significantly harder (boss enemy)
        moveSpeed = 4.5f;
        health = 600f;
        damage = 50f;
        attackCooldown = 1.8f;
        attackRange = 2f;

        // Store base values for rage calculations
        baseDamage = damage;
        baseAttackCooldown = attackCooldown;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(600f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(600f);
        }

        base.Start();
        
        // Trigger initial rage animation on spawn (just like attack/death triggers)
        TriggerRageAnimation();
    }
    
    private void TriggerRageAnimation()
    {
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null) return;
        
        // Check if rage parameter exists (same pattern as death trigger)
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == rageTriggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }
        
        if (hasParameter)
        {
            animator.SetTrigger(rageTriggerName);
            isRaging = true;
            
            // Stop movement during rage animation
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }
        }
    }
    
    protected override void Update()
    {
        // Stop all behavior if dead
        if (isDead) return;
        
        // Check if still in initial rage animation - prevent movement during animation
        if (isRaging && !hasEnteredRage)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                // Check if we're no longer in rage state
                if (!stateInfo.IsName("Rage"))
                {
                    isRaging = false;
                    // Re-enable movement after initial rage animation
                    if (navAgent != null)
                    {
                        navAgent.isStopped = false;
                    }
                }
                else
                {
                    // Still in initial rage animation - keep movement stopped
                    if (navAgent != null)
                    {
                        navAgent.isStopped = true;
                    }
                    // Only update animator velocity, don't move
                    UpdateAnimatorVelocity();
                    return;
                }
            }
        }
        
        // Normal update behavior
        base.Update();
    }

    public override void TakeDamage(float amount)
    {
        // Use Health component instead of base Enemy health system
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(amount);
            
            // Check if we should enter rage mode
            CheckRageMode();
        }
        else
        {
            base.TakeDamage(amount);
            CheckRageMode();
        }
    }
    
    private void CheckRageMode()
    {
        // Don't enter rage if already in rage mode
        if (hasEnteredRage) return;
        
        // Calculate health percentage
        float currentHealth = healthComponent != null ? healthComponent.GetHealth() : health;
        float maxHealth = healthComponent != null ? healthComponent.GetMaxHealth() : 600f;
        float healthPercentage = currentHealth / maxHealth;
        
        // Enter rage mode if below threshold
        if (healthPercentage <= rageHealthThreshold)
        {
            EnterRageMode();
        }
    }
    
    private void EnterRageMode()
    {
        if (hasEnteredRage) return; // Already in rage
        
        hasEnteredRage = true;
        isRaging = true;
        
        // Apply rage multipliers
        damage = baseDamage * rageDamageMultiplier;
        attackCooldown = baseAttackCooldown * rageAttackSpeedMultiplier;
        
        // Update NavMeshAgent speed (optional: could also increase move speed)
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed; // Keep same speed, or could increase
        }
        
        // Visual effect: Create red aura
        if (rageAuraPrefab != null)
        {
            rageAuraInstance = Instantiate(rageAuraPrefab, transform);
            rageAuraInstance.transform.localPosition = Vector3.zero;
        }
        else
        {
            // Create simple visual effect programmatically
            CreateRageAuraEffect();
        }
        
        // Optional: Play rage sound effect
        // AudioManager.Instance?.PlaySound("MutantZombieRage");
    }
    
    private void CreateRageAuraEffect()
    {
        // Create a simple particle system for rage aura
        GameObject auraObj = new GameObject("RageAura");
        auraObj.transform.SetParent(transform);
        auraObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem ps = auraObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.3f;
        main.startColor = Color.red;
        main.loop = true;
        main.maxParticles = 50;
        
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        ps.Play();
        rageAuraInstance = auraObj;
    }
    
    protected override void AttackTower()
    {
        if (isDead) return;
        
        // Check if we should do ground slam instead of normal attack
        if (ShouldPerformGroundSlam())
        {
            PerformGroundSlam();
            return;
        }
        
        // Normal attack
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            if (targetTower != null)
            {
                targetTower.TakeDamage(damage);
                attackCount++;
                lastAttackTime = Time.time;
                TriggerAttackAnimation();
            }
        }
    }
    
    private bool ShouldPerformGroundSlam()
    {
        // Can't slam if on cooldown
        if (Time.time - lastSlamTime < groundSlamCooldown) return false;
        
        // Determine how many attacks before slam
        int attacksNeeded = hasEnteredRage ? rageAttacksBeforeSlam : normalAttacksBeforeSlam;
        
        // Check if we've done enough attacks
        return attackCount >= attacksNeeded;
    }
    
    private void PerformGroundSlam()
    {
        // Reset attack count
        attackCount = 0;
        lastSlamTime = Time.time;
        
        // Find all towers within radius
        BaseTower[] allTowers = FindObjectsOfType<BaseTower>();
        int towersHit = 0;
        
        foreach (BaseTower tower in allTowers)
        {
            if (tower == null || tower.GetCurrentHealth() <= 0) continue;
            
            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if (distance <= groundSlamRadius)
            {
                // Deal damage to tower
                tower.TakeDamage(groundSlamDamage);
                towersHit++;
            }
        }
        
        // Visual effect
        if (groundSlamEffectPrefab != null)
        {
            GameObject effect = Instantiate(groundSlamEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            CreateGroundSlamEffect();
        }
        
        // Trigger attack animation (or special slam animation if you have one)
        TriggerAttackAnimation();
        
        // Optional: Screen shake or sound effect
        // CameraShake.Instance?.Shake(0.2f, 0.5f);
    }
    
    private void CreateGroundSlamEffect()
    {
        // Create shockwave particle effect
        GameObject effectObj = new GameObject("GroundSlamEffect");
        effectObj.transform.position = transform.position;
        
        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.8f;
        main.startSpeed = 8f;
        main.startSize = 0.5f;
        main.startColor = new Color(0.8f, 0.2f, 0.2f); // Dark red
        main.maxParticles = 100;
        
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 100)
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(8f);
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        ps.Play();
        Destroy(effectObj, 2f);
    }

    public override void Die()
    {
        // Clean up rage aura
        if (rageAuraInstance != null)
        {
            Destroy(rageAuraInstance);
        }
        
        base.Die();
    }

    // Override to make MutantZombie target the strongest tower instead of closest
    protected override void FindTargetTower()
    {
        BaseTower[] towers = FindObjectsOfType<BaseTower>();
        BaseTower strongest = null;
        float highestHealth = 0;

        foreach (BaseTower t in towers)
        {
            if (t.GetCurrentHealth() <= 0) continue;
            if (t.GetCurrentHealth() > highestHealth)
            {
                highestHealth = t.GetCurrentHealth();
                strongest = t;
            }
        }

        targetTower = strongest;
    }
}
