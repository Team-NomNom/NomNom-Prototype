using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class ProjectileBase : NetworkBehaviour, IProjectile
{
    protected Rigidbody rb;
    protected ProjectileConfig config;
    protected Transform shooterRoot;

    public NetworkVariable<ulong> ownerId = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public virtual void Initialize(ulong shooterId, GameObject shooterRootObj)
    {
        ownerId.Value = shooterId;
        shooterRoot = shooterRootObj.transform;
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
}
