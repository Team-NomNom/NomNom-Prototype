using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileFactory : NetworkBehaviour, IProjectileFactoryUser
{
    [Header("Weapon Slots")]
    public List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Header("Spawn Info")]
    [SerializeField] private Transform muzzleTransform;

    [System.Serializable]
    public class WeaponAmmoSettings
    {
        public int maxAmmo = 3;
        public float cooldownBetweenShots = 0.5f;
        public float reloadTimePerShot = 1.0f;
    }

    public class AmmoState
    {
        public int currentAmmo;
        public float reloadTimer;
        public bool isShotCooldown;
    }

    [System.Serializable]
    public class WeaponSlot
    {
        public string name;
        public GameObject projectilePrefab;
        public ProjectileConfig config;
        public WeaponAmmoSettings ammoSettings = new WeaponAmmoSettings();

        [System.NonSerialized] public AmmoState ammoState = new AmmoState();
    }

    public struct AmmoInfo
    {
        public int currentAmmo;
        public int maxAmmo;
        public float reloadProgress;
    }

    // ✅ Initialize inline to satisfy Unity Netcode
    private NetworkList<int> syncedAmmo = new NetworkList<int>();

    private ulong OwnerId => GetComponent<NetworkObject>().NetworkObjectId;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            syncedAmmo.Clear();
            foreach (var slot in weaponSlots)
            {
                syncedAmmo.Add(slot.ammoSettings.maxAmmo);
            }
        }
    }

    private void Start()
    {
        foreach (var slot in weaponSlots)
        {
            slot.ammoState.currentAmmo = slot.ammoSettings.maxAmmo;
        }
    }

    private void Update()
    {
        foreach (var slot in weaponSlots)
        {
            HandleReload(slot.ammoState, slot.ammoSettings);
        }
    }

    public void TryFireWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponSlots.Count) return;

        var slot = weaponSlots[weaponIndex];
        var ammoState = slot.ammoState;

        if (!IsOwner) return;

        if (ammoState.isShotCooldown)
        {
            Debug.Log($"[ProjectileFactory] Cannot fire {slot.name} → cooldown in progress.");
            return;
        }

        ammoState.isShotCooldown = true;
        StartCoroutine(ShotCooldownCoroutine(ammoState, slot.ammoSettings.cooldownBetweenShots));

        SpawnProjectileServerRpc(weaponIndex, muzzleTransform.position, muzzleTransform.rotation, OwnerId);

        Debug.Log($"[ProjectileFactory] Client requested fire for {slot.name}.");
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

            int index = weaponSlots.FindIndex(slot => slot.ammoState == ammoState);
            if (index != -1 && index < syncedAmmo.Count)
                syncedAmmo[index] = ammoState.currentAmmo;

            Debug.Log($"[ProjectileFactory] Reloaded 1 ammo. Ammo: {ammoState.currentAmmo}/{settings.maxAmmo}");
        }
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(int weaponIndex, Vector3 position, Quaternion rotation, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponSlots.Count) return;

        var slot = weaponSlots[weaponIndex];
        var ammoState = slot.ammoState;

        if (ammoState.currentAmmo <= 0)
        {
            Debug.LogWarning($"[ProjectileFactory] Cannot spawn projectile — no ammo on server.");
            return;
        }

        ammoState.currentAmmo--;

        if (weaponIndex < syncedAmmo.Count)
            syncedAmmo[weaponIndex] = ammoState.currentAmmo;

        Debug.Log($"[ProjectileFactory] Server fired {slot.name}. Ammo now: {ammoState.currentAmmo}/{slot.ammoSettings.maxAmmo}");

        var prefab = slot.projectilePrefab;

        if (prefab == null)
        {
            Debug.LogError($"ProjectileFactory: Weapon slot {weaponIndex} has no prefab assigned.");
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
            proj.Initialize(shooterId, gameObject, this, weaponIndex);
            proj.ApplyConfig(slot.config);
        }

        IgnoreCollisionWithShooter(instance, shooterId);
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

    public AmmoInfo GetAmmoInfo(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponSlots.Count)
            return new AmmoInfo();

        var slot = weaponSlots[weaponIndex];
        var current = (IsServer || weaponIndex >= syncedAmmo.Count)
            ? slot.ammoState.currentAmmo
            : syncedAmmo[weaponIndex];

        return new AmmoInfo
        {
            currentAmmo = current,
            maxAmmo = slot.ammoSettings.maxAmmo,
            reloadProgress = (current < slot.ammoSettings.maxAmmo)
                ? Mathf.Clamp01(slot.ammoState.reloadTimer / slot.ammoSettings.reloadTimePerShot)
                : 1.0f
        };
    }

    public void OnProjectileReturned(int weaponIndex)
    {
        Debug.Log($"[ProjectileFactory] OnProjectileReturned called for weapon {weaponIndex}");

        if (weaponIndex < 0 || weaponIndex >= weaponSlots.Count)
        {
            Debug.LogWarning($"[ProjectileFactory] Invalid weaponIndex {weaponIndex} in return callback");
            return;
        }

        var slot = weaponSlots[weaponIndex];
        var ammoState = slot.ammoState;

        if (ammoState.currentAmmo < slot.ammoSettings.maxAmmo)
        {
            ammoState.currentAmmo++;

            if (weaponIndex < syncedAmmo.Count)
                syncedAmmo[weaponIndex] = ammoState.currentAmmo;

            Debug.Log($"[ProjectileFactory] Projectile returned → granted 1 ammo to {slot.name}. Ammo: {ammoState.currentAmmo}/{slot.ammoSettings.maxAmmo}");
        }
        else
        {
            Debug.Log($"[ProjectileFactory] Ammo already full — no ammo added.");
        }
    }
}
