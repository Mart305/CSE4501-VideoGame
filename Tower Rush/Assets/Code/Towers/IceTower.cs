using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ice Tower - Add this component to a Tower to give it slow effects
// This works alongside the regular Tower component
public class IceTower : MonoBehaviour
{
    [Header("Ice/Slow Effect")]
    [SerializeField] private float slowAmount = 0.9f; // 90% slow (enemies move at 10% speed - nearly frozen!)
    [SerializeField] private float slowDuration = 3f; // Slow lasts 3 seconds
    [SerializeField] private Color iceBeamColor = new Color(0.5f, 0.8f, 1f, 1f); // Light blue
    [SerializeField] private ParticleSystem iceImpactEffect; // Optional particle effect
    
    private Tower towerComponent;
    private float lastSlowTime;
    private float slowCooldown = 0.1f; // Apply slow every 0.1 seconds

    void Start()
    {
        // Get the Tower component on this GameObject
        towerComponent = GetComponent<Tower>();
        if (towerComponent == null)
        {
            Debug.LogError("IceTower requires a Tower component on the same GameObject!");
            enabled = false;
            return;
        }
        
        // Set ice beam color if laser line exists
        LineRenderer laserLine = GetComponentInChildren<LineRenderer>();
        if (laserLine != null)
        {
            laserLine.startColor = iceBeamColor;
            laserLine.endColor = iceBeamColor;
        }
    }

    void Update()
    {
        // Apply slow effect periodically to nearby enemies
        if (Time.time >= lastSlowTime + slowCooldown)
        {
            ApplySlowToNearbyEnemies();
            lastSlowTime = Time.time;
        }
    }

    private void ApplySlowToNearbyEnemies()
    {
        // Find enemies within tower range
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float towerRange = 10f; // Default range, could get from Tower component if accessible
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= towerRange)
            {
                // Check if enemy is alive
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null && enemyHealth.GetHealth() > 0)
                {
                    ApplySlowEffect(enemy);
                }
            }
        }
    }

    void ApplySlowEffect(GameObject enemy)
    {
        if (enemy == null) return;
        
        // Get or add SlowEffect component
        SlowEffect slowEffect = enemy.GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = enemy.AddComponent<SlowEffect>();
        }
        
        // Apply the slow
        slowEffect.ApplySlow(slowAmount, slowDuration);
        
        // Spawn ice impact effect if available
        if (iceImpactEffect != null)
        {
            Vector3 enemyCenter = enemy.transform.position + Vector3.up * 1f;
            ParticleSystem effect = Instantiate(iceImpactEffect, enemyCenter, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw tower range in editor with ice color
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f); // Light blue for ice tower
        Gizmos.DrawWireSphere(transform.position, 10f); // Default range
    }
}
