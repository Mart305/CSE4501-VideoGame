using UnityEngine;

/// <summary>
/// Creates a continuous beam effect from tower to enemy
/// Perfect for laser/lightning tower attacks
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BeamAttackFX : MonoBehaviour
{
    [Header("Beam Settings")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float beamWidth = 0.1f;
    [SerializeField] private Gradient beamColor;
    
    [Header("Animation")]
    [SerializeField] private bool animateBeam = true;
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Impact Effect")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float impactEffectScale = 1f;
    
    private LineRenderer lineRenderer;
    private GameObject impactEffect;
    private Material beamMaterial;
    private float baseWidth;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        baseWidth = beamWidth;
        
        SetupLineRenderer();
    }

    void Start()
    {
        if (impactEffectPrefab != null && endPoint != null)
        {
            impactEffect = Instantiate(impactEffectPrefab, endPoint.position, Quaternion.identity);
            impactEffect.transform.localScale = Vector3.one * impactEffectScale;
        }
    }

    void Update()
    {
        UpdateBeamPositions();
        
        if (animateBeam)
        {
            AnimateBeam();
        }
        
        if (impactEffect != null && endPoint != null)
        {
            impactEffect.transform.position = endPoint.position;
        }
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.positionCount = 2;
        
        if (beamColor != null)
        {
            lineRenderer.colorGradient = beamColor;
        }
        
        // Get material for animation
        beamMaterial = lineRenderer.material;
    }

    private void UpdateBeamPositions()
    {
        if (lineRenderer == null) return;
        
        Vector3 start = startPoint != null ? startPoint.position : transform.position;
        Vector3 end = endPoint != null ? endPoint.position : transform.position + transform.forward * 10f;
        
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void AnimateBeam()
    {
        // Scroll texture
        if (beamMaterial != null)
        {
            float offset = Time.time * scrollSpeed;
            beamMaterial.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
        
        // Pulse width
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        float currentWidth = baseWidth + pulse;
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;
    }

    public void SetStartPoint(Transform start)
    {
        startPoint = start;
    }

    public void SetEndPoint(Transform end)
    {
        endPoint = end;
        
        if (impactEffect != null && end != null)
        {
            impactEffect.transform.position = end.position;
        }
    }

    public void SetBeamColor(Gradient color)
    {
        beamColor = color;
        if (lineRenderer != null)
        {
            lineRenderer.colorGradient = color;
        }
    }

    public void SetBeamWidth(float width)
    {
        baseWidth = width;
        beamWidth = width;
        
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }

    void OnDestroy()
    {
        if (impactEffect != null)
        {
            Destroy(impactEffect);
        }
    }
}
