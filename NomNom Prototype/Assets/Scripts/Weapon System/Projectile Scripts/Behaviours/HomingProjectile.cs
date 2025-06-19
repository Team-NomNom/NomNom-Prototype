using UnityEngine;
using Unity.Netcode;

public class HomingProjectile : ProjectileBase
{
    [Header("Homing Settings")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float turnSpeed = 4f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Transform visualTransform;

    private Transform currentTarget;

    protected override void InitializeMotion()
    {
        // Ensure missile launches in the direction it's facing
        Vector3 launchDir = transform.forward;

        rb.linearVelocity = launchDir * config.speed;

        if (visualTransform != null)
            visualTransform.rotation = Quaternion.LookRotation(launchDir);
        else
            rb.rotation = Quaternion.LookRotation(launchDir);

        // Initial target acquisition
        AcquireTarget();
    }


    private void FixedUpdate()
    {
        if (!IsServer || config == null) return;

        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            AcquireTarget();
        }

        if (currentTarget != null)
        {
            Vector3 toTarget = (currentTarget.position - transform.position).normalized;
            Vector3 newDir = Vector3.RotateTowards(rb.linearVelocity.normalized, toTarget, turnSpeed * Time.fixedDeltaTime, 0f);
            rb.linearVelocity = newDir * config.speed;

            // Optional: rotate visuals toward direction
            if (visualTransform != null)
                visualTransform.rotation = Quaternion.LookRotation(newDir);
            else
                rb.MoveRotation(Quaternion.LookRotation(newDir));
        }
    }

    private void AcquireTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayers);
        float closestDistance = float.MaxValue;
        Transform bestTarget = null;

        foreach (var hit in hits)
        {
            // Must have a NetworkTankController in parent
            var tank = hit.GetComponentInParent<NetworkTankController>();
            if (tank == null) continue;

            // Skip self (owner of projectile)
            if (shooterRoot != null && tank.transform.root == shooterRoot.transform.root)
                continue;

            // skip dead tanks or non-players
            // if (!tank.IsAlive) continue;

            float distance = Vector3.Distance(transform.position, tank.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = tank.transform;
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            Debug.Log($"[HomingMissile] Acquired target: {currentTarget.name}");
        }
        else
        {
            Debug.Log("[HomingMissile] No valid enemy targets found.");
        }
    }


    protected override void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        Debug.Log($"[HomingMissile] Hit {collision.collider.name}");

        if (ShouldSkipTarget(collision.collider)) return;

        OnHit(collision.collider);
        GetComponent<NetworkObject>().Despawn();
    }

    protected override void OnHit(Collider other)
    {
        if (ShouldSkipTarget(other)) return;

        if (other.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            dmg.TakeDamage(config.damage);
        }

        if (config.hitEffectPrefab)
        {
            Instantiate(config.hitEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}
