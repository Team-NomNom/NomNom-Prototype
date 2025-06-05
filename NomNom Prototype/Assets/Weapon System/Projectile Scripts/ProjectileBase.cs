using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class ProjectileBase : NetworkBehaviour, IProjectile
{
    protected Rigidbody rb;
    protected ProjectileConfig config;

    public NetworkVariable<ulong> ownerId = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public virtual void Initialize(ulong shooterId)
    {
        ownerId.Value = shooterId;
    }

    public virtual void ApplyConfig(ProjectileConfig cfg)
    {
        config = Instantiate(cfg); // Clone to avoid shared SO issues
        Debug.Log($"[ProjectileBase] Config applied to {gameObject.name} | Speed={config.speed}, Lifetime={config.lifetime}, AffectsOwner={config.affectsOwner}");
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
        if (config == null)
        {
            Debug.LogError($"[ProjectileBase] InitializeMotion failed on {gameObject.name}: config was null.");
            return;
        }

        rb.linearVelocity = transform.forward * config.speed;
        Debug.Log($"[ProjectileBase] {gameObject.name} velocity set to {rb.linearVelocity}");
    }

    private IEnumerator DestroyAfterLifetime()
    {
        float timeout = 2f;
        float waited = 0f;

        while (config == null && waited < timeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (config == null)
        {
            Debug.LogError($"[ProjectileBase] {gameObject.name} config never set. Despawning.");
            GetComponent<NetworkObject>().Despawn();
            yield break;
        }

        yield return new WaitForSeconds(config.lifetime);

        if (IsServer)
        {
            OnLifetimeExpired();
            GetComponent<NetworkObject>().Despawn();
        }
    }

    protected virtual void OnLifetimeExpired()
    {
        Debug.Log($"[ProjectileBase] {gameObject.name} expired after {config.lifetime} seconds.");
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        var netObj = collision.collider.GetComponentInParent<NetworkObject>();
        ulong hitId = netObj?.NetworkObjectId ?? 999999;

        Debug.Log($"[ProjectileBase] {gameObject.name} hit {collision.collider.name} | Owner={ownerId.Value} | HitId={hitId} | AffectsOwner={config?.affectsOwner}");

        // Skip owner if not supposed to damage them
        if (netObj != null && netObj.NetworkObjectId == ownerId.Value)
        {
            if (!config.affectsOwner)
            {
                Debug.Log($"[ProjectileBase] {gameObject.name} hit owner but affectsOwner == false → no damage");
                return;
            }
            else
            {
                Debug.Log($"[ProjectileBase] {gameObject.name} hit owner and affectsOwner == true → proceed with damage");
            }
        }

        OnHit(collision.collider);

        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    protected virtual void OnHit(Collider other)
    {
        if (other.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            Debug.Log($"[ProjectileBase] {gameObject.name} dealt {config.damage} damage to {other.name}");
            dmg.TakeDamage(config.damage);
        }
        else
        {
            Debug.LogWarning($"[ProjectileBase] {gameObject.name} hit {other.name} but no IDamagable found in parent.");
        }
    }
}
