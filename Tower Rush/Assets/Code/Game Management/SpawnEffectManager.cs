using UnityEngine;
using System.Collections;

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
    
    [Header("Audio")]
    public AudioClip portalOpenSound;
    public AudioClip portalCloseSound;
    public float audioVolume = 0.7f;
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = audioVolume;
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
    
    public IEnumerator SpawnWithEffect(GameObject enemyPrefab, Vector3 portalPosition, Vector3 enemySpawnPosition, string enemyType)
    {
        // Get appropriate effect for enemy type
        GameObject effectPrefab = GetSpawnEffectForEnemy(enemyType);
        
        GameObject effect = null;
        if (effectPrefab != null)
        {
            effect = Instantiate(effectPrefab, portalPosition, Quaternion.identity);
            
            // Play portal open sound
            if (portalOpenSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(portalOpenSound, audioVolume);
            }
        }
        else
        {
            // Fallback: Create a simple portal effect
            effect = CreateSimplePortalEffect(portalPosition, enemyType);
        }
        
        // Wait for effect to play
        yield return new WaitForSeconds(spawnDelay);
        
        // Spawn enemy at the elevated position
        GameObject enemy = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);
        
        // Clean up effect
        if (effect != null)
        {
            Destroy(effect, portalDuration - spawnDelay);
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
