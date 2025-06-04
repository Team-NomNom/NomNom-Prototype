using UnityEngine;

/// <summary>
/// Abstract base class for all projectile behaviors.
/// Behaviors are ScriptableObjects that define specific actions or characteristics
/// of a projectile throughout its lifecycle (e.g., movement, collision, destruction).
/// Each behavior instance is typically unique to a projectile instance at runtime.
/// </summary>
[CreateAssetMenu(fileName = "NewProjectileBehavior", menuName = "Projectile Factory/Projectile Data/Standard Projectile Behavior")]
public class ProjectileBehavior : ScriptableObject
{
    /// <summary>
    /// Gets the Projectile instance that this behavior is attached to and acting upon.
    /// This is set during the Initialize phase.
    /// </summary>
    protected Projectile ProjectileOwner { get; private set; }

    /// <summary>
    /// Initializes the behavior with its owning Projectile.
    /// This is called by the Projectile when it instantiates its behaviors.
    /// Base implementation sets the ProjectileOwner. Derived classes can override
    /// to perform additional setup but should call base.Initialize().
    /// </summary>
    /// <param name="projectile">The projectile instance this behavior will belong to.</param>
    public virtual void Initialize(Projectile projectile)
    {
        ProjectileOwner = projectile;
    }

    /// <summary>
    /// Called every frame by the ProjectileOwner, similar to MonoBehaviour.Update().
    /// Use for continuous logic that needs to run while the projectile is active and launched.
    /// </summary>
    public virtual void Tick() { }

    /// <summary>
    /// Called every frame by the ProjectileOwner after all Tick() calls, similar to MonoBehaviour.LateUpdate().
    /// Use for logic that needs to run after other updates, e.g., camera following or adjustments based on final positions.
    /// </summary>
    public virtual void LateTick() { }

