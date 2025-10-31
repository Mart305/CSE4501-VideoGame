using UnityEngine;
using System.Collections.Generic;

/// Generic object pooling system for improved performance.
/// Manages pools of reusable GameObjects to avoid constant instantiation/destruction.
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [Header("Pool Configuration")]
    [SerializeField] private List<Pool> pools = new List<Pool>();
    
    [Header("Settings")]
    [SerializeField] private bool expandPools = true; // Allow pools to grow if needed
    [SerializeField] private Transform poolParent; // Optional parent for organization

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> prefabDictionary;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();

        // Create pool parent if not assigned
        if (poolParent == null)
        {
            GameObject parentObj = new GameObject("Object Pools");
            poolParent = parentObj.transform;
            poolParent.SetParent(transform);
        }

        // Initialize each pool
        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                continue;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();
            prefabDictionary[pool.tag] = pool.prefab;

            // Create pool container for organization
            GameObject poolContainer = new GameObject($"Pool - {pool.tag}");
            poolContainer.transform.SetParent(poolParent);

            // Pre-instantiate objects
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, poolContainer.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// Spawn an object from the pool
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return null;
        }

        GameObject objectToSpawn;

        // If pool is empty and expansion is allowed, create new object
        if (poolDictionary[tag].Count == 0)
        {
            if (expandPools)
            {
                GameObject newObj = Instantiate(prefabDictionary[tag]);
                newObj.name = prefabDictionary[tag].name;
                objectToSpawn = newObj;
            }
            else
            {
                return null;
            }
        }
        else
        {
            objectToSpawn = poolDictionary[tag].Dequeue();
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Call OnObjectSpawn on IPooledObject interface if it exists
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    /// Return an object to the pool
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
    }

    /// Return an object to pool after a delay
    public void ReturnToPool(string tag, GameObject obj, float delay)
    {
        StartCoroutine(ReturnToPoolAfterDelay(tag, obj, delay));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDelay(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(tag, obj);
    }

    /// Clear all pools (useful for scene transitions)
    public void ClearAllPools()
    {
        foreach (var pool in poolDictionary.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        poolDictionary.Clear();
        prefabDictionary.Clear();
    }

    /// Get the current size of a pool
    public int GetPoolSize(string tag)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            return poolDictionary[tag].Count;
        }
        return 0;
    }

    /// Add a new pool at runtime
    public void AddPool(string tag, GameObject prefab, int size)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            return;
        }

        Queue<GameObject> objectPool = new Queue<GameObject>();
        prefabDictionary[tag] = prefab;

        GameObject poolContainer = new GameObject($"Pool - {tag}");
        poolContainer.transform.SetParent(poolParent);

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, poolContainer.transform);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(tag, objectPool);
    }
}

/// Interface for objects that need to reset when spawned from pool
public interface IPooledObject
{
    void OnObjectSpawn();
}
