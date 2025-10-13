using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    [Header("Wave Settings")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int maxWaves = -1; // Set to -1 for infinite waves
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float waveStartDelay = 2f;
    
    [Header("Enemy Count Scaling")]
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private float enemyCountMultiplier = 1.3f;
    
    [Header("Batch Spawning")]
    [SerializeField] private bool useBatchSpawning = true;
    [SerializeField] private int baseBatchSize = 2; // How many enemies spawn together
    [SerializeField] private float batchSizeMultiplier = 1.2f; // Batch size increases with waves
    [SerializeField] private float timeBetweenBatches = 3f; // Time between each batch
    [SerializeField] private float batchSpawnDelay = 0.2f; // Small delay between enemies in same batch
    
    [Header("Wave Composition")]
    [SerializeField] private float zombieChance = 0.4f; // 40% zombies
    [SerializeField] private float ghostChance = 0.3f; // 30% ghosts  
    [SerializeField] private float skeletonChance = 0.3f; // 30% skeletons
    [SerializeField] private float bossWaveInterval = 5f; // Every 5 waves
    [SerializeField] private bool enableBossWaves = true;
    
    [Header("Future Enemy Types")]
    [SerializeField] private GameObject[] futureEnemyTypes; // For later enemy variety
    [SerializeField] private int[] enemyUnlockWaves; // Which waves unlock new enemy types
    
    [Header("Events")]
    public UnityEvent<int> OnWaveStarted;
    public UnityEvent<int> OnWaveCompleted;
    public UnityEvent OnAllWavesCompleted;
    public UnityEvent<int> OnEnemySpawned;
    public UnityEvent<int> OnBatchSpawned;
    
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameHUD gameHUD;
    
    // Wave state
    private bool isWaveActive = false;
    private int enemiesSpawnedThisWave = 0;
    private int enemiesKilledThisWave = 0;
    private int totalEnemiesThisWave = 0;
    private Coroutine currentWaveCoroutine;
    
    public int GetCurrentWave() => currentWave;
    public int GetMaxWaves() => maxWaves;
    public bool IsWaveActive() => isWaveActive;
    public int GetEnemiesRemaining() => totalEnemiesThisWave - enemiesKilledThisWave;
    public float GetWaveProgress() => (float)enemiesKilledThisWave / totalEnemiesThisWave;
    public int GetTotalEnemiesThisWave() => totalEnemiesThisWave;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize events
            if (OnWaveStarted == null)
                OnWaveStarted = new UnityEvent<int>();
            if (OnWaveCompleted == null)
                OnWaveCompleted = new UnityEvent<int>();
            if (OnAllWavesCompleted == null)
                OnAllWavesCompleted = new UnityEvent();
            if (OnEnemySpawned == null)
                OnEnemySpawned = new UnityEvent<int>();
            if (OnBatchSpawned == null)
                OnBatchSpawned = new UnityEvent<int>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

	void Start()
	{
		StartCoroutine(InitializeAfterSceneLoad());
	}

	private IEnumerator InitializeAfterSceneLoad()
	{
		// If gameplaySceneNames is set, load the first gameplay scene additively
		if (gameplaySceneNames != null && gameplaySceneNames.Length > 0) {
			SceneManager.LoadSceneAsync(gameplaySceneNames[0], LoadSceneMode.Additive);
			Debug.Log($"Loading first gameplay scene: {gameplaySceneNames[0]}");
		}

		// Wait until the gameplay scene is loaded (sceneCount > 1)
		while (SceneManager.sceneCount < 2)
			yield return null;
		Debug.Log("First gameplay scene loaded and active.");
		// Now the gameplay scene is loaded, so EnemySpawner exists
		if (enemySpawner == null)
			enemySpawner = FindObjectOfType<EnemySpawner>();
		if (gameHUD == null)
			gameHUD = GameHUD.Instance;

		StartCoroutine(WaveSystemLoop());
	}

	IEnumerator WaveSystemLoop()
	{
		while (maxWaves == -1 || currentWave <= maxWaves) {
			yield return StartCoroutine(StartWave());
			yield return StartCoroutine(WaitForWaveCompletion());

			OnWaveCompleted?.Invoke(currentWave);
			Debug.Log($"Wave {currentWave} completed!");

			Debug.Log($"About to check scene change for wave {currentWave}");
			yield return StartCoroutine(CheckAndChangeScene());  // Changed to coroutine
			Debug.Log($"Finished checking scene change for wave {currentWave}");

			if (maxWaves != -1 && currentWave >= maxWaves) {
				OnAllWavesCompleted?.Invoke();
				Debug.Log("All waves completed! Victory!");
				yield break;
			}

			currentWave++;
			if (gameHUD != null) {
				gameHUD.UpdateWaveDisplay(currentWave, maxWaves);
			}
			yield return new WaitForSeconds(timeBetweenWaves);
		}
	}

	IEnumerator StartWave()
    {
        isWaveActive = true;
        enemiesSpawnedThisWave = 0;
        enemiesKilledThisWave = 0;
        
        // Calculate total enemies for this wave
        totalEnemiesThisWave = CalculateEnemiesForWave(currentWave);
        
        // Update HUD
        if (gameHUD != null)
        {
            gameHUD.UpdateWaveDisplay(currentWave, maxWaves);
        }
        
        // Notify wave started
        OnWaveStarted?.Invoke(currentWave);
        Debug.Log($"Starting Wave {currentWave} - {totalEnemiesThisWave} enemies");
        
        // Brief delay before spawning
        yield return new WaitForSeconds(waveStartDelay);
        
        // Spawn enemies for this wave
        currentWaveCoroutine = StartCoroutine(SpawnWaveEnemies());
    }
    
    IEnumerator SpawnWaveEnemies()
    {
        if (useBatchSpawning)
        {
            yield return StartCoroutine(SpawnEnemiesInBatches());
        }
        else
        {
            yield return StartCoroutine(SpawnEnemiesOneByOne());
        }
    }
    
    IEnumerator SpawnEnemiesInBatches()
    {
        int batchSize = CalculateBatchSize();
        int enemiesSpawned = 0;
        int batchNumber = 1;
        
        while (enemiesSpawned < totalEnemiesThisWave)
        {
            // Calculate how many enemies to spawn in this batch
            int enemiesInThisBatch = Mathf.Min(batchSize, totalEnemiesThisWave - enemiesSpawned);
            
            Debug.Log($"Spawning Batch {batchNumber} - {enemiesInThisBatch} enemies");
            
            // Spawn all enemies in this batch
            for (int i = 0; i < enemiesInThisBatch; i++)
            {
                GameObject enemyPrefab = DetermineEnemyType();
                SpawnEnemy(enemyPrefab);
                
                enemiesSpawned++;
                enemiesSpawnedThisWave++;
                OnEnemySpawned?.Invoke(enemiesSpawnedThisWave);
                
                // Small delay between enemies in the same batch
                if (i < enemiesInThisBatch - 1) // Don't wait after the last enemy in batch
                {
                    yield return new WaitForSeconds(batchSpawnDelay);
                }
            }
            
            OnBatchSpawned?.Invoke(batchNumber);
            batchNumber++;
            
            // Wait between batches (unless this was the last batch)
            if (enemiesSpawned < totalEnemiesThisWave)
            {
                yield return new WaitForSeconds(timeBetweenBatches);
            }
        }
    }
    
    IEnumerator SpawnEnemiesOneByOne()
    {
        while (enemiesSpawnedThisWave < totalEnemiesThisWave)
        {
            // Determine enemy type
            GameObject enemyPrefab = DetermineEnemyType();
            
            // Spawn enemy
            SpawnEnemy(enemyPrefab);
            
            enemiesSpawnedThisWave++;
            OnEnemySpawned?.Invoke(enemiesSpawnedThisWave);
            
            // Wait between spawns (scales with wave difficulty)
            float spawnDelay = CalculateSpawnDelay();
            yield return new WaitForSeconds(spawnDelay);
        }
    }
    
    IEnumerator WaitForWaveCompletion()
    {
        while (isWaveActive && enemiesKilledThisWave < totalEnemiesThisWave)
        {
            // Check for enemy deaths by counting remaining enemies
            CheckEnemyDeaths();
            yield return new WaitForSeconds(0.1f);
        }
        
        isWaveActive = false;
        
        // Stop any ongoing spawn coroutine
        if (currentWaveCoroutine != null)
        {
            StopCoroutine(currentWaveCoroutine);
            currentWaveCoroutine = null;
        }
    }
    
    private void CheckEnemyDeaths()
    {
        // Count current enemies on the field
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int currentEnemies = enemies.Length;
        
        // Calculate how many have died
        int expectedEnemies = enemiesSpawnedThisWave;
        int deadEnemies = expectedEnemies - currentEnemies;
        
        // Update killed count if it's higher than before
        if (deadEnemies > enemiesKilledThisWave)
        {
            enemiesKilledThisWave = deadEnemies;
            Debug.Log($"Enemy killed! ({enemiesKilledThisWave}/{totalEnemiesThisWave})");
        }
    }
    
    private int CalculateEnemiesForWave(int wave)
    {
        // Boss wave logic - fewer enemies but much stronger
        if (enableBossWaves && wave % bossWaveInterval == 0)
        {
            // Boss waves have fewer enemies but they're much stronger
            return Mathf.Max(1, Mathf.RoundToInt(baseEnemiesPerWave * 0.5f));
        }
        
        // Normal wave logic - exponential growth with some randomness
        float baseCount = baseEnemiesPerWave;
        float scaledCount = baseCount * Mathf.Pow(enemyCountMultiplier, wave - 1);
        
        // Add some randomness (±20%)
        float randomFactor = Random.Range(0.8f, 1.2f);
        
        return Mathf.RoundToInt(scaledCount * randomFactor);
    }
    
    private int CalculateBatchSize()
    {
        // Batch size increases with wave difficulty
        float scaledBatchSize = baseBatchSize * Mathf.Pow(batchSizeMultiplier, currentWave - 1);
        
        // Add some randomness (±15%)
        float randomFactor = Random.Range(0.85f, 1.15f);
        
        return Mathf.Max(1, Mathf.RoundToInt(scaledBatchSize * randomFactor));
    }
    
    private GameObject DetermineEnemyType()
    {
        if (enemySpawner == null) return null;
        
        // Check for future enemy types that should be unlocked
        if (futureEnemyTypes != null && futureEnemyTypes.Length > 0 && enemyUnlockWaves != null)
        {
            for (int i = 0; i < futureEnemyTypes.Length && i < enemyUnlockWaves.Length; i++)
            {
                if (currentWave >= enemyUnlockWaves[i] && futureEnemyTypes[i] != null)
                {
                    // This enemy type is unlocked for this wave
                    // For now, still use basic enemies, but this is ready for future types
                    // TODO: Implement enemy type selection logic
                }
            }
        }
        
        // Boss wave logic - spawn MutantZombie bosses
        if (enableBossWaves && currentWave % bossWaveInterval == 0)
        {
            // Every 5th wave is a boss wave - spawn MutantZombie
            return enemySpawner.mutantZombiePrefab;
        }
        
        // Normal wave logic - mix of all enemy types using chance variables
        float randomValue = Random.value;
        
        if (randomValue < zombieChance)
        {
            return enemySpawner.zombiePrefab;
        }
        else if (randomValue < zombieChance + ghostChance)
        {
            return enemySpawner.ghostPrefab;
        }
        else if (randomValue < zombieChance + ghostChance + skeletonChance)
        {
            return enemySpawner.skeletonPrefab;
        }
        else
        {
            // Fallback to zombie if random value is outside expected range
            return enemySpawner.zombiePrefab;
        }
    }
    
    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null || enemySpawner == null) return;
        
        // Get spawn point
        Transform spawnPoint = enemySpawner.spawnPoints[Random.Range(0, enemySpawner.spawnPoints.Length)];
        
        // Spawn enemy (no stat scaling - enemies keep their base stats)
        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    }
    
    private float CalculateSpawnDelay()
    {
        // Spawn faster in later waves for more intensity
        float baseDelay = 1.5f;
        float waveFactor = Mathf.Max(0.3f, 1f - (currentWave * 0.05f)); // Minimum 0.3s delay
        return baseDelay * waveFactor;
    }
    
    public void CompleteCurrentWave()
    {
        if (isWaveActive)
        {
            enemiesKilledThisWave = totalEnemiesThisWave;
        }
    }
    
    public void ToggleBatchSpawning()
    {
        useBatchSpawning = !useBatchSpawning;
        Debug.Log($"Batch spawning: {(useBatchSpawning ? "ON" : "OFF")}");
    }


	// Add these fields to WaveManager
	[SerializeField] private string[] gameplaySceneNames; // Assign your 4 scene names in the Inspector
	private int currentSceneIndex = 0;

	// Call this after each wave completes (e.g., at the end of WaitForWaveCompletion or after OnWaveCompleted)
	private IEnumerator CheckAndChangeScene()
	{
		Debug.Log($"CheckAndChangeScene called. Current wave: {currentWave}");
		bool condition = currentWave > 1 && (currentWave) % 5 == 0;
		Debug.Log($"Scene change condition: {condition} (Wave {currentWave}, (Wave)%5 = {(currentWave) % 5})");

		if (condition) {
			// Clear all towers first
			if (TowerPlacementManager.Instance != null)
				TowerPlacementManager.Instance.ClearPlacedTowers();

			string currentScene = gameplaySceneNames[currentSceneIndex];
			Debug.Log("Unloading scene: " + currentScene);

			// Wait for current scene to fully unload
			var unloadOperation = SceneManager.UnloadSceneAsync(currentScene);
			while (!unloadOperation.isDone)
				yield return null;

			// Force a garbage collection to clean up resources
			System.GC.Collect();
			yield return new WaitForSeconds(0.1f); // Small delay to ensure cleanup

			currentSceneIndex = (currentSceneIndex + 1) % gameplaySceneNames.Length;
			string nextScene = gameplaySceneNames[currentSceneIndex];
			Debug.Log("Loading scene: " + nextScene);

			// Load the new scene with explicit settings
			var loadOperation = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
			loadOperation.allowSceneActivation = false; // Prevent immediate activation

			// Wait until the scene is almost ready
			while (loadOperation.progress < 0.9f)
				yield return null;

			// Force terrain to update before activation
			Terrain[] terrains = FindObjectsOfType<Terrain>();
			foreach (var terrain in terrains) {
				if (terrain != null) {
					// Force terrain update
					terrain.enabled = false;
					yield return new WaitForEndOfFrame();
					terrain.enabled = true;
				}
			}

			// Now allow the scene to activate
			loadOperation.allowSceneActivation = true;
			while (!loadOperation.isDone)
				yield return null;

			// Additional terrain refresh after scene is loaded
			yield return StartCoroutine(RefreshTerrains());

			// Re-find the EnemySpawner in the new scene
			enemySpawner = FindObjectOfType<EnemySpawner>();
			Debug.Log($"Found new EnemySpawner: {(enemySpawner != null)}");
		}
	}

	// Add this new coroutine to help refresh terrains
	private IEnumerator RefreshTerrains()
	{
		yield return new WaitForSeconds(0.1f); // Give Unity a moment to initialize

		Terrain[] terrains = FindObjectsOfType<Terrain>();
		foreach (var terrain in terrains) {
			if (terrain != null) {
				// Force terrain data to refresh
				terrain.Flush();

				// Toggle terrain to force refresh
				terrain.enabled = false;
				yield return new WaitForEndOfFrame();
				terrain.enabled = true;

				// Ensure terrain settings are properly applied
				terrain.gameObject.SetActive(false);
				yield return new WaitForEndOfFrame();
				terrain.gameObject.SetActive(true);
			}
		}
	}
}