using System;
using Unity.Netcode;
using UnityEngine;
public class RicochetProjectile : ProjectileBase
{
    [SerializeField] private PhysicsMaterial bounceMaterial;
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private float rotationX = 0f;
    [SerializeField] private float rotationZ = 0f;

    private bool lockRotation = true;
    private float hitDamage;
    // Inherits all behavior from ProjectileBase
    // Add debug logging to confirm it uses updated base
    private void Start()
    {
        Debug.Log("[Ricochet] Initialized with base behavior");
    }

    protected override void InitializeMotion()
    {
        if (bounceMaterial != null)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.material = bounceMaterial;
            }
        }
        rb.linearVelocity = transform.forward * config.speed;
        hitDamage = config.damage;
    }
    protected virtual void OnCollisionEnter(Collision collision)
    {
        rb.linearVelocity = transform.forward * config.speed;
        
        if (!IsServer) return;

        if (ShouldSkipTarget(collision.collider)) return;

        if (collision.collider.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            dmg.TakeDamage(config.damage);
            GetComponent<NetworkObject>().Despawn();
        }

        hitDamage *= damageMultiplier;
    }

    private void LateUpdate()
    {
        if (lockRotation)
        {
            float currentY = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(rotationX, currentY, rotationZ);
        }
    }
}