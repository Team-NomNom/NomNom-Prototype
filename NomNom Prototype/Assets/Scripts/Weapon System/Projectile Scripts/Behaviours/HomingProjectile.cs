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
            var tank = hit.GetComponentInParent<NetworkTankController>();
            if (tank == null) continue;

            // ——— 1.  Skip the shooter itself ———————————————
            if (shooterRoot != null && tank.transform.root == shooterRoot.transform.root)
                continue;

            // ——— 2.  Skip same-team tanks ———
            if (GameManagerNew.Instance != null)
            {
                int shooterTeam = GameManagerNew.Instance.GetTeam(ownerId.Value);
                int targetTeam = GameManagerNew.Instance.GetTeam(tank.OwnerClientId);
                if (shooterTeam == targetTeam)                  // <<<  new line
                    continue;                                   // teammates are ignored
            }

            // ——— 3.  Pick the closest remaining tank ———
            float distance = Vector3.Distance(transform.position, tank.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = tank.transform;
            }
        }

        currentTarget = bestTarget;
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
            dmg.TakeDamage(config.damage, ownerId.Value);
        }

        if (config.hitEffectPrefab)
        {
            Instantiate(config.hitEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}
