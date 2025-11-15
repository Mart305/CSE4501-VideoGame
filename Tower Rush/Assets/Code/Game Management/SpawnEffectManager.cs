using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnEffectManager : MonoBehaviour
{
    [Header("Portal Effects")]
    public GameObject zombiePortalPrefab;     // Portal red.prefab
    public GameObject ghostPortalPrefab;      // Portal blue.prefab
    public GameObject skeletonPortalPrefab;   // Portal yellow.prefab
    public GameObject mutantPortalPrefab;     // Portal green.prefab

    [Header("Magic Circle Effects")]
    public GameObject magicCirclePrefab;      // Magic circle.prefab
    public GameObject freezeCirclePrefab;     // Freeze circle.prefab

    [Header("Settings")]
    public float portalDuration = 2f;
    public float spawnDelay = 1f;
    public bool useMagicCircles = false; // Toggle between portals and circles

    [Header("Pooling Settings")]
    public int initialPoolSize = 5;
    public int maxPoolSize = 15;

    [Header("Audio")]
    public AudioClip portalOpenSound;
    public AudioClip portalCloseSound;
    public float audioVolume = 0.7f;

    private AudioSource audioSource;
    private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> activePrefabs = new Dictionary<string, GameObject>();

    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = audioVolume;

        // Pre-warm the effect pools
        PreWarmPools();
    }

    void PreWarmPools()
    {
        // Pre-warm pools for each enemy type
        string[] enemyTypes = { "zombie", "ghost", "skeleton", "mutantzombie" };

        foreach (string enemyType in enemyTypes)
        {
            GameObject prefab = GetPrefabForEnemyType(enemyType);
            if (prefab != null)
            {
                string poolKey = "Effect_" + enemyType;
                effectPools[poolKey] = new Queue<GameObject>();
                activePrefabs[poolKey] = prefab;

                for (int i = 0; i < initialPoolSize; i++)
                {
                    GameObject effect = Instantiate(prefab);
                    effect.SetActive(false);
                    effect.transform.SetParent(transform);
                    effectPools[poolKey].Enqueue(effect);
                }
            }
        }
    }

    GameObject GetPrefabForEnemyType(string enemyType)
    {
        if (useMagicCircles)
        {
            return enemyType.ToLower() switch
            {
                "zombie" => freezeCirclePrefab,
                "ghost" => magicCirclePrefab,
                "skeleton" => magicCirclePrefab,
                "mutantzombie" => freezeCirclePrefab,
                _ => magicCirclePrefab
            };
        }
        else
        {
            return enemyType.ToLower() switch
            {
                "zombie" => zombiePortalPrefab,
                "ghost" => ghostPortalPrefab,
                "skeleton" => skeletonPortalPrefab,
                "mutantzombie" => mutantPortalPrefab,
                _ => zombiePortalPrefab
            };
        }
    }
    
    public GameObject GetSpawnEffectForEnemy(string enemyType)
    {
        if (useMagicCircles)
        {
            return enemyType.ToLower() switch
            {
                "zombie" => freezeCirclePrefab,
                "ghost" => magicCirclePrefab,
                "skeleton" => magicCirclePrefab,
                "mutantzombie" => freezeCirclePrefab,
                _ => magicCirclePrefab
            };
        }
        else
        {
            return enemyType.ToLower() switch
            {
                "zombie" => zombiePortalPrefab,
                "ghost" => ghostPortalPrefab,
                "skeleton" => skeletonPortalPrefab,
                "mutantzombie" => mutantPortalPrefab,
                _ => zombiePortalPrefab
            };
        }
    }

    GameObject GetEffectFromPool(string enemyType, Vector3 position)
    {
        string poolKey = "Effect_" + enemyType.ToLower();

        if (!effectPools.ContainsKey(poolKey))
        {
            effectPools[poolKey] = new Queue<GameObject>();
            GameObject prefab = GetPrefabForEnemyType(enemyType);
            if (prefab != null)
            {
                activePrefabs[poolKey] = prefab;
            }
        }

        GameObject effect;
        if (effectPools[poolKey].Count > 0)
        {
            effect = effectPools[poolKey].Dequeue();
            effect.transform.position = position;
            effect.SetActive(true);
        }
        else
        {
            GameObject prefab = activePrefabs.ContainsKey(poolKey) ? activePrefabs[poolKey] : GetPrefabForEnemyType(enemyType);
            if (prefab != null)
            {
                effect = Instantiate(prefab, position, Quaternion.identity);
                effect.transform.SetParent(transform);
            }
            else
            {
                return null;
            }
        }

        return effect;
    }

    void ReturnEffectToPool(GameObject effect, string enemyType)
    {
        if (effect == null) return;

        string poolKey = "Effect_" + enemyType.ToLower();

        effect.SetActive(false);

        if (effectPools.ContainsKey(poolKey) && effectPools[poolKey].Count < maxPoolSize)
        {
            effectPools[poolKey].Enqueue(effect);
        }
        else
        {
            Destroy(effect);
        }
    }

    public IEnumerator SpawnWithEffect(GameObject enemyPrefab, Vector3 portalPosition, Vector3 enemySpawnPosition, string enemyType)
    {
        // Get effect from pool
        GameObject effect = GetEffectFromPool(enemyType, portalPosition);

        if (effect == null)
        {
            // Fallback: Create a simple portal effect
            effect = CreateSimplePortalEffect(portalPosition, enemyType);
        }
        else
        {
            // Play portal open sound
            if (portalOpenSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(portalOpenSound, audioVolume);
            }
        }

        // Wait for effect to play
        yield return new WaitForSeconds(spawnDelay);

        // Spawn enemy at the elevated position
        GameObject enemy = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);

        // Return effect to pool after duration
        float remainingTime = portalDuration - spawnDelay;
        yield return new WaitForSeconds(remainingTime);

        if (effect != null)
        {
            ReturnEffectToPool(effect, enemyType);
        }

        yield return enemy;
    }
    
    private GameObject CreateSimplePortalEffect(Vector3 position, string enemyType)
    {
        // Create a simple portal effect using particle systems
        GameObject portal = new GameObject("SimplePortal_" + enemyType);
        portal.transform.position = position;
        
        // Add particle system for portal effect
        ParticleSystem particles = portal.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = portalDuration;
        main.startSpeed = 1f;
        main.startSize = 2f;
        main.startColor = GetColorForEnemyType(enemyType);
        main.maxParticles = 50;
        
        var emission = particles.emission;
        emission.rateOverTime = 100f;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1f;
        
        // Add rotation to the portal
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);
        
        return portal;
    }
    
    private Color GetColorForEnemyType(string enemyType)
    {
        return enemyType.ToLower() switch
        {
            "zombie" => Color.red,
            "ghost" => Color.blue,
            "skeleton" => Color.yellow,
            "mutantzombie" => Color.green,
            _ => Color.red
        };
    }
}
