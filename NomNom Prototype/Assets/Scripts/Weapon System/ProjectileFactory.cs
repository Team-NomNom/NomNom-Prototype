using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
    [SerializeField] private Transform muzzleTransform;

    [System.Serializable]
    public class WeaponAmmoSettings
    {
        public int maxAmmo = 3;
        public float cooldownBetweenShots = 0.5f;
        public float reloadTimePerShot = 1.0f;
    }

    [Header("Ammo Settings")]
    public WeaponAmmoSettings simpleAmmoSettings = new WeaponAmmoSettings();
    public WeaponAmmoSettings homingAmmoSettings = new WeaponAmmoSettings();
    public WeaponAmmoSettings arcAmmoSettings = new WeaponAmmoSettings();

    private class AmmoState
    {
        public int currentAmmo;
        public float reloadTimer;
        public bool isShotCooldown;
    }

    private AmmoState simpleAmmo = new AmmoState();
    private AmmoState homingAmmo = new AmmoState();
    private AmmoState arcAmmo = new AmmoState();

    private ulong OwnerId => GetComponent<NetworkObject>().NetworkObjectId;

    private void Start()
    {
        simpleAmmo.currentAmmo = simpleAmmoSettings.maxAmmo;
        homingAmmo.currentAmmo = homingAmmoSettings.maxAmmo;
        arcAmmo.currentAmmo = arcAmmoSettings.maxAmmo;
    }

    private void Update()
    {
        HandleReload(simpleAmmo, simpleAmmoSettings);
        HandleReload(homingAmmo, homingAmmoSettings);
        HandleReload(arcAmmo, arcAmmoSettings);
    }

    public void TryFireSimpleProjectile()
    {
        TryFire(simpleAmmo, simpleAmmoSettings, simpleProjectilePrefab.name, simpleConfig);
    }

    public void TryFireHomingMissile()
    {
        TryFire(homingAmmo, homingAmmoSettings, homingMissilePrefab.name, homingConfig);
    }

    public void TryFireArcGrenade()
    {
        TryFire(arcAmmo, arcAmmoSettings, arcGrenadePrefab.name, arcConfig);
    }

    private void TryFire(AmmoState ammoState, WeaponAmmoSettings settings, string prefabName, ProjectileConfig config)
    {
        if (!IsOwner) return;

        if (ammoState.isShotCooldown)
        {
            Debug.Log($"[ProjectileFactory] Cannot fire {prefabName} → cooldown in progress.");
            return;
        }

        if (ammoState.currentAmmo <= 0)
        {
            Debug.Log($"[ProjectileFactory] Cannot fire {prefabName} → no ammo.");
            return;
        }

        // Fire!
        SpawnProjectileServerRpc(prefabName, muzzleTransform.position, muzzleTransform.rotation, OwnerId);

        ammoState.currentAmmo--;
        ammoState.isShotCooldown = true;
        StartCoroutine(ShotCooldownCoroutine(ammoState, settings.cooldownBetweenShots));

        Debug.Log($"[ProjectileFactory] Fired {prefabName}. Ammo left: {ammoState.currentAmmo}/{settings.maxAmmo}");
    }

    private IEnumerator ShotCooldownCoroutine(AmmoState ammoState, float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        ammoState.isShotCooldown = false;
    }

    private void HandleReload(AmmoState ammoState, WeaponAmmoSettings settings)
    {
        if (ammoState.currentAmmo >= settings.maxAmmo)
        {
            ammoState.reloadTimer = 0f;
            return;
        }

        ammoState.reloadTimer += Time.deltaTime;

        if (ammoState.reloadTimer >= settings.reloadTimePerShot)
        {
            ammoState.currentAmmo++;
            ammoState.reloadTimer = 0f;
            Debug.Log($"[ProjectileFactory] Reloaded 1 ammo. Ammo: {ammoState.currentAmmo}/{settings.maxAmmo}");
        }
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(string prefabName, Vector3 position, Quaternion rotation, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        GameObject prefab = LookupProjectilePrefab(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"ProjectileFactory: Could not find prefab named {prefabName}.");
            return;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        var netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Projectile prefab must contain a NetworkObject component.");
            Destroy(instance);
            return;
        }

        netObj.Spawn();

        var proj = instance.GetComponent<IProjectile>();
        if (proj != null)
        {
            proj.Initialize(shooterId, gameObject); // Pass factory's gameObject as shooter root
            proj.ApplyConfig(LookupProjectileConfig(prefabName));
        }

        IgnoreCollisionWithShooter(instance, shooterId);
    }

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

    // ----- UI support -----

    public struct AmmoInfo
    {
        public int currentAmmo;
        public int maxAmmo;
        public float reloadProgress; // 0.0 → fully reloading, 1.0 → ready to fire
    }

    public AmmoInfo GetSimpleAmmoInfo()
    {
        return new AmmoInfo
        {
            currentAmmo = simpleAmmo.currentAmmo,
            maxAmmo = simpleAmmoSettings.maxAmmo,
            reloadProgress = (simpleAmmo.currentAmmo < simpleAmmoSettings.maxAmmo)
                ? Mathf.Clamp01(simpleAmmo.reloadTimer / simpleAmmoSettings.reloadTimePerShot)
                : 1.0f
        };
    }


    public AmmoInfo GetHomingAmmoInfo()
    {
        return new AmmoInfo
        {
            currentAmmo = homingAmmo.currentAmmo,
            maxAmmo = homingAmmoSettings.maxAmmo,
            reloadProgress = (homingAmmo.currentAmmo < homingAmmoSettings.maxAmmo)
                ? Mathf.Clamp01(homingAmmo.reloadTimer / homingAmmoSettings.reloadTimePerShot)
                : 1.0f
        };
    }

    public AmmoInfo GetArcAmmoInfo()
    {
        return new AmmoInfo
        {
            currentAmmo = arcAmmo.currentAmmo,
            maxAmmo = arcAmmoSettings.maxAmmo,
            reloadProgress = (arcAmmo.currentAmmo < arcAmmoSettings.maxAmmo)
                ? Mathf.Clamp01(arcAmmo.reloadTimer / arcAmmoSettings.reloadTimePerShot)
                : 1.0f
        };
    }

}
