using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Reflection; // Required for deeper UnityEvent copying if needed

// --- Placeholder Types (Copied from your thought block for completeness) ---
// (These would be in their own files in a real project)
public class SpawnBehaviorModification : ProjectileBehavior {
    public virtual void OnSpawn(Projectile projectile) { /* Default to calling OnLaunch if not overridden */ OnLaunch(projectile); }
}
public class ProjectileObserver : ProjectileBehavior { }
public class TrajectoryBehavior : ProjectileBehavior { }

public class ProjectileSpawner : MonoBehaviour {
    public UnityEvent<Projectile> ProjectileOnLaunchUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile> OnProjectileStoppedUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile> OnReturnToPoolUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile> OnGetFromPoolUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile, Collision, GameObject, Vector3> OnCollisionEnterUnityEvent = new UnityEvent<Projectile, Collision, GameObject, Vector3>();
    public UnityEvent<Projectile, Collision, GameObject, Vector3> OnCollisionExitUnityEvent = new UnityEvent<Projectile, Collision, GameObject, Vector3>();
    public UnityEvent<Projectile, Collision, GameObject, Vector3> OnCollisionStayUnityEvent = new UnityEvent<Projectile, Collision, GameObject, Vector3>();
    public UnityEvent<Projectile, Collider, GameObject, Vector3> OnTriggerEnterUnityEvent = new UnityEvent<Projectile, Collider, GameObject, Vector3>();
    public UnityEvent<Projectile, Collider, GameObject, Vector3> OnTriggerExitUnityEvent = new UnityEvent<Projectile, Collider, GameObject, Vector3>();
    public UnityEvent<Projectile, Collider, GameObject, Vector3> OnTriggerStayUnityEvent = new UnityEvent<Projectile, Collider, GameObject, Vector3>();
    public UnityEvent<Projectile> OnDoDestroyUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile> OnDoEnableUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile> OnDoDisableUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<Projectile> OnResetUnityEvent = new UnityEvent<Projectile>();
    public UnityEvent<ProjectileSpawner, Projectile> OnProjectileSpawnerSetUnityEvent = new UnityEvent<ProjectileSpawner, Projectile>();
    public LayerMask collisionMask;
    public List<ProjectileObserver> Observers = new List<ProjectileObserver>(); // Observers defined on spawner
    public void AddProjectileObserversTo(Projectile projectileInstance) {
        foreach (var observerAsset in Observers) {
            if (observerAsset != null) {
                var observerInstance = Instantiate(observerAsset); // Instantiate for this projectile
                projectileInstance.AddObserver(observerInstance); // AddObserver will initialize it
            }
        }
    }
}

[System.Serializable] public class SpawnPoint { public Transform transform; public Transform rotatingTransform; public Transform tiltingTransform;}
// --- End Placeholder Types ---

// --- Custom UnityEvent types for better Inspector organization ---
[System.Serializable] public class ProjectileUnityEvent : UnityEvent<Projectile> { }
[System.Serializable] public class ProjectileCollisionUnityEvent : UnityEvent<Projectile, Collision, GameObject, Vector3> { }
[System.Serializable] public class ProjectileTriggerUnityEvent : UnityEvent<Projectile, Collider, GameObject, Vector3> { }
[System.Serializable] public class ProjectileSpawnerSetUnityEvent : UnityEvent<ProjectileSpawner, Projectile> { }


public class Projectile : MonoBehaviour
{
    [Header("Data & Core Definition")]
    [SerializeField] private ProjectileData _projectileData;
    public ProjectileData ProjectileData { get => _projectileData; set => _projectileData = value; }

    [Tooltip("Defines how this type of projectile is spawned. Used by the ProjectileSpawner.")]
    [SerializeField] private SpawnBehavior _spawnBehaviorAsset;
    public SpawnBehavior SpawnBehaviorAsset { get => _spawnBehaviorAsset; }


