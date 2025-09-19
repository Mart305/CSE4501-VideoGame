using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public GameObject zombiePrefab;
    public GameObject ghostPrefab;

    public float spawnInterval = 3f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    public void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemyPrefab = (Random.value > 0.5f) ? zombiePrefab : ghostPrefab;

        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    }
}
