using Unity.Netcode;
using UnityEngine;

public class NetworkTankController : NetworkBehaviour
{
    private TankController localTank;
    private CameraFollow mainCamFollow;

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
        // Always disable the TankController component so FixedUpdate never fires
        localTank.enabled = false;

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
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float forward = Input.GetAxis(localTank.profile.forwardAxis);
        float strafe  = Input.GetAxis(localTank.profile.strafeAxis);
        float turn    = Input.GetAxis(localTank.profile.turnAxis);

        SubmitMovementServerRpc(forward, strafe, turn);
    }

    [ServerRpc]
    private void SubmitMovementServerRpc(
        float forward, float strafe, float turn,
        ServerRpcParams rpcParams = default)
    {
        // Only the server (or host) will execute this. It calls TankController.HandleNetworkedMovement exactly once.
        localTank.HandleNetworkedMovement(forward, strafe, turn);
    }
}
