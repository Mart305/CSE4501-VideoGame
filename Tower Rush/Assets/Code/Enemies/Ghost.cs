using UnityEngine;

public class Ghost : Enemy
{
    private Health healthComponent;
    
    protected override void Start()
    {
        // Ghost stats - significantly harder
        moveSpeed = 7f;    // Much faster
        health = 50f;      // More health
        damage = 10f;      // More damage
        attackCooldown = 1f;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component instead of the base Enemy health
        healthComponent.SetMaxHealth(50f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(50f);
        }

        base.Start();
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
    
    public override void Die()
    {
        if (isDead) return;
        isDead = true;
        
        // Immediately stop all movement and attacks
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        // Stop attacking
        isAttacking = false;
        
        // Create death particle effect
        CreateDeathEffect();
        
        // Disable colliders to prevent further interactions
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Hide the ghost model immediately
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Destroy immediately (no delay)
        Destroy(gameObject);
    }
    
    private void CreateDeathEffect()
    {
        // Create subtle ethereal death particle effect
        GameObject effectObj = new GameObject("GhostDeathEffect");
        effectObj.transform.position = transform.position;
        
        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        
        // Stop the particle system first before modifying properties
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.5f;
        main.startSpeed = 1f;
        main.startSize = 0.15f; // Smaller particles
        main.startColor = new Color(0.7f, 0.9f, 1f, 0.4f); // More transparent (lower alpha)
        main.maxParticles = 8; // Fewer particles
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 8) // Fewer particles
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f; // Smaller radius
        
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.8f); // Slower expansion
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Play the effect
        ps.Play();
        
        // Clean up after effect finishes
        Destroy(effectObj, 0.8f);
    }
}