using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A basic projectile that just flies straight ahead (inherited from ProjectileBase).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleProjectile : ProjectileBase
{
    [Header("Simple Projectile Extras")]
    [Tooltip("Name of particle system prefab to spawn on hit (registered in Resources or assigned elsewhere).")]
    [SerializeField] private GameObject hitEffectPrefab;

    protected override void OnHit(Collider hitCollider)
    {
        // Example: deal damage if the thing we hit has an IDamagable interface
        var damagable = hitCollider.GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            damagable.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning($"OnHit: Could not find IDamagable on {hitCollider.name} or its parents.");
        }

        // Spawn hit effect on the server so clients see it
        if (hitEffectPrefab != null)
        {
            GameObject vfx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            var vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null)
            {
                vfxNetObj.Spawn();
            }
            else
            {
                Debug.LogWarning("[SimpleProjectile] hitEffectPrefab needs a NetworkObject if you want it networked.");
            }
        }

        // Finally, call the base so it logs debug if you want
        base.OnHit(hitCollider);
    }

    protected override void OnLifetimeExpired()
    {
        // If you want a “vanish” effect when the bullet times out:
        if (hitEffectPrefab != null)
        {
            GameObject vfx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            var vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null)
                vfxNetObj.Spawn();
        }
        base.OnLifetimeExpired();
    }
}
