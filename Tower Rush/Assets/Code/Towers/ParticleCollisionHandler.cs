using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Handles particle collision detection and damage application for tower projectiles
/// Attach this to the particle system GameObject
public class ParticleCollisionHandler : MonoBehaviour
{
    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    
    // Damage and tower ability parameters
    [HideInInspector] public float damage = 0f;
    [HideInInspector] public string towerType = "";
    [HideInInspector] public Vector3 towerPosition;
    [HideInInspector] public ParticleSystem explosionFX;
    
    // Tower-specific ability parameters
    [HideInInspector] public float areaRadius = 0f;
    [HideInInspector] public float areaMultiplier = 0f;
    [HideInInspector] public float slowAmount = 0f;
    [HideInInspector] public float slowDuration = 0f;
    [HideInInspector] public int chainCount = 0;
    [HideInInspector] public float chainRange = 0f;
    [HideInInspector] public float chainReduction = 0f;
    [HideInInspector] public float warpChance = 0f;
    [HideInInspector] public float warpDistance = 0f;
    
    // Track which enemies we've already hit to prevent multiple damage per projectile
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
    private bool hasDealtDamage = false;
    
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        
        if (ps != null)
        {
            // Ensure collision module is enabled
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.sendCollisionMessages = true;
            
            // Set collision mode to 3D
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            
            // Enable collision with specific layers (Enemies and Ground)
            int enemiesLayer = LayerMask.NameToLayer("Enemies");
            int groundLayer = LayerMask.NameToLayer("Ground");
            collision.collidesWith = (1 << enemiesLayer) | (1 << groundLayer);
        }
    }
    
    void OnParticleCollision(GameObject other)
    {
        // Prevent multiple damage applications
        if (hasDealtDamage) return;
        
        // Get collision events
        int numCollisionEvents = 0;
        if (ps != null)
        {
            numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);
        }
        
        if (numCollisionEvents == 0) return;
        
        // Check if we hit an enemy
        int hitLayer = other.layer;
        int enemiesLayer = LayerMask.NameToLayer("Enemies");
        int groundLayer = LayerMask.NameToLayer("Ground");
        
        bool isEnemyLayer = hitLayer == enemiesLayer;
        bool isGroundLayer = hitLayer == groundLayer;
        
        // Only deal damage to enemies
        if (isEnemyLayer && damage > 0)
        {
            // Try to find Health/Enemy on the hit object first, then root
            GameObject rootObject = other.transform.root.gameObject;
            
            // Prevent hitting the same enemy multiple times
            if (hitEnemies.Contains(rootObject)) return;
            hitEnemies.Add(rootObject);
            
            Health enemyHealth = other.GetComponent<Health>() ?? rootObject.GetComponent<Health>();
            Enemy enemyComponent = other.GetComponent<Enemy>() ?? rootObject.GetComponent<Enemy>();
            
            if (enemyHealth != null || enemyComponent != null)
            {
                hasDealtDamage = true;
                
                // Get collision position
                Vector3 hitPosition = collisionEvents[0].intersection;
                
                // Deal primary damage
                if (enemyHealth != null)
                    enemyHealth.TakeDamage(damage);
                else if (enemyComponent != null)
                    enemyComponent.TakeDamage(damage);
                
                // Play explosion effect at collision point
                if (explosionFX != null)
                {
                    explosionFX.transform.position = hitPosition;
                    explosionFX.Play();
                }
                
                // Apply tower-specific abilities
                ApplyTowerAbility(other, rootObject, hitPosition);
                
                // Stop emitting new particles but keep existing ones
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
                
                // Destroy projectile after particles finish (longer delay for visual completion)
                Destroy(gameObject, ps.main.startLifetime.constantMax);
            }
        }
        else if (isGroundLayer)
        {
            // Hit ground, destroy projectile
            Destroy(gameObject, 0.1f);
        }
    }
    
    private void ApplyTowerAbility(GameObject hitObject, GameObject rootObject, Vector3 hitPosition)
    {
        // Fire Tower - Area Damage
        if (areaRadius > 0f)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(hitPosition, areaRadius);
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
    
    private IEnumerator ApplySlowEffect(Enemy enemy)
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
            if (enemy == fromEnemy || hitEnemies.Contains(enemy)) continue;
            float dist = Vector3.Distance(fromEnemy.transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }
        
        if (closest != null)
        {
            hitEnemies.Add(closest);
            
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
