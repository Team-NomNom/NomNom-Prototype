using System;
using Unity.Netcode;
using UnityEngine;

public class RicochetProjectile : ProjectileBase
{
    [Header("Bounce Settings")]
    [SerializeField] private PhysicsMaterial bounceMaterial;
    [SerializeField] private float initialDamage = 10f;
    [SerializeField] private float damageFalloffPerBounce = 0.8f;
    [SerializeField] private float rotationX = 0f;
    [SerializeField] private float rotationZ = 0f;

    public enum LifetimeMode { MaxLifetime, MaxBounces, Either }

    [Header("Lifetime Settings")]
    [SerializeField] private LifetimeMode lifetimeMode = LifetimeMode.MaxLifetime;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private int maxBounces = 3;

    [Header("Visuals")]
    [SerializeField] private GameObject sparkEffectPrefab;

    private int currentBounces = 0;
    private float lifetimeTimer = 0f;
    private bool lockRotation = true;
    private float currentDamage;

    private float bounceCooldown = 0.05f; // Minimum time between valid bounces
    private float lastBounceTime = -999f; // Initialized to long ago


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
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        rb.linearVelocity = transform.forward * config.speed;

        if (!IsServer) return;

        // Prevent multiple bounces in same frame or close succession
        if (Time.time - lastBounceTime < bounceCooldown) return;
        lastBounceTime = Time.time;

        if (ShouldSkipTarget(collision.collider)) return;

        ContactPoint contact = collision.GetContact(0);
        SpawnSparkEffectClientRpc(contact.point, contact.normal);

        if (collision.collider.GetComponentInParent<IDamagable>() is IDamagable dmg)
        {
            dmg.TakeDamage(currentDamage);
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        currentDamage *= damageFalloffPerBounce;
        currentBounces++;

        Debug.Log($"[Ricochet] Bounce #{currentBounces} | Damage now: {currentDamage}");

        if (ShouldDespawn())
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }



    private void Update()
    {
        if (!IsServer) return;

        lifetimeTimer += Time.deltaTime;

        if (ShouldDespawn())
        {
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

    [ClientRpc]
    private void SpawnSparkEffectClientRpc(Vector3 position, Vector3 normal)
    {
        if (sparkEffectPrefab == null) return;

        Quaternion rot = Quaternion.LookRotation(normal);
        GameObject spark = Instantiate(sparkEffectPrefab, position, rot);
        Destroy(spark, 1f);
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
