using System.Collections.Generic;
using UnityEngine;

public class BallistaTower : BaseTower
{
    // Ballista Tower - High Damage
    // This tower deals high single-target damage with slower fire rate
    // Perfect for taking down tough enemies
    
    public override void Start()
    {
        // Set ballista tower specific stats - HIGH DAMAGE
        damage = 80f; // Increased from 50 to emphasize high damage
        fireRate = 0.6f; // Slower but very powerful
        range = 25f; // Slightly longer range
        
        base.Start();
    }
    
    protected override void PerformAttack()
    {
        // For Ballista, find the child attackFX and instantiate a copy
        Transform attackFXTransform = transform.Find("SingleArrowFx");
        if (attackFXTransform == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No SingleArrowFx child found!");
            return;
        }
        
        // Instantiate a copy preserving position and rotation
        GameObject attackFXObj = Instantiate(attackFXTransform.gameObject, attackFXTransform.position, attackFXTransform.rotation);
        
        // Add particle collision handler if not already present
        ParticleCollisionHandler collisionHandler = attackFXObj.GetComponent<ParticleCollisionHandler>();
        if (collisionHandler == null)
        {
            collisionHandler = attackFXObj.AddComponent<ParticleCollisionHandler>();
        }
        collisionHandler.damage = damage;
        collisionHandler.towerPosition = transform.position;
        collisionHandler.explosionFX = explosionFX;
        
        // Add simple projectile script if not already present
        SimpleProjectile projectile = attackFXObj.GetComponent<SimpleProjectile>();
        if (projectile == null)
        {
            projectile = attackFXObj.AddComponent<SimpleProjectile>();
        }
        projectile.Initialize(currentTarget, 0f, explosionFX);
        projectile.towerPosition = transform.position;
        projectile.disableHoming = true; // Ballista shoots straight
        
        // Set tower-specific abilities
        ConfigureProjectile(collisionHandler);
        
        // Play particle system
        ParticleSystem ps = attackFXObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
    }
}
