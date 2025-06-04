using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for FindObjectsOfTypeAll

/// <summary>
/// The FactoryManager is a central registry for ProjectileSpawners and Projectiles.
/// It also facilitates communication with GlobalObservers by relaying projectile lifecycle events.
/// The documentation suggests it's a "static ScriptableObject," which is unusual for active
/// scene management. A more common pattern would be a Singleton MonoBehaviour or a
/// ScriptableObject that primarily holds configuration and is accessed via a static wrapper.
///
/// This implementation attempts to follow the "ScriptableObject that acts static" idea.
/// It will need to be present as an asset in your Resources folder or manually loaded.
/// </summary>
[CreateAssetMenu(fileName = "FactoryManager", menuName = "Projectile Factory/Managers/Factory Manager")]
public class FactoryManager : ScriptableObject
{
    private static FactoryManager _instance;
    public static FactoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Attempt to load from Resources if not found
                _instance = Resources.Load<FactoryManager>("FactoryManager");

                if (_instance == null)
                {
                    // Fallback: Try to find any instance in the project (editor-only or if preloaded)
                    // This might not be reliable in builds if not in Resources or preloaded.
                    var managers = Resources.FindObjectsOfTypeAll<FactoryManager>();
                    if (managers.Length > 0)
                    {
                        _instance = managers[0];
                        if (managers.Length > 1)
                        {
                            Debug.LogWarning("Multiple FactoryManager instances found. Using the first one. Ensure only one 'FactoryManager.asset' exists or is loaded.");
                        }
                    }
                }

