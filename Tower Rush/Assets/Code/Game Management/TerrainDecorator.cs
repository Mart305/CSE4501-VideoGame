using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns decorative objects (rocks, trees, bushes) on the terrain to enhance visual design
/// Add this script to your scene and configure the decoration prefabs in the Inspector
/// </summary>
public class TerrainDecorator : MonoBehaviour
{
    [Header("Decoration Prefabs")]
    [Tooltip("Array of rock prefabs to randomly place on terrain")]
    [SerializeField] private GameObject[] rockPrefabs;

    [Tooltip("Array of tree prefabs to randomly place on terrain")]
    [SerializeField] private GameObject[] treePrefabs;

    [Tooltip("Array of bush prefabs to randomly place on terrain")]
    [SerializeField] private GameObject[] bushPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Total number of decorative objects to spawn")]
    [SerializeField] private int totalObjectsToSpawn = 50;

    [Tooltip("Minimum distance between spawned objects")]
    [SerializeField] private float minSpacingBetweenObjects = 5f;

    [Tooltip("Random rotation variance for natural look")]
    [SerializeField] private bool randomizeRotation = true;

    [Tooltip("Random scale variance (e.g., 0.2 means 80% to 120% of original scale)")]
    [SerializeField] private float scaleVariance = 0.2f;

    [Header("Spawn Weights")]
    [Tooltip("Probability weight for spawning rocks (higher = more rocks)")]
    [SerializeField] private float rockSpawnWeight = 0.5f;

    [Tooltip("Probability weight for spawning trees")]
    [SerializeField] private float treeSpawnWeight = 0.3f;

    [Tooltip("Probability weight for spawning bushes")]
    [SerializeField] private float bushSpawnWeight = 0.2f;

    [Header("Terrain Bounds")]
    [Tooltip("Automatically detect terrain bounds, or manually set if unchecked")]
    [SerializeField] private bool autoDetectBounds = true;

    [Tooltip("Manual terrain bounds (only used if autoDetectBounds is false)")]
    [SerializeField] private Vector3 spawnAreaMin = new Vector3(-50, 0, -50);
    [SerializeField] private Vector3 spawnAreaMax = new Vector3(50, 100, 50);

    [Header("Exclusion Zones")]
    [Tooltip("Layer mask for areas where decorations should NOT spawn (e.g., paths, spawn points)")]
    [SerializeField] private LayerMask exclusionLayerMask;

    [Tooltip("Radius around exclusion objects to avoid")]
    [SerializeField] private float exclusionRadius = 10f;

    [Header("Parent Container")]
    [Tooltip("Parent object to organize spawned decorations (auto-created if null)")]
    [SerializeField] private Transform decorationParent;

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private Terrain terrain;

    void Start()
    {
        // Auto-create parent container if not assigned
        if (decorationParent == null)
        {
            GameObject parentObj = new GameObject("Terrain Decorations");
            decorationParent = parentObj.transform;
        }

        // Find terrain
        terrain = FindObjectOfType<Terrain>();

        // Spawn decorations
        SpawnDecorations();
    }

    private void SpawnDecorations()
    {
        if (rockPrefabs.Length == 0 && treePrefabs.Length == 0 && bushPrefabs.Length == 0)
        {
            return;
        }

        // Calculate total weight
        float totalWeight = rockSpawnWeight + treeSpawnWeight + bushSpawnWeight;

        int successfulSpawns = 0;
        int attempts = 0;
        int maxAttempts = totalObjectsToSpawn * 10; // Allow multiple attempts per object

        while (successfulSpawns < totalObjectsToSpawn && attempts < maxAttempts)
        {
            attempts++;

            // Generate random position
            Vector3 randomPosition = GetRandomTerrainPosition();

            // Check if position is valid
            if (!IsValidSpawnPosition(randomPosition))
                continue;

            // Determine which type of decoration to spawn based on weights
            float randomWeight = Random.value * totalWeight;
            GameObject prefabToSpawn = null;

            if (randomWeight < rockSpawnWeight && rockPrefabs.Length > 0)
            {
                prefabToSpawn = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            }
            else if (randomWeight < rockSpawnWeight + treeSpawnWeight && treePrefabs.Length > 0)
            {
                prefabToSpawn = treePrefabs[Random.Range(0, treePrefabs.Length)];
            }
            else if (bushPrefabs.Length > 0)
            {
                prefabToSpawn = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
            }

            if (prefabToSpawn == null)
                continue;

            // Spawn the decoration
            SpawnDecoration(prefabToSpawn, randomPosition);
            spawnedPositions.Add(randomPosition);
            successfulSpawns++;
        }
    }

