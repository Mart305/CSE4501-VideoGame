using UnityEngine;

/// Stores damage info for projectiles and triggers damage/effects on impact
public class ProjectileDamage : MonoBehaviour
{
    [HideInInspector] public float damage;
    [HideInInspector] public ParticleSystem explosionFX;
    [HideInInspector] public BaseTower tower;
    
    private bool hasDealtDamage = false;

    public void TriggerImpact(GameObject target)
    {
        if (hasDealtDamage) return; // Only deal damage once
        hasDealtDamage = true;
        
        // Damage is now dealt directly in BaseTower.PerformAttack()
        // This class is kept for potential future projectile-based damage
        
        // Play explosion effect at impact point
        if (explosionFX != null && target != null)
        {
            Vector3 impactPos = target.transform.position + Vector3.up * 1f;
            explosionFX.transform.position = impactPos;
            explosionFX.Play();
        }
    }
}
