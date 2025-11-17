using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidTower : BaseTower
{
    [Header("Void Tower - Warping Abilities")]
    [SerializeField] private float warpDistance = 30f; // How far to teleport enemy away from tower
    [SerializeField] private float warpChance = 1.0f; // 100% chance to warp on hit
    
    public override void Start()
    {
        // Set void tower specific stats
        damage = 25f;
        fireRate = 1f; 
        range = 20f;
        
        base.Start();
    }

    protected override void ConfigureProjectile(SimpleProjectile projectile)
    {
        // Void Tower - Warp Enemy
        projectile.towerType = "Void";
        projectile.warpChance = 1.0f; // 100% chance to warp
        projectile.warpDistance = 20f; // Warp 20 units away
    }
    
    protected void PerformAttack_OLD()
    {
        if (currentTarget == null) return;

        // Play tower shooting sound
        if (AudioManager.Instance != null)
        {
            string towerType = this.GetType().Name;
            Vector3 soundPosition = firePoint != null ? firePoint.position : transform.position;
            AudioManager.Instance.PlayTowerShootSound(towerType, soundPosition);
        }

        // Create attackFX
        if (attackFXPrefab != null)
        {
            Vector3 attackPos = firePoint != null ? firePoint.position : transform.position;
            GameObject attackFXObj = Instantiate(attackFXPrefab);
            attackFX = attackFXObj.GetComponent<ParticleSystem>();

            attackFXObj.transform.localScale = Vector3.one * 0.3f;
            attackFXObj.transform.position = attackPos;
            
            // Also scale all children
            foreach (Transform child in attackFXObj.transform)
            {
                child.localScale = Vector3.one * 0.3f;
            }

            Vector3 enemyCenter = currentTarget.transform.position + Vector3.up * 1f;
            attackFXObj.transform.LookAt(enemyCenter);

            if (attackFX != null)
            {
                // Override particle system stop action to prevent auto-destruction
                var main = attackFX.main;
                main.stopAction = ParticleSystemStopAction.None;
                main.loop = false;
                
                attackFX.Play();
                // TEMPORARILY DISABLED: StartCoroutine(DestroyAttackFXAfterDelay(attackFXObj));
            }
        }

        // Deal damage to target
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

        // WARPING ABILITY: Chance to teleport enemy backwards
        if (Random.value <= warpChance)
        {
            WarpEnemy(currentTarget);
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
    
    private void WarpEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        // Get enemy's current position
        Vector3 currentPos = enemy.transform.position;
        
        // Calculate direction from tower to enemy (to push them away from tower)
        Vector3 directionFromTower = (currentPos - transform.position).normalized;
        
        // Calculate warp position (away from tower)
        Vector3 warpPosition = currentPos + (directionFromTower * warpDistance);
        
        // Keep Y position the same (don't warp vertically)
        warpPosition.y = currentPos.y;
        
        // Check if warp position is valid (on NavMesh if enemy uses NavMesh)
        UnityEngine.AI.NavMeshAgent navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            // Sample NavMesh to find valid position
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(warpPosition, out hit, warpDistance, UnityEngine.AI.NavMesh.AllAreas))
            {
                // Warp to valid NavMesh position
                navAgent.Warp(hit.position);
            }
        }
        else
        {
            // Direct teleport if no NavMeshAgent
            enemy.transform.position = warpPosition;
        }
        
        // Optional: Play warp effect at both old and new positions
        if (explosionFX != null)
        {
            // Show effect at new position
            explosionFX.transform.position = warpPosition + Vector3.up * 1f;
            explosionFX.Play();
        }
    }
}
