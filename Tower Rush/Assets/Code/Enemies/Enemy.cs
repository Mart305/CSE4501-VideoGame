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
    protected Animator animator;
    
    [Header("NavMesh Settings")]
    [SerializeField] private float updateDestinationInterval = 0.5f;
    
    private float lastDestinationUpdateTime;
    private float lastTargetEvaluationTime = 0f;
    
    [Header("Effects")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float spawnEffectDuration = 1f;
    [SerializeField] private float deathEffectDuration = 2f;
    
    [Header("Animation")]
    [SerializeField] private string velocityParameterName = "velocity";
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string deathTriggerName = "death";
    private bool isAttacking = false;
    private bool isDead = false;

    protected virtual void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        if (!navAgent.enabled)
        {
            InitializeNavMeshAgent();
        }
        else
        {
            ConfigureNavMeshAgent();
        }
        
        FindTargetTower();
        
        slowEffect = GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = gameObject.AddComponent<SlowEffect>();
        }
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
    
    public void OnObjectSpawn()
    {
        InitializeNavMeshAgent();
        FindTargetTower();
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }
    
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
        
        ConfigureNavMeshAgent();
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 0.5f, NavMesh.AllAreas))
        {
            navAgent.Warp(hit.position);
            navAgent.enabled = true;
        }
        else
        {
            navAgent.enabled = true;
        }
    }
    
    private void ConfigureNavMeshAgent()
    {
        if (navAgent == null) return;
        
        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = attackRange + 1f;
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 120f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    protected virtual void Update()
    {
        if (navAgent == null) return;
        
        if (Time.time - lastTargetEvaluationTime >= updateDestinationInterval)
        {
            FindTargetTower();
            lastTargetEvaluationTime = Time.time;
        }
        
        if (targetTower == null || targetTower.GetCurrentHealth() <= 0)
        {
            FindTargetTower();
            navAgent.isStopped = true;
            UpdateAnimatorVelocity();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTower.transform.position);

        if (distance > attackRange)
        {
            isAttacking = false;
            MoveTowardsTower();
        }
        else
        {
            navAgent.isStopped = true;
            isAttacking = true;
            AttackTower();
        }
        
        UpdateAnimatorVelocity();
        UpdateAnimatorAttack();
    }

    protected virtual void MoveTowardsTower()
    {
        if (navAgent == null || targetTower == null) return;
        
        float effectiveMoveSpeed = moveSpeed;
        if (slowEffect != null)
        {
            effectiveMoveSpeed *= slowEffect.GetSpeedMultiplier();
        }
        
        navAgent.speed = effectiveMoveSpeed;
        navAgent.isStopped = false;
        
        if (Time.time - lastDestinationUpdateTime >= updateDestinationInterval)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetTower.transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
            else
            {
                navAgent.SetDestination(targetTower.transform.position);
            }
            
            lastDestinationUpdateTime = Time.time;
        }
        
        if (navAgent.velocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = (targetTower.transform.position - transform.position);
            lookDirection.y = 0;
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
            TriggerAttackAnimation();
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
        
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        TriggerDeathAnimation();
        
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        PlayDeathEffect();
        Invoke(nameof(DestroyAfterDeathEffect), deathEffectDuration);
    }
    
    private void DestroyAfterDeathEffect()
    {
        Destroy(gameObject);
    }
    
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
    
    protected virtual void PlaySpawnEffect()
    {
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
            if (effect != null)
            {
                Destroy(effect, spawnEffectDuration);
            }
        }
        else
        {
            CreateSimpleSpawnEffect();
        }
    }
    
    protected virtual void PlayDeathEffect()
    {
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            if (effect != null)
            {
                Destroy(effect, deathEffectDuration);
            }
        }
        else
        {
            CreateSimpleDeathEffect();
        }
    }
    
    private void CreateSimpleSpawnEffect()
    {
        GameObject effectObject = new GameObject("SpawnEffect");
        effectObject.transform.position = transform.position;
        
        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = spawnEffectDuration;
        main.startSpeed = 2f;
        main.startSize = 0.5f;
        main.startColor = Color.green;
        main.maxParticles = 20;
        
        var emission = particles.emission;
        emission.rateOverTime = 50f;
        
        Destroy(effectObject, spawnEffectDuration);
    }
    
    private void CreateSimpleDeathEffect()
    {
        GameObject effectObject = new GameObject("DeathEffect");
        effectObject.transform.position = transform.position;
        
        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = deathEffectDuration;
        main.startSpeed = 3f;
        main.startSize = 0.3f;
        main.startColor = Color.red;
        main.maxParticles = 30;
        
        var emission = particles.emission;
        emission.rateOverTime = 100f;
        
        Destroy(effectObject, deathEffectDuration);
    }
    
    private void SetEnemyVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
    
    protected virtual void UpdateAnimatorVelocity()
    {
        if (animator == null || navAgent == null || isDead) return;
        
        float velocity = isAttacking ? 0f : navAgent.velocity.magnitude;
        animator.SetFloat(velocityParameterName, velocity);
    }
    
    protected virtual void TriggerAttackAnimation()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger(attackTriggerName);
    }
    
    protected virtual void TriggerDeathAnimation()
    {
        if (animator == null) return;
        
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == deathTriggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }
        
        if (!hasParameter) return;
        
        animator.SetTrigger(deathTriggerName);
    }
    
    protected virtual void UpdateAnimatorAttack()
    {
        if (animator == null || isDead) return;
    }
}
