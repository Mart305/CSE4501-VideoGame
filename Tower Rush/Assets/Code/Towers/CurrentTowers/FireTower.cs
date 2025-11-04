using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTower : BaseTower
{
    [Header("Fire Tower - Area Damage")]
    [SerializeField] private float areaOfEffectRadius = 3f;
    [SerializeField] private float areaOfEffectDamageMultiplier = 0.5f; // 50% damage to nearby enemies
    
    public override void Start()
    {
        // Set fire tower specific stats
        damage = 30f;
        fireRate = 1.5f;
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

        // Create attackFX
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

        // Deal damage to primary target
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

        // AREA DAMAGE: Find all enemies in radius and damage them
        Vector3 targetPos = currentTarget.transform.position;
        Collider[] hitColliders = Physics.OverlapSphere(targetPos, areaOfEffectRadius);
        
        foreach (Collider col in hitColliders)
        {
            if (col.gameObject != currentTarget && (col.CompareTag("Enemy") || col.GetComponent<Enemy>() != null))
            {
                Health nearbyHealth = col.GetComponent<Health>();
                Enemy nearbyEnemy = col.GetComponent<Enemy>();
                
                float areaDamage = damage * areaOfEffectDamageMultiplier;
                
                if (nearbyHealth != null)
                {
                    nearbyHealth.TakeDamage(areaDamage);
                }
                else if (nearbyEnemy != null)
                {
                    nearbyEnemy.TakeDamage(areaDamage);
                }
            }
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
}
