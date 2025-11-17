using System.Collections;
using UnityEngine;

/// <summary>
/// Simple projectile that follows and destroys itself when hitting an enemy
/// </summary>
public class SimpleProjectile : MonoBehaviour
{
    private GameObject target;
    private bool hasHit = false;
    private float speed = 20f;
    private float damage = 0f;
    private ParticleSystem explosionFX;
    private float lifetime = 0f;
    private float maxLifetime = 5f; // Destroy after 5 seconds if no hit
    
    // Tower-specific ability parameters
    public string towerType = ""; // "Fire", "Ice", "Lightning", etc.
    public float areaRadius = 0f;
    public float areaMultiplier = 0f;
    public float slowAmount = 0f;
    public float slowDuration = 0f;
    public int chainCount = 0;
    public float chainRange = 0f;
    public float chainReduction = 0f;
    public float warpChance = 0f;
    public float warpDistance = 0f;
    public Vector3 towerPosition; // Position of the tower that fired this projectile
    public bool disableHoming = false; // Disable homing for straight-shot projectiles
    
    public void Initialize(GameObject targetEnemy, float projectileDamage = 0f, ParticleSystem explosion = null)
    {
        target = targetEnemy;
        damage = projectileDamage;
        explosionFX = explosion;
    }
    
    void Update()
    {
        // Don't move if we've already hit
        if (hasHit) return;
        
        // Track lifetime and destroy if too old
        lifetime += Time.deltaTime;
        if (lifetime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        // If target is dead/null, destroy immediately
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Home towards target if it exists (unless homing is disabled)
        if (target != null && !disableHoming)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1f;
            Vector3 direction = (targetPos - transform.position).normalized;
            
            // Smoothly rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        
        // Move forward in the direction we're facing
        transform.position += transform.forward * speed * Time.deltaTime;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Trigger collision used for towers that don't use particle collision (e.g., Lightning)
        // Also serves as backup for other towers
        
        // Prevent multiple hits
        if (hasHit) return;
        
        // Check if we hit Enemies or Ground layer
        int hitLayer = other.gameObject.layer;
        int enemiesLayer = LayerMask.NameToLayer("Enemies");
        int groundLayer = LayerMask.NameToLayer("Ground");
        
        bool isEnemyLayer = hitLayer == enemiesLayer;
        bool isGroundLayer = hitLayer == groundLayer;
        
        if (isEnemyLayer || isGroundLayer)
        {
            hasHit = true;
            
            // If we hit an enemy, try to deal damage via ParticleCollisionHandler
            if (isEnemyLayer && damage == 0)
            {
                // Get ParticleCollisionHandler and manually trigger damage
                ParticleCollisionHandler collisionHandler = GetComponent<ParticleCollisionHandler>();
                if (collisionHandler != null)
                {
                    GameObject rootObject = other.transform.root.gameObject;
                    Health enemyHealth = other.GetComponent<Health>() ?? rootObject.GetComponent<Health>();
                    Enemy enemyComponent = other.GetComponent<Enemy>() ?? rootObject.GetComponent<Enemy>();
                    
                    if (enemyHealth != null || enemyComponent != null)
                    {
                        // Deal damage
                        if (enemyHealth != null)
                            enemyHealth.TakeDamage(collisionHandler.damage);
                        else if (enemyComponent != null)
                            enemyComponent.TakeDamage(collisionHandler.damage);
                        
                        // Play explosion at hit point
                        if (explosionFX != null)
                        {
                            explosionFX.transform.position = transform.position;
                            explosionFX.Play();
                        }
                        
                        // Apply tower abilities
                        ApplyTowerAbility(other.gameObject, rootObject);
                    }
                }
            }
            
            // Stop emitting new particles but let existing ones finish
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = false;
                
                // Destroy after particles finish
                Destroy(gameObject, ps.main.startLifetime.constantMax);
            }
            else
            {
                // No particle system, destroy immediately
                Destroy(gameObject);
            }
        }
    }
    
    private void ApplyTowerAbility(GameObject hitObject, GameObject rootObject)
    {
        Vector3 hitPos = transform.position;
        
        // Fire Tower - Area Damage
        if (areaRadius > 0f)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(hitPos, areaRadius);
            foreach (Collider col in nearbyEnemies)
            {
                if (col.gameObject == hitObject || col.gameObject == rootObject) continue;
                
                Health health = col.GetComponent<Health>();
                Enemy enemy = col.GetComponent<Enemy>();
                
                if (health != null)
                    health.TakeDamage(damage * areaMultiplier);
                else if (enemy != null)
                    enemy.TakeDamage(damage * areaMultiplier);
            }
        }
        
        // Ice Tower - Slow Effect
        if (slowAmount > 0f && slowDuration > 0f)
        {
            Enemy enemy = hitObject.GetComponent<Enemy>() ?? rootObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                StartCoroutine(ApplySlowEffect(enemy));
            }
        }
        
        // Lightning Tower - Chain Attack
        if (chainCount > 0 && chainRange > 0f)
        {
            ChainLightning(rootObject, chainCount);
        }
        
        // Void Tower - Warp Enemy
        if (warpChance > 0f && Random.value <= warpChance)
        {
            WarpEnemy(rootObject);
        }
    }
    
    private System.Collections.IEnumerator ApplySlowEffect(Enemy enemy)
    {
        if (enemy == null) yield break;
        float originalSpeed = enemy.moveSpeed;
        enemy.moveSpeed *= (1f - slowAmount);
        yield return new WaitForSeconds(slowDuration);
        if (enemy != null)
            enemy.moveSpeed = originalSpeed;
    }
    
    private void ChainLightning(GameObject fromEnemy, int remaining)
    {
        if (remaining <= 0) return;
        
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float closestDist = chainRange;
        
        foreach (GameObject enemy in allEnemies)
        {
            if (enemy == fromEnemy) continue;
            float dist = Vector3.Distance(fromEnemy.transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }
        
        if (closest != null)
        {
            Health health = closest.GetComponent<Health>();
            Enemy enemy = closest.GetComponent<Enemy>();
            float chainDamage = damage * Mathf.Pow(chainReduction, chainCount - remaining + 1);
            
            if (health != null)
                health.TakeDamage(chainDamage);
            else if (enemy != null)
                enemy.TakeDamage(chainDamage);
            
            ChainLightning(closest, remaining - 1);
        }
    }
    
    private void WarpEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        UnityEngine.AI.NavMeshAgent navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        Vector3 currentPos = enemy.transform.position;
        // Warp away from tower position
        Vector3 directionFromTower = (currentPos - towerPosition).normalized;
        Vector3 warpPos = currentPos + directionFromTower * warpDistance;
        warpPos.y = currentPos.y;
        
        if (navAgent != null)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(warpPos, out hit, warpDistance, UnityEngine.AI.NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }
        }
        else
        {
            enemy.transform.position = warpPos;
        }
    }
}
