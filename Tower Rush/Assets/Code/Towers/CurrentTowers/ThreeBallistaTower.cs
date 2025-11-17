using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeBallistaTower : BaseTower
{
    [Header("Three Ballista Tower - Multi-Shot")]
    [SerializeField] private int numberOfShots = 3; // Fire at 3 targets simultaneously
    
    public override void Start()
    {
        // Set three ballista tower specific stats
        damage = 35f;
        fireRate = 1.2f;
        range = 20f;
        
        base.Start();
    }

    protected override void ConfigureProjectile(SimpleProjectile projectile)
    {
        // Three Ballista - Multi-shot damage (hits 2 additional nearby enemies)
        projectile.towerType = "ThreeBallista";
        projectile.areaRadius = 8f; // Search radius for additional targets
        projectile.areaMultiplier = 1.0f; // Full damage to additional targets
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

        // MULTI-SHOT: Find multiple targets to shoot at
        List<GameObject> targets = FindMultipleTargets();
        
        foreach (GameObject target in targets)
        {
            if (target == null) continue;
            
            // Create attackFX for each target
            if (attackFXPrefab != null)
            {
                Vector3 attackPos = firePoint != null ? firePoint.position : transform.position;
                GameObject attackFXObj = Instantiate(attackFXPrefab);
                ParticleSystem attackParticle = attackFXObj.GetComponent<ParticleSystem>();

                attackFXObj.transform.localScale = Vector3.one * 0.3f;
                attackFXObj.transform.position = attackPos;
                
                // Also scale all children
                foreach (Transform child in attackFXObj.transform)
                {
                    child.localScale = Vector3.one * 0.3f;
                }

                Vector3 enemyCenter = target.transform.position + Vector3.up * 1f;
                attackFXObj.transform.LookAt(enemyCenter);

                if (attackParticle != null)
                {
                    // Override particle system stop action to prevent auto-destruction
                    var main = attackParticle.main;
                    main.stopAction = ParticleSystemStopAction.None;
                    main.loop = false;
                    
                    attackParticle.Play();
                    // TEMPORARILY DISABLED: StartCoroutine(DestroyAttackFXAfterDelay(attackFXObj));
                }
            }

            // Deal damage to each target
            Health enemyHealth = target.GetComponent<Health>();
            Enemy enemyComponent = target.GetComponent<Enemy>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            else if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(damage);
            }

            // Play explosion effect at each target
            if (explosionFX != null)
            {
                Vector3 enemyCenter = target.transform.position + Vector3.up * 1f;
                explosionFX.transform.position = enemyCenter;
                explosionFX.Play();
            }
        }
        
        if (explosionFX != null)
        {
            StartCoroutine(StopExplosionAfterDelay());
        }
    }
    
    private List<GameObject> FindMultipleTargets()
    {
        List<GameObject> targets = new List<GameObject>();
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        // If tag search fails, try component search
        if (allEnemies.Length == 0)
        {
            Enemy[] enemyComponents = FindObjectsOfType<Enemy>();
            allEnemies = new GameObject[enemyComponents.Length];
            for (int i = 0; i < enemyComponents.Length; i++)
            {
                allEnemies[i] = enemyComponents[i].gameObject;
            }
        }
        
        // Sort enemies by distance
        List<GameObject> enemiesInRange = new List<GameObject>();
        foreach (GameObject enemy in allEnemies)
        {
            if (enemy == null) continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= range)
            {
                enemiesInRange.Add(enemy);
            }
        }
        
        // Sort by distance (closest first)
        enemiesInRange.Sort((a, b) => 
        {
            float distA = Vector3.Distance(transform.position, a.transform.position);
            float distB = Vector3.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        // Take up to numberOfShots targets
        for (int i = 0; i < Mathf.Min(numberOfShots, enemiesInRange.Count); i++)
        {
            targets.Add(enemiesInRange[i]);
        }
        
        return targets;
    }
}
