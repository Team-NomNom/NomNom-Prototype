using System.Collections;
using System.Collections.Generic;
using System.Linq;                           // for ToArray()
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
public abstract class ProjectileBase : NetworkBehaviour, IProjectile
{
    protected Rigidbody rb;
    protected ProjectileConfig config;
    protected Transform shooterRoot;
    protected IProjectileFactoryUser factoryUser;
    protected int weaponIndex = -1;

    protected ulong factoryObjectId;

    public NetworkVariable<ulong> ownerId = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /* ═════════ INITIALISE ═════════ */

    public virtual void Initialize(ulong shooterId, GameObject shooterRootObj,
                                   IProjectileFactoryUser factoryUser = null,
                                   int weaponIndex = -1)
    {
        ownerId.Value = shooterId;
        shooterRoot = shooterRootObj.transform;
        this.factoryUser = factoryUser;
        this.weaponIndex = weaponIndex;

        if (factoryUser is NetworkBehaviour nb)
            factoryObjectId = nb.NetworkObject.NetworkObjectId;
    }

    public virtual void ApplyConfig(ProjectileConfig cfg) =>
        config = Instantiate(cfg);

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        /* ─── Guarantee server ownership ─── */
        if (!IsServer && IsOwner)
            SubmitOwnershipServerRpc();          // transfer to server

        /* ─── Clients: run visuals only ─── */
        if (!IsServer)
            rb.isKinematic = true;

        if (IsServer)
        {
            StartCoroutine(WaitAndInitializeMotion());
            StartCoroutine(DestroyAfterLifetime());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitOwnershipServerRpc(ServerRpcParams _ = default) { /* noop */ }

    private IEnumerator WaitAndInitializeMotion()
    {
        while (config == null) yield return null;
        InitializeMotion();
    }

    protected virtual void InitializeMotion()
    {
        if (config == null) return;

        Vector3 inheritVel = Vector3.zero;
        if (shooterRoot != null &&
            shooterRoot.TryGetComponent(out Rigidbody shooterRb))
        {
            inheritVel = shooterRb.linearVelocity;
        }
        rb.linearVelocity = inheritVel + transform.forward * config.speed;
    }

    private IEnumerator DestroyAfterLifetime()
    {
        while (config == null) yield return null;
        yield return new WaitForSeconds(config.lifetime);
        if (IsServer) { OnLifetimeExpired(); NetworkObject.Despawn(); }
    }

    protected virtual void OnLifetimeExpired() { }

    /* ═════════ Collision / Damage ═════════ */

    protected virtual void OnCollisionEnter(Collision col)
    {
        if (!IsServer) return;
        if (ShouldSkipTarget(col.collider)) return;

        OnHit(col.collider);
        NetworkObject.Despawn();
    }

    protected virtual void OnHit(Collider other)
    {
        if (ShouldSkipTarget(other)) return;
        if (other.GetComponentInParent<IDamagable>() is { } dmg)
            dmg.TakeDamage(config.damage, ownerId.Value);
    }

    /* ═════════ Hit Filtering ═════════ */

    protected virtual bool ShouldSkipTarget(Collider hit)
    {
        /* --- Self-damage flag --- */
        bool isShooter = shooterRoot != null &&
                         hit.transform.root == shooterRoot.transform;
        if (isShooter && !config.affectsOwner) return true;

        /* --- Ally fire flag --- */
        if (!config.affectsAllies && GameManagerNew.Instance != null)
        {
            var tank = hit.GetComponentInParent<NetworkTankController>();
            if (tank != null)
            {
                int shooterTeam = GameManagerNew.Instance.GetTeam(ownerId.Value);
                int targetTeam = GameManagerNew.Instance.GetTeam(tank.OwnerClientId);
                if (shooterTeam == targetTeam) return true;
            }
        }
        return false;
    }

    /* ═════════ Notify factory (boomerang, etc.) ═════════ */

    protected void NotifyFactoryProjectileReturned()
    {
        if (!IsServer) return;
        if (factoryObjectId == 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(factoryObjectId, out var obj) &&
            obj.TryGetComponent<IProjectileFactoryUser>(out var fac))
        {
            fac.OnProjectileReturned(weaponIndex);
        }
    }
}
