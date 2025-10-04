using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask damageableLayers = -1;
    
    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem impactEffect;
    [SerializeField] private Light projectileLight;
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private Color projectileColor = new Color(1f, 0.9f, 0.3f);
    
    private Rigidbody rb;
    private Collider projectileCollider;
    private float lifeTimer;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        projectileCollider = GetComponent<Collider>();
        if (projectileCollider == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.05f;
            projectileCollider = sphere;
        }
        
        projectileCollider.isTrigger = true;
        
        SetupVisualEffects();
    }
    
    void Start()
    {
        rb.velocity = transform.forward * speed;
        lifeTimer = lifeTime;
    }
    
    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & damageableLayers) != 0)
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            
            PlayImpactEffect(other.ClosestPoint(transform.position));
            
            Destroy(gameObject);
        }
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }
    
    void SetupVisualEffects()
    {
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.15f;  // Reduced from 0.2f for better performance
            trailRenderer.startWidth = 0.08f;  // Slightly reduced
            trailRenderer.endWidth = 0.01f;  // Reduced for optimization
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;  // Disable shadows for performance

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(projectileColor, 0.0f),
                    new GradientColorKey(projectileColor * 0.7f, 0.5f),
                    new GradientColorKey(new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }

        if (projectileLight == null)
        {
            GameObject lightObj = new GameObject("ProjectileLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            projectileLight = lightObj.AddComponent<Light>();
            projectileLight.type = LightType.Point;
            projectileLight.color = projectileColor;
            projectileLight.intensity = lightIntensity * 0.7f;  // Reduced intensity for performance
            projectileLight.range = 4f;  // Reduced range from 5f for better performance
            projectileLight.renderMode = LightRenderMode.ForceVertex;  // Use vertex lighting for performance
            projectileLight.shadows = LightShadows.None;  // Disable shadows for performance
        }
        
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = projectileColor;
            mat.SetFloat("_Emission", 1f);
            mat.SetColor("_EmissionColor", projectileColor * 2f);
            meshRenderer.material = mat;
        }
    }
    
    void PlayImpactEffect(Vector3 impactPoint)
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, impactPoint, Quaternion.LookRotation(-transform.forward));
        }
        else
        {
            GameObject impactObj = new GameObject("ImpactEffect");
            impactObj.transform.position = impactPoint;

            ParticleSystem ps = impactObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.15f;  // Reduced from 0.2f for performance
            main.startLifetime = 0.25f;  // Reduced from 0.3f
            main.startSpeed = 4f;  // Reduced from 5f
            main.startSize = 0.08f;  // Reduced from 0.1f
            main.startColor = projectileColor;
            main.maxParticles = 10;  // Limit max particles for performance

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 10)  // Reduced from 15 particles
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;  // Reduced from 0.1f

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;  // Disable shadows

            Destroy(impactObj, 1f);  // Reduced from 2f to clean up faster
        }
    }
}