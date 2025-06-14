using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class ProjectileBase : NetworkBehaviour, IProjectile
{
    protected Rigidbody rb;
    protected ProjectileConfig config;
    protected Transform shooterRoot;
    protected IProjectileFactoryUser factoryUser;
    protected int weaponIndex = -1;

    protected ulong factoryObjectId = 0;

    public NetworkVariable<ulong> ownerId = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public virtual void Initialize(ulong shooterId, GameObject shooterRootObj, IProjectileFactoryUser factoryUser = null, int weaponIndex = -1)
    {
        ownerId.Value = shooterId;
        shooterRoot = shooterRootObj.transform;
        this.factoryUser = factoryUser;
        this.weaponIndex = weaponIndex;

        if (factoryUser is NetworkBehaviour netBehaviour)
            factoryObjectId = netBehaviour.NetworkObject.NetworkObjectId;
        else
            Debug.LogWarning("[ProjectileBase] Could not assign factoryObjectId!");
    }

    public virtual void ApplyConfig(ProjectileConfig cfg)
    {
        config = Instantiate(cfg);
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (IsServer)
        {
            StartCoroutine(WaitAndInitializeMotion());
            StartCoroutine(DestroyAfterLifetime());
        }
    }

    private IEnumerator WaitAndInitializeMotion()
    {
        while (config == null)
            yield return null;

        InitializeMotion();
    }

    protected virtual void InitializeMotion()
    {
        if (config == null) return;
        Vector3 shooterVelocity = Vector3.zero;

        if (shooterRoot != null && shooterRoot.TryGetComponent<Rigidbody>(out var shooterRb))
        {
            shooterVelocity = shooterRb.linearVelocity;
        }

        rb.linearVelocity = shooterVelocity + transform.forward * config.speed;
    }

    private IEnumerator DestroyAfterLifetime()
    {
        while (config == null)
            yield return null;

        yield return new WaitForSeconds(config.lifetime);

        if (IsServer)
        {
            OnLifetimeExpired();
            GetComponent<NetworkObject>().Despawn();
        }
    }

    protected virtual void OnLifetimeExpired() { }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        Debug.Log($"[ProjectileBase] OnCollisionEnter: {name} hit {collision.collider.name}");

        if (ShouldSkipTarget(collision.collider)) return;

        OnHit(collision.collider);
        GetComponent<NetworkObject>().Despawn();
    }

    protected virtual void OnHit(Collider other)
    {
        if (ShouldSkipTarget(other)) return;

        if (other.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            Debug.Log($"[ProjectileBase] {gameObject.name} applied {config.damage} damage to {other.name}");
            dmg.TakeDamage(config.damage);
        }
        else
        {
            Debug.LogWarning($"[ProjectileBase] {gameObject.name} hit {other.name} but no IDamagable found.");
        }
    }

    protected bool ShouldSkipTarget(Collider hit)
    {
        var isShooter = shooterRoot != null && hit.transform.root == shooterRoot.transform;
        Debug.Log($"[Debug] Hit={hit.name}, Root={hit.transform.root.name}, Shooter={shooterRoot?.name}, IsShooter={isShooter}, AffectsOwner={config?.affectsOwner}");

        if (isShooter && !config.affectsOwner)
        {
            Debug.Log("[Debug] Skipping damage to self.");
            return true;
        }
        return false;
    }

    protected void NotifyFactoryProjectileReturned()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ProjectileBase] NotifyFactoryProjectileReturned called on client — ignoring");
            return;
        }

        Debug.Log($"[ProjectileBase] Notifying factory (ObjectId: {factoryObjectId}) for weaponIndex={weaponIndex}");

        if (factoryObjectId == 0)
        {
            Debug.LogWarning("[ProjectileBase] FactoryObjectId is 0 → not set?");
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(factoryObjectId, out var netObj))
        {
            Debug.Log("[ProjectileBase] Found factory NetworkObject!");

            if (netObj.TryGetComponent<IProjectileFactoryUser>(out var factory))
            {
                factory.OnProjectileReturned(weaponIndex);
                Debug.Log("[ProjectileBase] Successfully called OnProjectileReturned!");
            }
            else
            {
                Debug.LogWarning("[ProjectileBase] NetworkObject has no IProjectileFactoryUser!");
            }
        }
        else
        {
            Debug.LogWarning("[ProjectileBase] Could not find NetworkObject by factoryObjectId!");
        }
    }
}
