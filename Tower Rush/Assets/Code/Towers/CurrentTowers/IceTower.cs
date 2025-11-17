using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceTower : BaseTower
{
    [Header("Ice Tower - Slow Effect")]
    [SerializeField] private float slowAmount = 0.5f; // 50% slow
    [SerializeField] private float slowDuration = 2f; // 2 seconds
    
    public override void Start()
    {
        // Set ice tower specific stats
        damage = 20f;
        fireRate = 2f;
        range = 20f;
        
        base.Start();
    }

    protected override void ConfigureProjectile(ParticleCollisionHandler collisionHandler)
    {
        // Ice Tower - Slow Effect
        collisionHandler.towerType = "Ice";
        collisionHandler.slowAmount = 0.5f; // 50% slow
        collisionHandler.slowDuration = 3f; // 3 seconds
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

        // SLOW EFFECT: Apply slow to the enemy
        if (enemyComponent != null)
        {
            StartCoroutine(ApplySlowEffect(enemyComponent));
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
    
    private IEnumerator ApplySlowEffect(Enemy enemy)
    {
        if (enemy == null) yield break;
        
        // Store original speed
        float originalSpeed = enemy.moveSpeed;
        
        // Apply slow
        enemy.moveSpeed *= (1f - slowAmount);
        
        // Wait for duration
        yield return new WaitForSeconds(slowDuration);
        
        // Restore original speed (if enemy still exists)
        if (enemy != null)
        {
            enemy.moveSpeed = originalSpeed;
        }
    }
}
