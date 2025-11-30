using UnityEngine;
using System.Collections;

public class SkeletonNecromancer : Enemy
{
    private Health healthComponent;
    
    [Header("Animation")]
    [SerializeField] private string spawnTriggerName = "spawn";
    
    [Header("Summon Settings")]
    [SerializeField] private GameObject skeletonPrefab; // Assign in Inspector or get from EnemySpawner
    [SerializeField] private GameObject ghostPrefab; // Assign in Inspector or get from EnemySpawner
    [SerializeField] private float summonCooldown = 1.2f; // Summon every 1.2 seconds (faster for crew building)
    [SerializeField] private float initialSummonDelay = 0.5f; // Start summoning 0.5 seconds after spawn
    [SerializeField] private float summonRange = 3f; // Spawn minions nearby
    [SerializeField] private int maxActiveMinions = 6; // Limit active minions (increased for crew)
    [SerializeField] private float skeletonSpawnChance = 0.6f; // 60% skeleton, 40% ghost
    private float lastSummonTime = 0f;
    private int activeMinionCount = 0;
    
    [Header("Dark Bolt Projectile")]
    [SerializeField] private GameObject darkBoltPrefab; // Optional: custom projectile prefab
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private Transform firePoint; // Optional: where to spawn projectile from
    
    protected override void Start()
    {
        // Skeleton Necromancer stats - mid-tier threat (between regular enemies and mutant zombies)
        // Slower than minions (skeletons 10f, ghosts 7f) so minions protect the mage
        moveSpeed = 2f;    // Much slower than minions - mage stays protected
        health = 180f;     // More health than regular enemies, less than mutant zombie
        damage = 25f;      // Moderate damage
        attackCooldown = 2.2f; // Slower attack speed
        attackRange = 12f;  // Increased ranged attack range

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(180f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(180f);
        }

        base.Start();
        
        // Get minion prefabs from EnemySpawner if not assigned
        if (skeletonPrefab == null || ghostPrefab == null)
        {
            GetMinionPrefabsFromSpawner();
        }
        
        // Initialize summon timer - start 1 second after spawn
        lastSummonTime = Time.time + initialSummonDelay;
    }
    
