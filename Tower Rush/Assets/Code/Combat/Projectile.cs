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
        }
        
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
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
            trailRenderer.time = 0.2f;
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0.02f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startColor = projectileColor;
            trailRenderer.endColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0);
        }
        
        if (projectileLight == null)
        {
            GameObject lightObj = new GameObject("ProjectileLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            projectileLight = lightObj.AddComponent<Light>();
            projectileLight.type = LightType.Point;
            projectileLight.color = projectileColor;
            projectileLight.intensity = lightIntensity;
            projectileLight.range = 5f;
        }
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = projectileColor;
            renderer.material.SetFloat("_Metallic", 0.8f);
            renderer.material.SetFloat("_Smoothness", 0.8f);
            
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", projectileColor * 2f);
            }
        }
    }
    
    void PlayImpactEffect(Vector3 impactPoint)
    {
        if (impactEffect != null)
        {
            ParticleSystem effect = Instantiate(impactEffect, impactPoint, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }
        else
        {
            GameObject impactObj = new GameObject("ImpactEffect");
            impactObj.transform.position = impactPoint;
            
            ParticleSystem ps = impactObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.2f;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = projectileColor;
            
            var emission = ps.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 20));
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            
            Destroy(impactObj, 2f);
        }
    }
}