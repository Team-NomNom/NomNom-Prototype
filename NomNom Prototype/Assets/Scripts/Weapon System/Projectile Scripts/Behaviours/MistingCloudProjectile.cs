using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Emz-style spray: 3 forward rows (1-2-3 puffs) that drift outward,
/// ticking damage while enemies remain inside the cloud.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
public class MistingCloudProjectile : ProjectileBase
{
    /* ───────────── Geometry ───────────── */
    [Header("Row & Puff Layout")]
    [SerializeField] private int rowCount = 3;          // rows: 1,2,3
    [SerializeField] private float rowSpacing = 1.5f;       // distance between rows
    [SerializeField] private float puffSpacing = 1.5f;       // left/right gap in a row
    [SerializeField] private Vector3 puffSize = new(1.2f, 1.2f, 1.2f);

    /* ───────────── Drift ───────────── */
    [Header("Drift")]
    [SerializeField] private float driftSpeed = 2f;            // metres per second

    /* ───────────── Damage Tick ───────────── */
    [Header("Damage Tick")]
    [SerializeField] private float tickInterval = 0.33f;
    [SerializeField] private float puffLifetime = 1.0f;        // how long each puff lasts

    /* ───────────── Visuals ───────────── */
    [Header("Visuals (optional)")]
    [SerializeField] private GameObject puffVfxPrefab;

    /* ───────────── Internals ───────────── */
    private struct Puff { public Vector3 basePos; }
    private readonly List<Puff> puffs = new();
    private readonly Dictionary<Collider, float> lastHit = new();

    private float spawnTime;
    private Vector3 forwardDir;

    /* ═════════════════ Initialization ═════════════════ */
    protected override void InitializeMotion()
    {
        rb.linearVelocity = Vector3.zero;      // spray stays attached logically
        forwardDir = transform.forward; // snapshot shooter’s facing dir
        BuildSprayGeometry();
        spawnTime = Time.time;

        if (IsServer)
        {
            StartCoroutine(DamageTickCoroutine());
            StartCoroutine(LifetimeCoroutine(puffLifetime));
        }
    }

    /* ═════════════════ Geometry helpers ═════════════════ */
    private void BuildSprayGeometry()
    {
        for (int row = 0; row < rowCount; row++)
        {
            int puffsThisRow = row + 1;                   // 1-2-3
            float forward = rowSpacing * (row + 1);    // metres ahead

            for (int i = 0; i < puffsThisRow; i++)
            {
                // Center each row (indices −1…0…+1, etc.)
                float offsetIdx = i - (puffsThisRow - 1) / 2f;
                float lateral = offsetIdx * puffSpacing;

                Vector3 worldPos = transform.position
                                   + forwardDir * forward
                                   + transform.right * lateral;

                puffs.Add(new Puff { basePos = worldPos });
                SpawnPuffVfx(worldPos);
            }
        }
    }

    private void SpawnPuffVfx(Vector3 pos)
    {
        if (puffVfxPrefab == null) return;
        var vfx = Instantiate(puffVfxPrefab, pos, transform.rotation);
        Destroy(vfx, puffLifetime);
    }

    /* ═════════════════ Damage logic ═════════════════ */
    private IEnumerator DamageTickCoroutine()
    {
        float elapsed = 0f;
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
            Vector3 center = puff.basePos + forwardDir * drift;

            Collider[] hits = Physics.OverlapBox(
                center, puffSize / 2f, transform.rotation,
                ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                if (ShouldSkipTarget(col)) continue;

                // throttle per-collider ticks
                if (!lastHit.TryGetValue(col, out var last) ||
                    Time.time - last >= tickInterval - 0.01f)
                {
                    lastHit[col] = Time.time;

                    if (col.GetComponentInParent<IDamagable>() is IDamagable dmg)
                        dmg.TakeDamage(config.damage, ownerId.Value);
                }
            }
        }
    }

    /* ═════════════════ Lifetime & despawn ═════════════════ */
    private IEnumerator LifetimeCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (IsServer) GetComponent<NetworkObject>().Despawn();
    }

    /* ═════════════════ Gizmos (editor) ═════════════════ */
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.15f);
        float drift = Application.isPlaying ? driftSpeed * (Time.time - spawnTime) : 0f;
        Vector3 dir = Application.isPlaying ? forwardDir : transform.forward;

        for (int row = 0; row < rowCount; row++)
        {
            int puffsThisRow = row + 1;
            float fwd = rowSpacing * (row + 1);

            for (int i = 0; i < puffsThisRow; i++)
            {
                float idx = i - (puffsThisRow - 1) / 2f;
                float lateral = idx * puffSpacing;
                Vector3 center = transform.position + dir * (fwd + drift) + transform.right * lateral;

                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, puffSize);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }
        }
    }
#endif
}
