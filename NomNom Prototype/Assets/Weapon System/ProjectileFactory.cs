using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Responsible for instantiating different types of projectiles.
/// Reads from modular configuration (ScriptableObjects).
/// </summary>
public class ProjectileFactory : NetworkBehaviour
{
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject simpleProjectilePrefab;
    [SerializeField] private GameObject homingMissilePrefab;
    [SerializeField] private GameObject arcGrenadePrefab;

    [Header("Projectile Configs")]
    [SerializeField] private ProjectileConfig simpleConfig;
    [SerializeField] private ProjectileConfig homingConfig;
    [SerializeField] private ProjectileConfig arcConfig;

    [Header("Spawn Info")]
    [Tooltip("Where projectiles originate from.")]
    [SerializeField] private Transform muzzleTransform;

    private ulong OwnerId => GetComponent<NetworkObject>().NetworkObjectId;

    // --------- Public firing methods ---------

    public void FireSimpleProjectile()
    {
        if (!IsOwner || simpleProjectilePrefab == null) return;
        SpawnProjectileServerRpc(simpleProjectilePrefab.name, muzzleTransform.position, muzzleTransform.rotation, OwnerId);
    }

    public void FireHomingMissile()
    {
        if (!IsOwner || homingMissilePrefab == null) return;
        SpawnProjectileServerRpc(homingMissilePrefab.name, muzzleTransform.position, muzzleTransform.rotation, OwnerId);
    }

    public void FireArcGrenade()
    {
        if (!IsOwner || arcGrenadePrefab == null) return;
        SpawnProjectileServerRpc(arcGrenadePrefab.name, muzzleTransform.position, muzzleTransform.rotation, OwnerId);
    }

    // --------- Server side spawn logic ---------

    [ServerRpc]
    private void SpawnProjectileServerRpc(string prefabName, Vector3 position, Quaternion rotation, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        GameObject prefab = LookupProjectilePrefab(prefabName);
        ProjectileConfig config = LookupProjectileConfig(prefabName);

        if (prefab == null || config == null)
        {
            Debug.LogError($"ProjectileFactory: Missing prefab or config for {prefabName}");
            return;
        }

        GameObject instance = Instantiate(prefab, position, rotation);

        if (!instance.TryGetComponent<NetworkObject>(out var netObj))
        {
            Debug.LogError("Projectile prefab must contain a NetworkObject component.");
            Destroy(instance);
            return;
        }

        netObj.Spawn();

        if (instance.TryGetComponent<IProjectile>(out var projectile))
        {
            projectile.Initialize(shooterId);
            projectile.ApplyConfig(config);
        }

        IgnoreCollisionWithShooter(instance, shooterId);
    }

    // --------- Helper methods ---------

    private GameObject LookupProjectilePrefab(string name)
    {
        if (name == simpleProjectilePrefab.name) return simpleProjectilePrefab;
        if (name == homingMissilePrefab.name) return homingMissilePrefab;
        if (name == arcGrenadePrefab.name) return arcGrenadePrefab;
        return null;
    }

    private ProjectileConfig LookupProjectileConfig(string name)
    {
        if (name == simpleProjectilePrefab.name) return simpleConfig;
        if (name == homingMissilePrefab.name) return homingConfig;
        if (name == arcGrenadePrefab.name) return arcConfig;
        return null;
    }

    private void IgnoreCollisionWithShooter(GameObject projectile, ulong shooterId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNetObj)) return;

        Collider[] shooterColliders = shooterNetObj.GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();

        foreach (var sCol in shooterColliders)
            foreach (var pCol in projectileColliders)
                Physics.IgnoreCollision(sCol, pCol, true);
    }
}
