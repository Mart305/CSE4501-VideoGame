using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public GameObject zombiePrefab;
    public GameObject ghostPrefab;
    public GameObject skeletonPrefab;
    public GameObject mutantZombiePrefab;
    
    [Header("Debug")]
    [SerializeField] private bool showSpawnPoints = true;
    
    void Start()
    {
        // Validate spawn points and prefabs
        // Silent validation - errors will be apparent during gameplay if missing
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
            Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        }
    }
    
    // Method for WaveManager to spawn specific enemy types
    public GameObject SpawnEnemyAtPoint(GameObject enemyPrefab, int spawnPointIndex = -1)
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return null;
        
        int pointIndex = spawnPointIndex >= 0 ? spawnPointIndex : Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[pointIndex];
        
        return Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    }
}
