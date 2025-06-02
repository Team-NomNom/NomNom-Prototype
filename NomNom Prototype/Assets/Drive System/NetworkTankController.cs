using Unity.Netcode;
using UnityEngine;

// [RequireComponent(typeof(NetworkObject))]
public class NetworkTankController : NetworkBehaviour
{
    private TankController localTank;
    private CameraFollow mainCamFollow; 

    private void Awake()
    {
        localTank = GetComponent<TankController>();
        var cam = GameObject.Find("LocalCamera");
        if(cam != null)
        {
            mainCamFollow = cam.GetComponent<CameraFollow>();
        }
        if (localTank == null)
        {
            Debug.LogError("NetworkTankController requires a TankController component on the same GameObject.");
        }
    }

    public override void OnNetworkSpawn()
    {
        // If this tank is not owned by this client, disable TankController entirely. Only the owner should run its own input/FixedUpdate logic.
        if (IsOwner)
        {
            if(mainCamFollow != null)
            {
                mainCamFollow.target = this.transform;
                mainCamFollow.enabled = true;
            }

            var sceneCam = Camera.main;
            if(sceneCam != null && sceneCam.GetComponent<CameraFollow>() != mainCamFollow)
            {
                sceneCam.enabled = false;
            }
        }
        else
        {
            localTank.enabled = false;
        }


    }

    private void Update()
    {
        if (!IsOwner) return;

        float forward = Input.GetAxis(localTank.profile.forwardAxis);
        float strafe = Input.GetAxis(localTank.profile.strafeAxis);
        float turn = Input.GetAxis(localTank.profile.turnAxis);

        // Every frame, send the latest input snapshot to the server
        SubmitMovementServerRpc(forward, strafe, turn);
    }

    [ServerRpc]
    private void SubmitMovementServerRpc(
        float forward, float strafe, float turn,
        ServerRpcParams rpcParams = default)
    {
        // This runs on the server / host
        localTank.HandleNetworkedMovement(forward, strafe, turn);
    }
}
