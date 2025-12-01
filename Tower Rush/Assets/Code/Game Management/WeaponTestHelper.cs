using UnityEngine;

/// <summary>
/// Quick test helper for iterating on weapon feel and player movement
/// Attach to an object in a test scene for quick debugging and tuning
/// </summary>
public class WeaponTestHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWeapon playerWeapon;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private GameObject playerObject;

    [Header("Test Controls")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool freezeTime = false;
    [SerializeField] private float timeScale = 1f;

    [Header("Spawn Test Targets")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private int targetCount = 5;
    [SerializeField] private float targetSpawnRadius = 10f;
    [SerializeField] private KeyCode spawnTargetsKey = KeyCode.T;

    [Header("Wall Test")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private KeyCode spawnWallKey = KeyCode.Y;
    [SerializeField] private float wallDistance = 3f;

    private GameObject[] spawnedTargets;
    private GameObject spawnedWall;

    void Start()
    {
        // Auto-find references if not assigned
        if (playerWeapon == null)
            playerWeapon = FindObjectOfType<PlayerWeapon>();

        if (weaponManager == null)
            weaponManager = FindObjectOfType<WeaponManager>();

        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        HandleTimeControls();
        HandleTestSpawns();

        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    void HandleTimeControls()
    {
        if (freezeTime)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScale;
        }

        // Quick time scale controls
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            timeScale = Mathf.Max(0.1f, timeScale - 0.1f);
            Debug.Log($"Time Scale: {timeScale:F1}x");
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            timeScale = Mathf.Min(2f, timeScale + 0.1f);
            Debug.Log($"Time Scale: {timeScale:F1}x");
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            timeScale = 1f;
            Debug.Log("Time Scale: Reset to 1.0x");
        }
    }

    void HandleTestSpawns()
    {
        // Spawn test targets
        if (Input.GetKeyDown(spawnTargetsKey))
        {
            SpawnTestTargets();
        }

        // Spawn wall for clipping test
        if (Input.GetKeyDown(spawnWallKey))
        {
            SpawnTestWall();
        }

        // Clear spawned objects
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearTestObjects();
        }
    }

    void SpawnTestTargets()
    {
        ClearTestObjects();

        if (targetPrefab == null || playerObject == null)
        {
            Debug.LogWarning("Missing target prefab or player object");
            return;
        }

        spawnedTargets = new GameObject[targetCount];
        Vector3 playerPos = playerObject.transform.position;

        for (int i = 0; i < targetCount; i++)
        {
            float angle = (360f / targetCount) * i;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * targetSpawnRadius;
            Vector3 spawnPos = playerPos + offset;

            spawnedTargets[i] = Instantiate(targetPrefab, spawnPos, Quaternion.LookRotation(playerPos - spawnPos));
            spawnedTargets[i].name = $"TestTarget_{i + 1}";
        }

        Debug.Log($"Spawned {targetCount} test targets");
    }

    void SpawnTestWall()
    {
        if (spawnedWall != null)
        {
            Destroy(spawnedWall);
        }

        if (wallPrefab == null || playerObject == null)
        {
            // Create a simple wall
            spawnedWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedWall.transform.localScale = new Vector3(5, 3, 0.2f);
        }
        else
        {
            spawnedWall = Instantiate(wallPrefab);
        }

        Vector3 playerPos = playerObject.transform.position;
        Vector3 playerForward = playerObject.transform.forward;
        spawnedWall.transform.position = playerPos + playerForward * wallDistance;
        spawnedWall.transform.rotation = Quaternion.LookRotation(-playerForward);
        spawnedWall.name = "TestWall";

        Debug.Log("Spawned test wall for clipping test");
    }

    void ClearTestObjects()
    {
        if (spawnedTargets != null)
        {
            foreach (var target in spawnedTargets)
            {
                if (target != null)
                    Destroy(target);
            }
            spawnedTargets = null;
        }

        if (spawnedWall != null)
        {
            Destroy(spawnedWall);
            spawnedWall = null;
        }

        Debug.Log("Cleared test objects");
    }

    void DrawDebugInfo()
    {
        // This will be visible in the scene view
        if (playerWeapon != null)
        {
            Debug.DrawRay(playerWeapon.transform.position, playerWeapon.transform.forward * 2f, Color.yellow);
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Box("Weapon Test Helper");

        GUILayout.Label($"Time Scale: {timeScale:F1}x");
        GUILayout.Label($"FPS: {1f / Time.unscaledDeltaTime:F0}");

        if (playerWeapon != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Weapon: {playerWeapon.GetWeaponName()}");
            GUILayout.Label($"Ammo: {playerWeapon.GetCurrentAmmo()}/{playerWeapon.GetMaxAmmo()}");
            GUILayout.Label($"Reloading: {playerWeapon.IsReloading()}");
        }

        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label($"[{spawnTargetsKey}] - Spawn Targets");
        GUILayout.Label($"[{spawnWallKey}] - Spawn Wall");
        GUILayout.Label("[C] - Clear Objects");
        GUILayout.Label("[-/+] - Time Scale");
        GUILayout.Label("[Backspace] - Reset Time");

        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        ClearTestObjects();
    }
}
