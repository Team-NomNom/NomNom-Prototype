using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HomingProjectile : ProjectileBase
{
    [SerializeField] private float turnSpeed = 90f;
    private Transform target;

    public void SetTarget(Transform tgt)
    {
        target = tgt;
    }

    protected override void InitializeMotion()
    {
        rb.linearVelocity = transform.forward * config.speed;
    }

    private void FixedUpdate()
    {
        if (!IsServer || target == null) return;

        Vector3 toTarget = (target.position - transform.position).normalized;
        Vector3 newDir = Vector3.RotateTowards(transform.forward, toTarget, turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
        rb.linearVelocity = newDir * config.speed;
        rb.MoveRotation(Quaternion.LookRotation(newDir));
    }
}