using UnityEngine;

/// <summary>
/// Abstract base class for Global Observers.
/// Global Observers are MonoBehaviours that subscribe to the FactoryManager
/// to receive and react to lifecycle events from *any* registered Projectile
/// in the game.
///
/// Derived classes will implement specific reactions to these global events.
/// </summary>
public abstract class GlobalObserver : MonoBehaviour
{
    /// <summary>
    /// When the GlobalObserver is enabled, it subscribes to the FactoryManager.
    /// </summary>
    protected virtual void OnEnable()
    {
        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.SubscribeGlobalObserver(this);
        }
        else
        {
            // It's possible FactoryManager.Instance isn't ready if this OnEnable runs before FactoryManager's.
            // Consider a delayed subscription or a check in Start if issues arise.
            // For most cases, if FactoryManager is a Resource-loaded SO, it should be available.
            Debug.LogWarning($"GlobalObserver '{this.GetType().Name}' on '{gameObject.name}' could not subscribe: FactoryManager.Instance is null. Ensure FactoryManager.asset exists in Resources.", this);
        }
    }

    /// <summary>
    /// When the GlobalObserver is disabled, it unsubscribes from the FactoryManager
    /// to prevent memory leaks or calls to a disabled object.
    /// </summary>
    protected virtual void OnDisable()
    {
        // Check Application.isPlaying to prevent errors if FactoryManager.Instance is accessed
        // during editor shutdown or when the scene is being unloaded in a way that makes Instance null.
        if (FactoryManager.Instance != null && Application.isPlaying)
        {
            FactoryManager.Instance.UnsubscribeGlobalObserver(this);
        }
    }

    #region Global Event Handlers
    // These methods are called by the FactoryManager when it relays Projectile events.
    // Derived classes should override these to implement specific logic.

    /// <summary>
    /// Called globally when any registered projectile is launched.
    /// </summary>
    /// <param name="projectile">The projectile that was launched.</param>
    public virtual void GlobalOnLaunch(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile's spawner stops its current firing sequence.
    /// </summary>
    /// <param name="projectile">The projectile whose spawner has stopped.</param>
    public virtual void GlobalOnProjectileStopped(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile is being returned to an object pool.
    /// </summary>
    /// <param name="projectile">The projectile being returned to the pool.</param>
    public virtual void GlobalOnReturnToPool(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile is retrieved from an object pool.
    /// </summary>
    /// <param name="projectile">The projectile retrieved from the pool.</param>
    public virtual void GlobalOnGetFromPool(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile's Rigidbody/Collider enters a collision.
    /// </summary>
    public virtual void GlobalOnCollisionEnter(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called globally when any registered projectile's Rigidbody/Collider exits a collision.
    /// </summary>
    public virtual void GlobalOnCollisionExit(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called globally while any registered projectile's Rigidbody/Collider is colliding.
    /// </summary>
    public virtual void GlobalOnCollisionStay(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint) { }


    /// <summary>
    /// Called globally when any registered projectile's Trigger enters another Collider.
    /// </summary>
    public virtual void GlobalOnTriggerEnter(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called globally when any registered projectile's Trigger exits another Collider.
    /// </summary>
    public virtual void GlobalOnTriggerExit(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called globally while any registered projectile's Trigger is overlapping another Collider.
    /// </summary>
    public virtual void GlobalOnTriggerStay(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called globally when any registered projectile is explicitly told to "destroy" itself.
    /// </summary>
    /// <param name="projectile">The projectile being destroyed/pooled.</param>
    public virtual void GlobalOnDoDestroy(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile's GameObject is enabled.
    /// </summary>
    /// <param name="projectile">The projectile that was enabled.</param>
    public virtual void GlobalOnDoEnable(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile's GameObject is disabled.
    /// </summary>
    /// <param name="projectile">The projectile that was disabled.</param>
    public virtual void GlobalOnDoDisable(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile needs to be reset to its initial state.
    /// </summary>
    /// <param name="projectile">The projectile being reset.</param>
    public virtual void GlobalOnReset(Projectile projectile) { }

    /// <summary>
    /// Called globally when any registered projectile's assigned ProjectileSpawner is set or changed.
    /// </summary>
    /// <param name="spawner">The ProjectileSpawner that was set.</param>
    /// <param name="projectile">The projectile whose spawner was set.</param>
    public virtual void GlobalOnProjectileSpawnerSet(ProjectileSpawner spawner, Projectile projectile) { }

    #endregion
}