    /// <summary>
    /// Called when the ProjectileOwner is launched.
    /// This is typically after SpawnBehaviorModifications have run and the projectile is considered "live".
    /// </summary>
    /// <param name="projectile">The launching projectile (same as ProjectileOwner).</param>
    public virtual void OnLaunch(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner's spawner stops its current firing sequence.
    /// Useful for behaviors that need to react to the "cease fire" command (e.g., a beam shutting off).
    /// </summary>
    /// <param name="projectile">The projectile whose spawner has stopped (same as ProjectileOwner).</param>
    public virtual void OnProjectileStopped(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner is being returned to an object pool.
    /// The projectile GameObject will typically be deactivated shortly after this.
    /// </summary>
    /// <param name="projectile">The projectile being returned to the pool (same as ProjectileOwner).</param>
    public virtual void OnReturnToPool(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner is retrieved from an object pool and is about to be reused.
    /// This is called before ResetBehavior and OnEnable for the projectile GameObject.
    /// </summary>
    /// <param name="projectile">The projectile retrieved from the pool (same as ProjectileOwner).</param>
    public virtual void OnGetFromPool(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner's Rigidbody/Collider enters a collision with another Collider.
    /// Corresponds to MonoBehaviour.OnCollisionEnter.
    /// </summary>
    /// <param name="projectile">The projectile involved in the collision (same as ProjectileOwner).</param>
    /// <param name="collision">Unity's Collision data.</param>
    /// <param name="objectHit">The GameObject that was hit.</param>
    /// <param name="contactPoint">The primary contact point of the collision.</param>
    public virtual void CollisionEnter(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called when the ProjectileOwner's Rigidbody/Collider stops colliding with another Collider.
    /// Corresponds to MonoBehaviour.OnCollisionExit.
    /// </summary>
    /// <param name="projectile">The projectile involved in the collision (same as ProjectileOwner).</param>
    /// <param name="collision">Unity's Collision data.</param>
    /// <param name="objectHit">The GameObject that was previously being collided with.</param>
    /// <param name="contactPoint">Not typically provided by OnCollisionExit, using projectile position as fallback.</param>
    public virtual void CollisionExit(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called every frame while the ProjectileOwner's Rigidbody/Collider is colliding with another Collider.
    /// Corresponds to MonoBehaviour.OnCollisionStay.
    /// </summary>
    /// <param name="projectile">The projectile involved in the collision (same as ProjectileOwner).</param>
    /// <param name="collision">Unity's Collision data.</param>
    /// <param name="objectHit">The GameObject being collided with.</param>
    /// <param name="contactPoint">A contact point of the ongoing collision.</param>
    public virtual void CollisionStay(Projectile projectile, Collision collision, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called when the ProjectileOwner's Collider (marked as Trigger) enters another Collider.
    /// Corresponds to MonoBehaviour.OnTriggerEnter.
    /// </summary>
    /// <param name="projectile">The projectile whose trigger was activated (same as ProjectileOwner).</param>
    /// <param name="other">The other Collider involved in the trigger event.</param>
    /// <param name="objectHit">The GameObject associated with the other Collider.</param>
    /// <param name="contactPoint">The closest point on the other collider to the projectile's trigger, or projectile position.</param>
    public virtual void TriggerEnter(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called when the ProjectileOwner's Collider (marked as Trigger) exits another Collider.
    /// Corresponds to MonoBehaviour.OnTriggerExit.
    /// </summary>
    /// <param name="projectile">The projectile whose trigger was exited (same as ProjectileOwner).</param>
    /// <param name="other">The other Collider that was exited.</param>
    /// <param name="objectHit">The GameObject associated with the other Collider.</param>
    /// <param name="contactPoint">Not typically provided by OnTriggerExit, using projectile position as fallback.</param>
    public virtual void TriggerExit(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called every frame while the ProjectileOwner's Collider (marked as Trigger) is overlapping another Collider.
    /// Corresponds to MonoBehaviour.OnTriggerStay.
    /// </summary>
    /// <param name="projectile">The projectile whose trigger is active (same as ProjectileOwner).</param>
    /// <param name="other">The other Collider involved in the trigger event.</param>
    /// <param name="objectHit">The GameObject associated with the other Collider.</param>
    /// <param name="contactPoint">The closest point on the other collider to the projectile's trigger, or projectile position.</param>
    public virtual void TriggerStay(Projectile projectile, Collider other, GameObject objectHit, Vector3 contactPoint) { }

    /// <summary>
    /// Called when the ProjectileOwner is explicitly told to "destroy" itself (e.g., by a lifetime expiring or impact).
    /// The base implementation handles returning the projectile to the pool if pooling is enabled,
    /// or destroying the GameObject otherwise.
    /// Behaviors can override this to add custom logic before or instead of the default destruction/pooling.
    /// </summary>
    /// <param name="projectile">The projectile to be destroyed/pooled (same as ProjectileOwner).</param>
    public virtual void DoDestroy(Projectile projectile)
    {
        if (projectile == null) return;

        if (projectile.UseObjectPool && ProjectilePoolManager.Instance != null)
        {
            // The ProjectilePoolManager should handle deactivating the GameObject.
            ProjectilePoolManager.Instance.PutBackProjectile(projectile);
        }
        else
        {
            // Ensure we are not trying to destroy an already destroyed or null object
            if (projectile.gameObject != null)
            {
                GameObject.Destroy(projectile.gameObject);
            }
        }
    }

    /// <summary>
    /// Called when the ProjectileOwner's GameObject is enabled.
    /// This can happen when it's first instantiated or when retrieved from a pool.
    /// </summary>
    /// <param name="projectile">The projectile that was enabled (same as ProjectileOwner).</param>
    public virtual void DoEnable(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner's GameObject is disabled.
    /// This can happen when it's being returned to a pool or destroyed.
    /// </summary>
    /// <param name="projectile">The projectile that was disabled (same as ProjectileOwner).</param>
    public virtual void DoDisable(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner needs to be reset to its initial state,
    /// typically when being retrieved from an object pool or if explicitly reset.
    /// Behaviors should reset any internal state here.
    /// </summary>
    /// <param name="projectile">The projectile being reset (same as ProjectileOwner).</param>
    public virtual void ResetBehavior(Projectile projectile) { }

    /// <summary>
    /// Called when the ProjectileOwner's assigned ProjectileSpawner is set or changed.
    /// This happens when the projectile is first spawned and may happen if the spawner is reassigned.
    /// </summary>
    /// <param name="spawner">The ProjectileSpawner that was set.</param>
    /// <param name="projectile">The projectile whose spawner was set (same as ProjectileOwner).</param>
    public virtual void OnProjectileSpawnerSet(ProjectileSpawner spawner, Projectile projectile) { }


#if UNITY_EDITOR
    // Optional: You can add a header to the ScriptableObject Inspector for clarity
    // Or use custom editor scripts for more advanced inspector UIs for behaviors.
    // [Header("Base Projectile Behavior Settings")] // Example
#endif
}