using Unity.Netcode;
using UnityEngine;

public class BoomerangProjectileBehaviour : ProjectileBase
{
    [Header("Boomerang Settings")]
    [SerializeField] private float forwardDuration = 1.0f;
    [SerializeField] private float returnSpeedMultiplier = 1.5f;
    [SerializeField] private float stopDistance = 1.0f;

    private bool isReturning = false;
    private float elapsedTime = 0f;

    protected override void InitializeMotion()
    {
        rb.linearVelocity = transform.forward * config.speed;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        elapsedTime += Time.fixedDeltaTime;

        if (!isReturning)
        {
            if (elapsedTime >= forwardDuration)
            {
                isReturning = true;
                Debug.Log("[BoomerangProjectile] Timer expired → switching to RETURN mode");
            }
        }

        if (isReturning)
        {
            if (shooterRoot == null)
            {
                GetComponent<NetworkObject>().Despawn();
                return;
            }

            Vector3 toShooter = (shooterRoot.position - transform.position).normalized;
            rb.linearVelocity = toShooter * config.speed * returnSpeedMultiplier;
            rb.MoveRotation(Quaternion.LookRotation(toShooter));

            float distanceToShooter = Vector3.Distance(transform.position, shooterRoot.position);
            if (distanceToShooter <= stopDistance)
            {
                Debug.Log("[BoomerangProjectile] Reached shooter → notifying factory and despawning");
                factoryUser?.OnProjectileReturned(weaponIndex);
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }

    // HERE IS THE KEY — we override OnCollisionEnter to STOP the base behavior from despawning!
    protected override void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        Debug.Log($"[BoomerangProjectile] OnCollisionEnter: {name} hit {collision.collider.name}");

        if (ShouldSkipTarget(collision.collider)) return;

        // Call our OnHit (apply damage + force return mode)
        OnHit(collision.collider);

        // Do NOT despawn here — we want the boomerang to fly back
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