    private void GetMinionPrefabsFromSpawner()
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            if (skeletonPrefab == null)
                skeletonPrefab = spawner.skeletonPrefab;
            if (ghostPrefab == null)
                ghostPrefab = spawner.ghostPrefab;
        }
    }
    
    protected override void Update()
    {
        // Stop all behavior if dead
        if (isDead) return;
        
        // Update minion count
        UpdateMinionCount();
        
        // Check if we should summon minions
        if (Time.time - lastSummonTime >= summonCooldown && activeMinionCount < maxActiveMinions)
        {
            SummonMinion();
            lastSummonTime = Time.time;
        }
        
        // Normal update behavior (movement, targeting, etc.)
        base.Update();
    }
    
    private void UpdateMinionCount()
    {
        // Count active minions (ghosts and skeletons with "Enemy" tag)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int count = 0;
        float maxDistance = 20f; // Only count minions nearby
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || enemy == gameObject) continue;
            
            // Check if it's a ghost or skeleton
            Ghost ghost = enemy.GetComponent<Ghost>();
            Skeleton skeleton = enemy.GetComponent<Skeleton>();
            
            if ((ghost != null || skeleton != null))
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= maxDistance)
                {
                    count++;
                }
            }
        }
        
        activeMinionCount = count;
    }
    
    private void SummonMinion()
    {
        // Determine which minion to spawn (random weighted)
        GameObject minionPrefab = Random.value < skeletonSpawnChance ? skeletonPrefab : ghostPrefab;
        
        if (minionPrefab == null) return;
        
        // Trigger spawn animation
        TriggerSpawnAnimation();
        
        // Spawn minion in front of necromancer (where it's facing/moving)
        // Get forward direction - use NavMeshAgent velocity if moving, otherwise use transform.forward
        Vector3 forwardDirection = transform.forward;
        if (navAgent != null && navAgent.velocity.magnitude > 0.1f)
        {
            forwardDirection = navAgent.velocity.normalized;
        }
        
        // Spawn in front with some spread (left/right variation)
        float forwardDistance = summonRange * 0.7f; // Spawn mostly in front
        float sideSpread = summonRange * 0.4f; // Some left/right variation
        Vector3 forwardOffset = forwardDirection * forwardDistance;
        Vector3 sideOffset = transform.right * Random.Range(-sideSpread, sideSpread);
        
        Vector3 spawnPos = transform.position + forwardOffset + sideOffset;
        spawnPos.y = transform.position.y; // Keep on ground level
        
        // Ensure spawn position is on NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        
        // Instantiate minion (no portal effects for summoned minions)
        GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        
        // Simple, subtle visual effect - just a small flash
        CreateSubtleSummonEffect(spawnPos);
    }
    
    private void TriggerSpawnAnimation()
    {
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null) return;
        
        // Check if spawn parameter exists
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == spawnTriggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }
        
        if (hasParameter)
        {
            animator.SetTrigger(spawnTriggerName);
        }
    }
    
    private void CreateSubtleSummonEffect(Vector3 position)
    {
        // Create a subtle, minimal summon effect - just a small flash
        GameObject effectObj = new GameObject("SummonEffect");
        effectObj.transform.position = position;
        
        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        
        // Stop the particle system first before modifying properties
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.4f;
        main.startSpeed = 1f;
        main.startSize = 0.2f;
        main.startColor = new Color(0.6f, 0.2f, 0.9f, 0.7f); // Subtle purple, semi-transparent
        main.maxParticles = 10; // Much fewer particles
        
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 10)
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f; // Smaller radius
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Now play after all properties are set
        ps.Play();
        Destroy(effectObj, 0.5f); // Clean up quickly
    }
    
    protected override void AttackTower()
    {
        if (isDead) return;
        
        // Ranged attack - fire dark bolt projectile
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            if (targetTower != null)
            {
                FireDarkBolt();
                lastAttackTime = Time.time;
                TriggerAttackAnimation();
            }
        }
    }
    
    private void FireDarkBolt()
    {
        if (targetTower == null) return;
        
        // Create dark bolt projectile
        GameObject darkBolt;
        
        if (darkBoltPrefab != null)
        {
            darkBolt = Instantiate(darkBoltPrefab);
            
            // Clean up prefab - remove any conflicting components
            // Remove tower projectile scripts that are designed for hitting enemies
            MonoBehaviour[] scripts = darkBolt.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                // Keep only DarkBoltProjectile if it exists, remove everything else
                if (!(script is DarkBoltProjectile))
                {
                    DestroyImmediate(script);
                }
            }
            
            // Remove any existing colliders and rigidbodies
            Collider[] existingColliders = darkBolt.GetComponents<Collider>();
            foreach (Collider col in existingColliders)
            {
                DestroyImmediate(col);
            }
            
            Rigidbody[] existingRigidbodies = darkBolt.GetComponents<Rigidbody>();
            foreach (Rigidbody rb in existingRigidbodies)
            {
                DestroyImmediate(rb);
            }
            
            // Set up proper collider and rigidbody for tower detection
            SetupProjectileCollider(darkBolt);
        }
        else
        {
            darkBolt = CreateDarkBoltProjectile();
        }
        
        // Position projectile
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        darkBolt.transform.position = spawnPos;
        
        // Aim at tower
        Vector3 targetPos = targetTower.transform.position + Vector3.up * 1f;
        darkBolt.transform.LookAt(targetPos);
        
        // Add projectile component (or get existing one)
        DarkBoltProjectile projectile = darkBolt.GetComponent<DarkBoltProjectile>();
        if (projectile == null)
        {
            projectile = darkBolt.AddComponent<DarkBoltProjectile>();
        }
        
        projectile.Initialize(targetTower, damage, projectileSpeed);
    }
    
    private void SetupProjectileCollider(GameObject projectile)
    {
        // Add Rigidbody for proper trigger detection
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Add trigger collider
        SphereCollider trigger = projectile.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.3f;
        
        // Set layer
        projectile.layer = LayerMask.NameToLayer("Default");
    }
    
    private GameObject CreateDarkBoltProjectile()
    {
        // Create simple dark bolt projectile
        GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bolt.name = "DarkBolt";
        bolt.transform.localScale = Vector3.one * 0.3f;
        
        // Set layer to ignore enemy collisions
        bolt.layer = LayerMask.NameToLayer("Default");
        
        // Add Rigidbody for proper trigger detection
        Rigidbody rb = bolt.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Kinematic so it doesn't fall, but still detects triggers
        rb.useGravity = false;
        
        // Remove default collider and add trigger
        Destroy(bolt.GetComponent<Collider>());
        SphereCollider trigger = bolt.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.3f;
        
        // Set material color (dark purple/black)
        Renderer renderer = bolt.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.3f, 0.1f, 0.5f); // Dark purple
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Smoothness", 0.9f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.05f, 0.4f));
            renderer.material = mat;
        }
        
        return bolt;
    }

    public override void TakeDamage(float amount)
    {
        // Use Health component instead of base Enemy health system
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(amount);
        }
        else
        {
            base.TakeDamage(amount);
        }
    }
    
    // Override to make Necromancer target weakest tower (like Ghost)
    protected override void FindTargetTower()
    {
        BaseTower[] towers = FindObjectsOfType<BaseTower>();
        BaseTower weakest = null;
        float lowestHealth = Mathf.Infinity;

        foreach (BaseTower t in towers)
        {
            if (t.GetCurrentHealth() <= 0) continue;
            if (t.GetCurrentHealth() < lowestHealth)
            {
                lowestHealth = t.GetCurrentHealth();
                weakest = t;
            }
        }

        targetTower = weakest;
    }
}

