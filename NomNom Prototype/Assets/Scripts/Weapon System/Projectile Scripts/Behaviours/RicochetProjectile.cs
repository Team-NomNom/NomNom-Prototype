using System;
using Unity.Netcode;
using UnityEngine;

public class RicochetProjectile : ProjectileBase
{
    [Header("Physics")]
    [SerializeField] private PhysicsMaterial bounceMaterial;
    [SerializeField] private float rotationX = 0f;
    [SerializeField] private float rotationZ = 0f;
    [SerializeField] private bool lockRotation = true;

    [Header("Damage Settings")]
    [SerializeField] private float initialDamage = 10f;
    [SerializeField] private float damageFalloffPerBounce = 0.8f;

    public enum LifetimeMode { MaxLifetime, MaxBounces, Either }
    [Header("Lifetime Control")]
    [SerializeField] private LifetimeMode lifetimeMode = LifetimeMode.MaxLifetime;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private int maxBounces = 3;

    [Header("Bounce Cooldown")]
    [SerializeField] private float bounceCooldownTime = 0.1f;

    private float lifetimeTimer = 0f;
    private int currentBounces = 0;
    private float currentDamage;
    private float lastBounceTime = -1f;

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
        currentDamage = config.damage > 0f ? config.damage : initialDamage;
        lifetimeTimer = 0f;
        currentBounces = 0;
        lastBounceTime = -bounceCooldownTime;
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        rb.linearVelocity = transform.forward * config.speed;

        if (!IsServer) return;
        if (ShouldSkipTarget(collision.collider)) return;

        float timeSinceLastBounce = Time.time - lastBounceTime;
        if (timeSinceLastBounce < bounceCooldownTime)
        {
            Debug.Log($"[Ricochet] Ignored bounce (cooldown: {timeSinceLastBounce:F3}s)");
            return;
        }

        lastBounceTime = Time.time;

        if (collision.collider.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            dmg.TakeDamage(currentDamage);
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        currentDamage *= damageFalloffPerBounce;
        currentBounces++;

        Debug.Log($"[Ricochet] Bounce #{currentBounces} off {collision.collider.name} → Damage now {currentDamage:F2}");

        if (ShouldDespawn())
        {
            Debug.Log("[Ricochet] Despawning due to bounce/lifetime condition.");
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        lifetimeTimer += Time.deltaTime;

        if (ShouldDespawn())
        {
            Debug.Log("[Ricochet] Despawning due to lifetime condition.");
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private bool ShouldDespawn()
    {
        switch (lifetimeMode)
        {
            case LifetimeMode.MaxLifetime:
                return lifetimeTimer >= maxLifetime;
            case LifetimeMode.MaxBounces:
                return currentBounces >= maxBounces;
            case LifetimeMode.Either:
                return lifetimeTimer >= maxLifetime || currentBounces >= maxBounces;
            default:
                return false;
        }
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
