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

    protected override void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasExploded) return;

        var netObj = collision.collider.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.NetworkObjectId == ownerId.Value && !config.affectsOwner)
            return;

        if (collision.collider.TryGetComponent<IDamagable>(out var dmg))
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

        Debug.Log($"[ArcGrenade] Exploding at {transform.position} with radius {explosionRadius}");

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageableLayers);
        foreach (var hit in hits)
        {
            Debug.Log($"[ArcGrenade] Explosion hit {hit.name}");

            if (hit.GetComponentInParent<IDamagable>() is IDamagable dmg)
            {
                Debug.Log($"[ArcGrenade] Dealing {config.damage} to {hit.name}");
                dmg.TakeDamage(config.damage);
            }
        }

        if (explosionVfxPrefab)
        {
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        GetComponent<NetworkObject>().Despawn();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 1f); 
        Gizmos.DrawSphere(transform.position, explosionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif

}
