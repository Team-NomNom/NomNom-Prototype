using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A network‐enabled projectile factory that uses the Legacy Input System.
/// Clients call a ServerRpc to spawn bullets. Only the local owner can shoot.
/// </summary>
public class ProjectileFactory : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab must have a NetworkObject component and be registered in NetworkManager.")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Fire Mode")]
    [Tooltip("If true, turret rotates independently of tank drive.")]
    [SerializeField] private bool movesIndependently = true;
    [Tooltip("If true, fire continuously while holding Fire1; if false, fire only once per button press.")]
    [SerializeField] private bool fullAuto = true;
    [Tooltip("How many bullets to spawn per shot (e.g. shotguns).")]
    [SerializeField] private int numBullets = 1;
    [Tooltip("Degrees between each bullet in a multi‐bullet spread.")]
    [SerializeField] private float bulletSpreadAngle = 0f;
    [Tooltip("Maximum number of bullets spawned per second.")]
    [SerializeField] private float bulletsPerSecond = 1f;

    [Header("Recoil & Offsets")]
    [Tooltip("How much to shove the tank backwards when firing (optional).")]
    [SerializeField] private float recoilFactor = 0f;
    [Tooltip("Where on the turret the bullets should originate from.")]
    [SerializeField] private Transform shaftTransform;
    [Tooltip("If using a joystick, rotate turret by this offset.")]
    [SerializeField] private float joystickAngleOffset = 90f;

    [Header("UI (Optional)")]
    [Tooltip("If set, displays debug info about the # of bullets, spread, etc.")]
    [SerializeField] private Text debugScreenText;

    // Internal state
    private Coroutine bulletLoop;
    private bool canFire = true;
    private bool isFiring = false;

    // Legacy Input names
    private const string FIRE_BUTTON = "Fire1"; 
    private const string RS_HORIZ_AXIS = "RightStickHorizontal";
    private const string RS_VERT_AXIS = "RightStickVertical"; 

    void Update()
    {
        if (debugScreenText != null)
        {
            debugScreenText.text =
                $"# Bullets: {numBullets}\n" +
                $"Spread: {bulletSpreadAngle}°\n" +
                $"Bullets/sec: {bulletsPerSecond}\n" +
                $"Independent Aim? {movesIndependently}\n" +
                $"Full Auto? {fullAuto}";
        }

        // 2) Only let the local‐owner turret run aiming & firing logic
        if (!IsOwner)
            return;

        // Aiming logic: point the shaftTransform toward the cursor (mouse) or right stick
        if (movesIndependently)
        {
            // --- Mouse & Keyboard Aiming ---
            if (Input.mousePresent)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Vector3 targetPos = hit.point;
                    Vector3 dir = targetPos - shaftTransform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        shaftTransform.rotation = Quaternion.LookRotation(dir);
                    }
                }
            }
            else
            {
                float rsX = Input.GetAxis(RS_HORIZ_AXIS);
                float rsY = Input.GetAxis(RS_VERT_AXIS);
                Vector2 rsInput = new Vector2(rsX, rsY);

                if (rsInput.magnitude > 0.2f) //
                {
                    float angle = Mathf.Atan2(rsInput.y, rsInput.x) * Mathf.Rad2Deg;
                    shaftTransform.rotation = Quaternion.Euler(0f, angle + joystickAngleOffset, 0f);
                }
            }
        }

        // If fullAuto, hold “Fire1” to keep firing.
        if (fullAuto)
        {
            if (Input.GetButton(FIRE_BUTTON))
            {
                if (bulletLoop == null)
                    bulletLoop = StartCoroutine(SpawnBulletLoop());
            }
            else
            {
                // If button released, stop firing loop
                isFiring = false;
                if (bulletLoop != null)
                {
                    StopCoroutine(bulletLoop);
                    bulletLoop = null;
                }
            }
        }
        else
        {
            // Semi‐auto: only trigger when the button is first pressed
            if (Input.GetButtonDown(FIRE_BUTTON))
            {
                if (bulletLoop == null)
                    bulletLoop = StartCoroutine(SpawnBulletLoop());
            }
        }
    }

    private IEnumerator SpawnBulletLoop()
    {
        if (!canFire)
            yield return new WaitUntil(() => canFire);

        isFiring = true;
        while (isFiring)
        {
            float totalSpread = bulletSpreadAngle * (numBullets - 1);
            float startAngle = -totalSpread / 2f;

            for (int i = 0; i < numBullets; i++)
            {
                float angleOffset = startAngle + i * bulletSpreadAngle;
                Vector3 spawnPos = shaftTransform.position;
                Quaternion baseRot = shaftTransform.rotation;
                Quaternion spreadRot = Quaternion.Euler(0f, angleOffset, 0f);
                Quaternion finalRot = baseRot * spreadRot;

                var tankNetObj = GetComponent<NetworkObject>();
                ulong myTankId = tankNetObj.NetworkObjectId;
                SpawnBulletServerRpc(spawnPos, finalRot, myTankId);
            }

            yield return StartCoroutine(BulletCooldown());
            if (!fullAuto)
                yield break;
        }
    }

    private IEnumerator BulletCooldown()
    {
        canFire = false;
        yield return new WaitForSeconds(1f / bulletsPerSecond);
        canFire = true;
    }

    [ServerRpc]
    private void SpawnBulletServerRpc(
        Vector3 position,
        Quaternion rotation,
        ulong shooterNetworkObjectId,     
        ServerRpcParams rpcParams = default)
    {
        // Instantiate on the server
        GameObject bulletInstance = Instantiate(projectilePrefab, position, rotation);
        NetworkObject nob = bulletInstance.GetComponent<NetworkObject>();
        if (nob == null)
        {
            Debug.LogError("SpawnBulletServerRpc: projectilePrefab missing NetworkObject!");
            Destroy(bulletInstance);
            return;
        }

        // Spawn it first so that ownerId can be written safely
        nob.Spawn();

        // Set the projectile’s ownerId to the passed‐in tank ID
        var baseProj = bulletInstance.GetComponent<ProjectileBase>();
        if (baseProj != null)
        {
            baseProj.ownerId.Value = shooterNetworkObjectId;
        }

        // Find the shooter’s tank GameObject by its NetworkObjectId (on server)
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                shooterNetworkObjectId,
                out NetworkObject shooterNetObj))
        {
            GameObject shooterGO = shooterNetObj.gameObject;

            // Get all Colliders on the shooter tank (root + children)
            Collider[] shooterColliders = shooterGO.GetComponentsInChildren<Collider>(includeInactive: false);
            // Get all Colliders on the bullet (root + children)
            Collider[] bulletColliders = bulletInstance.GetComponentsInChildren<Collider>(includeInactive: false);

            // Tell Unity’s physics to ignore collisions between each pair
            foreach (var tankCol in shooterColliders)
            {
                foreach (var bulletCol in bulletColliders)
                {
                    Physics.IgnoreCollision(tankCol, bulletCol, true);
                }
            }
        }
        else
        {
            Debug.LogWarning($"SpawnBulletServerRpc: could not find shooter object {shooterNetworkObjectId}");
        }
    }
}
