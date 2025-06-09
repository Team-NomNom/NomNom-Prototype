using UnityEngine;
using Unity.Netcode;

public class CameraZone : MonoBehaviour
{
    private CameraFollow cam;

    void Awake()
    {
        cam = Camera.main.GetComponent<CameraFollow>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Find the root NetworkObject on other or its parents:
        var hitNetObj = other.GetComponentInParent<NetworkObject>();
        if (hitNetObj == null) return;

        // Only proceed if that NetworkObject is owned by this client:
        if (!hitNetObj.IsOwner) return;

        // Only proceed if the thing we hit is actually a "Tank" (has a TankController):
        var tank = hitNetObj.GetComponent<TankController>();
        if (tank == null) return;

        // When my own tank drives into the zone - freeze the camera
        cam?.FreezeFollow();
    }

    private void OnTriggerExit(Collider other)
    {
        var hitNetObj = other.GetComponentInParent<NetworkObject>();
        if (hitNetObj == null) return;
        if (!hitNetObj.IsOwner) return;

        var tank = hitNetObj.GetComponent<TankController>();
        if (tank == null) return;

        // If my own tank just left the zone - unfreeze the camera
        cam?.UnfreezeFollow();
    }
}
