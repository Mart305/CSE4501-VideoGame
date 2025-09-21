using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);
    [SerializeField] private bool faceCamera = true;
    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    private Camera playerCamera;

    public void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        transform.localPosition = offset;
    }
    public void Update()
    {
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
        UpdateColor(1f);
    }
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            float healthPercent = currentHealth / maxHealth;
            UpdateColor(healthPercent);
        }
    }
    private void UpdateColor(float healthPercent)
    {
        if (fillImage == null)
        {
            return;
        }
        if (healthPercent > 0.6f)
        {
            fillImage.color = healthyColor;
        }
        else if (healthPercent > 0.3f)
        {
            fillImage.color = damagedColor;
        }
        else
        {
            fillImage.color = criticalColor;
        }
    }
}
