using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TeleportBeaconController : NetworkBehaviour
{
    [Header("Beacon Settings")]
    [SerializeField] private GameObject beaconPrefab;
    [SerializeField] private Transform beaconSpawnPoint;
    [SerializeField] private float beaconLifetime = 5f;
    [SerializeField] private float cooldownDuration = 8f;

    private TeleportBeaconProjectile activeBeacon = null;
    private bool isOnCooldown = false;

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            TrySpawnBeacon();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TryTeleportToBeacon();
        }
    }

    private void TrySpawnBeacon()
    {
        if (isOnCooldown || activeBeacon != null) return;

        Debug.Log("[TeleportBeaconController] Attempting to spawn beacon.");
        SpawnBeaconServerRpc();
    }

    private void TryTeleportToBeacon()
    {
        if (activeBeacon != null)
        {
            Debug.Log("[TeleportBeaconController] Trying to teleport to beacon...");
            activeBeacon.TeleportShooterServerRpc();
            activeBeacon = null;
        }
        else
        {
            Debug.Log("[TeleportBeaconController] No active beacon to teleport to.");
        }
    }

    [ServerRpc]
    private void SpawnBeaconServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isOnCooldown) return;

        GameObject beaconObj = Instantiate(beaconPrefab);
        beaconObj.transform.SetPositionAndRotation(beaconSpawnPoint.position, beaconSpawnPoint.rotation);

        NetworkObject netObj = beaconObj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);

        TeleportBeaconProjectile beacon = beaconObj.GetComponent<TeleportBeaconProjectile>();
        beacon.Initialize(OwnerClientId, gameObject, null);
        beacon.Configure(this, beaconLifetime);

        SetBeaconClientRpc(netObj);
        Debug.Log($"[TeleportBeaconController] Beacon spawned at {beaconSpawnPoint.position}");
    }

    [ClientRpc]
    private void SetBeaconClientRpc(NetworkObjectReference beaconRef)
    {
        if (beaconRef.TryGet(out NetworkObject beaconObj))
        {
            activeBeacon = beaconObj.GetComponent<TeleportBeaconProjectile>();
            Debug.Log("[TeleportBeaconController] Client received beacon reference.");
        }
        else
        {
            Debug.LogWarning("[TeleportBeaconController] Client failed to resolve beacon reference.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyCooldownServerRpc()
    {
        if (isOnCooldown) return;

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        Debug.Log("[TeleportBeaconController] Cooldown started.");
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
        Debug.Log("[TeleportBeaconController] Cooldown ended.");
    }

    // 🔵 Visualize the spawn point in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (beaconSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(beaconSpawnPoint.position, 0.25f);
            Gizmos.DrawLine(transform.position, beaconSpawnPoint.position);
        }
    }
}
