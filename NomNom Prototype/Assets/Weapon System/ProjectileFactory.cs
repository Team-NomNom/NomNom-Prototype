// Cleaned-Up Projectile System
// All classes are structured to be modular, readable, and follow OOP principles

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ProjectileFactory : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject homingMissilePrefab;

    [Header("Fire Mode")]
    [SerializeField] private bool movesIndependently = true;
    [SerializeField] private bool fullAuto = true;
    [SerializeField] private int numBullets = 1;
    [SerializeField] private float bulletSpreadAngle = 0f;
    [SerializeField] private float bulletsPerSecond = 1f;

    [Header("Homing Missile")]
    [SerializeField] private bool allowHoming = false;
    [SerializeField] private float homingCooldown = 1f;

    [Header("Recoil & Offsets")]
    [SerializeField] private float recoilFactor = 0f;
    [SerializeField] private Transform shaftTransform;
    [SerializeField] private float joystickAngleOffset = 90f;

    [Header("UI (Optional)")]
    [SerializeField] private Text debugScreenText;

    private Coroutine bulletLoop;
    private bool canFire = true;
    private bool canFireHoming = true;
    private const string FIRE_BUTTON = "Fire1";

    void Update()
    {
        if (!IsOwner) return;

        UpdateDebugUI();
        HandleAiming();
        HandleFiring();
    }

    private void UpdateDebugUI()
    {
        if (debugScreenText != null)
        {
            debugScreenText.text =
                $"# Bullets: {numBullets}\nSpread: {bulletSpreadAngle}°\nBullets/sec: {bulletsPerSecond}\n" +
                $"Independent Aim? {movesIndependently}\nFull Auto? {fullAuto}\n" +
                $"Allow Homing? {allowHoming}\nHoming CD: {homingCooldown:F1}s";
        }
    }

    private void HandleAiming()
    {
        if (!movesIndependently) return;

        if (Input.mousePresent && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 dir = hit.point - shaftTransform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    shaftTransform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }
        else
        {
            float x = Input.GetAxis("RightStickHorizontal");
            float y = Input.GetAxis("RightStickVertical");
            Vector2 input = new Vector2(x, y);

            if (input.magnitude > 0.2f)
            {
                float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg + joystickAngleOffset;
                shaftTransform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }
    }

    private void HandleFiring()
    {
        if (fullAuto)
        {
            if (Input.GetButton(FIRE_BUTTON) && bulletLoop == null)
            {
                bulletLoop = StartCoroutine(SpawnBulletLoop());
            }
            else if (!Input.GetButton(FIRE_BUTTON) && bulletLoop != null)
            {
                StopCoroutine(bulletLoop);
                bulletLoop = null;
            }
        }
        else if (Input.GetButtonDown(FIRE_BUTTON))
        {
            StartCoroutine(SpawnBulletLoop());
        }

        if (allowHoming && canFireHoming && Input.GetMouseButtonDown(1))
        {
            FireHomingMissile();
        }
    }

    private IEnumerator SpawnBulletLoop()
    {
        if (!canFire)
            yield return new WaitUntil(() => canFire);

        while (true)
        {
            float totalSpread = bulletSpreadAngle * (numBullets - 1);
            float startAngle = -totalSpread / 2f;
            ulong shooterId = GetComponent<NetworkObject>().NetworkObjectId;

            for (int i = 0; i < numBullets; i++)
            {
                Quaternion spreadRot = Quaternion.Euler(0f, startAngle + i * bulletSpreadAngle, 0f);
                Quaternion finalRot = shaftTransform.rotation * spreadRot;
                SpawnBulletServerRpc(shaftTransform.position, finalRot, shooterId);
            }

            yield return new WaitForSeconds(1f / bulletsPerSecond);
            if (!fullAuto) yield break;
        }
    }

    private void FireHomingMissile()
    {
        canFireHoming = false;
        ulong shooterId = GetComponent<NetworkObject>().NetworkObjectId;
        SpawnHomingMissileServerRpc(shaftTransform.position, shaftTransform.rotation, shooterId);
        StartCoroutine(ResetHomingCooldown());
    }

    private IEnumerator ResetHomingCooldown()
    {
        yield return new WaitForSeconds(homingCooldown);
        canFireHoming = true;
    }

    [ServerRpc]
    private void SpawnBulletServerRpc(Vector3 pos, Quaternion rot, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        GameObject bullet = Instantiate(projectilePrefab, pos, rot);
        var nob = bullet.GetComponent<NetworkObject>();
        if (nob == null) { Destroy(bullet); return; }

        nob.Spawn();
        var proj = bullet.GetComponent<ProjectileBase>();
        if (proj != null) proj.ownerId.Value = shooterId;
        IgnoreCollisionWithShooter(bullet, shooterId);
    }

    [ServerRpc]
    private void SpawnHomingMissileServerRpc(Vector3 pos, Quaternion rot, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        GameObject missile = Instantiate(homingMissilePrefab, pos, rot);
        var nob = missile.GetComponent<NetworkObject>();
        if (nob == null) { Destroy(missile); return; }

        nob.Spawn();
        var proj = missile.GetComponent<ProjectileBase>();
        if (proj != null) proj.ownerId.Value = shooterId;
        IgnoreCollisionWithShooter(missile, shooterId);

        var homing = missile.GetComponent<HomingProjectile>();
        if (homing != null) homing.SelectTarget();
    }

    private void IgnoreCollisionWithShooter(GameObject projectile, ulong shooterId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out NetworkObject shooterObj)) return;

        Collider[] tankCols = shooterObj.GetComponentsInChildren<Collider>();
        Collider[] projCols = projectile.GetComponentsInChildren<Collider>();

        foreach (var t in tankCols)
            foreach (var p in projCols)
                Physics.IgnoreCollision(t, p, true);
    }
}