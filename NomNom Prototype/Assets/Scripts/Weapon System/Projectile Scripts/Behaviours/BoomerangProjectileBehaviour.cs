using Unity.Netcode;
using UnityEngine;

public class BoomerangProjectileBehaviour: ProjectileBase
{
    [Header("Boomerang Settings")]
    [SerializeField] private float forwardDuration = 1.0f;  // seconds moving forward
    [SerializeField] private float returnSpeedMultiplier = 1.5f;  // speed when returning
    [SerializeField] private float stopDistance = 1.0f; // how close to shooter before despawning

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
            // Forward phase
            if (elapsedTime >= forwardDuration)
            {
                isReturning = true;
                Debug.Log("[BoomerangProjectile] Switching to RETURN mode");
            }
        }

        if (isReturning)
        {
            if (shooterRoot == null)
            {
                // No valid shooter → just self-destruct
                GetComponent<NetworkObject>().Despawn();
                return;
            }

            Vector3 toShooter = (shooterRoot.position - transform.position).normalized;
            rb.linearVelocity = toShooter * config.speed * returnSpeedMultiplier;

            // Optionally rotate to face shooter
            rb.MoveRotation(Quaternion.LookRotation(toShooter));

            // Stop if close enough to shooter
            float distanceToShooter = Vector3.Distance(transform.position, shooterRoot.position);
            if (distanceToShooter <= stopDistance)
            {
                Debug.Log("[BoomerangProjectile] Reached shooter → despawning");
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }

    protected override void OnHit(Collider other)
    {
        if (ShouldSkipTarget(other)) return;

        if (other.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            Debug.Log($"[BoomerangProjectile] {gameObject.name} applied {config.damage} damage to {other.name}");
            dmg.TakeDamage(config.damage);

            // Spawn hit effect if assigned
            if (config.hitEffectPrefab != null)
            {
                Instantiate(config.hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // Optional: despawn immediately on hit (only during return phase if you want)
            // if (isReturning)
            //     GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            Debug.LogWarning($"[BoomerangProjectile] {gameObject.name} hit {other.name} but no IDamagable found.");
        }
    }
}
