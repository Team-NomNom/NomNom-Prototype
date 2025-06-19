using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TeleportBeaconProjectile : ProjectileBase
{
    private TeleportBeaconController ownerController;
    private float beaconLifetime;
    private bool isAnchored = false;

    public override void Initialize(ulong shooterId, GameObject shooterRootObj, IProjectileFactoryUser factoryUser = null, int weaponIndex = -1)
    {
        ownerId.Value = shooterId;
        shooterRoot = shooterRootObj.transform;
    }

    public override void ApplyConfig(ProjectileConfig cfg)
    {
        base.ApplyConfig(cfg); // still clone the config object if needed

        if (cfg != null)
        {
            beaconLifetime = cfg.lifetime;
            Debug.Log($"[TeleportBeacon] Lifetime set from config: {beaconLifetime}");
        }
        else
        {
            beaconLifetime = 5f; // fallback default
            Debug.LogWarning("[TeleportBeacon] Config was null. Using default lifetime.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            AnchorNow();
            StartCoroutine(DestroyAfterLifetime());
        }
    }

    private void AnchorNow()
    {
        if (isAnchored) return;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        isAnchored = true;

        Debug.Log("[TeleportBeacon] Anchored immediately.");
    }

    private IEnumerator DestroyAfterLifetime()
    {
        float safeLifetime = (beaconLifetime > 0f) ? beaconLifetime : 5f;
        Debug.Log($"[TeleportBeacon] Waiting for lifetime: {safeLifetime}");

        yield return new WaitForSeconds(safeLifetime);

        if (!IsServer) yield break;

        Debug.Log("[TeleportBeacon] Beacon expired — applying cooldown.");

        if (ownerController != null)
        {
            ownerController.ApplyCooldownServerRpc();
        }

        GetComponent<NetworkObject>().Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportShooterServerRpc()
    {
        Debug.Log("[TeleportBeacon] TeleportShooterServerRpc called.");

        if (!isAnchored || shooterRoot == null)
        {
            Debug.LogWarning("[TeleportBeacon] Cannot teleport — beacon not anchored or shooter missing.");
            return;
        }

        Debug.Log($"[TeleportBeacon] Shooter was at: {shooterRoot.position}");
        Debug.Log($"[TeleportBeacon] Beacon position: {transform.position}");

        shooterRoot.position = transform.position + Vector3.up * 1.5f;

        if (ownerController != null)
        {
            ownerController.ApplyCooldownServerRpc();
        }

        GetComponent<NetworkObject>().Despawn();
    }

    // Override defaults to disable damage/impact logic
    protected override void OnHit(Collider other) { }

    protected override void OnCollisionEnter(Collision collision) { }

    protected override bool ShouldSkipTarget(Collider hit) => false;

    // Optional setter for ownerController (if still needed)
    public void SetController(TeleportBeaconController controller)
    {
        ownerController = controller;
    }
}
