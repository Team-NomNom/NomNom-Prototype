using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // for Text

public class NetworkTankController : NetworkBehaviour
{
    private TankController localTank;
    private CameraFollow mainCamFollow;

    [Header("Player UI Prefab")]
    public GameObject playerUIPrefab;

    private GameObject playerUIInstance; // track instance so we can clean up if needed

    private void Awake()
    {
        localTank = GetComponent<TankController>();
        if (localTank == null)
            Debug.LogError("NetworkTankController requires a TankController component on the same GameObject.");

        var camGO = GameObject.Find("LocalCamera");
        if (camGO != null)
            mainCamFollow = camGO.GetComponent<CameraFollow>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            localTank.enabled = false;
        }

        if (IsOwner)
        {
            // Enable and retarget the local camera
            if (mainCamFollow != null)
            {
                mainCamFollow.target = transform;
                mainCamFollow.enabled = true;
                mainCamFollow.GetComponent<Camera>().enabled = true;
            }

            // Disable any other main camera in the scene
            var other = Camera.main;
            if (other != null && (mainCamFollow == null || other != mainCamFollow.GetComponent<Camera>()))
                other.enabled = false;

            // Instantiate PlayerUI for local player
            if (playerUIPrefab != null)
            {
                playerUIInstance = Instantiate(playerUIPrefab);

                // Parent it to InLobbyPanel (inside Canvas), fallback to Canvas
                GameObject inLobbyPanel = GameObject.Find("InLobbyPanel");
                if (inLobbyPanel != null)
                {
                    playerUIInstance.transform.SetParent(inLobbyPanel.transform, false);
                    Debug.Log("NetworkTankController: PlayerUI parented to InLobbyPanel.");
                }
                else
                {
                    Debug.LogWarning("NetworkTankController: Could not find InLobbyPanel. Falling back to Canvas.");
                    GameObject canvas = GameObject.Find("Canvas");
                    if (canvas != null)
                    {
                        playerUIInstance.transform.SetParent(canvas.transform, false);
                        Debug.Log("NetworkTankController: PlayerUI parented to Canvas.");
                    }
                    else
                    {
                        Debug.LogError("NetworkTankController: Could not find Canvas either!");
                    }
                }

                // Assign Health UI to Health script
                Health health = GetComponent<Health>();

                Text sceneHealthText = playerUIInstance.transform.Find("MyHealthText")?.GetComponent<Text>();
                if (sceneHealthText != null)
                {
                    health.SetHealthText(sceneHealthText);
                }
                else
                {
                    Debug.LogWarning("NetworkTankController: MyHealthText not found in PlayerUI.");
                }

                // 🚀 FIX → Force ResetHealth so UI shows correct value
                health.ResetHealth();
            }
            else
            {
                Debug.LogError("NetworkTankController: PlayerUIPrefab not assigned!");
            }
        }

        // Register this tank with RespawnManager (server only)
        if (IsServer)
        {
            RespawnManager respawnManager = FindObjectOfType<RespawnManager>();

            if (respawnManager != null)
            {
                Health health = GetComponent<Health>();

                health.OnDeath -= OnTankDeathForRespawn;
                health.OnDeath += OnTankDeathForRespawn;
            }
            else
            {
                Debug.LogError("NetworkTankController: Could not find RespawnManager in scene.");
            }
        }
    }

    private void OnTankDeathForRespawn(Health h)
    {
        RespawnManager respawnManager = FindObjectOfType<RespawnManager>();

        if (respawnManager != null)
        {
            respawnManager.RespawnTank(gameObject, h.OwnerClientId);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        var health = GetComponent<Health>();
        if (health != null && !health.IsAlive)
            return; // skip input if dead

        float forward = Input.GetAxis(localTank.profile.forwardAxis);
        float strafe = Input.GetAxis(localTank.profile.strafeAxis);
        float turn = Input.GetAxis(localTank.profile.turnAxis);

        SubmitMovementServerRpc(forward, strafe, turn);

        // TEST: Press K to force tank death for respawn testing
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (IsServer)
                health.TakeDamage(health.MaxHealth);
            else
                DebugKillTankServerRpc();
        }
    }

    [ServerRpc]
    private void SubmitMovementServerRpc(
        float forward, float strafe, float turn,
        ServerRpcParams rpcParams = default)
    {
        localTank.StoreInput(forward, strafe, turn);
    }

    [ServerRpc]
    private void DebugKillTankServerRpc(ServerRpcParams rpcParams = default)
    {
        var health = GetComponent<Health>();
        if (health != null)
        {
            Debug.Log("[Debug] Forcing tank death (via ServerRpc).");
            health.TakeDamage(health.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        // Clean up PlayerUI if we own it
        if (IsOwner && playerUIInstance != null)
        {
            Destroy(playerUIInstance);
        }
    }
}