    [Header("Behaviors (Instantiated at Runtime)")]
    [Tooltip("Behaviors that fire once immediately after spawn. These are ScriptableObject assets.")]
    [SerializeField] private List<SpawnBehaviorModification> _spawnBehaviorModificationAssets = new List<SpawnBehaviorModification>();
    private List<SpawnBehaviorModification> _runtimeSpawnBehaviorModifications = new List<SpawnBehaviorModification>();
    public IReadOnlyList<SpawnBehaviorModification> SpawnBehaviorModifications => _runtimeSpawnBehaviorModifications.AsReadOnly();

    [Tooltip("Core behaviors for movement, collision, destruction, etc. These are ScriptableObject assets.")]
    [SerializeField] private List<ProjectileBehavior> _behaviorAssets = new List<ProjectileBehavior>();
    private List<ProjectileBehavior> _runtimeBehaviors = new List<ProjectileBehavior>();
    public IReadOnlyList<ProjectileBehavior> Behaviors => _runtimeBehaviors.AsReadOnly();


    private List<ProjectileObserver> _runtimeObservers = new List<ProjectileObserver>();
    public IReadOnlyList<ProjectileObserver> Observers => _runtimeObservers.AsReadOnly();
    public int CountObservers => _runtimeObservers.Count;
    public int CountBehaviors => _runtimeBehaviors.Count;


    [Header("State & Options")]
    [SerializeField] private bool _registerWithFactoryManager = true;
    public bool RegisterWithFactoryManager { get => _registerWithFactoryManager; set => _registerWithFactoryManager = value; }

    [SerializeField] private bool _useObjectPool = true;
    public bool UseObjectPool { get => _useObjectPool; set => _useObjectPool = value; }

    [Tooltip("Optional pre-launch trajectory behavior asset. Distinct from active in-flight trajectories which are standard behaviors.")]
    [SerializeField] private TrajectoryBehavior _preLaunchTrajectoryBehaviorAsset;
    private TrajectoryBehavior _runtimePreLaunchTrajectoryBehavior; // Instantiated if used
    public TrajectoryBehavior PreLaunchTrajectoryBehavior => _runtimePreLaunchTrajectoryBehavior;

    [SerializeField] private bool _overrideCollisionMask = false;
    public bool OverrideCollisionMask { get => _overrideCollisionMask; set => _overrideCollisionMask = value; }

    [SerializeField] private LayerMask _collisionMask;
    public LayerMask CollisionMask
    {
        get => _collisionMask;
        set => _collisionMask = value;
    }

    public bool IsInPool { get; private set; }
    public bool Launched { get; private set; }

    public ProjectileSpawner AssignedSpawner { get; private set; }
    public GameObject OriginalPrefab { get; set; } // Set by Spawner for pooling key


    // --- UnityEvents (Listeners are copied from ProjectileSpawner instance) ---
    [HideInInspector] public ProjectileUnityEvent ProjectileOnLaunchUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnProjectileStoppedUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnReturnToPoolUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnGetFromPoolUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileCollisionUnityEvent OnCollisionEnterUnityEvent = new ProjectileCollisionUnityEvent();
    [HideInInspector] public ProjectileCollisionUnityEvent OnCollisionExitUnityEvent = new ProjectileCollisionUnityEvent();
    [HideInInspector] public ProjectileCollisionUnityEvent OnCollisionStayUnityEvent = new ProjectileCollisionUnityEvent();
    [HideInInspector] public ProjectileTriggerUnityEvent OnTriggerEnterUnityEvent = new ProjectileTriggerUnityEvent();
    [HideInInspector] public ProjectileTriggerUnityEvent OnTriggerExitUnityEvent = new ProjectileTriggerUnityEvent();
    [HideInInspector] public ProjectileTriggerUnityEvent OnTriggerStayUnityEvent = new ProjectileTriggerUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnDoDestroyUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnDoEnableUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnDoDisableUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileUnityEvent OnResetUnityEvent = new ProjectileUnityEvent();
    [HideInInspector] public ProjectileSpawnerSetUnityEvent OnProjectileSpawnerSetUnityEvent = new ProjectileSpawnerSetUnityEvent();

    private bool _isInitializedForRuntime = false;


