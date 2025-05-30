using UnityEngine;

public class CameraZone : MonoBehaviour
{
    private CameraFollow cam;

    void Awake()
        => cam = Camera.main.GetComponent<CameraFollow>();

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            cam?.FreezeFollow();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            cam?.UnfreezeFollow();
    }
}
