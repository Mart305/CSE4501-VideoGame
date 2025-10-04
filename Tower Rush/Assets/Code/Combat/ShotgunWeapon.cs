using UnityEngine;
using System.Collections;

public class ShotgunWeapon : PlayerWeapon
{
    [Header("Shotgun Specific Settings")]
    [SerializeField] private int pelletsPerShot = 8;
    [SerializeField] private float spreadAngle = 15f;
    [SerializeField] private float shotgunDamagePerPellet = 12f;
    [SerializeField] private float shotgunRange = 15f;

    public new void Fire()
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

    void CreatePelletTrail(Vector3 start, Vector3 end)
    {
        GameObject trailObj = new GameObject("PelletTrail");
        trailObj.transform.position = start;

        LineRenderer lineRenderer = trailObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.5f, 0f, 0.6f);  // Orange color
        lineRenderer.material = mat;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Fade out the trail
        StartCoroutine(FadeOutTrail(lineRenderer, trailObj));
    }

    IEnumerator FadeOutTrail(LineRenderer lineRenderer, GameObject trailObj)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        Color startColor = lineRenderer.material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            Color newColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lineRenderer.material.color = newColor;
            yield return null;
        }

        Destroy(trailObj);
    }

    void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        GameObject impactObj = new GameObject("ShotgunImpact");
        impactObj.transform.position = position;
        impactObj.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem ps = impactObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.startLifetime = 0.2f;
        main.startSpeed = 3f;
        main.startSize = 0.05f;
        main.startColor = new Color(1f, 0.5f, 0f);  // Orange impact
        main.maxParticles = 5;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 5)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.05f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Destroy(impactObj, 0.5f);
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

    protected void PlayFireEffects()
    {
        GetType().BaseType.GetMethod("PlayFireEffects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(this, null);
    }
}
