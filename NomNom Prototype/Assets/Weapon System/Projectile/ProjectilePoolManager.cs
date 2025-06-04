using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent; // For thread-safe collections if needed, though Unity's main thread usage simplifies this.
                                     // For typical Unity usage, standard Dictionary/Queue are often fine if not accessed from other threads.
                                     // The documentation mentions ConcurrentDictionary, so we'll use it for adherence.

/// <summary>
/// Manages object pooling for Projectiles and other GameObjects.
/// This component should be placed on a GameObject in your scene.
/// It uses a Singleton pattern for easy access.
/// </summary>
public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Enable debug log messages from the Pool Manager.")]
    public bool enableDebugLogs = false;

    [Tooltip("Default initial size for new pools created for projectiles or GameObjects.")]
    public int defaultInitialPoolSize = 10;

    // Using ConcurrentDictionary as specified in the documentation.
    // Key: The original prefab GameObject used to create the instances.
    // Value: A queue of inactive instances of that prefab.
    private ConcurrentDictionary<GameObject, Queue<Projectile>> _projectilePool = new ConcurrentDictionary<GameObject, Queue<Projectile>>();

    // Key: The name of the original prefab GameObject (with "(Clone)" removed).
    // Value: A queue of inactive instances of that prefab.
    private ConcurrentDictionary<string, Queue<GameObject>> _gameObjectPool = new ConcurrentDictionary<string, Queue<GameObject>>();

    private Dictionary<GameObject, GameObject> _instanceToPrefabMap = new Dictionary<GameObject, GameObject>(); // Helper to map instance back to its projectile prefab

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if your pool manager needs to persist across scenes
        }
        else
        {
            Debug.LogWarning("ProjectilePoolManager already exists. Destroying duplicate.", this);
            Destroy(gameObject);
        }
    }

    #region Projectile Pooling

    /// <summary>
    /// Retrieves a Projectile instance from the pool.
    /// If the pool for the given prefab is empty or doesn't exist, it can optionally create a new instance.
    /// </summary>
    /// <param name="projectilePrefab">The prefab of the Projectile to retrieve.</param>
    /// <returns>An active Projectile instance, or null if pooling is disabled or instantiation fails.</returns>
    public Projectile GetProjectile(GameObject projectilePrefab)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("GetProjectile: projectilePrefab cannot be null.", this);
            return null;
        }

        Projectile projectileComponentOnPrefab = projectilePrefab.GetComponent<Projectile>();
        if (projectileComponentOnPrefab == null)
        {
            Debug.LogError($"GetProjectile: Prefab '{projectilePrefab.name}' does not have a Projectile component.", this);
            return null;
        }

        // Check if this projectile type is even configured to use pooling
        if (!projectileComponentOnPrefab.UseObjectPool)
        {
            if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] GetProjectile: '{projectilePrefab.name}' is not configured for pooling. Instantiating new.", this);
            Projectile newInstance = Instantiate(projectilePrefab).GetComponent<Projectile>();
            if (newInstance != null)
            {
                newInstance.OriginalPrefab = projectilePrefab; // Store original prefab for later
                _instanceToPrefabMap[newInstance.gameObject] = projectilePrefab;
            }
            return newInstance;
        }


        if (_projectilePool.TryGetValue(projectilePrefab, out Queue<Projectile> pool) && pool.Count > 0)
        {
            Projectile pooledProjectile = pool.Dequeue();
            if (pooledProjectile != null) // Should always be true if it was in the queue
            {
                if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] GetProjectile: Reusing '{projectilePrefab.name}' from pool. Pool size now: {pool.Count}", this);
                // Note: The Projectile's OnEnable will handle its internal reset and event calls (like OnGetFromPool)
                // pooledProjectile.gameObject.SetActive(true); // Projectile's OnEnable should handle this
                return pooledProjectile;
            }
        }

        // Pool is empty or doesn't exist, create a new one if initial pool not pre-warmed
        if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] GetProjectile: Pool for '{projectilePrefab.name}' empty or not found. Instantiating new.", this);
        Projectile newSpawnedInstance = Instantiate(projectilePrefab).GetComponent<Projectile>();
        if (newSpawnedInstance != null)
        {
            newSpawnedInstance.OriginalPrefab = projectilePrefab;
            _instanceToPrefabMap[newSpawnedInstance.gameObject] = projectilePrefab;
        }
        return newSpawnedInstance;
    }

    /// <summary>
    /// Returns a Projectile instance to the pool.
    /// </summary>
    /// <param name="projectileInstance">The Projectile instance to return.</param>
    /// <param name="delay">Optional delay before returning the object to the pool.</param>
    public void PutBackProjectile(Projectile projectileInstance, float delay = 0f)
    {
        if (projectileInstance == null || projectileInstance.gameObject == null)
        {
            Debug.LogWarning("PutBackProjectile: Attempted to pool a null or already destroyed projectile instance.", this);
            return;
        }

        // If this projectile type wasn't meant for pooling, just destroy it
        if (!projectileInstance.UseObjectPool)
        {
            if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] PutBackProjectile: '{projectileInstance.name}' not configured for pooling. Destroying.", this);
            if(delay > 0f) Destroy(projectileInstance.gameObject, delay);
            else Destroy(projectileInstance.gameObject);
            _instanceToPrefabMap.Remove(projectileInstance.gameObject);
            return;
        }

        if (delay > 0f)
        {
            StartCoroutine(DelayedPutBackProjectileRoutine(projectileInstance, delay));
        }
        else
        {
            ActualPutBackProjectile(projectileInstance);
        }
    }

    private System.Collections.IEnumerator DelayedPutBackProjectileRoutine(Projectile projectileInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        ActualPutBackProjectile(projectileInstance);
    }

    private void ActualPutBackProjectile(Projectile projectileInstance)
    {
        if (projectileInstance == null || projectileInstance.gameObject == null)
        {
            //Debug.LogWarning("ActualPutBackProjectile: Projectile instance became null before pooling, possibly destroyed elsewhere.", this);
            return; // Object might have been destroyed during the delay
        }
        if (!projectileInstance.gameObject.activeSelf && projectileInstance.IsInPool) {
            // Already pooled and inactive, common if multiple systems try to pool it.
            // if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] ActualPutBackProjectile: '{projectileInstance.name}' is already inactive and marked as in pool. Skipping.", this);
            return;
        }


        // The Projectile's OnDisable should trigger its OnReturnToPool event.
        projectileInstance.gameObject.SetActive(false); // This will call OnDisable on the projectile

        GameObject prefabKey = projectileInstance.OriginalPrefab;
        if (prefabKey == null && _instanceToPrefabMap.TryGetValue(projectileInstance.gameObject, out GameObject mappedPrefab)) {
            prefabKey = mappedPrefab;
        }


        if (prefabKey == null)
        {
            Debug.LogError($"PutBackProjectile: Could not determine original prefab for '{projectileInstance.name}'. Cannot pool. Destroying instead.", this);
            Destroy(projectileInstance.gameObject);
            _instanceToPrefabMap.Remove(projectileInstance.gameObject);
            return;
        }

        if (!_projectilePool.TryGetValue(prefabKey, out Queue<Projectile> pool))
        {
            pool = new Queue<Projectile>();
            _projectilePool[prefabKey] = pool;
        }

        pool.Enqueue(projectileInstance);
        if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] PutBackProjectile: Returned '{projectileInstance.name}' (Prefab: {prefabKey.name}) to pool. Pool size now: {pool.Count}", this);
    }

    /// <summary>
    /// Pre-warms the pool for a specific projectile prefab.
    /// </summary>
    /// <param name="projectilePrefab">The projectile prefab to pool.</param>
    /// <param name="count">Number of instances to create and pool.</param>
    public void PrewarmProjectilePool(GameObject projectilePrefab, int? count = null)
    {
        if (projectilePrefab == null) return;
        int amount = count ?? defaultInitialPoolSize;

        Projectile projectileComponentOnPrefab = projectilePrefab.GetComponent<Projectile>();
        if (projectileComponentOnPrefab == null || !projectileComponentOnPrefab.UseObjectPool) {
            if(enableDebugLogs && projectileComponentOnPrefab != null && !projectileComponentOnPrefab.UseObjectPool)
                Debug.Log($"PrewarmProjectilePool: '{projectilePrefab.name}' is not configured for pooling. Skipping prewarm.", this);
            return;
        }

        if (!_projectilePool.TryGetValue(projectilePrefab, out Queue<Projectile> pool))
        {
            pool = new Queue<Projectile>();
            _projectilePool[projectilePrefab] = pool;
        }

        for (int i = 0; i < amount; i++)
        {
            Projectile instance = Instantiate(projectilePrefab).GetComponent<Projectile>();
            if (instance != null)
            {
                instance.OriginalPrefab = projectilePrefab;
                _instanceToPrefabMap[instance.gameObject] = projectilePrefab;
                instance.gameObject.SetActive(false); // Deactivate after instantiating
                pool.Enqueue(instance);
            }
        }
        if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] PrewarmProjectilePool: Pre-warmed '{projectilePrefab.name}' with {amount} instances. Pool size: {pool.Count}", this);
    }

    #endregion

    #region Generic GameObject Pooling

    private string GetGameObjectPoolKey(GameObject objectToPool)
    {
        // Remove "(Clone)" from the name to use the base prefab name as key
        return objectToPool.name.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// Retrieves a generic GameObject from the pool.
    /// </summary>
    /// <param name="gameObjectPrefab">The prefab of the GameObject to retrieve.</param>
    /// <returns>An active GameObject instance, or null.</returns>
    public GameObject GetGameObject(GameObject gameObjectPrefab)
    {
        if (gameObjectPrefab == null)
        {
            Debug.LogError("GetGameObject: gameObjectPrefab cannot be null.", this);
            return null;
        }

        string key = GetGameObjectPoolKey(gameObjectPrefab); // Use original prefab name for key

        if (_gameObjectPool.TryGetValue(key, out Queue<GameObject> pool) && pool.Count > 0)
        {
            GameObject pooledObject = pool.Dequeue();
            if (pooledObject != null)
            {
                if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] GetGameObject: Reusing '{key}' from pool. Pool size now: {pool.Count}", this);
                // pooledObject.SetActive(true); // Responsibility of the caller or OnEnable of the object itself
                return pooledObject;
            }
        }

        if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] GetGameObject: Pool for '{key}' empty or not found. Instantiating new.", this);
        return Instantiate(gameObjectPrefab);
    }

    /// <summary>
    /// Returns a generic GameObject to its pool.
    /// </summary>
    /// <param name="objectToPutBack">The GameObject instance to return.</param>
    /// <param name="delay">Optional delay before returning.</param>
    public void PutBackGameObject(GameObject objectToPutBack, float delay = 0f)
    {
        if (objectToPutBack == null)
        {
            Debug.LogWarning("PutBackGameObject: Attempted to pool a null GameObject instance.", this);
            return;
        }

        if (delay > 0f)
        {
            StartCoroutine(DelayedPutBackGameObjectRoutine(objectToPutBack, delay));
        }
        else
        {
            ActualPutBackGameObject(objectToPutBack);
        }
    }

    private System.Collections.IEnumerator DelayedPutBackGameObjectRoutine(GameObject objectToPutBack, float delay)
    {
        yield return new WaitForSeconds(delay);
        ActualPutBackGameObject(objectToPutBack);
    }

    private void ActualPutBackGameObject(GameObject objectToPutBack)
    {
        if (objectToPutBack == null)
        {
            //Debug.LogWarning("ActualPutBackGameObject: GameObject instance became null before pooling.", this);
            return;
        }
         if (!objectToPutBack.activeSelf) { // Already inactive
            // if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] ActualPutBackGameObject: '{objectToPutBack.name}' is already inactive. Assuming pooled.", this);
            // return; // Potentially, but let's ensure it's in the right queue if it was just set inactive.
        }


        objectToPutBack.SetActive(false);
        string key = GetGameObjectPoolKey(objectToPutBack); // Key based on instance name (potentially with (Clone) removed)

        if (!_gameObjectPool.TryGetValue(key, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _gameObjectPool[key] = pool;
        }
        pool.Enqueue(objectToPutBack);
        if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] PutBackGameObject: Returned '{objectToPutBack.name}' (Key: {key}) to pool. Pool size now: {pool.Count}", this);
    }


    /// <summary>
    /// Pre-warms the pool for a specific generic GameObject prefab.
    /// </summary>
    public void PrewarmGameObjectPool(GameObject gameObjectPrefab, int? count = null)
    {
        if (gameObjectPrefab == null) return;
        int amount = count ?? defaultInitialPoolSize;
        string key = GetGameObjectPoolKey(gameObjectPrefab);

        if (!_gameObjectPool.TryGetValue(key, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _gameObjectPool[key] = pool;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject instance = Instantiate(gameObjectPrefab);
            if (instance != null)
            {
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }
        if (enableDebugLogs) Debug.Log($"[Frame {Time.frameCount}] PrewarmGameObjectPool: Pre-warmed '{key}' with {amount} instances. Pool size: {pool.Count}", this);
    }

    #endregion

    void OnDestroy()
    {
        // Clear pools and destroy any remaining pooled objects to prevent issues
        // This is important if DontDestroyOnLoad isn't used and the scene changes
        // or the pool manager itself is destroyed.
        foreach (var kvp in _projectilePool)
        {
            while (kvp.Value.Count > 0)
            {
                Projectile p = kvp.Value.Dequeue();
                if (p != null && p.gameObject != null) Destroy(p.gameObject);
            }
        }
        _projectilePool.Clear();

        foreach (var kvp in _gameObjectPool)
        {
            while (kvp.Value.Count > 0)
            {
                GameObject go = kvp.Value.Dequeue();
                if (go != null) Destroy(go);
            }
        }
        _gameObjectPool.Clear();
        _instanceToPrefabMap.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}