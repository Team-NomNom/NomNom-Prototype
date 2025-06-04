using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingProjectile : ProjectileBase
{
    [Header("Homing Settings")]
    [Tooltip("Degrees per second this missile can turn toward its target.")]
    [SerializeField] private float turnSpeed = 120f;

    // Holds the chosen target’s NetworkObjectId (0 means “no target yet”).
    public NetworkVariable<ulong> targetNetworkObjectId = new NetworkVariable<ulong>(
        0u,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Cached Transform of the chosen target (filled in after spawning).
    private Transform targetTransform;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    protected override void MoveProjectile()
    {
        if (!IsServer) return;

        // If our targetTransform is not yet initialized, and we have a target ID, grab it
        if (targetTransform == null && targetNetworkObjectId.Value != 0u)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    targetNetworkObjectId.Value,
                    out NetworkObject netObj))
            {
                targetTransform = netObj.transform;
            }
        }

        if (targetTransform == null)
            return; // No valid target yet (or maybe the target died)

        // Direction toward target
        Vector3 toTarget = (targetTransform.position - transform.position).normalized;
        // Current forward
        Vector3 currentForward = transform.forward;
        // How far we can rotate this frame (in radians)
        float maxTurnThisFrame = turnSpeed * Time.fixedDeltaTime * Mathf.Deg2Rad;
        // Slerp/rotate our forward toward the “toTarget”
        Vector3 newDir = Vector3.RotateTowards(currentForward, toTarget, maxTurnThisFrame, 0f).normalized;
        // Build rotation so forward = newDir
        Quaternion targetRot = Quaternion.LookRotation(newDir, Vector3.up);
        rb.MoveRotation(targetRot);
        // Update velocity along newDir
        rb.linearVelocity = newDir * speed;
    }

    // Call this (from the server) once ownerId.Value is already set, to pick the nearest “other” tank as our target.
    public void SelectTarget()
    {
        if (!IsServer) return;

        // If someone already set a target, don’t override
        if (targetNetworkObjectId.Value != 0u) return;

        ulong chosenId = 0u;
        float bestDistSqr = float.MaxValue;

        // Loop all spawned objects, pick nearest TankController that isn’t owner
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            NetworkObject netObj = kvp.Value;
            if (netObj == null) continue;

            // Skip ourselves
            if (netObj.NetworkObjectId == ownerId.Value)
                continue;

            // Only consider things with a TankController
            var tc = netObj.GetComponent<TankController>();
            if (tc == null) continue;

            float distSqr = (netObj.transform.position - transform.position).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                chosenId = netObj.NetworkObjectId;
            }
        }

        targetNetworkObjectId.Value = chosenId;

        // Cache the Transform right away if possible:
        if (chosenId != 0u &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(chosenId, out NetworkObject chosenNetObj))
        {
            targetTransform = chosenNetObj.transform;
        }
    }
}
