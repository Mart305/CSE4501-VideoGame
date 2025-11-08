using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviour, IPooledObject
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
    protected NavMeshAgent navAgent;
    
    [Header("Animation")]
    protected Animator animator;
    
    [Header("NavMesh Settings")]
    [SerializeField] private float updateDestinationInterval = 0.5f; // Update destination every 0.5s instead of every frame
    
    private float lastDestinationUpdateTime;
    private float lastTargetEvaluationTime = 0f; // Track when we last evaluated which tower to target
    
    [Header("Effects")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float spawnEffectDuration = 1f;
    [SerializeField] private float deathEffectDuration = 2f;

    protected virtual void Start()
    {
        // Spawn effects are now handled by the portal system in SpawnEffectManager
        
        // Get Animator component
        animator = GetComponent<Animator>();
        
        // Get or add NavMeshAgent component
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        // Enable NavMeshAgent if it was disabled (from prefab or pool)
        if (!navAgent.enabled)
        {
            InitializeNavMeshAgent();
        }
        else
        {
            // Just configure if already enabled
            ConfigureNavMeshAgent();
        }
        
        FindTargetTower();
        
        // Get or add SlowEffect component
        slowEffect = GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = gameObject.AddComponent<SlowEffect>();
        }
        
        // Subscribe to Health component death event if it exists
        Health healthComponent = GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.OnDeath.AddListener(OnHealthComponentDeath);
        }
    }
    
    private void OnHealthComponentDeath()
    {
        // Health component detected death, trigger Enemy.Die() for animation
        if (!isDead)
        {
            Die();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from Health component event
        Health healthComponent = GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.OnDeath.RemoveListener(OnHealthComponentDeath);
        }
    }
    
    // IPooledObject interface - called when spawned from pool
    public void OnObjectSpawn()
    {
        // Initialize NavMeshAgent when spawned at valid position
        InitializeNavMeshAgent();
        FindTargetTower();
    }
    
    // Initialize and enable NavMeshAgent
    private void InitializeNavMeshAgent()
    {
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
        }
        
        // Configure NavMeshAgent settings
        ConfigureNavMeshAgent();
        
        // If enemy has Rigidbody, set it to kinematic (required for NavMeshAgent)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Warp to nearest NavMesh position to prevent warnings and enable agent
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 0.5f, NavMesh.AllAreas)) // Reduced from 10f to prevent distant teleporting
        {
            navAgent.Warp(hit.position);
            navAgent.enabled = true;
        }
        else
        {
            // If no NavMesh found, enable anyway (spawn position should be valid)
            navAgent.enabled = true;
        }
    }
    
    // Configure NavMeshAgent settings (without enabling/warping)
    private void ConfigureNavMeshAgent()
    {
        if (navAgent == null) return;
        
        navAgent.speed = moveSpeed;
        // Add small buffer to stoppingDistance to prevent enemies going inside towers
        navAgent.stoppingDistance = attackRange + 1f; // Added 1f buffer
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 120f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    protected virtual void Update()
    {
        if (navAgent == null) return;
        
        // Update animations
        UpdateAnimations();
        
        // Re-evaluate target to catch new towers being placed
        if (Time.time - lastTargetEvaluationTime >= updateDestinationInterval)
        {
            FindTargetTower();
            lastTargetEvaluationTime = Time.time;
        }
        
        if (targetTower == null || targetTower.GetCurrentHealth() <= 0)
        {
            FindTargetTower();
            navAgent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTower.transform.position);

        if (distance > attackRange)
        {
            MoveTowardsTower();
        }
        else
        {
            // Stop moving when in attack range
            navAgent.isStopped = true;
            AttackTower();
        }
    }

    protected virtual void MoveTowardsTower()
    {
        if (navAgent == null || targetTower == null) return;
        
        // Apply slow effect if present
        float effectiveMoveSpeed = moveSpeed;
        if (slowEffect != null)
        {
            effectiveMoveSpeed *= slowEffect.GetSpeedMultiplier();
        }
        
        navAgent.speed = effectiveMoveSpeed;
        navAgent.isStopped = false;
        
        // Update destination periodically (not every frame for performance)
        if (Time.time - lastDestinationUpdateTime >= updateDestinationInterval)
        {
            // Check if destination is valid and on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetTower.transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
            else
            {
                // Fallback: try to set destination directly (may fail if off NavMesh)
                navAgent.SetDestination(targetTower.transform.position);
            }
            
            lastDestinationUpdateTime = Time.time;
        }
        
        // Face the target tower while moving
        if (navAgent.velocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = (targetTower.transform.position - transform.position);
            lookDirection.y = 0; // Keep rotation on horizontal plane
            if (lookDirection.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    Quaternion.LookRotation(lookDirection.normalized), 
                    Time.deltaTime * 5f
                );
            }
        }
    }

    protected virtual void AttackTower()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // Trigger attack animation
            isAttacking = true;
            if (animator != null)
            {
                animator.SetBool("IsAttacking", true);
            }
            
            targetTower.TakeDamage(damage);
            lastAttackTime = Time.time;
            
            // Reset attack animation after attack duration
            Invoke(nameof(ResetAttackAnimation), 0.5f);
        }
    }
    
    private void ResetAttackAnimation()
    {
        isAttacking = false;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
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
        if (isDead) return;
        isDead = true;
        
        // Stop NavMeshAgent
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", false);
            
            // For skeletons with two-stage death (Fall → Dead), the Animator Controller
            // will handle the transition from Fall to Dead state automatically
        }
        
        // Disable colliders so dead enemies don't block anything
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Play death effect before destroying
        PlayDeathEffect();
        
        // Wait for death animation to play, then hide and destroy
        // Death animation length varies, so we'll wait a bit longer
        Invoke(nameof(HideEnemyAfterDeath), 1.5f);
        Invoke(nameof(DestroyAfterDeathEffect), deathEffectDuration);
    }
    
    private void HideEnemyAfterDeath()
    {
        // Hide the enemy visually after death animation plays
        SetEnemyVisibility(false);
    }
    
    private void DestroyAfterDeathEffect()
    {
        // Cancel any pending invokes before destroying
        CancelInvoke();
        Destroy(gameObject);
    }
    
    // Public methods for wave scaling
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
        }
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
    
    // Effect Methods
    protected virtual void PlaySpawnEffect()
    {
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
            
            // Destroy effect after duration
            if (effect != null)
            {
                Destroy(effect, spawnEffectDuration);
            }
        }
        else
        {
            // Fallback: Simple particle effect using built-in components
            CreateSimpleSpawnEffect();
        }
    }
    
    protected virtual void PlayDeathEffect()
    {
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            
            // Destroy effect after duration
            if (effect != null)
            {
                Destroy(effect, deathEffectDuration);
            }
        }
        else
        {
            // Fallback: Simple particle effect using built-in components
            CreateSimpleDeathEffect();
        }
    }
    
    private void CreateSimpleSpawnEffect()
    {
        // Create a simple spawn effect using a particle system
        GameObject effectObject = new GameObject("SpawnEffect");
        effectObject.transform.position = transform.position;
        
        // Add particle system
        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = spawnEffectDuration;
        main.startSpeed = 2f;
        main.startSize = 0.5f;
        main.startColor = Color.green;
        main.maxParticles = 20;
        
        var emission = particles.emission;
        emission.rateOverTime = 50f;
        
        // Destroy after duration
        Destroy(effectObject, spawnEffectDuration);
    }
    
    private void CreateSimpleDeathEffect()
    {
        // Create a simple death effect using a particle system
        GameObject effectObject = new GameObject("DeathEffect");
        effectObject.transform.position = transform.position;
        
        // Add particle system
        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = deathEffectDuration;
        main.startSpeed = 3f;
        main.startSize = 0.3f;
        main.startColor = Color.red;
        main.maxParticles = 30;
        
        var emission = particles.emission;
        emission.rateOverTime = 100f;
        
        // Destroy after duration
        Destroy(effectObject, deathEffectDuration);
    }
    
    // Helper method to show/hide enemy visual components
    private void SetEnemyVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
    
    // Animation state tracking
    private bool isDead = false;
    private bool isAttacking = false;
    
    // Animation update method
    protected virtual void UpdateAnimations()
    {
        if (animator == null || navAgent == null || isDead) return;
        
        // Check if enemy is moving
        bool isMoving = navAgent.velocity.magnitude > 0.1f && !navAgent.isStopped;
        
        // Set animation parameters
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsAttacking", isAttacking);
    }
}
