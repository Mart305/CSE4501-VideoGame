using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

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
    [SerializeField] private float enemyCountMultiplier = 1.15f; // Reduced from 1.3f to slow down scaling
    
    [Header("Batch Spawning")]
    [SerializeField] private bool useBatchSpawning = true;
    [SerializeField] private int baseBatchSize = 2; // How many enemies spawn together
    [SerializeField] private float batchSizeMultiplier = 1.2f; // Batch size increases with waves
    [SerializeField] private float timeBetweenBatches = 3f; // Time between each batch
    [SerializeField] private float batchSpawnDelay = 0.2f; // Small delay between enemies in same batch
    
    [Header("Wave Composition")]
    // Note: Spawn chances are now hardcoded in DetermineEnemyType() method for better control
    [SerializeField] private float bossWaveChance = 0.2f; // 20% chance for random boss waves
    [SerializeField] private bool enableRandomBossWaves = true;
    [SerializeField] private bool enableSkeletons = true; // Skeletons appear every wave
    [SerializeField] private int mutantZombieUnlockWave = 5; // Mutant zombies start appearing from wave 5
    
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
		// Don't load gameplay scene automatically - wait for Start button
		// Don't start wave system yet - wait for StartGameplay() call
		yield break; // End the coroutine
	}
	
	public void StartGameplay()
	{
		// Load the first gameplay scene and make it active
		if (gameplaySceneNames != null && gameplaySceneNames.Length > 0) {
			StartCoroutine(LoadFirstGameplayScene());
		}
	}
	
	private IEnumerator LoadFirstGameplayScene()
	{
		string targetSceneName = gameplaySceneNames[0];
		
		// Check if the scene is already loaded
		Scene existingScene = SceneManager.GetSceneByName(targetSceneName);
		if (existingScene.IsValid() && existingScene.isLoaded)
		{
			// Disable AudioListeners in current scene before switching
			Scene currentScene = SceneManager.GetActiveScene();
			if (currentScene.IsValid())
			{
				GameObject[] currentSceneObjects = currentScene.GetRootGameObjects();
				foreach (GameObject obj in currentSceneObjects)
				{
					AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
					foreach (AudioListener listener in listeners)
					{
						listener.enabled = false;
					}
				}
			}
			
			SceneManager.SetActiveScene(existingScene);
		}
		else
		{
			// Load the scene additively only if it's not already loaded
			AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
			yield return loadOp;
			
			// Disable AudioListeners in current scene before switching
			Scene currentScene = SceneManager.GetActiveScene();
			if (currentScene.IsValid())
			{
				GameObject[] currentSceneObjects = currentScene.GetRootGameObjects();
				foreach (GameObject obj in currentSceneObjects)
				{
					AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
					foreach (AudioListener listener in listeners)
					{
						listener.enabled = false;
					}
				}
			}
			
			// Set the loaded scene as the active scene IMMEDIATELY after loading
			Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);
			if (loadedScene.IsValid())
			{
				SceneManager.SetActiveScene(loadedScene);
				
				// Force lighting settings update
				DynamicGI.UpdateEnvironment();
				
				// Small delay to ensure lighting is properly applied
				yield return new WaitForSeconds(0.1f);
			}
		}
		
		// Unload the ManagerScene (but keep DontDestroyOnLoad objects)
		Scene managerScene = SceneManager.GetSceneByName("ManagerScene");
		if (managerScene.IsValid() && managerScene.isLoaded)
		{
			// Only unload if there are other scenes loaded
			if (SceneManager.sceneCount > 1)
			{
				AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(managerScene);
				yield return unloadOp;
				
				// Force another lighting update after manager scene is unloaded
				DynamicGI.UpdateEnvironment();
			}
		}
		
		// Find components in the active gameplay scene
		if (enemySpawner == null)
			enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
		if (gameHUD == null)
			gameHUD = GameHUD.Instance;

		StartCoroutine(WaveSystemLoop());
	}

	IEnumerator WaveSystemLoop()
	{
		while (maxWaves == -1 || currentWave <= maxWaves) {
			// Check if game is still active
			if (GameStateManager.Instance != null && !GameStateManager.Instance.IsGameActive())
			{
				yield return new WaitForSeconds(0.5f);
				continue;
			}
			
			// Wait for at least one tower to be placed before starting waves
			yield return StartCoroutine(WaitForFirstTower());
			
			yield return StartCoroutine(StartWave());
			yield return StartCoroutine(WaitForWaveCompletion());

			OnWaveCompleted?.Invoke(currentWave);
			yield return StartCoroutine(CheckAndChangeScene());

			if (maxWaves != -1 && currentWave >= maxWaves) {
				OnAllWavesCompleted?.Invoke();
				yield break;
			}

			currentWave++;
			if (gameHUD != null) {
				gameHUD.UpdateWaveDisplay(currentWave, maxWaves);
			}
			yield return new WaitForSeconds(timeBetweenWaves);
		}
	}

	IEnumerator WaitForFirstTower()
	{
		
		// Wait until at least one tower is placed
		while (true)
		{
			BaseTower[] towers = FindObjectsOfType<BaseTower>();
			if (towers != null && towers.Length > 0)
			{
				// Check if any tower exists and has health > 0
				foreach (BaseTower tower in towers)
				{
					if (tower != null && tower.GetCurrentHealth() > 0)
					{
						yield break; // Exit the loop, tower found
					}
				}
			}
			
			// Wait a bit before checking again
			yield return new WaitForSeconds(0.5f);
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
        }
    }
    
    private int CalculateEnemiesForWave(int wave)
    {
        // Exponential growth with some randomness
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
        
        // Check if this is a random boss wave (only after wave 7)
        bool isRandomBossWave = enableRandomBossWaves && currentWave >= 8 && Random.value < bossWaveChance;
        
        // Boss wave logic - higher mutant zombie spawn rate
        if (isRandomBossWave && currentWave >= mutantZombieUnlockWave && enemySpawner.mutantZombiePrefab != null)
        {
            float bossRoll = Random.value;
            // Boss waves: 40% mutant zombies, 30% skeletons, 20% ghosts, 10% zombies
            if (bossRoll < 0.4f)
                return enemySpawner.mutantZombiePrefab;
            else if (bossRoll < 0.7f)
                return enemySpawner.skeletonPrefab;
            else if (bossRoll < 0.9f)
                return enemySpawner.ghostPrefab;
            else
                return enemySpawner.zombiePrefab;
        }
        
        // Normal wave logic
        float roll = Random.value;
        
        // Before mutant zombies unlock (waves 1-4): 33% each
        if (currentWave < mutantZombieUnlockWave)
        {
            if (enableSkeletons && enemySpawner.skeletonPrefab != null)
            {
                if (roll < 0.33f)
                    return enemySpawner.zombiePrefab;
                else if (roll < 0.66f)
                    return enemySpawner.ghostPrefab;
                else
                    return enemySpawner.skeletonPrefab;
            }
            else
            {
                // If skeletons disabled, 50/50 zombies and ghosts
                return roll < 0.5f ? enemySpawner.zombiePrefab : enemySpawner.ghostPrefab;
            }
        }
        else
        {
            // After mutant zombies unlock (wave 5+): 30% each + 10% mutant zombies
            if (enableSkeletons && enemySpawner.skeletonPrefab != null && enemySpawner.mutantZombiePrefab != null)
            {
                if (roll < 0.3f)
                    return enemySpawner.zombiePrefab;
                else if (roll < 0.6f)
                    return enemySpawner.ghostPrefab;
                else if (roll < 0.9f)
                    return enemySpawner.skeletonPrefab;
                else
                    return enemySpawner.mutantZombiePrefab;
            }
            else if (enemySpawner.mutantZombiePrefab != null)
            {
                // If skeletons disabled but mutant zombies available
                if (roll < 0.45f)
                    return enemySpawner.zombiePrefab;
                else if (roll < 0.9f)
                    return enemySpawner.ghostPrefab;
                else
                    return enemySpawner.mutantZombiePrefab;
            }
            else
            {
                // Fallback to basic enemies if mutant zombies not available
                if (enableSkeletons && enemySpawner.skeletonPrefab != null)
                {
                    if (roll < 0.33f)
                        return enemySpawner.zombiePrefab;
                    else if (roll < 0.66f)
                        return enemySpawner.ghostPrefab;
                    else
                        return enemySpawner.skeletonPrefab;
                }
                else
                {
                    return roll < 0.5f ? enemySpawner.zombiePrefab : enemySpawner.ghostPrefab;
                }
            }
        }
    }
    
    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null || enemySpawner == null) return;
        
        // Get spawn point
        Transform spawnPoint = enemySpawner.spawnPoints[Random.Range(0, enemySpawner.spawnPoints.Length)];
        
        // Use the spawn point's actual Y position (don't force a specific height)
        Vector3 spawnPosition = spawnPoint.position;
        
        // Spawn enemy (no stat scaling - enemies keep their base stats)
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
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
    }


	// Add these fields to WaveManager
	[SerializeField] private string[] gameplaySceneNames; // Assign your 4 scene names in the Inspector
	private int currentSceneIndex = 0;

	// Call this after each wave completes (e.g., at the end of WaitForWaveCompletion or after OnWaveCompleted)
	private IEnumerator CheckAndChangeScene()
	{
		bool condition = currentWave > 1 && (currentWave) % 5 == 0;

		// Only change scenes if the condition is met
		if (condition && gameplaySceneNames != null && gameplaySceneNames.Length > 1 && currentSceneIndex < gameplaySceneNames.Length) {
			// Clear any placed towers before scene change
			if (TowerPlacementManager.Instance != null)
				TowerPlacementManager.Instance.ClearPlacedTowers();

			string currentScene = gameplaySceneNames[currentSceneIndex];
			currentSceneIndex = (currentSceneIndex + 1) % gameplaySceneNames.Length;
			string nextScene = gameplaySceneNames[currentSceneIndex];

			// Load the new scene FIRST, then unload the old one
			var loadOperation = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
			loadOperation.allowSceneActivation = false; // Prevent immediate activation

			// Wait until the scene is almost ready
			while (loadOperation.progress < 0.9f)
				yield return null;

			// Now allow the scene to activate
			loadOperation.allowSceneActivation = true;
			while (!loadOperation.isDone)
				yield return null;

			// Disable AudioListeners in the old scene before setting new scene as active
			Scene currentActiveScene = SceneManager.GetActiveScene();
			if (currentActiveScene.IsValid())
			{
				GameObject[] oldSceneObjects = currentActiveScene.GetRootGameObjects();
				foreach (GameObject obj in oldSceneObjects)
				{
					AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
					foreach (AudioListener listener in listeners)
					{
						listener.enabled = false;
					}
				}
			}

			// Set the new scene as active
			Scene newActiveScene = SceneManager.GetSceneByName(nextScene);
			if (newActiveScene.IsValid())
			{
				SceneManager.SetActiveScene(newActiveScene);
				
				// Force lighting settings update
				DynamicGI.UpdateEnvironment();
				
				// Small delay to ensure lighting is properly applied
				yield return new WaitForSeconds(0.1f);
			}

			// NOW unload the old scene (after new one is loaded and active)
			Scene sceneToUnload = SceneManager.GetSceneByName(currentScene);
			if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
			{
				var unloadOperation = SceneManager.UnloadSceneAsync(currentScene);
				while (!unloadOperation.isDone)
					yield return null;
				
				// Force another lighting update after old scene is unloaded
				DynamicGI.UpdateEnvironment();
			}

			// Re-find the EnemySpawner in the new scene
			enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
		}
	}
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