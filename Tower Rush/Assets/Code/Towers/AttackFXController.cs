using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Controls attack FX to properly target and follow enemies
/// Attach to attack particle systems or projectiles
public class AttackFXController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 20f;
    [SerializeField] private bool followTarget = true;
    [SerializeField] private bool rotateTowardsTarget = true;
    [SerializeField] private bool moveGameObject = false; // If false, only particles move, GameObject stays at spawn point
    
    private bool canDetectCollision = false;
    private float collisionEnableDelay = 0.1f; // Delay before collision detection starts
    
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 3f;
    [SerializeField] private bool destroyOnReachTarget = true;
    [SerializeField] private float reachDistance = 0.5f;
    
    [Header("Visual Connection")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private bool useLineRenderer = false;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Gradient lineColor;
    
    private float spawnTime;
    private Vector3 lastKnownTargetPosition;
    private bool targetLost = false;

    void Start()
    {
        spawnTime = Time.time;
        lastKnownTargetPosition = target != null ? target.position : transform.position;
        
        // Enable collision detection after a short delay
        StartCoroutine(EnableCollisionAfterDelay());
        
        // Setup line renderer if enabled
        if (useLineRenderer && lineRenderer != null)
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.colorGradient = lineColor;
        }
        
        // Ensure particle system uses local space so particles move with GameObject
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // Particles move with GameObject
            // Keep original start speed - particles need their velocity
        }
    }
    
    private IEnumerator EnableCollisionAfterDelay()
    {
        yield return new WaitForSeconds(collisionEnableDelay);
        canDetectCollision = true;
    }

    void Update()
    {
        // Check lifetime
        if (Time.time - spawnTime > maxLifetime)
        {
            DestroyFX();
            return;
        }
        
        // Particle collision system handles terrain detection
        
        // Update target position
        if (target != null)
        {
            lastKnownTargetPosition = target.position;
            targetLost = false;
        }
        else
        {
            targetLost = true;
        }
        
        // Move towards target (only if moveGameObject is true)
        if (followTarget && moveGameObject)
        {
            MoveTowardsTarget();
        }
        
        // Rotate towards target (only if moveGameObject is true)
        if (rotateTowardsTarget && !targetLost && moveGameObject)
        {
            RotateTowardsTarget();
        }
        
        // Update line renderer
        if (useLineRenderer && lineRenderer != null)
        {
            UpdateLineRenderer();
        }
        
        // Check if reached target
        if (!targetLost && destroyOnReachTarget && canDetectCollision)
        {
            float distance = Vector3.Distance(transform.position, lastKnownTargetPosition);
            if (distance < reachDistance)
            {
                OnReachTarget();
            }
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 targetPos = targetLost ? lastKnownTargetPosition : target.position;
        Vector3 direction = (targetPos - transform.position).normalized;
        
        Vector3 newPosition = transform.position + direction * speed * Time.deltaTime;
        
        // Particle collision system handles all collision detection
        // No raycast needed here
        
        transform.position = newPosition;
    }
    
    // Collision detection - this is the primary method
    private void OnCollisionEnter(Collision collision)
    {
        
        // Hit something - trigger explosion and destroy
        ProjectileDamage projDamage = GetComponent<ProjectileDamage>();
        if (projDamage != null && projDamage.explosionFX != null)
        {
            Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            projDamage.explosionFX.transform.position = impactPoint;
            projDamage.explosionFX.Play();
        }
        
        DestroyFX();
    }
    
    // Trigger detection as alternative
    private void OnTriggerEnter(Collider other)
    {
        
        // Hit something - trigger explosion and destroy
        ProjectileDamage projDamage = GetComponent<ProjectileDamage>();
        if (projDamage != null && projDamage.explosionFX != null)
        {
            projDamage.explosionFX.transform.position = transform.position;
            projDamage.explosionFX.Play();
        }
        
        DestroyFX();
    }
    
    // Particle collision detection
    private void OnParticleCollision(GameObject other)
    {
        // Ignore collisions until delay has passed
        if (!canDetectCollision)
        {
            return;
        }
        
        // Get the particle system to retrieve collision events
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
            int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);
            
            if (numCollisionEvents > 0)
            {
                // Use the first collision point
                Vector3 impactPoint = collisionEvents[0].intersection;
                
                // If intersection is zero, use transform position
                if (impactPoint == Vector3.zero)
                {
                    impactPoint = transform.position;
                }
                
                // Trigger explosion at impact point
                ProjectileDamage projDamage = GetComponent<ProjectileDamage>();
                if (projDamage != null && projDamage.explosionFX != null)
                {
                    projDamage.explosionFX.transform.position = impactPoint;
                    projDamage.explosionFX.Play();
                }
            }
        }
        
        DestroyFX();
    }

    private void RotateTowardsTarget()
    {
        Vector3 targetPos = target != null ? target.position : lastKnownTargetPosition;
        Vector3 direction = targetPos - transform.position;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.SetPosition(0, transform.position);
        
        if (target != null)
        {
            lineRenderer.SetPosition(1, target.position);
        }
        else
        {
            lineRenderer.SetPosition(1, lastKnownTargetPosition);
        }
    }

    private void OnReachTarget()
    {
        // Trigger damage on impact
        ProjectileDamage projDamage = GetComponent<ProjectileDamage>();
        if (projDamage != null && target != null)
        {
            projDamage.TriggerImpact(target.gameObject);
        }
        
        // Destroy projectile
        DestroyFX();
    }

    private void DestroyFX()
    {
        // Stop particle system if exists
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop();
            Destroy(gameObject, ps.main.duration);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            lastKnownTargetPosition = target.position;
            targetLost = false;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public float GetSpeed()
    {
        return speed;
    }

    public void SetMaxLifetime(float lifetime)
    {
        maxLifetime = lifetime;
    }
    
    public float GetMaxLifetime()
    {
        return maxLifetime;
    }
    
    public void SetMoveGameObject(bool shouldMove)
    {
        moveGameObject = shouldMove;
    }
}
