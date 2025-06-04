using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class for all networked projectiles.  Must be on a prefab that has:
///   • NetworkObject
///   • NetworkTransform (to sync position/rotation)
///   • Rigidbody (for physics)
/// 
/// Any child class can override MoveProjectile() or OnHit(…) to change behavior.
/// </summary>

public class ProjectileBase : NetworkBehaviour
{
    [Header("Common Projectile Settings")]
    [Tooltip("Speed (units/sec) at which this projectile initially travels.")]
    [SerializeField] protected float speed = 20f;

    [Tooltip("Amount of damage (or effect) this projectile does on hit.")]
    [SerializeField] protected float damage = 10f;

    [Tooltip("Seconds until this projectile self‐destructs.")]
    [SerializeField] protected float lifetime = 5f;

    // Holds ObjectID of whoever fired at tank
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(
    0u,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    protected Rigidbody rb;

    // Called by Netcode on both client & server when this NetworkObject is spawned.
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (IsServer)
        {
            // Initialize motion or any server‐only logic
            InitializeProjectile();

            // Start the self‐destruct coroutine on the server
            StartCoroutine(DestroyAfterLifetime());
        }
        else
        {
            // Clients: do not run movement or destruction logic here.
            // They will simply see transforms updated via NetworkTransform.
        }
    }

    /// Called on the server once to set up initial conditions
    /// Child classes can override this to do something more complex than "set forward velocity."
    protected virtual void InitializeProjectile()
    {
        rb.linearVelocity = transform.forward * speed;
    }

    // Called every physics tick on the server only. Override to implement other types of projectiles
    protected virtual void FixedUpdate()
    {
        if (!IsServer) return;

        MoveProjectile();
    }

    // Per‐tick movement logic (server only). By default, does nothing (we already set velocity once). Custom projectiles can override to adjust the velocity each tick.
    protected virtual void MoveProjectile()
    {
    }
    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        if (IsServer)
        {
            OnLifetimeExpired();
            // Despawn the network object so all clients remove it
            GetComponent<NetworkObject>().Despawn();
        }
    }

    /// <summary>
    /// Override in child to add effects (explosion, sound, etc.) when lifetime runs out (aka vfx/sfx).
    /// </summary>
    protected virtual void OnLifetimeExpired()
    {

    }

    /// This method is called on the server when this projectile collides with something. We assume there’s a NetworkObject on the collider to identify “who” got hit.
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // If we hit our own shooter, ignore entirely
        var hitNetObj = collision.collider.GetComponentInParent<NetworkObject>();
        if (hitNetObj != null && hitNetObj.NetworkObjectId == ownerId.Value)
        {
            // We collided with the tank that fired us—do nothing.
            return;
        }

        // Attempt to apply damage/effect to the thing we hit
        OnHit(collision.collider);

        // Destroy the projectile
        GetComponent<NetworkObject>().Despawn();
    }

    // Called when the projectile hits another collider (server only).
    // Child classes can override to implement area‐of‐effect, apply damage to specific scripts, etc.
    // <param name="hitCollider">The collider we struck.</param>
    protected virtual void OnHit(Collider hitCollider)
    {
        Debug.Log($"[ProjectileBase] Hit {hitCollider.name} for {damage} damage.");
    }
}
