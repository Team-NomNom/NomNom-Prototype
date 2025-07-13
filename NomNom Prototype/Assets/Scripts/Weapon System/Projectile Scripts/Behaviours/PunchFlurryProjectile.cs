using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// El-Primo-style 4-hit melee flurry. Spawns no moving body; uses
/// four rapid box-overlaps to apply damage at close range.
/// </summary>
[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class PunchFlurryProjectile : ProjectileBase
{
    [Header("Punch Geometry")]
    [SerializeField] private Vector3 hitBoxSize = new(1.8f, 1.4f, 1.2f);
    [SerializeField] private float reach = 1.8f;   // distance forward

    [Header("Flurry Settings")]
    [SerializeField] private int punchCount = 4;
    [SerializeField] private float punchInterval = 0.15f;
    [SerializeField] private float damagePerPunch = 6f;  // 4×6 = 24 total

    [Header("Visuals (optional)")]
    [SerializeField] private GameObject punchVfxPrefab;    // spawn per punch

    /* ─── internal ─── */
    private readonly Dictionary<Collider, float> lastHitTime = new();

    protected override void InitializeMotion()
    {
        rb.linearVelocity = Vector3.zero;  // no travel
        if (IsServer) StartCoroutine(PunchCoroutine());
    }

    /* ===== punch loop ===== */
    private IEnumerator PunchCoroutine()
    {
        for (int i = 0; i < punchCount; i++)
        {
            DoSinglePunch();
            yield return new WaitForSeconds(punchInterval);
        }

        if (IsServer) GetComponent<NetworkObject>().Despawn();
    }

    private void DoSinglePunch()
    {
        Vector3 center = transform.position + transform.forward * reach * 0.5f;
        Collider[] hits = Physics.OverlapBox(
            center,
            hitBoxSize * 0.5f,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (ShouldSkipTarget(col)) continue;

            // prevent duplicate damage if collider lingers between punches
            if (!lastHitTime.TryGetValue(col, out float last) ||
                Time.time - last >= punchInterval - 0.01f)
            {
                lastHitTime[col] = Time.time;

                if (col.GetComponentInParent<IDamagable>() is IDamagable dmg)
                    dmg.TakeDamage(damagePerPunch, ownerId.Value);
            }
        }

        if (punchVfxPrefab != null)
            SpawnPunchVfxClientRpc(center);
    }

    /* ===== optional VFX RPC (one-shot, cheap) ===== */
    [ClientRpc]
    private void SpawnPunchVfxClientRpc(Vector3 pos)
    {
        if (punchVfxPrefab == null) return;
        var fx = Instantiate(punchVfxPrefab, pos, transform.rotation);
        Destroy(fx, 1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
        Vector3 center = transform.position + transform.forward * reach * 0.5f;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, hitBoxSize);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}
