using Unity.Netcode;
using UnityEngine;

public class BoomerangProjectileBehaviour : ProjectileBase
{
    [Header("Boomerang Settings")]
    [SerializeField] private float forwardDuration = 1.0f;
    [SerializeField] private float returnSpeedMultiplier = 1.5f;
    [SerializeField] private float stopDistance = 1.0f;

    [Header("Optional Visual Flip Fix")]
    [Tooltip("Optional: If your mesh faces backwards, apply this offset.")]
    [SerializeField] private Vector3 visualRotationOffset = new Vector3(0f, 180f, 0f);

    [Tooltip("Assign if you want to rotate visuals separately from physics")]
    [SerializeField] private Transform visualTransform;

    private bool isReturning = false;
    private float elapsedTime = 0f;

    protected override void InitializeMotion()
    {
        rb.linearVelocity = transform.forward * config.speed;
    }

    private void FixedUpdate()
    {
        // ✅ Only the server controls movement and return logic
        if (!IsServer) return;

        elapsedTime += Time.fixedDeltaTime;

        if (!isReturning && elapsedTime >= forwardDuration)
        {
            isReturning = true;
            Debug.Log("[BoomerangProjectile] Timer expired → switching to RETURN mode");
        }

        if (isReturning)
        {
            if (shooterRoot == null)
            {
                Debug.LogWarning("[BoomerangProjectile] Shooter root missing — despawning.");
                GetComponent<NetworkObject>().Despawn();
                return;
            }

            Vector3 toShooter = (shooterRoot.position - transform.position).normalized;
            rb.linearVelocity = toShooter * config.speed * returnSpeedMultiplier;

            // ✅ Flip fix: rotate visual instead of Rigidbody if provided
            Quaternion desiredRotation = Quaternion.LookRotation(toShooter);
            if (visualTransform != null)
            {
                visualTransform.rotation = desiredRotation * Quaternion.Euler(visualRotationOffset);
            }
            else
            {
                rb.MoveRotation(desiredRotation * Quaternion.Euler(visualRotationOffset));
            }

            float distanceToShooter = Vector3.Distance(transform.position, shooterRoot.position);
            if (distanceToShooter <= stopDistance)
            {
                Debug.Log("[BoomerangProjectile] Reached shooter → notifying factory and despawning");
                NotifyFactoryProjectileReturned(); // ✅ Server-authoritative
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        Debug.Log($"[BoomerangProjectile] OnCollisionEnter: {name} hit {collision.collider.name}");

        if (ShouldSkipTarget(collision.collider)) return;

        OnHit(collision.collider);

        if (!isReturning)
        {
            isReturning = true;
            Debug.Log("[BoomerangProjectile] Hit target → switching to RETURN mode");
        }
    }

    protected override void OnHit(Collider other)
    {
        if (ShouldSkipTarget(other)) return;

        if (other.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            Debug.Log($"[BoomerangProjectile] {gameObject.name} applied {config.damage} damage to {other.name}");
            dmg.TakeDamage(config.damage);

            if (config.hitEffectPrefab != null)
            {
                Instantiate(config.hitEffectPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
