using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Tower Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] private Color healthyColor = Color.white;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    private Renderer towerRenderer;
    private Color originalColor;
    private HealthBar healthBar;
    public void Start()
    {
        currentHealth = maxHealth;
        towerRenderer = GetComponent<Renderer>();
        // Initializes visual feedback
        if (towerRenderer != null)
        {
            originalColor = towerRenderer.material.color;
        }
        // Initializes health bar
        healthBar = GetComponentInChildren<HealthBar>();
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
        }
    }
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0)
        {
            return;
        }
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        // Update visual feedback
        UpdateVisualFeedback();
        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        // Test damage 
        // Debug.Log($"Tower took {damage} damage. Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            DestroyTower();
        }
    }
    private void UpdateVisualFeedback()
    {
        if (towerRenderer == null) return;
        float healthPercent = currentHealth / maxHealth;
        Color targetColor;
        if (healthPercent > 0.6f)
        {
            targetColor = Color.Lerp(originalColor, healthyColor, 0.3f);
        }
        else if (healthPercent > 0.3f)
        {
            targetColor = Color.Lerp(originalColor, damagedColor, 0.5f);
        }
        else
        {
            targetColor = Color.Lerp(originalColor, criticalColor, 0.7f);
        }
        towerRenderer.material.color = targetColor;
    }
    private void DestroyTower()
    {
        // Test tower destruction
        // Debug.Log("Tower destroyed");
        Destroy(gameObject, 0.5f);
    }
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsDestroyed() => currentHealth <= 0;
}
