using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkTankController : NetworkBehaviour
{
    private TankController localTank;
    private CameraFollow mainCamFollow;

    [Header("Player UI Prefab")]
    public GameObject playerUIPrefab;

    private GameObject playerUIInstance;
    private bool isReadyToSendMovement = false; // NEW → fix Deferred OnSpawn

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
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.RegisterTank(gameObject);
                Debug.Log($"[NetworkTankController] Registered tank {gameObject.name} OnNetworkSpawn.");
            }
        }

        if (IsOwner)
        {
            if (mainCamFollow != null)
            {
                mainCamFollow.target = transform;
                mainCamFollow.enabled = true;
                mainCamFollow.GetComponent<Camera>().enabled = true;
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
                Text sceneHealthText = playerUIInstance.GetComponentInChildren<Text>(true);

                if (sceneHealthText != null)
                {
                    health.SetHealthText(sceneHealthText);
                }
            }

            // NEW → Enable movement after spawn delay → prevents Deferred OnSpawn warning
            StartCoroutine(EnableMovementAfterSpawn());
        }
    }

    private System.Collections.IEnumerator EnableMovementAfterSpawn()
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
            health.TakeDamage(health.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (IsOwner && playerUIInstance != null)
        {
            Destroy(playerUIInstance);
        }
    }
}
