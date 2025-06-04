using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class ArcGrenade : ProjectileBase
{
    [Header("Arc Parameters")]
    [Tooltip("Initial muzzle velocity (units/sec).")]
    [SerializeField] private float initialSpeed = 15f;

    [Tooltip("Launch elevation angle above horizontal (in degrees).")]
    [Range(0f, 89f)]
    [SerializeField] private float launchAngle = 45f;

    [Header("Bounce Settings")]
    [Tooltip("How many times the grenade will bounce before detonating.")]
    [SerializeField] private int maxBounces = 3;

    [Tooltip("Physics material with bounciness (optional).")]
    [SerializeField] private PhysicsMaterial bounceMaterial;

    [Header("Fuse Settings")]
    [Tooltip("Seconds before the grenade automatically explodes (fuse time).")]
    [SerializeField] private float fuseTime = 3f;

    [Header("Explosion Settings")]
    [Tooltip("Damage dealt to each IDamagable in the explosion radius.")]
    [SerializeField] private float explosionDamage = 40f;

    [Tooltip("Radius (in world units) of the explosion AOE.")]
    [SerializeField] private float explosionRadius = 4f;

    [Tooltip("LayerMask to filter which colliders are damageable.")]
    [SerializeField] private LayerMask damageableLayers = ~0;

    [Header("Optional VFX")]
    [Tooltip("Explosion effect prefab (must have a NetworkObject if you want it networked).")]
    [SerializeField] private GameObject explosionVfxPrefab;

    // Internal state
    private int bounceCount = 0;
    private bool hasExploded = false;

    // Cached reference (ProjectileBase already assigns rb in its OnNetworkSpawn)
    // protected Rigidbody rb; // inherited from ProjectileBase

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        // Assign the PhysicMaterial (if provided) to all child colliders
        if (bounceMaterial != null)
        {
            var cols = GetComponentsInChildren<Collider>(false);
            foreach (var col in cols)
            {
                col.material = bounceMaterial;
            }
        }

        // 1) Compute initial velocity from launchAngle & initialSpeed
        //    Decompose into horizontal and vertical components
        float rad = launchAngle * Mathf.Deg2Rad;
        Vector3 forwardDir = transform.forward;
        Vector3 horizontalDir = new Vector3(forwardDir.x, 0f, forwardDir.z).normalized;

        // Vertical component = v * sin(angle)
        float vY = initialSpeed * Mathf.Sin(rad);
        // Horizontal speed magnitude = v * cos(angle)
        float vH = initialSpeed * Mathf.Cos(rad);

        // Final velocity vector:
        Vector3 launchVelocity = horizontalDir * vH + Vector3.up * vY;
        rb.linearVelocity = launchVelocity;

        // 2) Start the fuse countdown
        StartCoroutine(FuseCountdown());
    }

    private IEnumerator FuseCountdown()
    {
        yield return new WaitForSeconds(fuseTime);
        if (IsServer && !hasExploded)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasExploded)
            return;

        // 0) Skip if we hit our owner
        var hitNetObj = collision.collider.GetComponentInParent<NetworkObject>();
        if (hitNetObj != null && hitNetObj.NetworkObjectId == ownerId.Value)
            return;

        // 1) On contact with an IDamagable (not the owner), apply small direct damage
        var damagable = collision.collider.GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            // 20% of explosionDamage on direct hit
            damagable.TakeDamage(explosionDamage * 0.2f);
        }

        // 2) Count this bounce; explode once we exceed maxBounces
        bounceCount++;
        if (bounceCount >= maxBounces)
        {
            Explode();
        }
        // Otherwise, physics handles the bounce (bounciness given by PhysicMaterial)
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        Vector3 center = transform.position;

        // AOE damage: overlap sphere, then call TakeDamage on each IDamagable
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, damageableLayers, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            var dmg = col.GetComponentInParent<IDamagable>();
            if (dmg != null)
            {
                dmg.TakeDamage(explosionDamage);
            }
        }

        // Spawn explosion VFX (if assigned)
        if (explosionVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(explosionVfxPrefab, center, Quaternion.identity);
            var vfxNetObj = vfxInstance.GetComponent<NetworkObject>();
            if (vfxNetObj != null)
            {
                vfxNetObj.Spawn();
            }
            else
            {
                Debug.LogWarning("[ArcGrenade] explosionVfxPrefab has no NetworkObject component.");
            }
        }

        // Despawn this grenade from the network
        GetComponent<NetworkObject>().Despawn();
    }

    // Optional: draw the predicted max height in the editor for debugging
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Show the maximum height as a small sphere, if in edit mode
        // maxHeight = (vY^2)/(2*g), where vY = initialSpeed * sin(launchAngle)
        float g = Physics.gravity.y; // negative value, e.g. -9.81
        float vY = initialSpeed * Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        float maxH = (vY * vY) / (2f * -g);
        Vector3 topPoint = transform.position + Vector3.up * maxH;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(topPoint, 0.3f);
    }
#endif
}