                if (_instance == null)
                {
                    // As a last resort, create a temporary instance.
                    // This is not ideal as its data won't persist if it's not an asset.
                    // Debug.LogError("FactoryManager asset not found in Resources. Please create one and ensure it's in a Resources folder named 'FactoryManager'. A temporary instance is being used.");
                    // _instance = ScriptableObject.CreateInstance<FactoryManager>();
                    // Better to just log an error if it's critical and cannot be found.
                    Debug.LogError("FactoryManager asset not found in Resources. Please create one (e.g., via Assets/Create/Projectile Factory/Managers/Factory Manager) and ensure it's in a Resources folder named 'FactoryManager'.");

                } else {
                    // If instance was found/loaded, initialize its lists if they are null
                    // (this can happen after domain reloads in editor if not properly serialized/re-initialized)
                    _instance.InitializeLists();
                }
            }
            return _instance;
        }
    }

    // Using List<> as specified, though for frequent add/remove, HashSet might be better for performance
    // if order doesn't matter and uniqueness is guaranteed by registration logic.
    // However, ScriptableObjects don't serialize HashSet well by default. List is fine.
    [SerializeField] // Serialize to ensure data persists in the SO asset
    private List<ProjectileSpawner> _projectileSpawners = new List<ProjectileSpawner>();
    public static IReadOnlyList<ProjectileSpawner> ProjectileSpawners => Instance?._projectileSpawners.AsReadOnly();

    [SerializeField]
    private List<Projectile> _projectiles = new List<Projectile>();
    public static IReadOnlyList<Projectile> Projectiles => Instance?._projectiles.AsReadOnly();

    // GlobalObservers are MonoBehaviours, so we don't store them in the SO directly to avoid scene dependencies in an asset.
    // Instead, they register themselves with the static instance at runtime.
    private List<GlobalObserver> _globalObserversRuntime = new List<GlobalObserver>();


    private void InitializeLists()
    {
        // This ensures that after a domain reload in the editor, or first load,
        // the lists are not null. Serialized lists will be repopulated by Unity from the asset.
        // Runtime lists (like _globalObserversRuntime) need to be newed up.
        if (_projectileSpawners == null) _projectileSpawners = new List<ProjectileSpawner>();
        if (_projectiles == null) _projectiles = new List<Projectile>();
        if (_globalObserversRuntime == null) _globalObserversRuntime = new List<GlobalObserver>();
    }


    // Called when the ScriptableObject is loaded (e.g., game start, or editor load)
    // Or when it's created.
    private void OnEnable()
    {
        // If this instance becomes the active one, ensure lists are ready.
        // This is particularly important if it's being set as the static _instance.
        if (_instance == null || _instance == this)
        {
            _instance = this; // Ensure this specific asset instance is the one used
            InitializeLists();
            // Clear runtime lists on enable, as they are scene-dependent and shouldn't persist in the SO asset
            // Spawners and Projectiles will re-register themselves from their OnEnable.
            _projectileSpawners.Clear();
            _projectiles.Clear();
            _globalObserversRuntime.Clear();
            //Debug.Log("FactoryManager OnEnable: Lists cleared and initialized.");
        }
    }
     private void OnDisable()
    {
        // Optional: If you want to clean up when the SO is unloaded (less common for SOs acting as singletons)
        // if (_instance == this)
        // {
        //     _instance = null;
        // }
    }


    #region Registration Methods
    public void RegisterSpawner(ProjectileSpawner spawner)
    {
        if (spawner != null && !_projectileSpawners.Contains(spawner))
        {
            _projectileSpawners.Add(spawner);
        }
    }

    public void UnregisterSpawner(ProjectileSpawner spawner)
    {
        if (spawner != null)
        {
            _projectileSpawners.Remove(spawner);
        }
    }

    public void RegisterProjectile(Projectile projectile)
    {
        if (projectile != null && !_projectiles.Contains(projectile))
        {
            _projectiles.Add(projectile);
        }
    }

    public void UnregisterProjectile(Projectile projectile)
    {
        if (projectile != null)
        {
            _projectiles.Remove(projectile);
        }
    }

    public void SubscribeGlobalObserver(GlobalObserver observer)
    {
        if (observer != null && !_globalObserversRuntime.Contains(observer))
        {
            _globalObserversRuntime.Add(observer);
        }
    }

    public void UnsubscribeGlobalObserver(GlobalObserver observer)
    {
        if (observer != null)
        {
            _globalObserversRuntime.Remove(observer);
        }
    }
    #endregion

    #region Event Relay Methods for Global Observers
    // These methods are called by Projectiles when their lifecycle events occur.
    // The FactoryManager then relays these to all subscribed GlobalObservers.

    public void RelayLaunch(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList()) // ToList() for safe iteration if list is modified
        {
            observer?.GlobalOnLaunch(projectile);
        }
    }

    public void RelayProjectileStopped(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnProjectileStopped(projectile);
        }
    }

    public void RelayReturnToPool(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnReturnToPool(projectile);
        }
    }

    public void RelayGetFromPool(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnGetFromPool(projectile);
        }
    }

    public void RelayCollisionEnter(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnCollisionEnter(projectile, collision, objectHit, contactPoint);
        }
    }

    public void RelayCollisionExit(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnCollisionExit(projectile, collision, objectHit, contactPoint);
        }
    }

     public void RelayCollisionStay(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnCollisionStay(projectile, collision, objectHit, contactPoint);
        }
    }

    public void RelayTriggerEnter(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnTriggerEnter(projectile, other, objectHit, contactPoint);
        }
    }

    public void RelayTriggerExit(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnTriggerExit(projectile, other, objectHit, contactPoint);
        }
    }

    public void RelayTriggerStay(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnTriggerStay(projectile, other, objectHit, contactPoint);
        }
    }


    public void RelayDoDestroy(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnDoDestroy(projectile);
        }
    }

     public void RelayDoEnable(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnDoEnable(projectile);
        }
    }

    public void RelayDoDisable(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnDoDisable(projectile);
        }
    }

    public void RelayReset(Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnReset(projectile);
        }
    }

    public void RelayProjectileSpawnerSet(ProjectileSpawner spawner, Projectile projectile)
    {
        foreach (var observer in _globalObserversRuntime.ToList())
        {
            observer?.GlobalOnProjectileSpawnerSet(spawner, projectile);
        }
    }

    #endregion

    #if UNITY_EDITOR
    // Optional: A way to clear lists in the editor if they get cluttered with invalid references
    // during development, especially after scene changes without proper unregistration.
    [ContextMenu("Clear All Registered Lists (Editor Only)")]
    private void ClearAllRegisteredListsEditor()
    {
        if (!Application.isPlaying)
        {
            _projectileSpawners.Clear();
            _projectiles.Clear();
            _globalObserversRuntime.Clear(); // This list is runtime only anyway
            Debug.Log("FactoryManager: All registered lists cleared in editor.");
            UnityEditor.EditorUtility.SetDirty(this); // Mark SO as dirty to save changes
        }
        else
        {
            Debug.LogWarning("FactoryManager: ClearAllRegisteredListsEditor can only be used when not in Play Mode.");
        }
    }
    #endif
}