// Dark Bolt Projectile Component
public class DarkBoltProjectile : MonoBehaviour
{
    private BaseTower target;
    private float damage;
    private float speed;
    private bool hasHit = false;
    private float lifetime = 0f;
    private float maxLifetime = 5f;
    
    public void Initialize(BaseTower targetTower, float projectileDamage, float projectileSpeed)
    {
        target = targetTower;
        damage = projectileDamage;
        speed = projectileSpeed;
    }
    
    void Update()
    {
        if (hasHit) return;
        
        lifetime += Time.deltaTime;
        if (lifetime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        if (target == null || target.GetCurrentHealth() <= 0)
        {
            Destroy(gameObject);
            return;
        }
        
        // Home towards target
        Vector3 targetPos = target.transform.position + Vector3.up * 1f;
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        
        // Move forward
        transform.position += transform.forward * speed * Time.deltaTime;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // Check if we hit a tower - try multiple methods to find the tower
        BaseTower tower = other.GetComponent<BaseTower>();
        if (tower == null)
        {
            // Try parent
            tower = other.GetComponentInParent<BaseTower>();
        }
        if (tower == null)
        {
            // Try root
            tower = other.transform.root.GetComponent<BaseTower>();
        }
        if (tower == null)
        {
            // Try searching in children (in case collider is on a child object)
            tower = other.GetComponentInChildren<BaseTower>();
        }
        
        // If we have a target, prefer hitting that specific tower, otherwise hit any tower
        if (tower != null)
        {
            // If we have a specific target, only hit that one
            if (target != null && tower != target)
            {
                return; // Not our target, ignore
            }
            
            hasHit = true;
            
            // Deal damage - ensure damage is valid
            if (damage > 0 && tower != null)
            {
                tower.TakeDamage(damage);
            }
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }
    
    // Also check for non-trigger collisions (in case tower collider is not a trigger)
    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        Collider other = collision.collider;
        
        // Check if we hit a tower - try multiple methods to find the tower
        BaseTower tower = other.GetComponent<BaseTower>();
        if (tower == null)
        {
            // Try parent
            tower = other.GetComponentInParent<BaseTower>();
        }
        if (tower == null)
        {
            // Try root
            tower = other.transform.root.GetComponent<BaseTower>();
        }
        if (tower == null)
        {
            // Try searching in children
            tower = other.GetComponentInChildren<BaseTower>();
        }
        
        // If we have a target, prefer hitting that specific tower, otherwise hit any tower
        if (tower != null)
        {
            // If we have a specific target, only hit that one
            if (target != null && tower != target)
            {
                return; // Not our target, ignore
            }
            
            hasHit = true;
            
            // Deal damage - ensure damage is valid
            if (damage > 0 && tower != null)
            {
                tower.TakeDamage(damage);
            }
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }
}

