using UnityEngine;
using System.Collections.Generic;

public class WeaponEffectsPool : MonoBehaviour
{
    private static WeaponEffectsPool instance;
    public static WeaponEffectsPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject poolObj = new GameObject("WeaponEffectsPool");
                instance = poolObj.AddComponent<WeaponEffectsPool>();
                DontDestroyOnLoad(poolObj);
                instance.PreWarmPools();
            }
            return instance;
        }
    }

    private Dictionary<string, Queue<GameObject>> particlePools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> particlePrefabs = new Dictionary<string, GameObject>();
    private Dictionary<GameObject, PoolReturnInfo> activeParticles = new Dictionary<GameObject, PoolReturnInfo>();
    private int initialPoolSize = 3;
    private int maxPoolSize = 20;
    private float particleLifetime = 1f;

    private class PoolReturnInfo
    {
        public string poolKey;
        public float returnTime;
    }

    void PreWarmPools()
    {
        // Pre-warm pools for all weapon types
        WeaponType[] weaponTypes = { WeaponType.Pistol, WeaponType.Rifle, WeaponType.Shotgun, WeaponType.Sniper };

        foreach (WeaponType weaponType in weaponTypes)
        {
            string poolKey = "MuzzleFlash_" + weaponType.ToString();
            particlePools[poolKey] = new Queue<GameObject>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject particle = CreateMuzzleFlashParticle(weaponType, Vector3.zero, Quaternion.identity);
                particle.SetActive(false);
                particle.transform.SetParent(transform);
                particlePools[poolKey].Enqueue(particle);
            }
        }
    }

    void Update()
    {
        // Check for particles that need to be returned to the pool
        List<GameObject> particlesToReturn = new List<GameObject>();

        foreach (var kvp in activeParticles)
        {
            if (Time.time >= kvp.Value.returnTime)
            {
                particlesToReturn.Add(kvp.Key);
            }
        }

        foreach (GameObject particle in particlesToReturn)
        {
            ReturnParticleToPool(particle);
        }
    }

    public GameObject GetMuzzleFlashParticle(WeaponType weaponType, Vector3 position, Quaternion rotation)
    {
        string poolKey = "MuzzleFlash_" + weaponType.ToString();

        if (!particlePools.ContainsKey(poolKey))
        {
            particlePools[poolKey] = new Queue<GameObject>();
        }

        GameObject particle;
        if (particlePools[poolKey].Count > 0)
        {
            particle = particlePools[poolKey].Dequeue();
            particle.transform.position = position;
            particle.transform.rotation = rotation;
            particle.SetActive(true);
        }
        else
        {
            particle = CreateMuzzleFlashParticle(weaponType, position, rotation);
        }

        // Track particle for auto-return
        activeParticles[particle] = new PoolReturnInfo
        {
            poolKey = poolKey,
            returnTime = Time.time + particleLifetime
        };

        return particle;
    }

    GameObject CreateMuzzleFlashParticle(WeaponType weaponType, Vector3 position, Quaternion rotation)
    {
        GameObject particleObj = new GameObject("MuzzleFlash_" + weaponType);
        particleObj.transform.position = position;
        particleObj.transform.rotation = rotation;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.startLifetime = 0.15f;
        main.loop = false;
        main.playOnAwake = false;

        ConfigureParticleByWeapon(ps, weaponType);

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return particleObj;
    }

    void ConfigureParticleByWeapon(ParticleSystem ps, WeaponType weaponType)
    {
        var main = ps.main;
        var shape = ps.shape;
        var emission = ps.emission;

        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.05f;

        switch (weaponType)
        {
            case WeaponType.Pistol:
                main.startSpeed = 4f;
                main.startSize = 0.08f;
                main.startColor = new Color(1f, 0.8f, 0.3f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 5) });
                break;

            case WeaponType.Rifle:
                main.startSpeed = 6f;
                main.startSize = 0.1f;
                main.startColor = new Color(1f, 0.5f, 0.1f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) });
                break;

            case WeaponType.Shotgun:
                main.startSpeed = 8f;
                main.startSize = 0.12f;
                main.startColor = new Color(1f, 0.4f, 0f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 15) });
                break;

            case WeaponType.Sniper:
                main.startSpeed = 7f;
                main.startSize = 0.09f;
                main.startColor = new Color(0.7f, 0.8f, 1f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 8) });
                break;
        }
    }

    void ReturnParticleToPool(GameObject particle)
    {
        if (particle == null || !activeParticles.ContainsKey(particle))
            return;

        PoolReturnInfo info = activeParticles[particle];
        activeParticles.Remove(particle);

        particle.SetActive(false);

        if (particlePools.ContainsKey(info.poolKey) && particlePools[info.poolKey].Count < maxPoolSize)
        {
            particlePools[info.poolKey].Enqueue(particle);
        }
        else
        {
            Destroy(particle);
        }
    }

    public void ClearPool()
    {
        // Clear active particles
        foreach (var particle in activeParticles.Keys)
        {
            if (particle != null)
                Destroy(particle);
        }
        activeParticles.Clear();

        // Clear pooled particles
        foreach (var pool in particlePools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                    Destroy(obj);
            }
        }
        particlePools.Clear();
    }
}
