using UnityEngine;
using UnityEngine.AI;

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
    protected NavMeshAgent navAgent;
    
    [Header("NavMesh Settings")]
    [SerializeField] private float stoppingDistance = 1.2f;
    [SerializeField] private float updateDestinationInterval = 0.5f; // Update destination every 0.5s instead of every frame
    
    private float lastDestinationUpdateTime;
    
    [Header("Effects")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float spawnEffectDuration = 1f;
    [SerializeField] private float deathEffectDuration = 2f;

    protected virtual void Start()
    {
        // Spawn effects are now handled by the portal system in SpawnEffectManager
        // PlaySpawnEffect(); // Disabled - using portal effects instead
        
        // Get or add NavMeshAgent component
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        // Configure NavMeshAgent
        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 120f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        
        // If enemy has Rigidbody, set it to kinematic (required for NavMeshAgent)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
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
        if (navAgent == null) return;
        
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
        // Stop NavMeshAgent
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        // Play death effect before destroying
        PlayDeathEffect();
        
        // Destroy after effect duration
        Invoke(nameof(DestroyAfterDeathEffect), deathEffectDuration);
    }
    
    private void DestroyAfterDeathEffect()
    {
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
}
