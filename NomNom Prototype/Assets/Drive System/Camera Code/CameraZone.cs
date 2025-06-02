using Unity.Netcode;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    private CameraFollow cam;

    void Awake()
        => cam = Camera.main.GetComponent<CameraFollow>();

    void OnTriggerEnter(Collider other)
    {
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
            cam?.FreezeFollow();
    }

    void OnTriggerExit(Collider other)
    {
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
            cam?.UnfreezeFollow();
    }
}
