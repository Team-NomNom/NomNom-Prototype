using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// El-Primo-style 4-hit melee combo that follows the shooter’s movement.
/// </summary>
[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class PunchFlurryProjectile : ProjectileBase
{
    [Header("Punch Geometry")]
    [SerializeField] private Vector3 hitBoxSize = new(1.8f, 1.4f, 1.2f);
    [SerializeField] private float reach = 1.8f;   // centre of box from shooter

    [Header("Flurry Settings")]
    [SerializeField] private int punchCount = 4;
    [SerializeField] private float punchInterval = 0.15f;
    [SerializeField] private float damagePerPunch = 6f;

    [Header("Visuals")]
    [SerializeField] private GameObject punchVfxPrefab;

    private readonly Dictionary<Collider, float> lastHitTime = new();

    /* ═════════════════ INITIALISE ═════════════════ */
    protected override void InitializeMotion()
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        if (IsServer)
            StartCoroutine(PunchCoroutine());
    }

    /* ═════════════════ MAIN LOOP ═════════════════ */
    private IEnumerator PunchCoroutine()
    {
        for (int i = 0; i < punchCount; i++)
        {
            DoSinglePunch();
            yield return new WaitForSeconds(punchInterval);
        }

        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
    }

    /* ═════════════════ SINGLE PUNCH ═════════════════ */
    private void DoSinglePunch()
    {
        if (shooterRoot == null) return;   // safety

        Vector3 center = shooterRoot.position + shooterRoot.forward * reach * 0.5f;
        Quaternion rot = shooterRoot.rotation;

        Collider[] hits = Physics.OverlapBox(
            center,
            hitBoxSize * 0.5f,
            rot,
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (ShouldSkipTarget(col)) continue;

            if (!lastHitTime.TryGetValue(col, out var last) ||
                Time.time - last >= punchInterval - 0.01f)
            {
                lastHitTime[col] = Time.time;

                if (col.GetComponentInParent<IDamagable>() is IDamagable dmg)
                    dmg.TakeDamage(damagePerPunch, ownerId.Value);
            }
        }

        SpawnPunchVFX(center, rot);
    }

    /* ═════════════════ VFX (client-side) ═════════════════ */
    private void SpawnPunchVFX(Vector3 pos, Quaternion rot)
    {
        if (punchVfxPrefab == null) return;
        SpawnPunchVfxClientRpc(pos, rot);
    }

    [ClientRpc]
    private void SpawnPunchVfxClientRpc(Vector3 pos, Quaternion rot)
    {
        if (punchVfxPrefab == null) return;
        var fx = Instantiate(punchVfxPrefab, pos, rot);
        Destroy(fx, 1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (shooterRoot == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
        Vector3 center = shooterRoot.position + shooterRoot.forward * reach * 0.5f;
        Gizmos.matrix = Matrix4x4.TRS(center, shooterRoot.rotation, hitBoxSize); // #LinearAlgebra Om's so cool
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}