    private Vector3 GetRandomTerrainPosition()
    {
        Vector3 position;

        if (autoDetectBounds && terrain != null)
        {
            // Use terrain bounds
            Bounds terrainBounds = terrain.terrainData.bounds;
            Vector3 terrainPos = terrain.transform.position;

            float randomX = Random.Range(terrainPos.x, terrainPos.x + terrainBounds.size.x);
            float randomZ = Random.Range(terrainPos.z, terrainPos.z + terrainBounds.size.z);

            // Raycast down to find terrain height
            float raycastHeight = terrainPos.y + terrainBounds.size.y + 100f;
            position = new Vector3(randomX, raycastHeight, randomZ);
        }
        else
        {
            // Use manual bounds
            float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float randomZ = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
            position = new Vector3(randomX, spawnAreaMax.y, randomZ);
        }

        // Raycast down to find ground
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, 1000f))
        {
            return hit.point;
        }

        return position;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if too close to other decorations
        foreach (Vector3 spawnedPos in spawnedPositions)
        {
            if (Vector3.Distance(position, spawnedPos) < minSpacingBetweenObjects)
            {
                return false;
            }
        }

        // Check exclusion zones (e.g., don't spawn on paths or near spawn points)
        Collider[] nearbyColliders = Physics.OverlapSphere(position, exclusionRadius, exclusionLayerMask);
        if (nearbyColliders.Length > 0)
        {
            return false;
        }

        return true;
    }

    private void SpawnDecoration(GameObject prefab, Vector3 position)
    {
        // Instantiate the decoration
        GameObject decoration = Instantiate(prefab, position, Quaternion.identity, decorationParent);

        // Apply random rotation
        if (randomizeRotation)
        {
            float randomYRotation = Random.Range(0f, 360f);
            decoration.transform.rotation = Quaternion.Euler(0, randomYRotation, 0);
        }

        // Apply random scale variance
        if (scaleVariance > 0)
        {
            float scaleMultiplier = Random.Range(1f - scaleVariance, 1f + scaleVariance);
            decoration.transform.localScale *= scaleMultiplier;
        }

        // Align with terrain normal for better placement
        AlignToTerrainNormal(decoration, position);
    }

    private void AlignToTerrainNormal(GameObject obj, Vector3 position)
    {
        // Raycast to get terrain normal
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 100f))
        {
            // Align object's up vector with terrain normal (slight alignment, not perfect)
            Vector3 targetUp = Vector3.Lerp(Vector3.up, hit.normal, 0.5f);
            obj.transform.up = targetUp;
        }
    }

    // Editor utility: Clear all spawned decorations
    public void ClearDecorations()
    {
        if (decorationParent != null)
        {
            foreach (Transform child in decorationParent)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        spawnedPositions.Clear();
    }

    // Visualize spawn area in editor
    void OnDrawGizmosSelected()
    {
        if (!autoDetectBounds)
        {
            Gizmos.color = Color.green;
            Vector3 center = (spawnAreaMin + spawnAreaMax) / 2f;
            Vector3 size = spawnAreaMax - spawnAreaMin;
            Gizmos.DrawWireCube(center, size);
        }

        // Draw spawned positions
        Gizmos.color = Color.yellow;
        foreach (Vector3 pos in spawnedPositions)
        {
            Gizmos.DrawWireSphere(pos, minSpacingBetweenObjects);
        }
    }
}
