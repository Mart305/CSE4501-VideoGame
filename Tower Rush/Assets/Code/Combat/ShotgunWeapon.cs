using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShotgunWeapon : PlayerWeapon
{
    [Header("Shotgun Specific Settings")]
    [SerializeField] private int pelletsPerShot = 8;
    [SerializeField] private float spreadAngle = 15f;
    [SerializeField] private float shotgunDamagePerPellet = 12f;
    [SerializeField] private float shotgunRange = 15f;

    // Cached materials for performance
    private static Material trailMaterial;
    private static Material impactMaterial;

    // Object pools for trails and impacts
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private Queue<GameObject> impactPool = new Queue<GameObject>();
    private int maxPoolSize = 15;

    void Awake()
    {
        weaponType = WeaponType.Shotgun;
        weaponName = "Shotgun";
        InitializeMaterials();
        PreWarmPools();
    }

    void InitializeMaterials()
    {
        if (trailMaterial == null)
        {
            trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.color = new Color(1f, 0.5f, 0f, 0.6f);
        }

        if (impactMaterial == null)
        {
            impactMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void PreWarmPools()
    {
        // Pre-create some trail objects
        for (int i = 0; i < 5; i++)
        {
            GameObject trail = CreatePooledTrailObject();
            trail.SetActive(false);
            trailPool.Enqueue(trail);
        }

        // Pre-create some impact objects
        for (int i = 0; i < 5; i++)
        {
            GameObject impact = CreatePooledImpactObject();
            impact.SetActive(false);
            impactPool.Enqueue(impact);
        }
    }

    public override void Fire()
    {
        if (Time.time < nextFireTime || isReloading)
            return;

        if (GetCurrentAmmo() <= 0)
        {
            StartReload();
            return;
        }

        nextFireTime = Time.time + 0.8f;  // Slower fire rate for shotgun

        // Fire multiple pellets in a spread pattern
        for (int i = 0; i < pelletsPerShot; i++)
        {
            FirePellet();
        }

        PlayFireEffects();

        // Decrease ammo manually
        int currentAmmo = GetCurrentAmmo();
        if (currentAmmo > 0)
        {
            // Reflection to set currentAmmo since we can't directly access it
            var field = GetType().BaseType.GetField("currentAmmo",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(this, currentAmmo - 1);
        }
    }

    void FirePellet()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        // Calculate random spread
        float randomX = Random.Range(-spreadAngle, spreadAngle);
        float randomY = Random.Range(-spreadAngle, spreadAngle);

        Vector3 spreadDirection = Quaternion.Euler(randomX, randomY, 0) * playerCamera.transform.forward;

        // Raycast for hitscan
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, spreadDirection, out hit, shotgunRange))
        {
            // Apply damage
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(shotgunDamagePerPellet);
            }

            // Create visual impact at hit point
            CreateImpactEffect(hit.point, hit.normal);

            // Create pellet trail
            CreatePelletTrail(GetFirePoint().position, hit.point);
        }
        else
        {
            // Create pellet trail to max range
            Vector3 endPoint = playerCamera.transform.position + spreadDirection * shotgunRange;
            CreatePelletTrail(GetFirePoint().position, endPoint);
        }
    }

    GameObject CreatePooledTrailObject()
    {
        GameObject trailObj = new GameObject("PelletTrail");
        trailObj.transform.SetParent(transform);

        LineRenderer lineRenderer = trailObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = trailMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        return trailObj;
    }

    GameObject GetTrailFromPool()
    {
        GameObject trail;
        if (trailPool.Count > 0)
        {
            trail = trailPool.Dequeue();
            trail.SetActive(true);
        }
        else
        {
            trail = CreatePooledTrailObject();
        }
        return trail;
    }

    void ReturnTrailToPool(GameObject trail, float delay)
    {
        StartCoroutine(ReturnToPoolAfterDelay(trail, trailPool, delay));
    }

    void CreatePelletTrail(Vector3 start, Vector3 end)
    {
        GameObject trailObj = GetTrailFromPool();
        trailObj.transform.position = start;

        LineRenderer lineRenderer = trailObj.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        // Reset material color
        trailMaterial.color = new Color(1f, 0.5f, 0f, 0.6f);

        // Fade out the trail
        StartCoroutine(FadeOutTrail(lineRenderer, trailObj));
    }

    IEnumerator FadeOutTrail(LineRenderer lineRenderer, GameObject trailObj)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        Color startColor = trailMaterial.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            Color newColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            trailMaterial.color = newColor;
            yield return null;
        }

        // Return to pool instead of destroy
        trailObj.SetActive(false);
        if (trailPool.Count < maxPoolSize)
        {
            trailPool.Enqueue(trailObj);
        }
        else
        {
            Destroy(trailObj);
        }
    }

    GameObject CreatePooledImpactObject()
    {
        GameObject impactObj = new GameObject("ShotgunImpact");
        impactObj.transform.SetParent(transform);

        ParticleSystem ps = impactObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.startLifetime = 0.2f;
        main.startSpeed = 3f;
        main.startSize = 0.05f;
        main.startColor = new Color(1f, 0.5f, 0f);
        main.maxParticles = 5;
        main.loop = false;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 5)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.05f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = impactMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return impactObj;
    }

    GameObject GetImpactFromPool()
    {
        GameObject impact;
        if (impactPool.Count > 0)
        {
            impact = impactPool.Dequeue();
            impact.SetActive(true);
        }
        else
        {
            impact = CreatePooledImpactObject();
        }
        return impact;
    }

    void ReturnImpactToPool(GameObject impact)
    {
        impact.SetActive(false);
        if (impactPool.Count < maxPoolSize)
        {
            impactPool.Enqueue(impact);
        }
        else
        {
            Destroy(impact);
        }
    }

    IEnumerator ReturnToPoolAfterDelay(GameObject obj, Queue<GameObject> pool, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            obj.SetActive(false);
            if (pool.Count < maxPoolSize)
            {
                pool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }

    void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        GameObject impactObj = GetImpactFromPool();
        impactObj.transform.position = position;
        impactObj.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem ps = impactObj.GetComponent<ParticleSystem>();
        ps.Play();

        StartCoroutine(ReturnToPoolAfterDelay(impactObj, impactPool, 0.5f));
    }

    // Helper methods to access protected/private members from base class
    protected float GetNextFireTime()
    {
        return GetType().BaseType.GetField("nextFireTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(this) is float value ? value : 0f;
    }

    protected void SetNextFireTime(float time)
    {
        GetType().BaseType.GetField("nextFireTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(this, time);
    }

    protected float GetFireRate()
    {
        return GetType().BaseType.GetField("fireRate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(this) is float value ? value : 0.5f;
    }

    protected bool UseAmmo()
    {
        return GetType().BaseType.GetField("useAmmo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(this) is bool value ? value : false;
    }

    protected void DecrementAmmo()
    {
        var currentAmmoField = GetType().BaseType.GetField("currentAmmo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int currentAmmo = (int)currentAmmoField.GetValue(this);
        currentAmmoField.SetValue(this, currentAmmo - 1);
    }

    protected Transform GetFirePoint()
    {
        return GetType().BaseType.GetField("firePoint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(this) as Transform;
    }

    protected override void PlayFireEffects()
    {
        GetType().BaseType.GetMethod("PlayFireEffects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(this, null);
    }
}
