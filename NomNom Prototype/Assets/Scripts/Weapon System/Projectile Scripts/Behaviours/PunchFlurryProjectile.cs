using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class PunchFlurryProjectile : ProjectileBase
{
    [Header("Punch Geometry")]
    [SerializeField] private Vector3 hitBoxSize = new(1.8f, 1.4f, 1.2f);
    [SerializeField] private float reach = 1.8f;

    [Header("Flurry")]
    [SerializeField] private int punchCount = 4;
    [SerializeField] private float punchInterval = 0.15f;
    [SerializeField] private float damagePerPunch = 6f;

    [Header("Visuals")]
    [SerializeField] private GameObject punchVfxPrefab;

    private readonly Dictionary<Collider, float> lastHitTime = new();

    /* ===== Initialisation ===== */
    protected override void InitializeMotion()
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        if (IsServer)
            StartCoroutine(PunchCoroutine());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner && !IsServer)
            SubmitOwnershipServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitOwnershipServerRpc(ServerRpcParams _ = default) { /* noop */ }

    /* ===== Flurry loop ===== */
    private IEnumerator PunchCoroutine()
    {
        for (int i = 0; i < punchCount; i++)
        {
            DoSinglePunch();
            yield return new WaitForSeconds(punchInterval);
        }
        if (IsServer) NetworkObject.Despawn();
    }

    private void DoSinglePunch()
    {
        if (shooterRoot == null) return;

        Vector3 centre = shooterRoot.position + shooterRoot.forward * reach * 0.5f;
        Quaternion rot = shooterRoot.rotation;

        var hits = Physics.OverlapBox(
            centre, hitBoxSize * 0.5f, rot,
            ~0, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (ShouldSkipTarget(col)) continue;

            if (!lastHitTime.TryGetValue(col, out var last) ||
                Time.time - last >= punchInterval - 0.01f)
            {
                lastHitTime[col] = Time.time;

                if (col.GetComponentInParent<IDamagable>() is { } dmg)
                    dmg.TakeDamage(damagePerPunch, ownerId.Value);
            }
        }

        if (IsServer) SpawnPunchVFX(centre, rot);
    }

    /* ===== VFX ===== */
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
}
