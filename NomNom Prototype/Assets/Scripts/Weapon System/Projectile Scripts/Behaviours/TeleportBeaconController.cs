using UnityEngine;
using Unity.Netcode;

public class TeleportBeaconController : NetworkBehaviour
{
    [Header("Beacon Settings")]
    [SerializeField] private GameObject beaconPrefab;
    [SerializeField] private ProjectileConfig beaconConfig;
    [SerializeField] private Transform beaconSpawnPoint;
    [SerializeField] private float cooldownDuration = 5f;

    private TeleportBeaconProjectile activeBeacon;
    private bool isOnCooldown = false;

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Z) && !isOnCooldown && activeBeacon == null)
        {
            TrySpawnBeacon();
        }

        if (Input.GetKeyDown(KeyCode.C) && activeBeacon != null)
        {
            TryTeleportToBeacon();
        }
    }

    private void TrySpawnBeacon()
    {
        Debug.Log("[TeleportBeaconController] Attempting to spawn beacon.");
        SpawnBeaconServerRpc();
    }

    private void TryTeleportToBeacon()
    {
        Debug.Log("[TeleportBeaconController] Attempting teleport.");
        if (activeBeacon != null)
        {
            activeBeacon.TeleportShooterServerRpc();
        }
    }

    [ServerRpc]
    private void SpawnBeaconServerRpc(ServerRpcParams rpcParams = default)
    {
        if (activeBeacon != null)
        {
            Debug.LogWarning("[TeleportBeaconController] Beacon already exists — skipping spawn.");
            return;
        }

        GameObject beaconObj = Instantiate(beaconPrefab);
        beaconObj.transform.SetPositionAndRotation(beaconSpawnPoint.position, beaconSpawnPoint.rotation);

        NetworkObject netObj = beaconObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[TeleportBeaconController] Beacon prefab missing NetworkObject.");
            Destroy(beaconObj);
            return;
        }

        netObj.SpawnWithOwnership(OwnerClientId);

        var beacon = beaconObj.GetComponent<TeleportBeaconProjectile>();
        if (beacon != null)
        {
            beacon.Initialize(OwnerClientId, gameObject);
            beacon.ApplyConfig(beaconConfig); // pulls lifetime from config
            beacon.SetController(this);       // for cooldown callback
            activeBeacon = beacon;

            Debug.Log("[TeleportBeaconController] Beacon spawned and initialized.");
        }
        else
        {
            Debug.LogWarning("[TeleportBeaconController] Spawned beacon missing component.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyCooldownServerRpc()
    {
        if (isOnCooldown) return;

        Debug.Log("[TeleportBeaconController] Applying cooldown.");
        activeBeacon = null;
        StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
        Debug.Log("[TeleportBeaconController] Cooldown complete.");
    }
}
