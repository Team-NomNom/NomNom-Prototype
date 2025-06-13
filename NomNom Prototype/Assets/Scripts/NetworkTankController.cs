using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NetworkTankController : NetworkBehaviour
{
    private TankController localTank;
    private CameraFollow mainCamFollow;

    [Header("Player UI Prefab")]
    public GameObject playerUIPrefab;

    private GameObject playerUIInstance;
    private Text respawnCountdownText; // Respawn countdown UI element
    private Coroutine respawnCountdownCoroutine; // Track coroutine

    private bool isReadyToSendMovement = false; // Movement safe delay flag

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

        if (IsServer)
        {
            // Tank fully spawned -> ResetHealth here
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.ResetHealth();
                Debug.Log($"[NetworkTankController] ResetHealth called on server for tank {gameObject.name} (safe OnNetworkSpawn)");
            }
        }

        if (IsOwner)
        {
            // 🚀 Assign LocalPlayerFactory here → this solves the Ammo UI issue
            GameManager.LocalPlayerFactory = GetComponent<ProjectileFactory>();
            GameManager.OnLocalPlayerFactoryAssigned?.Invoke();
            Debug.Log("[NetworkTankController] Assigned LocalPlayerFactory (OnNetworkSpawn, IsOwner).");

            if (mainCamFollow != null)
            {
                mainCamFollow.target = transform;
                mainCamFollow.enabled = true;
                mainCamFollow.GetComponent<Camera>().enabled = true;

                // 🚀 Force snap camera after respawn
                mainCamFollow.ForceSnap();
            }

            var other = Camera.main;
            if (other != null && (mainCamFollow == null || other != mainCamFollow.GetComponent<Camera>()))
                other.enabled = false;

            if (playerUIPrefab != null)
            {
                playerUIInstance = Instantiate(playerUIPrefab);

                GameObject inLobbyPanel = GameObject.Find("InLobbyPanel");
                if (inLobbyPanel != null)
                {
                    playerUIInstance.transform.SetParent(inLobbyPanel.transform, false);
                }
                else
                {
                    GameObject canvas = GameObject.Find("Canvas");
                    if (canvas != null)
                    {
                        playerUIInstance.transform.SetParent(canvas.transform, false);
                    }
                }

                Health health = GetComponent<Health>();

                // Health text
                Text sceneHealthText = playerUIInstance.GetComponentInChildren<Text>(true);
                if (sceneHealthText != null)
                {
                    health.SetHealthText(sceneHealthText);
                }

                // Respawn countdown text → find and cache
                respawnCountdownText = playerUIInstance.transform.Find("RespawnCountdownText")?.GetComponent<Text>();
                if (respawnCountdownText != null)
                {
                    respawnCountdownText.gameObject.SetActive(false); // start hidden
                }

                // 🚩 TEMPORARY — removed unsafe OnDeath += OnTankDeath to avoid double subscription
                // We will add VisualDeathListener after testing
            }

            // Movement safe delay → prevents DeferredOnSpawn warning
            StartCoroutine(EnableMovementAfterSpawn());

            // Hide respawn countdown if re-spawning!
            if (respawnCountdownText != null)
            {
                respawnCountdownText.gameObject.SetActive(false);

                if (respawnCountdownCoroutine != null)
                {
                    StopCoroutine(respawnCountdownCoroutine);
                    respawnCountdownCoroutine = null;
                }
            }
        }
    }

    private IEnumerator EnableMovementAfterSpawn()
    {
        yield return null;
        isReadyToSendMovement = true;
    }

    void Update()
    {
        if (!IsOwner || !isReadyToSendMovement) return;

        var health = GetComponent<Health>();
        if (health != null && !health.IsAlive)
            return;

        float forward = Input.GetAxis(localTank.profile.forwardAxis);
        float strafe = Input.GetAxis(localTank.profile.strafeAxis);
        float turn = Input.GetAxis(localTank.profile.turnAxis);

        SubmitMovementServerRpc(forward, strafe, turn);

        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.JoystickButton3))
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
            health.TakeDamage(health.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (IsOwner && playerUIInstance != null)
        {
            Destroy(playerUIInstance);
        }

        var health = GetComponent<Health>();
        if (health != null)
        {
            // 🚩 TEMPORARY — we removed OnDeath += OnTankDeath → no need to unsubscribe
        }
    }

    // OnDeath handler for showing respawn countdown
    // This will be re-enabled later once we add proper VisualDeathListener
    private void OnTankDeath(Health health)
    {
        if (respawnCountdownText != null)
        {
            respawnCountdownText.gameObject.SetActive(true);

            // Cancel previous coroutine if needed
            if (respawnCountdownCoroutine != null)
                StopCoroutine(respawnCountdownCoroutine);

            // Get respawnDelay dynamically from RespawnManager
            RespawnManager respawnManager = FindObjectOfType<RespawnManager>();
            float respawnDelay = respawnManager != null ? respawnManager.RespawnDelay : 3f; // fallback default

            respawnCountdownCoroutine = StartCoroutine(RespawnCountdownCoroutine(respawnDelay));
        }
    }

    private IEnumerator RespawnCountdownCoroutine(float duration)
    {
        float timer = duration;

        while (timer > 0f)
        {
            respawnCountdownText.text = $"{Mathf.CeilToInt(timer)}";
            yield return null;
            timer -= Time.deltaTime;
        }

        respawnCountdownText.text = ""; // optional final message
        yield return null;
    }
}
