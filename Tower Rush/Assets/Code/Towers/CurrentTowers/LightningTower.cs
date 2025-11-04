using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningTower : BaseTower
{
    [Header("Lightning Tower - Chain Attack")]
    [SerializeField] private int maxChainTargets = 3; // How many enemies to chain to
    [SerializeField] private float chainRange = 8f; // Max distance to next chain target
    [SerializeField] private float chainDamageReduction = 0.7f; // Each chain does 70% of previous damage
    
    public override void Start()
    {
        // Set lightning tower specific stats
        damage = 40f;
        fireRate = 1f;
        range = 20f;
        
        base.Start();
    }

    protected override void PerformAttack()
    {
        if (currentTarget == null) return;

        // Play tower shooting sound
        if (AudioManager.Instance != null)
        {
            string towerType = this.GetType().Name;
            Vector3 soundPosition = firePoint != null ? firePoint.position : transform.position;
            AudioManager.Instance.PlayTowerShootSound(towerType, soundPosition);
        }

        // Create attackFX for primary target
        if (attackFXPrefab != null)
        {
            Vector3 attackPos = firePoint != null ? firePoint.position : transform.position;
            GameObject attackFXObj = Instantiate(attackFXPrefab);
            attackFX = attackFXObj.GetComponent<ParticleSystem>();

            float scaleFactor = transform.localScale.x;
            attackFXObj.transform.localScale = Vector3.one * scaleFactor;
            attackFXObj.transform.position = attackPos;

            Vector3 enemyCenter = currentTarget.transform.position + Vector3.up * 1f;
            attackFXObj.transform.LookAt(enemyCenter);

            if (attackFX != null)
            {
                attackFX.Play();
                StartCoroutine(DestroyAttackFXAfterDelay(attackFXObj));
            }
        }

        // CHAIN ATTACK: Start chain from primary target
        List<GameObject> chainedTargets = new List<GameObject>();
        chainedTargets.Add(currentTarget);
        
        float currentDamage = damage;
        GameObject currentChainTarget = currentTarget;
        
        // Deal damage and chain to nearby enemies
        for (int i = 0; i < maxChainTargets && currentChainTarget != null; i++)
        {
            // Deal damage to current target in chain
            Health enemyHealth = currentChainTarget.GetComponent<Health>();
            Enemy enemyComponent = currentChainTarget.GetComponent<Enemy>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(currentDamage);
            }
            else if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(currentDamage);
            }
            
            // Play explosion at current target
            if (explosionFX != null)
            {
                Vector3 enemyCenter = currentChainTarget.transform.position + Vector3.up * 1f;
                explosionFX.transform.position = enemyCenter;
                explosionFX.Play();
            }
            
            // Find next chain target
            GameObject nextTarget = FindNextChainTarget(currentChainTarget, chainedTargets);
            
            if (nextTarget != null)
            {
                chainedTargets.Add(nextTarget);
                currentDamage *= chainDamageReduction; // Reduce damage for next chain
                currentChainTarget = nextTarget;
            }
            else
            {
                break; // No more targets to chain to
            }
        }
        
        if (explosionFX != null)
        {
            StartCoroutine(StopExplosionAfterDelay());
        }
    }
    
    private GameObject FindNextChainTarget(GameObject fromTarget, List<GameObject> alreadyChained)
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closestEnemy = null;
        float closestDistance = chainRange;
        
        Vector3 fromPos = fromTarget.transform.position;
        
        foreach (GameObject enemy in allEnemies)
        {
            // Skip if already chained to this enemy
            if (alreadyChained.Contains(enemy)) continue;
            
            float distance = Vector3.Distance(fromPos, enemy.transform.position);
            if (distance <= chainRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        
        // Also check Enemy components if tag search fails
        if (closestEnemy == null)
        {
            Enemy[] enemyComponents = FindObjectsOfType<Enemy>();
            foreach (Enemy enemyComp in enemyComponents)
            {
                GameObject enemy = enemyComp.gameObject;
                if (alreadyChained.Contains(enemy)) continue;
                
                float distance = Vector3.Distance(fromPos, enemy.transform.position);
                if (distance <= chainRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        
        return closestEnemy;
    }
}