    #region Unity Lifecycle
    void Awake()
    {
        // Lists for runtime instances are initialized here or just before first use.
        // _runtimeSpawnBehaviorModifications, _runtimeBehaviors, _runtimeObservers
    }

    void OnEnable()
    {
        IsInPool = false;

        if (!_isInitializedForRuntime)
        {
            InstantiateAndInitializeBehaviors();
            _isInitializedForRuntime = true;
        }
        
        if (RegisterWithFactoryManager && FactoryManager.Instance != null)
        {
            FactoryManager.Instance.RegisterProjectile(this);
        }
        InternalOnDoEnable();
        InternalOnGetFromPool(); // This also calls ResetProjectile
    }

    void OnDisable()
    {
        InternalOnDoDisable();
        if (RegisterWithFactoryManager && FactoryManager.Instance != null && Application.isPlaying)
        {
            FactoryManager.Instance.UnregisterProjectile(this);
        }
        Launched = false;
        IsInPool = true;
        InternalOnReturnToPool();
    }

    void Update()
    {
        if (!Launched || IsInPool) return;

        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.Tick();
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.Tick();
    }

    void LateUpdate()
    {
        if (!Launched || IsInPool) return;

        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.LateTick();
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.LateTick();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!Launched || IsInPool) return;
        if ((_collisionMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            InternalOnCollisionEnter(collision, collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!Launched || IsInPool) return;
        if ((_collisionMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            InternalOnCollisionExit(collision, collision.gameObject, transform.position);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!Launched || IsInPool) return;
        if ((_collisionMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            InternalOnCollisionStay(collision, collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Launched || IsInPool) return;
        if ((_collisionMask.value & (1 << other.gameObject.layer)) != 0)
        {
            InternalOnTriggerEnter(other, other.gameObject, other.ClosestPoint(transform.position));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!Launched || IsInPool) return;
        if ((_collisionMask.value & (1 << other.gameObject.layer)) != 0)
        {
            InternalOnTriggerExit(other, other.gameObject, transform.position);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!Launched || IsInPool) return;
        if ((_collisionMask.value & (1 << other.gameObject.layer)) != 0)
        {
            InternalOnTriggerStay(other, other.gameObject, other.ClosestPoint(transform.position));
        }
    }
    #endregion

    #region Public Methods for Lifecycle & Control

    public void ApplySpawnModifications()
    {
        if (IsInPool) return;
        for (int i = 0; i < _runtimeSpawnBehaviorModifications.Count; i++)
        {
            _runtimeSpawnBehaviorModifications[i]?.OnSpawn(this);
        }
    }

    public void NotifyLaunched()
    {
        if (IsInPool) return;
        Launched = true;
        InternalOnLaunch();
    }
    
    public virtual void ResetProjectile()
    {
        Launched = false;
        // Reset transform, physics properties etc. as needed by your game.
        // e.g., if Rigidbody exists:
        // Rigidbody rb = GetComponent<Rigidbody>();
        // if (rb != null) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.ResetBehavior(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.ResetBehavior(this);
        for (int i = 0; i < _runtimeSpawnBehaviorModifications.Count; i++) _runtimeSpawnBehaviorModifications[i]?.ResetBehavior(this);
        _runtimePreLaunchTrajectoryBehavior?.ResetBehavior(this);

        InternalOnReset();
    }

    public void AddToCollisionMask(LayerMask maskToAdd)
    {
        if (_overrideCollisionMask) _collisionMask |= maskToAdd;
        else Debug.LogWarning($"Cannot modify CollisionMask on {name}: OverrideCollisionMask is false.", this);
    }

    public void RemoveFromCollisionMask(LayerMask maskToRemove)
    {
        if (_overrideCollisionMask) _collisionMask &= ~maskToRemove;
        else Debug.LogWarning($"Cannot modify CollisionMask on {name}: OverrideCollisionMask is false.", this);
    }

    public void AddObserver(ProjectileObserver observerInstance) // Expects an already instantiated observer
    {
        if (observerInstance != null && !_runtimeObservers.Contains(observerInstance))
        {
            observerInstance.Initialize(this);
            _runtimeObservers.Add(observerInstance);
            observerInstance.OnProjectileSpawnerSet(AssignedSpawner, this);
        }
    }

    public void RemoveObserver(ProjectileObserver observerInstance)
    {
        _runtimeObservers.Remove(observerInstance);
    }
    
    public void AddBehavior(ProjectileBehavior behaviorAsset) // Adds a new type of behavior at runtime
    {
        if (behaviorAsset != null)
        {
            ProjectileBehavior instance = Instantiate(behaviorAsset);
            instance.Initialize(this);
            _runtimeBehaviors.Add(instance);
            instance.OnProjectileSpawnerSet(AssignedSpawner, this);
            if(Launched && isActiveAndEnabled) instance.OnLaunch(this);
        }
    }

    public void RemoveBehavior(ProjectileBehavior behaviorInstance) // Removes a specific runtime instance
    {
        if (_runtimeBehaviors.Remove(behaviorInstance))
        {
            Destroy(behaviorInstance); // Clean up the SO instance
        }
    }

    public void TriggerCollisionWithObject(GameObject objectHit, Vector3 pointOfContact = default)
    {
        if (!Launched || IsInPool) return;
        if (pointOfContact == default && objectHit != null) pointOfContact = objectHit.transform.position;

        Collider hitCollider = objectHit?.GetComponent<Collider>();
        InternalOnTriggerEnter(hitCollider, objectHit, pointOfContact); // Use Trigger for flexibility
    }

    public void InvokeDestroy()
    {
        if (IsInPool && UseObjectPool) return; // Already pooled, or about to be.
        InternalOnDoDestroy();
    }

    public void SetProjectileSpawner(ProjectileSpawner spawner)
    {
        AssignedSpawner = spawner;
        if (!_overrideCollisionMask && spawner != null)
        {
            _collisionMask = spawner.collisionMask;
        }
        InternalOnProjectileSpawnerSet(spawner);
    }

    public void CopyUnityEventsFromSpawner(ProjectileSpawner spawner)
    {
        if (spawner == null) return;
        CopyListeners(spawner.ProjectileOnLaunchUnityEvent, ProjectileOnLaunchUnityEvent);
        CopyListeners(spawner.OnProjectileStoppedUnityEvent, OnProjectileStoppedUnityEvent);
        // ... copy all other events
        CopyListeners(spawner.OnCollisionEnterUnityEvent, OnCollisionEnterUnityEvent);
        CopyListeners(spawner.OnTriggerEnterUnityEvent, OnTriggerEnterUnityEvent);
        CopyListeners(spawner.OnDoDestroyUnityEvent, OnDoDestroyUnityEvent);
        CopyListeners(spawner.OnResetUnityEvent, OnResetUnityEvent);
        CopyListeners(spawner.OnProjectileSpawnerSetUnityEvent, OnProjectileSpawnerSetUnityEvent);
    }
    #endregion

    #region Internal Logic & Event Invokers
    private void InstantiateAndInitializeBehaviors()
    {
        _runtimeSpawnBehaviorModifications.Clear();
        foreach (var asset in _spawnBehaviorModificationAssets) {
            if (asset != null) {
                var instance = Instantiate(asset);
                instance.Initialize(this);
                _runtimeSpawnBehaviorModifications.Add(instance);
            }
        }

        _runtimeBehaviors.Clear();
        foreach (var asset in _behaviorAssets) {
            if (asset != null) {
                var instance = Instantiate(asset);
                instance.Initialize(this);
                _runtimeBehaviors.Add(instance);
            }
        }
        // Observers are added via AddObserver by the spawner, already instantiated.

        if (_preLaunchTrajectoryBehaviorAsset != null) {
            _runtimePreLaunchTrajectoryBehavior = Instantiate(_preLaunchTrajectoryBehaviorAsset);
            _runtimePreLaunchTrajectoryBehavior.Initialize(this);
        }
    }

    private void InternalOnLaunch()
    {
        ProjectileOnLaunchUnityEvent?.Invoke(this);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.OnLaunch(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.OnLaunch(this);
        // SpawnBehaviorMods are called via ApplySpawnModifications earlier
    }

    public void InternalOnProjectileStopped() // Public for Spawner to call
    {
        OnProjectileStoppedUnityEvent?.Invoke(this);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.OnProjectileStopped(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.OnProjectileStopped(this);
    }

    private void InternalOnReturnToPool()
    {
        OnReturnToPoolUnityEvent?.Invoke(this);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.OnReturnToPool(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.OnReturnToPool(this);
    }

    private void InternalOnGetFromPool()
    {
        OnGetFromPoolUnityEvent?.Invoke(this);
        // Initialize/Reset all behaviors *after* invoking the event, so they can react to "fresh state"
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.OnGetFromPool(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.OnGetFromPool(this);
        ResetProjectile(); // Ensures clean state
    }

    private void InternalOnCollisionEnter(Collision collision, GameObject objectHit, Vector3 contactPoint)
    {
        OnCollisionEnterUnityEvent?.Invoke(this, collision, objectHit, contactPoint);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.CollisionEnter(this, collision, objectHit, contactPoint);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.CollisionEnter(this, collision, objectHit, contactPoint);
    }
    private void InternalOnCollisionExit(Collision collision, GameObject objectHit, Vector3 contactPoint)
    {
        OnCollisionExitUnityEvent?.Invoke(this, collision, objectHit, contactPoint);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.CollisionExit(this, collision, objectHit, contactPoint);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.CollisionExit(this, collision, objectHit, contactPoint);
    }
    private void InternalOnCollisionStay(Collision collision, GameObject objectHit, Vector3 contactPoint)
    {
        OnCollisionStayUnityEvent?.Invoke(this, collision, objectHit, contactPoint);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.CollisionStay(this, collision, objectHit, contactPoint);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.CollisionStay(this, collision, objectHit, contactPoint);
    }

    private void InternalOnTriggerEnter(Collider other, GameObject objectHit, Vector3 contactPoint)
    {
        OnTriggerEnterUnityEvent?.Invoke(this, other, objectHit, contactPoint);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.TriggerEnter(this, other, objectHit, contactPoint);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.TriggerEnter(this, other, objectHit, contactPoint);
    }
    private void InternalOnTriggerExit(Collider other, GameObject objectHit, Vector3 contactPoint)
    {
        OnTriggerExitUnityEvent?.Invoke(this, other, objectHit, contactPoint);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.TriggerExit(this, other, objectHit, contactPoint);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.TriggerExit(this, other, objectHit, contactPoint);
    }
    private void InternalOnTriggerStay(Collider other, GameObject objectHit, Vector3 contactPoint)
    {
        OnTriggerStayUnityEvent?.Invoke(this, other, objectHit, contactPoint);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.TriggerStay(this, other, objectHit, contactPoint);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.TriggerStay(this, other, objectHit, contactPoint);
    }

    private void InternalOnDoDestroy()
    {
        OnDoDestroyUnityEvent?.Invoke(this);
        bool destroyedByBehavior = false;
        for (int i = _runtimeBehaviors.Count - 1; i >= 0; i--) {
            _runtimeBehaviors[i]?.DoDestroy(this);
            // A behavior might set IsInPool or disable the GameObject
            if (IsInPool || (this != null && !gameObject.activeSelf)) destroyedByBehavior = true;
        }
        for (int i = _runtimeObservers.Count - 1; i >= 0; i--) {
             _runtimeObservers[i]?.DoDestroy(this);
            if (IsInPool || (this != null && !gameObject.activeSelf)) destroyedByBehavior = true;
        }

        if (!destroyedByBehavior && (this != null && gameObject.activeSelf)) { // Fallback if no behavior handled it
            if (UseObjectPool && ProjectilePoolManager.Instance != null) {
                ProjectilePoolManager.Instance.PutBackProjectile(this);
            } else {
                Destroy(gameObject);
            }
        }
    }

    private void InternalOnDoEnable()
    {
        OnDoEnableUnityEvent?.Invoke(this);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.DoEnable(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.DoEnable(this);
    }
    private void InternalOnDoDisable()
    {
        OnDoDisableUnityEvent?.Invoke(this);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.DoDisable(this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.DoDisable(this);
    }

    private void InternalOnReset()
    {
        OnResetUnityEvent?.Invoke(this);
    }

    private void InternalOnProjectileSpawnerSet(ProjectileSpawner spawner)
    {
        OnProjectileSpawnerSetUnityEvent?.Invoke(spawner, this);
        for (int i = 0; i < _runtimeBehaviors.Count; i++) _runtimeBehaviors[i]?.OnProjectileSpawnerSet(spawner, this);
        for (int i = 0; i < _runtimeObservers.Count; i++) _runtimeObservers[i]?.OnProjectileSpawnerSet(spawner, this);
    }

    private void CopyListeners<TUnityEvent>(TUnityEvent sourceEvent, TUnityEvent destinationEvent) where TUnityEvent : UnityEventBase, new()
    {
        if (sourceEvent == null || destinationEvent == null) return;
        destinationEvent.RemoveAllListeners(); // Clear existing listeners on the projectile's event

        for (int i = 0; i < sourceEvent.GetPersistentEventCount(); i++)
        {
            Object target = sourceEvent.GetPersistentTarget(i);
            string methodName = sourceEvent.GetPersistentMethodName(i);

            if (target != null && !string.IsNullOrEmpty(methodName))
            {
                // This is a simplified way to re-add persistent listeners.
                // It relies on the delegate creation matching the original signature.
                // This might not perfectly replicate all types of persistent listeners
                // (e.g. static methods, or methods with specific argument types from fields).
                // A more robust solution might involve reflection on UnityEventTools or
                // the spawner directly adding its listening methods programmatically.
                // For common cases of "drag object, select public method", this might work.

                // Create a base UnityAction and then try to cast or add based on the destinationEvent type
                // This part is tricky without knowing the exact signature of the delegate needed by destinationEvent.
                // Let's assume the events are of the same type argument-wise.
                var dynamicListener = new UnityAction<Projectile>((p) => {
                    // This is a generic wrapper, won't work for events with more args.
                    // We need specific handlers for each event type.
                });

                // Example for ProjectileUnityEvent (UnityEvent<Projectile>)
                if (destinationEvent is UnityEvent<Projectile> destProjectileEvent && sourceEvent is UnityEvent<Projectile> srcProjectileEvent)
                {
                    UnityAction<Projectile> action = (param) => {}; // Placeholder
                    try {
                        action = (UnityAction<Projectile>)System.Delegate.CreateDelegate(typeof(UnityAction<Projectile>), target, methodName);
                        destProjectileEvent.AddListener(action);
                    } catch (System.Exception ex) {
                        Debug.LogWarning($"Could not create delegate for {methodName} on {target.name} for ProjectileEvent: {ex.Message}", this);
                    }
                }
                // Example for ProjectileCollisionUnityEvent (UnityEvent<Projectile, Collision, GameObject, Vector3>)
                else if (destinationEvent is UnityEvent<Projectile, Collision, GameObject, Vector3> destCollisionEvent && 
                         sourceEvent is UnityEvent<Projectile, Collision, GameObject, Vector3> srcCollisionEvent)
                {
                     UnityAction<Projectile, Collision, GameObject, Vector3> action = (p,c,go,v3) => {};
                     try {
                        action = (UnityAction<Projectile, Collision, GameObject, Vector3>)System.Delegate.CreateDelegate(typeof(UnityAction<Projectile, Collision, GameObject, Vector3>), target, methodName);
                        destCollisionEvent.AddListener(action);
                    } catch (System.Exception ex) {
                        Debug.LogWarning($"Could not create delegate for {methodName} on {target.name} for ProjectileCollisionEvent: {ex.Message}", this);
                    }
                }
                // Add more else-if blocks for other event signatures (ProjectileTriggerUnityEvent, ProjectileSpawnerSetUnityEvent)
                 else if (destinationEvent is UnityEvent<Projectile, Collider, GameObject, Vector3> destTriggerEvent && 
                         sourceEvent is UnityEvent<Projectile, Collider, GameObject, Vector3> srcTriggerEvent)
                {
                     UnityAction<Projectile, Collider, GameObject, Vector3> action = (p,c,go,v3) => {};
                     try {
                        action = (UnityAction<Projectile, Collider, GameObject, Vector3>)System.Delegate.CreateDelegate(typeof(UnityAction<Projectile, Collider, GameObject, Vector3>), target, methodName);
                        destTriggerEvent.AddListener(action);
                    } catch (System.Exception ex) {
                        Debug.LogWarning($"Could not create delegate for {methodName} on {target.name} for ProjectileTriggerEvent: {ex.Message}", this);
                    }
                }
                 else if (destinationEvent is UnityEvent<ProjectileSpawner, Projectile> destSpawnerSetEvent && 
                         sourceEvent is UnityEvent<ProjectileSpawner, Projectile> srcSpawnerSetEvent)
                {
                     UnityAction<ProjectileSpawner, Projectile> action = (ps,p) => {};
                     try {
                        action = (UnityAction<ProjectileSpawner, Projectile>)System.Delegate.CreateDelegate(typeof(UnityAction<ProjectileSpawner, Projectile>), target, methodName);
                        destSpawnerSetEvent.AddListener(action);
                    } catch (System.Exception ex) {
                        Debug.LogWarning($"Could not create delegate for {methodName} on {target.name} for ProjectileSpawnerSetEvent: {ex.Message}", this);
                    }
                }


            }
        }
    }


    #endregion

    void OnDestroy()
    {
        // Cleanup instantiated ScriptableObject behaviors
        foreach (var instance in _runtimeSpawnBehaviorModifications) if (instance != null) Destroy(instance);
        _runtimeSpawnBehaviorModifications.Clear();

        foreach (var instance in _runtimeBehaviors) if (instance != null) Destroy(instance);
        _runtimeBehaviors.Clear();

        foreach (var instance in _runtimeObservers) if (instance != null) Destroy(instance); // Observers are also SO instances
        _runtimeObservers.Clear();

        if (_runtimePreLaunchTrajectoryBehavior != null) Destroy(_runtimePreLaunchTrajectoryBehavior);
    }
}

// Placeholder for SpawnBehavior (which is a ProjectileBehavior)
public class SpawnBehavior : ProjectileBehavior {
    public virtual void TriggerLaunch(ProjectileSpawner spawner, Projectile projectilePrefab) {
        // This method would be called by the ProjectileSpawner.
        // It then orchestrates the spawning of one or more projectile instances.
        Debug.Log($"SpawnBehavior on {projectilePrefab.name} is triggering launch via {spawner.name}");

        // Example for a single projectile spawn:
        // Projectile instance = GetFromPoolOrInstantiate(projectilePrefab);
        // if (instance != null) {
        //    instance.gameObject.SetActive(true); // Ensure active
        //    instance.SetProjectileSpawner(spawner);
        //    instance.OriginalPrefab = projectilePrefab.gameObject; // For pooling key
        //    instance.CopyUnityEventsFromSpawner(spawner);
        //    spawner.AddProjectileObserversTo(instance); // Spawner adds its defined observers
        //    instance.ApplySpawnModifications();
        //    instance.NotifyLaunched();
        // }
    }

    protected Projectile GetFromPoolOrInstantiate(Projectile prefab, ProjectileSpawner spawner) {
        Projectile instance = null;
        if (prefab.UseObjectPool && ProjectilePoolManager.Instance != null) {
            instance = ProjectilePoolManager.Instance.GetProjectile(prefab.gameObject);
        }
        if (instance == null) {
            instance = Instantiate(prefab);
        }
        // Basic setup here, more detailed setup after this method returns
        if(instance != null) {
            instance.transform.position = spawner.transform.position; // Example, use actual spawn point
            instance.transform.rotation = spawner.transform.rotation; // Example
        }
        return instance;
    }
}