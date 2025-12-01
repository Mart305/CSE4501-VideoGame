using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
	public static WaveManager Instance { get; private set; }

	[Header("Wave Settings")]
	[SerializeField] private int currentWave = 1;
	[SerializeField] private int maxWaves = -1; // Set to -1 for infinite waves
	[SerializeField] private float timeBetweenWaves = 5f;
	[SerializeField] private float waveStartDelay = 2f;

	[Header("Enemy Count Scaling")]
	[SerializeField] private int baseEnemiesPerWave = 12; 
	[SerializeField] private float enemyCountMultiplier = 1.3f; // Reduced to make game easier

	[Header("Batch Spawning")]
	[SerializeField] private bool useBatchSpawning = true;
	[SerializeField] private int baseBatchSize = 2; // How many enemies spawn together
	[SerializeField] private float batchSizeMultiplier = 1.5f; // Batch size increases with waves
	[SerializeField] private float timeBetweenBatches = 3f; // Time between each batch
	[SerializeField] private float batchSpawnDelay = 0.2f; // Small delay between enemies in same batch

	[Header("Wave Composition")]
	// Note: Spawn chances are now hardcoded in DetermineEnemyType() method for better control
	[SerializeField] private float bossWaveChance = 0.12f; // Reduced from 20% to 12% chance for random boss waves
	[SerializeField] private bool enableRandomBossWaves = true;
	[SerializeField] private int mutantZombieUnlockWave = 7; // Mutant zombies start appearing from wave 7
	[SerializeField] private int necromancerUnlockWave = 4; // Necromancers start appearing from wave 4

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
	private int totalEnemiesDefeated = 0; // Track across all waves
	private Coroutine currentWaveCoroutine;

	[SerializeField] private string[] gameplaySceneNames; // Assign your 4 scene names in the Inspector
	private int currentSceneIndex = 0;

	private Dictionary<string, int> originalTowerCosts = new Dictionary<string, int>();
	private bool hasStoredOriginalCosts = false;

	// No-tower gold drain
	[Header("No-Tower Gold Drain")]
	[SerializeField] private bool enableNoTowerGoldDrain = true;
	[SerializeField] private int goldDrainPerSecondWhenNoTowers = 200;
	[SerializeField] private float goldDrainTickSeconds = 1f;
	private Coroutine noTowersDrainMonitor;
	private Coroutine noTowersDrainCoroutine;

	public int GetCurrentWave() => currentWave;
	public int GetMaxWaves() => maxWaves;
	public bool IsWaveActive() => isWaveActive;
	public int GetEnemiesRemaining() => totalEnemiesThisWave - enemiesKilledThisWave;
	public float GetWaveProgress() => (float)enemiesKilledThisWave / totalEnemiesThisWave;
	public int GetTotalEnemiesThisWave() => totalEnemiesThisWave;

	void Awake()
	{
		// Singleton pattern
		if (Instance == null) {
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
		else {
			Destroy(gameObject);
		}
	}


	public void StartGameplay()
	{
		// Stop all existing coroutines BEFORE starting new game
		StopAllCoroutines();

		// Load the first gameplay scene and make it active
		if (gameplaySceneNames != null && gameplaySceneNames.Length > 0) {
			StartCoroutine(RestartGameFromBeginning());
		}
	}

	private IEnumerator RestartGameFromBeginning()
	{
		// Reset wave state FIRST (before loading scenes)
		ResetWaveStateImmediate();

		// Then load the first scene (so we always have at least one scene)
		yield return StartCoroutine(LoadFirstGameplayScene());

		// Then unload all other gameplay scenes
		yield return StartCoroutine(UnloadAllGameplayScenes());
	}

	public void ResetWaveStateImmediate()
	{
		// Reset to wave 1
		currentWave = 1;
		currentSceneIndex = 0;

		// Reset tower costs tracking
		hasStoredOriginalCosts = false;
		originalTowerCosts.Clear();

		// Reset wave tracking
		isWaveActive = false;
		enemiesSpawnedThisWave = 0;
		enemiesKilledThisWave = 0;
		totalEnemiesThisWave = 0;
		totalEnemiesDefeated = 0; // Reset total count
		currentWaveCoroutine = null;

		// Force refresh of references
		enemySpawner = null;
		gameHUD = null;

		// Update HUD to show wave 1 (will be null, will refresh later)
		if (gameHUD != null) {
			gameHUD.UpdateWaveDisplay(currentWave, maxWaves);
		}
	}

	// Public method to force storage of current tower costs as base costs
	public void ForceStoreBaseTowerCosts()
	{
		hasStoredOriginalCosts = false;
		originalTowerCosts.Clear();
		StoreOriginalTowerCosts();
		hasStoredOriginalCosts = true;
	}

	private IEnumerator UnloadAllGameplayScenes()
	{
		// Don't unload the first gameplay scene (it should already be loaded)
		string firstSceneName = gameplaySceneNames != null && gameplaySceneNames.Length > 0 ? gameplaySceneNames[0] : "";

		// Collect scenes to unload first (don't modify during iteration)
		List<Scene> scenesToUnload = new List<Scene>();

		int sceneCount = SceneManager.sceneCount;
		for (int i = 0; i < sceneCount; i++) {
			Scene scene = SceneManager.GetSceneAt(i);

			// Don't unload ManagerScene, first gameplay scene, or DontDestroyOnLoad objects
			if (scene.name != "ManagerScene" && scene.name != firstSceneName && scene.isLoaded) {
				// Check if it's a gameplay scene
				bool isGameplayScene = false;
				if (gameplaySceneNames != null) {
					foreach (string sceneName in gameplaySceneNames) {
						if (scene.name == sceneName) {
							isGameplayScene = true;
							break;
						}
					}
				}

				// Add to unload list (except the first one)
				if (isGameplayScene) {
					scenesToUnload.Add(scene);
				}
			}
		}

		// Now unload all collected scenes
		foreach (Scene scene in scenesToUnload) {
			if (scene.isLoaded) {
				yield return SceneManager.UnloadSceneAsync(scene);
			}
		}

		// Force garbage collection to clean up old scene resources
		System.GC.Collect();
		Resources.UnloadUnusedAssets();
		
		// CRITICAL: Clean up any NavMesh objects that might have persisted
		CleanupPersistentNavMeshObjects();
	}
	
	private void CleanupPersistentNavMeshObjects()
	{
		// Find all objects in DontDestroyOnLoad scene
		GameObject[] allObjects = FindObjectsOfType<GameObject>();
		foreach (GameObject obj in allObjects)
		{
			if (obj.scene.name == "DontDestroyOnLoad")
			{
				// Destroy any NavMesh-related objects that shouldn't persist
				if (obj.GetComponent("NavMeshSurface") != null || 
				    obj.GetComponent("NavMeshModifier") != null ||
				    obj.GetComponent("NavMeshModifierVolume") != null ||
				    obj.name.ToLower().Contains("navmesh"))
				{
					Debug.LogWarning($"[WaveManager] Destroying persistent NavMesh object: {obj.name}");
					Destroy(obj);
				}
			}
		}
	}

	private IEnumerator LoadFirstGameplayScene()
	{
		string targetSceneName = gameplaySceneNames[0];

		// Check if the scene is already loaded
		Scene existingScene = SceneManager.GetSceneByName(targetSceneName);
		if (existingScene.IsValid() && existingScene.isLoaded) {
			// Disable AudioListeners in current scene before switching
			Scene currentScene = SceneManager.GetActiveScene();
			if (currentScene.IsValid()) {
				GameObject[] currentSceneObjects = currentScene.GetRootGameObjects();
				foreach (GameObject obj in currentSceneObjects) {
					AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
					foreach (AudioListener listener in listeners) {
						listener.enabled = false;
					}
				}
			}

			SceneManager.SetActiveScene(existingScene);
		}
		else {
			// Load the scene additively only if it's not already loaded
			AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
			yield return loadOp;

			// Disable AudioListeners in current scene before switching
			Scene currentScene = SceneManager.GetActiveScene();
			if (currentScene.IsValid()) {
				GameObject[] currentSceneObjects = currentScene.GetRootGameObjects();
				foreach (GameObject obj in currentSceneObjects) {
					AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
					foreach (AudioListener listener in listeners) {
						listener.enabled = false;
					}
				}
			}

			// Set the loaded scene as the active scene IMMEDIATELY after loading
			Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);
			if (loadedScene.IsValid()) {
				SceneManager.SetActiveScene(loadedScene);

				// Force lighting settings update
				DynamicGI.UpdateEnvironment();

				// Small delay to ensure lighting is properly applied
				yield return new WaitForSeconds(0.1f);
			}
		}

		// Switch to first gameplay scene music (index 1) when starting gameplay
		// currentSceneIndex is 0 here (first gameplay scene), so we use index 1 for music
		if (AudioManager.Instance != null)
			AudioManager.Instance.PlaySceneMusicByIndex(1, skipFade: true);

		// Unload the ManagerScene (but keep DontDestroyOnLoad objects)
		Scene managerScene = SceneManager.GetSceneByName("ManagerScene");
		if (managerScene.IsValid() && managerScene.isLoaded) {
			// Only unload if there are other scenes loaded
			if (SceneManager.sceneCount > 1) {
				AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(managerScene);
				yield return unloadOp;

				// Force another lighting update after manager scene is unloaded
				DynamicGI.UpdateEnvironment();
			}
		}

		// Always refresh components from the active gameplay scene
		enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
		gameHUD = GameHUD.Instance;


		// Update HUD with current wave
		if (gameHUD != null) {
			gameHUD.UpdateWaveDisplay(currentWave, maxWaves);
		}

		StartCoroutine(WaveSystemLoop());
	}

	IEnumerator WaveSystemLoop()
	{
		while (maxWaves == -1 || currentWave <= maxWaves) {
			if (GameStateManager.Instance != null && !GameStateManager.Instance.IsGameActive()) {
				yield return new WaitForSeconds(0.5f);
				continue;
			}

			// Wait for at least one tower to be placed before starting waves
			yield return StartCoroutine(WaitForFirstTower());

			yield return StartCoroutine(StartWave());
			yield return StartCoroutine(WaitForWaveCompletion());

			OnWaveCompleted?.Invoke(currentWave);

			// Handle scene change every 5 waves
			yield return StartCoroutine(CheckAndChangeScene());

			// Evaluate defeat right after the wave (and after potential scene/cost/UI updates)
			CheckDefeatPostWave();

			if (maxWaves != -1 && currentWave >= maxWaves) {
				OnAllWavesCompleted?.Invoke();

				if (GameStateManager.Instance != null) {
					GameStateManager.Instance.ShowVictory(totalEnemiesDefeated);
				}
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
		while (true) {
			BaseTower[] towers = FindObjectsOfType<BaseTower>();
			if (towers != null && towers.Length > 0) {
				// Check if any tower exists and has health > 0
				foreach (BaseTower tower in towers) {
					if (tower != null && tower.GetCurrentHealth() > 0) {
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

		// CRITICAL: Refresh enemySpawner reference from current active scene
		enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
		if (enemySpawner == null) {
			Debug.LogError($"WaveManager: No EnemySpawner found in scene for wave {currentWave}!");
		}

		// Calculate total enemies for this wave
		totalEnemiesThisWave = CalculateEnemiesForWave(currentWave);

		// Update HUD
		if (gameHUD != null) {
			gameHUD.UpdateWaveDisplay(currentWave, maxWaves);
		}

		// Start monitoring gold drain while no towers (during the wave)
		if (enableNoTowerGoldDrain) {
			if (noTowersDrainMonitor != null) {
				StopCoroutine(noTowersDrainMonitor);
				noTowersDrainMonitor = null;
			}
			noTowersDrainMonitor = StartCoroutine(MonitorNoTowersDrain());
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
		if (useBatchSpawning) {
			yield return StartCoroutine(SpawnEnemiesInBatches());
		}
		else {
			yield return StartCoroutine(SpawnEnemiesOneByOne());
		}
	}

	IEnumerator SpawnEnemiesInBatches()
	{
		// Ensure we have a valid spawner reference
		if (enemySpawner == null) {
			enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
			if (enemySpawner == null) {
				yield break;
			}
		}

		int batchSize = CalculateBatchSize();
		int enemiesSpawned = 0;
		int batchNumber = 1;

		while (enemiesSpawned < totalEnemiesThisWave) {
			// Calculate how many enemies to spawn in this batch
			int enemiesInThisBatch = Mathf.Min(batchSize, totalEnemiesThisWave - enemiesSpawned);

			// Spawn all enemies in this batch
			for (int i = 0; i < enemiesInThisBatch; i++) {
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
			if (enemiesSpawned < totalEnemiesThisWave) {
				yield return new WaitForSeconds(timeBetweenBatches);
			}
		}
	}

	IEnumerator SpawnEnemiesOneByOne()
	{
		// Ensure we have a valid spawner reference
		if (enemySpawner == null) {
			enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
			if (enemySpawner == null) {
				yield break;
			}
		}

		while (enemiesSpawnedThisWave < totalEnemiesThisWave) {
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
		while (isWaveActive && enemiesKilledThisWave < totalEnemiesThisWave) {
			// Immediate defeat if no towers and cannot afford any tower
			if (ShouldDefeatNow()) {
				isWaveActive = false;
				StopNoTowerDrain();
				GameStateManager.Instance?.ShowDefeat();
				yield break;
			}

			// Existing tracking
			CheckEnemyDeaths();
			yield return new WaitForSeconds(0.1f);
		}

		isWaveActive = false;

		// Stop no-tower drain monitors when the wave ends normally
		StopNoTowerDrain();

		if (currentWaveCoroutine != null) {
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
		if (deadEnemies > enemiesKilledThisWave) {
			int newKills = deadEnemies - enemiesKilledThisWave;
			enemiesKilledThisWave = deadEnemies;
			totalEnemiesDefeated += newKills; // Add to total count
		}
	}

	// Helper method to calculate scene-relative wave number
	private int GetSceneRelativeWaveNumber()
	{
		// Calculate wave number within current scene (1-5)
		// Wave 1, 6, 11, 16, etc. all return 1 (first wave of scene)
		// Wave 5, 10, 15, 20, etc. all return 5 (last wave of scene)
		return ((currentWave - 1) % 5) + 1;
	}

	private int CalculateEnemiesForWave(int wave)
	{
		// Use scene-relative wave so each scene starts easier
		int relativeWave = GetSceneRelativeWaveNumber();
		
		// Calculate which scene we're in (0-based: 0 = waves 1-5, 1 = waves 6-10, etc.)
		int sceneNumber = (wave - 1) / 5;
		
		// Base enemy count scaling within scene (waves 1-5 within each scene)
		float baseCount = baseEnemiesPerWave;
		float scaledCount = baseCount * Mathf.Pow(enemyCountMultiplier, relativeWave - 1);
		
		// Progressive difficulty bonus: each new scene is 30% harder than previous
		float sceneDifficultyMultiplier = 1.0f + (sceneNumber * 0.35f);
		
		// Global reduction: make every scene spawn ~35% fewer enemies than before
		sceneDifficultyMultiplier *= 0.70f;
		
		// Add some randomness (±20%)
		float randomFactor = Random.Range(0.8f, 1.2f);

		return Mathf.RoundToInt(scaledCount * sceneDifficultyMultiplier * randomFactor);
	}

	private int CalculateBatchSize()
	{
		// Use scene-relative wave for batch size calculation
		int relativeWave = GetSceneRelativeWaveNumber();
		
		// Batch size increases with wave difficulty within scene
		float scaledBatchSize = baseBatchSize * Mathf.Pow(batchSizeMultiplier, relativeWave - 1);

		// Add some randomness (±15%)
		float randomFactor = Random.Range(0.85f, 1.15f);

		return Mathf.Max(1, Mathf.RoundToInt(scaledBatchSize * randomFactor));
	}

	private GameObject DetermineEnemyType()
	{
		if (enemySpawner == null) {
			return null;
		}

		// Check if any prefabs are assigned
		if (enemySpawner.zombiePrefab == null && enemySpawner.ghostPrefab == null &&
			enemySpawner.skeletonPrefab == null && enemySpawner.mutantZombiePrefab == null &&
			enemySpawner.necromancerPrefab == null) {
			return null;
		}

		// Check if this is a random boss wave (only after wave 7)
		bool isRandomBossWave = enableRandomBossWaves && currentWave >= 8 && Random.value < bossWaveChance;

		// Boss wave logic - includes necromancers and mutant zombies
		if (isRandomBossWave) {
			float bossRoll = Random.value;
			// Boss waves: 9% mutant zombies (if unlocked), 30% skeletons, 22% ghosts, 9% necromancers (if unlocked), 30% zombies
			if (currentWave >= mutantZombieUnlockWave && bossRoll < 0.09f && enemySpawner.mutantZombiePrefab != null)
				return enemySpawner.mutantZombiePrefab;
			else if (bossRoll < 0.39f && enemySpawner.skeletonPrefab != null)
				return enemySpawner.skeletonPrefab;
			else if (bossRoll < 0.61f && enemySpawner.ghostPrefab != null)
				return enemySpawner.ghostPrefab;
			else if (currentWave >= necromancerUnlockWave && bossRoll < 0.70f && enemySpawner.necromancerPrefab != null)
				return enemySpawner.necromancerPrefab;
			else
				return enemySpawner.zombiePrefab;
		}

		// Normal wave logic
		float roll = Random.value;

		// Wave 1-3: Basic enemies only (skeletons always enabled)
		if (currentWave < necromancerUnlockWave) {
			if (enemySpawner.skeletonPrefab != null) {
				if (roll < 0.33f)
					return enemySpawner.zombiePrefab;
				else if (roll < 0.66f)
					return enemySpawner.ghostPrefab;
				else
					return enemySpawner.skeletonPrefab;
			}
			else {
				// Fallback: 50/50 zombies and ghosts
				return roll < 0.5f ? enemySpawner.zombiePrefab : enemySpawner.ghostPrefab;
			}
		}
		// Wave 4-6: Necromancers unlock (before mutant zombies)
		else if (currentWave < mutantZombieUnlockWave) {
			if (currentWave >= necromancerUnlockWave && enemySpawner.necromancerPrefab != null) {
				// Wave 4-6: 32% skeletons, 32% zombies, 27% ghosts, 9% necromancers
				if (enemySpawner.skeletonPrefab != null) {
					if (roll < 0.32f)
						return enemySpawner.skeletonPrefab;
					else if (roll < 0.64f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.91f)
						return enemySpawner.ghostPrefab;
					else
						return enemySpawner.necromancerPrefab;
				}
				else {
					// Fallback: 45% zombies, 46% ghosts, 9% necromancers
					if (roll < 0.45f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.91f)
						return enemySpawner.ghostPrefab;
					else
						return enemySpawner.necromancerPrefab;
				}
			}
			else {
				// Fallback if necromancer prefab not available
				if (enemySpawner.skeletonPrefab != null) {
					if (roll < 0.33f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.66f)
						return enemySpawner.ghostPrefab;
					else
						return enemySpawner.skeletonPrefab;
				}
				else {
					return roll < 0.5f ? enemySpawner.zombiePrefab : enemySpawner.ghostPrefab;
				}
			}
		}
		// Wave 7+: Both necromancers and mutant zombies
		else {
			if (currentWave >= necromancerUnlockWave && enemySpawner.necromancerPrefab != null) {
				// Wave 7+: 31% skeletons, 31% zombies, 20% ghosts, 9% necromancers, 9% mutant zombies
				if (enemySpawner.skeletonPrefab != null && enemySpawner.mutantZombiePrefab != null) {
					if (roll < 0.31f)
						return enemySpawner.skeletonPrefab;
					else if (roll < 0.62f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.82f)
						return enemySpawner.ghostPrefab;
					else if (roll < 0.91f)
						return enemySpawner.necromancerPrefab;
					else
						return enemySpawner.mutantZombiePrefab;
				}
				else if (enemySpawner.mutantZombiePrefab != null) {
					// Fallback if skeleton prefab is missing but mutants are available
					if (roll < 0.5f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.9f)
						return enemySpawner.ghostPrefab;
					else
						return enemySpawner.mutantZombiePrefab;
				}
				else {
					// Mutant zombies not available but necromancers are
					// 32% skeletons, 32% zombies, 27% ghosts, 9% necromancers
					if (enemySpawner.skeletonPrefab != null) {
						if (roll < 0.32f)
							return enemySpawner.skeletonPrefab;
						else if (roll < 0.64f)
							return enemySpawner.zombiePrefab;
						else if (roll < 0.91f)
							return enemySpawner.ghostPrefab;
						else
							return enemySpawner.necromancerPrefab;
					}
					else {
						// Fallback without skeleton prefab
						if (roll < 0.5f)
							return enemySpawner.zombiePrefab;
						else
							return enemySpawner.ghostPrefab;
					}
				}
			}
			else {
				// Fallback if necromancer prefab not available
				if (enemySpawner.skeletonPrefab != null && enemySpawner.mutantZombiePrefab != null) {
					// 35% each basic + 5% mutant zombies
					if (roll < 0.35f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.7f)
						return enemySpawner.ghostPrefab;
					else if (roll < 0.95f)
						return enemySpawner.skeletonPrefab;
					else
						return enemySpawner.mutantZombiePrefab;
				}
				else if (enemySpawner.mutantZombiePrefab != null) {
					if (roll < 0.475f)
						return enemySpawner.zombiePrefab;
					else if (roll < 0.95f)
						return enemySpawner.ghostPrefab;
					else
						return enemySpawner.mutantZombiePrefab;
				}
				else {
					// Fallback to basic enemies
					if (enemySpawner.skeletonPrefab != null) {
						if (roll < 0.33f)
							return enemySpawner.zombiePrefab;
						else if (roll < 0.66f)
							return enemySpawner.ghostPrefab;
						else
							return enemySpawner.skeletonPrefab;
					}
					else {
						return roll < 0.5f ? enemySpawner.zombiePrefab : enemySpawner.ghostPrefab;
					}
				}
			}
		}
	}

	private void SpawnEnemy(GameObject enemyPrefab)
	{
		if (enemyPrefab == null) {
			return;
		}

		if (enemySpawner == null) {
			enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
			if (enemySpawner == null) {
				return;
			}
		}

		if (enemySpawner.spawnPoints == null || enemySpawner.spawnPoints.Length == 0) {
			return;
		}

		// Use the new portal spawn method instead of direct instantiation
		enemySpawner.SpawnEnemyWithPortal(enemyPrefab);
	}

	private float CalculateSpawnDelay()
	{
		// Spawn faster in later waves for more intensity
		float baseDelay = 1.5f;
		float waveFactor = Mathf.Max(0.3f, 1f - (currentWave * 0.05f)); // Minimum 0.3s delay
		return baseDelay * waveFactor;
	}


	private IEnumerator CheckAndChangeScene()
	{
		bool condition = currentWave > 1 && (currentWave) % 5 == 0;

		if (condition && gameplaySceneNames != null && gameplaySceneNames.Length > 1 && currentSceneIndex < gameplaySceneNames.Length) {
			// CRITICAL: Stop all spawning immediately BEFORE fade
			if (currentWaveCoroutine != null)
			{
				StopCoroutine(currentWaveCoroutine);
				currentWaveCoroutine = null;
			}
			
			// Stop wave to prevent any further spawning
			isWaveActive = false;
			
			// Disable the current EnemySpawner to prevent any spawns
			if (enemySpawner != null)
			{
				enemySpawner.enabled = false;
			}
			
			// Also find and disable ALL enemy spawners in all scenes
			EnemySpawner[] allSpawners = FindObjectsOfType<EnemySpawner>();
			foreach (EnemySpawner spawner in allSpawners)
			{
				if (spawner != null)
				{
					spawner.enabled = false;
				}
			}
			
			// CRITICAL: Clear towers FIRST to prevent them from spawning enemies
			if (TowerPlacementManager.Instance != null)
			{
				TowerPlacementManager.Instance.ClearPlacedTowers();
			}
			
			// Also manually destroy any remaining tower objects
			BaseTower[] allTowers = FindObjectsOfType<BaseTower>();
			foreach (BaseTower tower in allTowers)
			{
				if (tower != null && tower.gameObject != null)
				{
					DestroyImmediate(tower.gameObject);
				}
			}
			
			// Start continuously destroying enemies during fade
			Coroutine enemyCleanup = StartCoroutine(ContinuouslyDestroyEnemies());
			
			// Fade to black before transition
			if (ScreenFader.Instance != null)
			{
				yield return StartCoroutine(ScreenFader.Instance.FadeOut(0.3f));
			}
			
			// Stop the continuous cleanup
			if (enemyCleanup != null)
			{
				StopCoroutine(enemyCleanup);
			}
			
			// Clear all remaining enemies AGGRESSIVELY using IMMEDIATE destruction
			// Collect all enemies first to avoid modification during iteration
			System.Collections.Generic.List<GameObject> enemiesToDestroy = new System.Collections.Generic.List<GameObject>();
			
			// Method 1: By tag
			GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (GameObject enemy in enemies)
			{
				if (enemy != null && !enemiesToDestroy.Contains(enemy))
				{
					enemiesToDestroy.Add(enemy);
				}
			}
			
			// Method 2: By component (catches enemies without tag)
			Enemy[] enemyComponents = FindObjectsOfType<Enemy>();
			foreach (Enemy enemy in enemyComponents)
			{
				if (enemy != null && enemy.gameObject != null && !enemiesToDestroy.Contains(enemy.gameObject))
				{
					enemiesToDestroy.Add(enemy.gameObject);
				}
			}
			
			// Method 3: Check all objects in all scenes including DontDestroyOnLoad
			for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
			{
				UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
				GameObject[] rootObjects = scene.GetRootGameObjects();
				foreach (GameObject obj in rootObjects)
				{
					if (obj != null && (obj.CompareTag("Enemy") || obj.GetComponent<Enemy>() != null))
					{
						if (!enemiesToDestroy.Contains(obj))
						{
							enemiesToDestroy.Add(obj);
						}
					}
				}
			}
			
			// Now destroy all collected enemies IMMEDIATELY (not deferred)
			foreach (GameObject enemy in enemiesToDestroy)
			{
				if (enemy != null)
				{
					DestroyImmediate(enemy);
				}
			}
			
			// Force garbage collection to clean up
			System.GC.Collect();
			
			// Wait a frame to ensure cleanup is complete
			yield return null;
			
			if (TowerPlacementManager.Instance != null)
				TowerPlacementManager.Instance.ClearPlacedTowers();

		string currentScene = gameplaySceneNames[currentSceneIndex];
		currentSceneIndex++;
		if (currentSceneIndex >= gameplaySceneNames.Length)
		{
			currentSceneIndex = gameplaySceneNames.Length - 1;
		}
		string nextScene = gameplaySceneNames[currentSceneIndex];

			// Switch background music for this scene (index 1-4 for gameplay scenes)
			// Use instant switch (no fade) during scene changes for immediate music response
			if (AudioManager.Instance != null)
				AudioManager.Instance.PlaySceneMusicByIndex(currentSceneIndex + 1, skipFade: true);

			// Update costs before load (keeps internal state in sync)
			ApplyTowerCostMultiplier();

			var loadOperation = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
			loadOperation.allowSceneActivation = false;
			while (loadOperation.progress < 0.9f)
				yield return null;

			loadOperation.allowSceneActivation = true;
			while (!loadOperation.isDone)
				yield return null;

			Scene currentActiveScene = SceneManager.GetActiveScene();
			if (currentActiveScene.IsValid()) {
				GameObject[] oldSceneObjects = currentActiveScene.GetRootGameObjects();
				foreach (GameObject obj in oldSceneObjects) {
					AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
					foreach (AudioListener listener in listeners) {
						listener.enabled = false;
					}
				}
			}

			Scene newActiveScene = SceneManager.GetSceneByName(nextScene);
			if (newActiveScene.IsValid()) {
				SceneManager.SetActiveScene(newActiveScene);
				DynamicGI.UpdateEnvironment();
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
			}
			
			// CRITICAL: Clean up any NavMesh objects from DontDestroyOnLoad after scene change
			CleanupPersistentNavMeshObjects();

			yield return StartCoroutine(RefreshTerrains());

			Scene sceneToUnload = SceneManager.GetSceneByName(currentScene);
			if (sceneToUnload.IsValid() && sceneToUnload.isLoaded) {
				var unloadOperation = SceneManager.UnloadSceneAsync(currentScene);
				while (!unloadOperation.isDone)
					yield return null;

				System.GC.Collect();
				DynamicGI.UpdateEnvironment();
				yield return StartCoroutine(RefreshTerrains());
			}

			enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();

			// Let the scene settle a moment, then re-apply costs and refresh UI
			yield return new WaitForSeconds(0.2f);
			ApplyTowerCostMultiplier(true);

			if (gameHUD != null) {
				StartCoroutine(ShowTowerCostNotification());
			}

			// Start continuous enemy cleanup during fade in
			Coroutine fadeInCleanup = StartCoroutine(ContinuouslyDestroyEnemies());

			// Fade in from black after scene is loaded
			if (ScreenFader.Instance != null)
			{
				yield return StartCoroutine(ScreenFader.Instance.FadeIn(0.5f));
			}

			// Stop continuous cleanup
			if (fadeInCleanup != null)
			{
				StopCoroutine(fadeInCleanup);
			}

			// Final cleanup pass after fade completes
			GameObject[] finalEnemies = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (GameObject enemy in finalEnemies)
			{
				if (enemy != null)
				{
					DestroyImmediate(enemy);
				}
			}

			// Also evaluate defeat at scene-change points
			CheckDefeatPostWave();
		}
	}

	private bool ShouldDefeatNow()
	{
		// Any towers alive?
		BaseTower[] towers = FindObjectsOfType<BaseTower>();
		foreach (var t in towers) {
			if (t != null && t.GetCurrentHealth() > 0f) {
				return false;
			}
		}

		// None alive: can we afford any tower?
		var tpm = TowerPlacementManager.Instance;
		if (tpm == null) return true;

		var available = tpm.GetAvailableTowers();
		if (available == null || available.Count == 0) return true;

		int minCost = int.MaxValue;
		foreach (var td in available) {
			if (td != null && td.cost >= 0 && td.cost < minCost) {
				minCost = td.cost;
			}
		}
		if (minCost == int.MaxValue) return true;

		int currency = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrentCurrency() : 0;
		return currency < minCost;
	}

	private void CheckDefeatPostWave()
	{
		// 1) Any towers alive?
		BaseTower[] towers = FindObjectsOfType<BaseTower>();
		bool anyAlive = false;
		foreach (var t in towers) {
			if (t != null && t.GetCurrentHealth() > 0f) {
				anyAlive = true;
				break;
			}
		}
		if (anyAlive) return;

		// 2) If none alive, can the player afford at least one tower?
		var tpm = TowerPlacementManager.Instance;
		if (tpm == null) return;

		var available = tpm.GetAvailableTowers();
		if (available == null || available.Count == 0) return;

		int minCost = int.MaxValue;
		foreach (var td in available) {
			if (td != null && td.cost >= 0 && td.cost < minCost) {
				minCost = td.cost;
			}
		}
		if (minCost == int.MaxValue) return;

		int currency = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrentCurrency() : 0;

		if (currency < minCost) {
			// Defeat: no towers left and cannot afford any tower
			if (GameStateManager.Instance != null) {
				GameStateManager.Instance.ShowDefeat();
			}
		}
	}

	// Modify this method to have a force refresh parameter
	private void ApplyTowerCostMultiplier(bool forceRefresh = false)
	{
		if (TowerPlacementManager.Instance == null) return;

		// Store original costs if we haven't already
		if (!hasStoredOriginalCosts) {
			StoreOriginalTowerCosts();
			hasStoredOriginalCosts = true;
		}

		// Calculate multiplier: 1.5^(currentSceneIndex) for gradual scaling
		float multiplier = Mathf.Pow(1.5f, currentSceneIndex);

		// Apply multiplier to each tower
		var towers = TowerPlacementManager.Instance.GetAvailableTowers();
		foreach (var tower in towers) {
			if (originalTowerCosts.TryGetValue(tower.towerName, out int originalCost)) {
				tower.cost = Mathf.RoundToInt(originalCost * multiplier);
			}
		}

		// Force direct UI update if after scene change
		if (forceRefresh && gameHUD != null) {
			// First clear and recreate all buttons
			gameHUD.InitializeTowerButtons();

			// Then wait a frame and force update the button costs
			StartCoroutine(ForceUpdateTowerButtons());
		}
		// Regular UI refresh
		else if (GameHUD.Instance != null) {
			GameHUD.Instance.InitializeTowerButtons();
		}
	}

	// Add this method to force update the tower buttons
	private IEnumerator ForceUpdateTowerButtons()
	{
		// Wait a frame to ensure buttons are created
		yield return null;

		// Get the tower button container
		if (GameHUD.Instance?.GetTowerButtonContainer() == null) yield break;

		// Find all tower buttons
		TowerButton[] buttons = GameHUD.Instance.GetTowerButtonContainer().GetComponentsInChildren<TowerButton>();

		// Update each button with the current cost from available towers
		var towers = TowerPlacementManager.Instance.GetAvailableTowers();
		foreach (var tower in towers) {
			foreach (var button in buttons) {
				if (button.GetTowerData()?.towerName == tower.towerName) {
					// Force button to update with new cost
					button.UpdateCost(tower.cost);
				}
			}
		}
	}

	private void StoreOriginalTowerCosts()
	{
		if (TowerPlacementManager.Instance == null) return;

		originalTowerCosts.Clear();
		var towers = TowerPlacementManager.Instance.GetAvailableTowers();

		foreach (var tower in towers) {
			originalTowerCosts[tower.towerName] = tower.cost;
		}
	}

	private IEnumerator ShowTowerCostNotification()
	{
		if (gameHUD?.placementText == null) yield break;

		// Calculate the current multiplier (matches ApplyTowerCostMultiplier logic)
		int multiplier = 1;
		for (int i = 0; i < currentSceneIndex; i++) {
			multiplier *= 2;
		}

		string originalText = gameHUD.placementText.text;
		gameHUD.placementText.text = $"New scene! Tower costs increased to {multiplier}x";

		yield return new WaitForSeconds(3f);

		// Only restore original text if it hasn't been changed by something else
		if (gameHUD.placementText.text.Contains("Tower costs increased")) {
			gameHUD.placementText.text = originalText;
		}
	}

	private IEnumerator RefreshTerrains()
	{
		yield return new WaitForSeconds(0.1f); // Give Unity a moment to initialize

		// Find all terrain objects in the active scene only
		Scene activeScene = SceneManager.GetActiveScene();
		if (!activeScene.IsValid()) yield break;

		Terrain[] terrains = FindObjectsOfType<Terrain>();

		foreach (var terrain in terrains) {
			if (terrain != null && terrain.gameObject.scene == activeScene) {
				// Force terrain data to refresh
				terrain.Flush();

				// Force redraw with double-buffering technique
				terrain.enabled = false;
				yield return null; // Wait one frame
				terrain.enabled = true;
				yield return null; // Wait another frame

				// REMOVED SyncHeightmap() - can cause hangs
				// The terrain should already be correct from the scene load
			}
		}

		// Final frame wait to ensure all updates are applied
		yield return new WaitForEndOfFrame();
	}

	// ===== Enemy cleanup helper =====
	
	private IEnumerator ContinuouslyDestroyEnemies()
	{
		// Continuously destroy enemies every frame until stopped
		while (true)
		{
			// Find and destroy all enemies
			GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
			foreach (GameObject enemy in enemies)
			{
				if (enemy != null)
				{
					DestroyImmediate(enemy);
				}
			}
			
			// Also destroy by component
			Enemy[] enemyComponents = FindObjectsOfType<Enemy>();
			foreach (Enemy enemy in enemyComponents)
			{
				if (enemy != null && enemy.gameObject != null)
				{
					DestroyImmediate(enemy.gameObject);
				}
			}
			
			yield return null; // Wait one frame and repeat
		}
	}

	// ===== No-tower gold drain helpers =====

	private IEnumerator MonitorNoTowersDrain()
	{
		while (isWaveActive) {
			if (!AnyTowerAlive()) {
				if (noTowersDrainCoroutine == null) {
					noTowersDrainCoroutine = StartCoroutine(DrainGoldWhileNoTowers());
				}
			}
			else {
				if (noTowersDrainCoroutine != null) {
					StopCoroutine(noTowersDrainCoroutine);
					noTowersDrainCoroutine = null;
				}
			}
			yield return new WaitForSeconds(0.2f);
		}

		// Safety stop
		if (noTowersDrainCoroutine != null) {
			StopCoroutine(noTowersDrainCoroutine);
			noTowersDrainCoroutine = null;
		}
	}

	private IEnumerator DrainGoldWhileNoTowers()
	{
		while (isWaveActive && !AnyTowerAlive()) {
			if (CurrencyManager.Instance != null) {
				int current = CurrencyManager.Instance.GetCurrentCurrency();
				int toSpend = Mathf.Min(goldDrainPerSecondWhenNoTowers, Mathf.Max(current, 0));
				if (toSpend > 0) {
					CurrencyManager.Instance.SpendCurrency(toSpend);
					if (GameHUD.Instance != null) {
						GameHUD.Instance.ShowCurrencyChange(-toSpend);
					}
				}
			}
			yield return new WaitForSeconds(goldDrainTickSeconds);
		}
	}

	private bool AnyTowerAlive()
	{
		BaseTower[] towers = FindObjectsOfType<BaseTower>();
		foreach (var t in towers) {
			if (t != null && t.GetCurrentHealth() > 0f) {
				return true;
			}
		}
		return false;
	}

	private void StopNoTowerDrain()
	{
		if (noTowersDrainMonitor != null) {
			StopCoroutine(noTowersDrainMonitor);
			noTowersDrainMonitor = null;
		}
		if (noTowersDrainCoroutine != null) {
			StopCoroutine(noTowersDrainCoroutine);
			noTowersDrainCoroutine = null;
		}
	}
}