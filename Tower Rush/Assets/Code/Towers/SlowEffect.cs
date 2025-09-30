using UnityEngine;
using System.Collections;

// Component that applies a slow effect to enemies, reducing their movement speed.
// Automatically manages slow duration and stacking.
public class SlowEffect : MonoBehaviour
{
    private float currentSlowAmount = 0f; // Current slow multiplier (0 = no slow, 1 = full slow)
    private float slowEndTime = 0f;
    private Coroutine slowCoroutine;
    
    // Visual feedback - frozen effect
    private Color slowTintColor = new Color(0.1f, 0.4f, 1f, 1f); // Deep ice blue tint
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isSlowed = false;
    
    // Additional visual effects
    private Vector3 originalScale;
    private bool hasScaleEffect = false;

    void Start()
    {
        // Cache renderers for visual feedback
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    // Safely get original color from custom shaders
                    Material mat = renderers[i].material;
                    if (mat.HasProperty("_Color"))
                    {
                        originalColors[i] = mat.color;
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        originalColors[i] = mat.GetColor("_BaseColor");
                    }
                    else if (mat.HasProperty("_MainColor"))
                    {
                        originalColors[i] = mat.GetColor("_MainColor");
                    }
                    else
                    {
                        // Default to white if no color property found
                        originalColors[i] = Color.white;
                    }
                }
            }
        }
        
        // Store original scale for visual effect
        originalScale = transform.localScale;
    }

    public void ApplySlow(float slowAmount, float duration)
    {
        // Update slow amount (take the strongest slow if multiple sources)
        if (slowAmount > currentSlowAmount)
        {
            currentSlowAmount = slowAmount;
        }
        
        // Extend slow duration
        float newEndTime = Time.time + duration;
        if (newEndTime > slowEndTime)
        {
            slowEndTime = newEndTime;
        }
        
        // Start slow coroutine if not already running
        if (slowCoroutine == null)
        {
            slowCoroutine = StartCoroutine(SlowCoroutine());
        }
        
        // Apply visual feedback
        if (!isSlowed)
        {
            ApplySlowVisuals();
            isSlowed = true;
        }
    }

    private IEnumerator SlowCoroutine()
    {
        while (Time.time < slowEndTime)
        {
            yield return null;
        }
        
        // Slow expired
        currentSlowAmount = 0f;
        slowCoroutine = null;
        
        // Remove visual feedback
        RemoveSlowVisuals();
        isSlowed = false;
    }

    public float GetSpeedMultiplier()
    {
        if (Time.time < slowEndTime)
        {
            return 1f - currentSlowAmount; // If slowed by 0.5, return 0.5 (50% speed)
        }
        return 1f; // Normal speed
    }

    public bool IsSlowed()
    {
        return Time.time < slowEndTime && currentSlowAmount > 0f;
    }

    public float GetSlowAmount()
    {
        return IsSlowed() ? currentSlowAmount : 0f;
    }

    private void ApplySlowVisuals()
    {
        if (renderers == null || renderers.Length == 0) return;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                Material mat = renderers[i].material;
                
                // Create a frozen effect (95% blue blend - almost completely blue)
                Color blendedColor = Color.Lerp(originalColors[i], slowTintColor, 0.95f);
                
                // Apply to different shader property names
                if (mat.HasProperty("_Color"))
                {
                    mat.color = blendedColor;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", blendedColor);
                }
                else if (mat.HasProperty("_MainColor"))
                {
                    mat.SetColor("_MainColor", blendedColor);
                }
                else if (mat.HasProperty("_Tint"))
                {
                    mat.SetColor("_Tint", blendedColor);
                }
            }
        }
        
        // Add scale effect for extra visual feedback - frozen enemies shrink more
        if (!hasScaleEffect)
        {
            transform.localScale = originalScale * 0.8f; // Much smaller when frozen
            hasScaleEffect = true;
        }
    }

    private void RemoveSlowVisuals()
    {
        if (renderers == null || renderers.Length == 0 || originalColors == null) return;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null && i < originalColors.Length)
            {
                Material mat = renderers[i].material;
                
                // Restore original color to different shader property names
                if (mat.HasProperty("_Color"))
                {
                    mat.color = originalColors[i];
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", originalColors[i]);
                }
                else if (mat.HasProperty("_MainColor"))
                {
                    mat.SetColor("_MainColor", originalColors[i]);
                }
                else if (mat.HasProperty("_Tint"))
                {
                    mat.SetColor("_Tint", originalColors[i]);
                }
            }
        }
        
        // Restore original scale
        if (hasScaleEffect)
        {
            transform.localScale = originalScale;
            hasScaleEffect = false;
        }
    }

    void OnDestroy()
    {
        // Clean up coroutine
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
    }
}
