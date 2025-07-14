using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
public class MistingCloudProjectile : ProjectileBase
{
    /* Geometry -------------------------------------------------- */
    [Header("Row & Puff Layout")]
    [SerializeField] private int rowCount = 3;
    [SerializeField] private float rowSpacing = 1.5f;
    [SerializeField] private float puffSpacing = 1.5f;
    [SerializeField] private Vector3 puffSize = new(1.2f, 1.2f, 1.2f);

    /* Drift / lifetime ----------------------------------------- */
    [Header("Drift")]
    [SerializeField] private float driftSpeed = 2f;
    [Header("Damage Tick")]
    [SerializeField] private float tickInterval = 0.33f;
    [SerializeField] private float puffLifetime = 1f;

    /* Visuals --------------------------------------------------- */
    [Header("Visuals (optional)")]
    [SerializeField] private GameObject puffVfxPrefab;

    private struct Puff { public Vector3 basePos; }
    private readonly List<Puff> puffs = new();
    private readonly Dictionary<Collider, float> lastHit = new();

    private float spawnTime;
    private Vector3 forwardDir;

    /* ===== Initialisation ===== */
    protected override void InitializeMotion()
    {
        rb.linearVelocity = Vector3.zero;
        forwardDir = transform.forward;
        BuildSprayGeometry();
        spawnTime = Time.time;

        if (IsServer)
        {
            StartCoroutine(DamageTickCoroutine());
            StartCoroutine(LifetimeCoroutine(puffLifetime));
        }
    }

    /* ---- geometry ---- */
    private void BuildSprayGeometry()
    {
        for (int row = 0; row < rowCount; row++)
        {
            int puffInRow = row + 1;
            float fwd = rowSpacing * (row + 1);

            for (int i = 0; i < puffInRow; i++)
            {
                float lateralIdx = i - (puffInRow - 1) / 2f;
                float lateral = lateralIdx * puffSpacing;

                Vector3 pos = transform.position +
                              forwardDir * fwd +
                              transform.right * lateral;

                puffs.Add(new Puff { basePos = pos });

                /* Visuals only once from server */
                if (IsServer) SpawnPuffVfx(pos);
            }
        }
    }

    private void SpawnPuffVfx(Vector3 pos)
    {
        if (puffVfxPrefab == null) return;
        SpawnPuffVfxClientRpc(pos, transform.rotation);
    }

    [ClientRpc]
    private void SpawnPuffVfxClientRpc(Vector3 pos, Quaternion rot)
    {
        if (puffVfxPrefab == null) return;
        var fx = Instantiate(puffVfxPrefab, pos, rot);
        Destroy(fx, puffLifetime);
    }

    /* ---- Damage ---- */
    private IEnumerator DamageTickCoroutine()
    {
        float elapsed = 0;
        while (elapsed < puffLifetime)
        {
            DealDamageOnce();
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
    }

    private void DealDamageOnce()
    {
        float drift = driftSpeed * (Time.time - spawnTime);

        foreach (var puff in puffs)
        {
            Vector3 centre = puff.basePos + forwardDir * drift;

            var hits = Physics.OverlapBox(
                centre, puffSize * 0.5f, transform.rotation,
                ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                if (ShouldSkipTarget(col)) continue;

                if (!lastHit.TryGetValue(col, out var last) ||
                    Time.time - last >= tickInterval - 0.01f)
                {
                    lastHit[col] = Time.time;

                    if (col.GetComponentInParent<IDamagable>() is { } dmg)
                        dmg.TakeDamage(config.damage, ownerId.Value);
                }
            }
        }
    }

    /* ---- Lifetime ---- */
    private IEnumerator LifetimeCoroutine(float secs)
    {
        yield return new WaitForSeconds(secs);
        if (IsServer) NetworkObject.Despawn();
    }
}
