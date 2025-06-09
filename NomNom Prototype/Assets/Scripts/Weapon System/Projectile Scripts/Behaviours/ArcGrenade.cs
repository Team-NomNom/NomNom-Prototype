using UnityEngine;
using Unity.Netcode;

public class ArcGrenade : ProjectileBase
{
    [SerializeField] private int maxBounces = 3;
    [SerializeField] private PhysicsMaterial bounceMaterial;
    [SerializeField] private GameObject explosionVfxPrefab;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private LayerMask damageableLayers = ~0;

    private int bounceCount = 0;
    private bool hasExploded = false;

    protected override void InitializeMotion()
    {
        if (bounceMaterial != null)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
                col.material = bounceMaterial;
        }

        float angle = 45f * Mathf.Deg2Rad;
        Vector3 dir = transform.forward;
        float vY = config.speed * Mathf.Sin(angle);
        float vXZ = config.speed * Mathf.Cos(angle);
        rb.linearVelocity = dir.normalized * vXZ + Vector3.up * vY;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasExploded) return;

        if (ShouldSkipTarget(collision.collider)) return;

        if (collision.collider.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            dmg.TakeDamage(config.damage * 0.2f);
        }

        bounceCount++;
        if (bounceCount >= maxBounces)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageableLayers);
        foreach (var hit in hits)
        {
            if (ShouldSkipTarget(hit)) continue;

            if (hit.GetComponentInParent<IDamagable>() is IDamagable dmg)
            {
                dmg.TakeDamage(config.damage);
            }
        }

        if (explosionVfxPrefab)
        {
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        GetComponent<NetworkObject>().Despawn();
    }
}
