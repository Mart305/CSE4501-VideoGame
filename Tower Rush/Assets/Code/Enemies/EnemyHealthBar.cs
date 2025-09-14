using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, 0);
    [SerializeField] private Gradient healthColorGradient;
    
    private Health health;
    private Camera mainCamera;
    private Canvas canvas;
    private Image fillImage;
    
    void Start()
    {
        // Create a Canvas if it doesn't exist
        canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            CreateHealthBarUI();
        }
        
        // Get the Health component
        health = GetComponentInParent<Health>();
        if (health == null)
        {
            Debug.LogWarning("EnemyHealthBar: No Health component found on parent!");
            return;
        }
        
        // Subscribe to health changes
        health.OnHealthChanged.AddListener(UpdateHealthBar);
        
        // Get camera reference
        mainCamera = Camera.main;
        
        // Initialize the health bar
        UpdateHealthBar(health.GetHealthPercentage());
    }
    
    void CreateHealthBarUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Set canvas size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1, 0.2f);
        canvasRect.localPosition = offset;
        canvasRect.localScale = Vector3.one * 0.01f;
        
        // Create background
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasObj.transform);
        Image background = backgroundObj.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(100, 20);
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvasObj.transform);
        healthSlider = sliderObj.AddComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(100, 20);
        sliderRect.anchoredPosition = Vector2.zero;
        
        // Create fill area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.sizeDelta = new Vector2(90, 10);
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.green;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(90, 10);
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.anchoredPosition = Vector2.zero;
        
        healthSlider.fillRect = fillRect;
        healthSlider.targetGraphic = fillImage;
        
        // Setup gradient
        if (healthColorGradient == null)
        {
            healthColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            healthColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    void Update()
    {
        if (canvas != null && mainCamera != null)
        {
            // Make the health bar face the camera
            canvas.transform.LookAt(canvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }
    
    void UpdateHealthBar(float healthPercentage)
    {
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
            
            if (fillImage != null && healthColorGradient != null)
            {
                fillImage.color = healthColorGradient.Evaluate(healthPercentage);
            }
        }
    }
    
    void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }
}