using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public GameObject zombiePrefab;
    public GameObject ghostPrefab;
    public GameObject skeletonPrefab;
    public GameObject mutantZombiePrefab;
    
    [Header("Spawn Effects")]
    public SpawnEffectManager spawnEffectManager;
    public bool usePortalEffects = true;
    
    [Header("Debug")]
    [SerializeField] private bool showSpawnPoints = true;
    
    void Start()
    {
        // Validate spawn points and prefabs
        // Silent validation - errors will be apparent during gameplay if missing
        
        // Auto-find SpawnEffectManager if not assigned
        if (spawnEffectManager == null)
        {
            spawnEffectManager = FindObjectOfType<SpawnEffectManager>();
            if (spawnEffectManager == null)
            {
                GameObject effectManagerObj = new GameObject("SpawnEffectManager");
                spawnEffectManager = effectManagerObj.AddComponent<SpawnEffectManager>();
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showSpawnPoints || spawnPoints == null) return;
        
        // Draw spawn points in scene view
        Gizmos.color = Color.red;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
            }
        }
    }
    
    // This method is now called by WaveManager instead of running automatically
    public void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;
        
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyPrefab = (Random.value > 0.5f) ? zombiePrefab : ghostPrefab;
        
        if (enemyPrefab != null)
        {
            // Force Y position to 15.3 to prevent enemies from spawning in the air
            Vector3 spawnPosition = new Vector3(spawnPoint.position.x, 15.3f, spawnPoint.position.z);
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
    
    // Method for WaveManager to spawn specific enemy types
    public GameObject SpawnEnemyAtPoint(GameObject enemyPrefab, int spawnPointIndex = -1)
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return null;
        
        int pointIndex = spawnPointIndex >= 0 ? spawnPointIndex : Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[pointIndex];
        
        // Use portal effects if enabled
        if (usePortalEffects && spawnEffectManager != null)
        {
            Vector3 portalPosition = new Vector3(spawnPoint.position.x, 16.3f, spawnPoint.position.z); 
            Vector3 enemySpawnPosition = new Vector3(spawnPoint.position.x, 15.3f, spawnPoint.position.z); 
            StartCoroutine(SpawnWithPortalEffect(enemyPrefab, portalPosition, enemySpawnPosition, enemyPrefab.name));
            return null; // Will be spawned by coroutine
        }
        else
        {
            // Force Y position to 15.3 to prevent enemies from spawning in the air
            Vector3 spawnPosition = new Vector3(spawnPoint.position.x, 15.3f, spawnPoint.position.z);
            return Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
    
    // Enhanced spawn method with portal effects
    public GameObject SpawnEnemyWithPortal(GameObject enemyPrefab, int spawnPointIndex = -1)
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return null;
        
        int pointIndex = spawnPointIndex >= 0 ? spawnPointIndex : Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[pointIndex];
        
        Vector3 portalPosition = new Vector3(spawnPoint.position.x, 16.3f, spawnPoint.position.z); 
        Vector3 enemySpawnPosition = new Vector3(spawnPoint.position.x, 15.3f, spawnPoint.position.z); // Elevated for enemy
        
        // Start the portal spawn coroutine
        StartCoroutine(SpawnWithPortalEffect(enemyPrefab, portalPosition, enemySpawnPosition, enemyPrefab.name));
        
        return null; // Will be returned by coroutine
    }
    
    private IEnumerator SpawnWithPortalEffect(GameObject enemyPrefab, Vector3 portalPosition, Vector3 enemySpawnPosition, string enemyType)
    {
        if (spawnEffectManager != null)
        {
            // Use the SpawnEffectManager for consistent effects
            yield return StartCoroutine(spawnEffectManager.SpawnWithEffect(enemyPrefab, portalPosition, enemySpawnPosition, enemyType));
        }
        else
        {
            // Fallback: Simple spawn without effects
            yield return new WaitForSeconds(0.5f);
            Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);
        }
    }
}
