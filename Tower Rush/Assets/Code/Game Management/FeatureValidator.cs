using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Validation script to verify all new features are properly set up
/// Add this to a GameObject in your scene and run to check configuration
/// </summary>
public class FeatureValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool logDetailedResults = true;

    private int errorCount = 0;
    private int warningCount = 0;
    private int successCount = 0;

    void Start()
    {
        if (validateOnStart)
        {
            ValidateAllFeatures();
        }
    }

    [ContextMenu("Validate All Features")]
    public void ValidateAllFeatures()
    {
        Debug.Log("=== FEATURE VALIDATION STARTED ===");
        errorCount = 0;
        warningCount = 0;
        successCount = 0;

        ValidateAudioManager();
        ValidateTerrainDecorator();
        ValidateWaveManager();
        ValidateBaseTower();

        Debug.Log("=== VALIDATION SUMMARY ===");
        Debug.Log($"✓ Successes: {successCount}");
        Debug.Log($"⚠ Warnings: {warningCount}");
        Debug.Log($"✗ Errors: {errorCount}");

        if (errorCount == 0 && warningCount == 0)
        {
            Debug.Log("<color=green>ALL FEATURES VALIDATED SUCCESSFULLY!</color>");
        }
        else if (errorCount == 0)
        {
            Debug.Log("<color=yellow>VALIDATION PASSED WITH WARNINGS</color>");
        }
        else
        {
            Debug.Log("<color=red>VALIDATION FAILED - PLEASE FIX ERRORS</color>");
        }
    }

    private void ValidateAudioManager()
    {
        Debug.Log("\n--- Validating AudioManager ---");

        // Check if AudioManager exists
        if (AudioManager.Instance == null)
        {
            LogError("AudioManager.Instance is NULL - AudioManager GameObject not found in scene");
            LogError("Solution: Create AudioManager GameObject in ManagerScene and add AudioManager script");
            return;
        }
        else
        {
            LogSuccess("AudioManager.Instance found");
        }

        // Check if AudioManager persists
        if (AudioManager.Instance.gameObject.scene.name == "DontDestroyOnLoad")
        {
            LogSuccess("AudioManager persists across scenes (DontDestroyOnLoad)");
        }
        else
        {
            LogWarning("AudioManager not yet moved to DontDestroyOnLoad (this is normal before game starts)");
        }

        // Validate through reflection (checking if audio sources exist)
        var musicSource = AudioManager.Instance.GetType().GetField("musicSource",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (musicSource != null)
        {
            var musicSourceValue = musicSource.GetValue(AudioManager.Instance) as AudioSource;
            if (musicSourceValue != null)
            {
                LogSuccess("AudioManager has music source configured");
            }
            else
            {
                LogWarning("Music source not initialized");
            }
        }
    }

    private void ValidateTerrainDecorator()
    {
        Debug.Log("\n--- Validating TerrainDecorator ---");

        TerrainDecorator decorator = FindObjectOfType<TerrainDecorator>();

        if (decorator == null)
        {
            LogWarning("TerrainDecorator not found in current scene");
            LogWarning("This is optional but recommended for visual enhancement");
            LogWarning("Solution: Add TerrainDecorator GameObject to your gameplay scenes");
            return;
        }
        else
        {
            LogSuccess("TerrainDecorator found in scene");
        }

        // Check if terrain exists
        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain == null)
        {
            LogError("No Terrain found in scene - TerrainDecorator requires a Terrain");
        }
        else
        {
            LogSuccess("Terrain found for decoration spawning");
        }
    }

    private void ValidateWaveManager()
    {
        Debug.Log("\n--- Validating WaveManager ---");

        if (WaveManager.Instance == null)
        {
            LogWarning("WaveManager.Instance is NULL - This is normal in non-gameplay scenes");
            return;
        }
        else
        {
            LogSuccess("WaveManager.Instance found");
        }

        // Check if RefreshTerrains method exists via reflection
        var refreshMethod = WaveManager.Instance.GetType().GetMethod("RefreshTerrains",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (refreshMethod != null)
        {
            LogSuccess("WaveManager.RefreshTerrains() method found");
        }
        else
        {
            LogError("WaveManager.RefreshTerrains() method not found - terrain fix not implemented");
        }

        // Check CheckAndChangeScene method
        var sceneChangeMethod = WaveManager.Instance.GetType().GetMethod("CheckAndChangeScene",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (sceneChangeMethod != null)
        {
            LogSuccess("WaveManager.CheckAndChangeScene() method found");
        }
        else
        {
            LogError("WaveManager.CheckAndChangeScene() method not found");
        }
    }

    private void ValidateBaseTower()
    {
        Debug.Log("\n--- Validating BaseTower ---");

        BaseTower[] towers = FindObjectsOfType<BaseTower>();

        if (towers.Length == 0)
        {
            LogWarning("No BaseTower instances found in scene");
            LogWarning("This is normal before towers are placed");
            return;
        }
        else
        {
            LogSuccess($"Found {towers.Length} tower(s) in scene");
        }

        // Check if PerformAttack has AudioManager integration via reflection
        var performAttackMethod = typeof(BaseTower).GetMethod("PerformAttack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (performAttackMethod != null)
        {
            LogSuccess("BaseTower.PerformAttack() method found");

            // We can't easily check the method body, but we can verify the method exists
            if (logDetailedResults)
            {
                Debug.Log("Note: Cannot automatically verify AudioManager integration in PerformAttack");
                Debug.Log("Manual verification: Check BaseTower.cs:214-224 for AudioManager.PlayTowerShootSound() call");
            }
        }
        else
        {
            LogError("BaseTower.PerformAttack() method not found");
        }
    }

    private void LogSuccess(string message)
    {
        successCount++;
        if (logDetailedResults)
        {
            Debug.Log($"<color=green>✓</color> {message}");
        }
    }

    private void LogWarning(string message)
    {
        warningCount++;
        Debug.LogWarning($"⚠ {message}");
    }

    private void LogError(string message)
    {
        errorCount++;
        Debug.LogError($"✗ {message}");
    }

    // Runtime tests
    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        Debug.Log("=== TESTING AUDIO SYSTEM ===");

        if (AudioManager.Instance == null)
        {
            Debug.LogError("Cannot test - AudioManager not found");
            return;
        }

        // Test playing a sound
        Debug.Log("Attempting to play tower shoot sound...");
        AudioManager.Instance.PlayTowerShootSound("FireTower", transform.position);

        Debug.Log("If you heard a sound, the audio system is working!");
        Debug.Log("If not, check that audio clips are assigned in AudioManager Inspector");
    }

    [ContextMenu("Test Terrain Refresh")]
    public void TestTerrainRefresh()
    {
        Debug.Log("=== TESTING TERRAIN REFRESH ===");

        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No terrain found in scene");
            return;
        }

        Debug.Log("Flushing terrain...");
        terrain.Flush();

        if (terrain.terrainData != null)
        {
            Debug.Log("Syncing terrain heightmap...");
            terrain.terrainData.SyncHeightmap();
        }

        Debug.Log("Terrain refresh test complete");
    }
}
