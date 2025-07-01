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
    public GameObject worldHealthUIPrefab;

    private GameObject playerUIInstance;
    private Text respawnCountdownText;
    private Coroutine respawnCountdownCoroutine;

    private bool isReadyToSendMovement = false;
    private bool hasSubscribedToDeath = false; // Prevent double-subscription

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

        // Disable local TankController logic on non-server instances
        if (!IsServer)
            localTank.enabled = false;

        // Server-side: reset health on fresh spawn
        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
                health.ResetHealth();
        }

        // Owner-only setup (UI, camera, events, etc.)
        if (IsOwner)
        {
            // Expose this tank’s ProjectileFactory to the GameManager
            GameManager.LocalPlayerFactory = GetComponent<ProjectileFactory>();
            GameManager.OnLocalPlayerFactoryAssigned?.Invoke();

            // Camera follow / override main camera
            if (mainCamFollow != null)
            {
                mainCamFollow.target = transform;
                mainCamFollow.enabled = true;
                mainCamFollow.GetComponent<Camera>().enabled = true;
                mainCamFollow.ForceSnap();
            }

            // Disable any other main camera
            var otherCam = Camera.main;
            if (otherCam != null &&
                (mainCamFollow == null || otherCam != mainCamFollow.GetComponent<Camera>()))
            {
                otherCam.enabled = false;
            }

            // HUD-based player UI (numeric health text, respawn text)
            if (playerUIPrefab != null)
            {
                playerUIInstance = Instantiate(playerUIPrefab);

                // Correct parent → prefab becomes child of InLobbyPanel (or Canvas)
                Transform uiParent =
                    GameObject.Find("InLobbyPanel")?.transform ??
                    GameObject.Find("Canvas")?.transform;

                if (uiParent != null)
                    playerUIInstance.transform.SetParent(uiParent, false);
                else
                    Debug.LogWarning("[NetworkTankController] No InLobbyPanel or Canvas found; " +
                                     "player UI anchored at root.");

                var health = GetComponent<Health>();

                // Numeric health text hookup
                var sceneHealthText = playerUIInstance.GetComponentInChildren<Text>(true);
                if (sceneHealthText != null)
                    health.SetHealthText(sceneHealthText);

                // Respawn countdown text reference
                respawnCountdownText =
                    playerUIInstance.transform.Find("RespawnCountdownText")?.GetComponent<Text>();
                if (respawnCountdownText != null)
                    respawnCountdownText.gameObject.SetActive(false);
            }

            // World-space circular dial above the tank
            if (worldHealthUIPrefab != null)
            {
                var worldUIObj = Instantiate(worldHealthUIPrefab); // root-level (or parent to transform)
                var worldUI = worldUIObj.GetComponent<WorldHealthUI>();
                var health = GetComponent<Health>();

                if (worldUI != null && health != null)
                    worldUI.Init(health, transform); // binds, follows, billboards
            }

            // Subscribe to tank death event once
            var localHealth = GetComponent<Health>();
            if (localHealth != null && !hasSubscribedToDeath)
            {
                localHealth.OnDeath += OnTankDeath;
                hasSubscribedToDeath = true;
            }

            // Enable movement next frame & clear any countdown
            StartCoroutine(EnableMovementAfterSpawn());

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
        if (health != null && !health.IsAlive) return;

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
    private void SubmitMovementServerRpc(float forward, float strafe, float turn, ServerRpcParams rpcParams = default)
    {
        localTank.StoreInput(forward, strafe, turn);
    }

    [ServerRpc]
    private void DebugKillTankServerRpc(ServerRpcParams rpcParams = default)
    {
        var health = GetComponent<Health>();
        if (health != null)
            health.TakeDamage(health.MaxHealth);
    }

    private void OnDestroy()
    {
        if (IsOwner && playerUIInstance != null)
            Destroy(playerUIInstance);

        var health = GetComponent<Health>();
        if (health != null && hasSubscribedToDeath)
        {
            health.OnDeath -= OnTankDeath;
            hasSubscribedToDeath = false;
        }
    }

    private void OnTankDeath(Health health)
    {
        if (respawnCountdownText != null)
        {
            respawnCountdownText.gameObject.SetActive(true);

            if (respawnCountdownCoroutine != null)
                StopCoroutine(respawnCountdownCoroutine);

            RespawnManager respawnManager = FindObjectOfType<RespawnManager>();
            float respawnDelay = respawnManager != null ? respawnManager.RespawnDelay : 3f;

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

        respawnCountdownText.text = "";
        yield return null;
    }

    [ClientRpc]
    public void ShowRespawnCountdownClientRpc(float duration, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        if (respawnCountdownText != null)
        {
            respawnCountdownText.gameObject.SetActive(true);

            if (respawnCountdownCoroutine != null)
                StopCoroutine(respawnCountdownCoroutine);

            respawnCountdownCoroutine = StartCoroutine(RespawnCountdownCoroutine(duration));
        }
    }

    [ServerRpc]
    public void SubmitTankChoiceServerRpc(int tankIndex, ServerRpcParams rpcParams = default)
    {
        GameManager.Instance?.SetTankChoice(rpcParams.Receive.SenderClientId, tankIndex);
    }


}
