using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Health Bar Components - Assign in Inspector")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private bool faceCamera = true;
    
    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damageColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    
    private Camera playerCamera;
    private Health enemyHealth;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
        
        // Don't modify transform position - let it stay at parent's position
        // The offset will be handled by the canvas positioning
        
        // Get the Health component from parent
        enemyHealth = GetComponentInParent<Health>();
        if (enemyHealth != null)
        {
            // Initialize with max health first
            Initialize(enemyHealth.GetMaxHealth());
            
            // Subscribe to health events
            enemyHealth.OnHealthChanged.AddListener(OnHealthChanged);
            enemyHealth.OnDeath.AddListener(OnDeath);
            
            // Update with current health (this should be max health at start)
            UpdateHealth(enemyHealth.GetHealth(), enemyHealth.GetMaxHealth());
        }
        else
        {
            // Fallback initialization if no health component found
            Initialize(100f);
        }
    }
    
    void Update()
    {
        // Position health bar above enemy using world position
        if (transform.parent != null)
        {
            Vector3 worldOffset = transform.parent.position + offset;
            transform.position = worldOffset;
        }
        
        // Make health bar face camera
        if (faceCamera && playerCamera != null)
        {
            Vector3 direction = playerCamera.transform.position - transform.position;
            transform.rotation = Quaternion.LookRotation(-direction);
        }
    }
    
    public void Initialize(float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
        if (fillImage != null)
        {
            fillImage.fillAmount = 1.0f; // Start at full
            fillImage.color = healthyColor;
        }
    }
    
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (fillImage != null && maxHealth > 0)
        {
            float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
            fillImage.fillAmount = healthPercentage;
            
            // Update color based on health percentage
            if (healthPercentage > 0.6f)
                fillImage.color = healthyColor;
            else if (healthPercentage > 0.3f)
                fillImage.color = damageColor;
            else
                fillImage.color = criticalColor;
        }
    }
    
    private void UpdateColor(float healthPercent)
    {
        if (fillImage == null) return;
        
        if (healthPercent > 0.6f)
            fillImage.color = healthyColor;
        else if (healthPercent > 0.3f)
            fillImage.color = damageColor;
        else
            fillImage.color = criticalColor;
    }
    
    private void OnHealthChanged(float healthPercent)
    {
        if (enemyHealth != null)
        {
            UpdateHealth(enemyHealth.GetHealth(), enemyHealth.GetMaxHealth());
        }
    }
    
    private void OnDeath()
    {
        // Hide health bar when enemy dies
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            enemyHealth.OnDeath.RemoveListener(OnDeath);
        }
    }
}