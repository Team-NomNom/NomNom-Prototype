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

    private void Awake()
    {
        localTank = GetComponent<TankController>();
        if (localTank == null)
            Debug.LogError("NetworkTankController requires a TankController component.");

        var camGO = GameObject.Find("LocalCamera");
        if (camGO != null)
            mainCamFollow = camGO.GetComponent<CameraFollow>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            localTank.enabled = false;

        if (IsServer)
        {
            var health = GetComponent<Health>();
            health?.ResetHealth();
        }

        if (IsOwner)
        {
            GameManagerNew.LocalPlayerFactory = GetComponent<ProjectileFactory>();
            GameManagerNew.OnLocalPlayerFactoryAssigned?.Invoke();

            if (mainCamFollow != null)
            {
                mainCamFollow.target = transform;
                mainCamFollow.enabled = true;
                mainCamFollow.GetComponent<Camera>().enabled = true;
                mainCamFollow.ForceSnap();
            }

            var otherCam = Camera.main;
            if (otherCam != null && (mainCamFollow == null || otherCam != mainCamFollow.GetComponent<Camera>()))
                otherCam.enabled = false;

            if (playerUIPrefab != null)
            {
                playerUIInstance = Instantiate(playerUIPrefab);

                Transform uiParent =
                    GameObject.Find("InLobbyPanel")?.transform ??
                    GameObject.Find("Canvas")?.transform;

                if (uiParent != null)
                    playerUIInstance.transform.SetParent(uiParent, false);

                var health = GetComponent<Health>();

                var sceneHealthText = playerUIInstance.GetComponentInChildren<Text>(true);
                if (sceneHealthText != null)
                    health?.SetHealthText(sceneHealthText);

                respawnCountdownText = playerUIInstance.transform.Find("RespawnCountdownText")?.GetComponent<Text>();
                if (respawnCountdownText != null)
                    respawnCountdownText.gameObject.SetActive(false);
            }

            if (worldHealthUIPrefab != null)
            {
                var worldUIObj = Instantiate(worldHealthUIPrefab);
                var worldUI = worldUIObj.GetComponent<WorldHealthUI>();
                var health = GetComponent<Health>();

                if (worldUI != null && health != null)
                    worldUI.Init(health, transform);
            }

            // No death subscription needed anymore — handled server-side
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
        health?.TakeDamage(health.MaxHealth);
    }

    private void OnDestroy()
    {
        if (IsOwner && playerUIInstance != null)
            Destroy(playerUIInstance);
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
}
