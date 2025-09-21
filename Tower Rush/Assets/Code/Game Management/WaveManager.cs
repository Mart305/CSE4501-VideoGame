using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    public EnemySpawner spawner;
    public int waveNumber = 1;
    public float timeBetweenWaves = 5f;

    void Start()
    {
        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        while (true)
        {
            Debug.Log("Starting Wave " + waveNumber);

            for (int i = 0; i < waveNumber * 3; i++) // Scale with wave
            {
                spawner.SpawnEnemy();
                yield return new WaitForSeconds(1.5f);
            }

            waveNumber++;
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }
}
