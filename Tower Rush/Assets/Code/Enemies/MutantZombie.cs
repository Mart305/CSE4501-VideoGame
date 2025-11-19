using UnityEngine;

public class MutantZombie : Enemy
{
    private Health healthComponent;
    private bool isRaging = false;
    
    [Header("Rage Animation")]
    [SerializeField] private string rageTriggerName = "rage";
    
    protected override void Start()
    {
        // Mutant Zombie stats - significantly harder (boss enemy)
        moveSpeed = 4.5f;  // Much faster
        health = 600f;     // Much more health (boss)
        damage = 50f;      // Much more damage
        attackCooldown = 1.8f;
        attackRange = 2f;

        // Get or add Health component for health bar system
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            healthComponent = gameObject.AddComponent<Health>();
        }
        
        // Set health through the Health component
        healthComponent.SetMaxHealth(600f);
        
        // Initialize health bar if present
        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(600f);
        }

        base.Start();
        
        // Trigger rage animation on spawn (just like attack/death triggers)
        TriggerRageAnimation();
    }
    
    private void TriggerRageAnimation()
    {
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null) return;
        
        // Check if rage parameter exists (same pattern as death trigger)
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == rageTriggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }
        
        if (hasParameter)
        {
            animator.SetTrigger(rageTriggerName);
            isRaging = true;
            
            // Stop movement during rage
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }
        }
    }
    
    protected override void Update()
    {
        // Stop all behavior if dead
        if (isDead) return;
        
        // Check if still raging - prevent movement during rage
        if (isRaging)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                // Check if we're no longer in rage state
                if (!stateInfo.IsName("Rage"))
                {
                    isRaging = false;
                    // Re-enable movement after rage
                    if (navAgent != null)
                    {
                        navAgent.isStopped = false;
                    }
                }
                else
                {
                    // Still raging - keep movement stopped
                    if (navAgent != null)
                    {
                        navAgent.isStopped = true;
                    }
                    // Only update animator velocity, don't move
                    UpdateAnimatorVelocity();
                    return;
                }
            }
        }
        
        // Normal update behavior after rage completes
        base.Update();
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

    public override void Die()
    {
        // Boss death effects could be added here
        // For now, just destroy the object
        base.Die();
    }

    // Override to make MutantZombie target the strongest tower instead of closest
    protected override void FindTargetTower()
    {
        BaseTower[] towers = FindObjectsOfType<BaseTower>();
        BaseTower strongest = null;
        float highestHealth = 0;

        foreach (BaseTower t in towers)
        {
            if (t.GetCurrentHealth() <= 0) continue;
            if (t.GetCurrentHealth() > highestHealth)
            {
                highestHealth = t.GetCurrentHealth();
                strongest = t;
            }
        }

        targetTower = strongest;
    }
}
