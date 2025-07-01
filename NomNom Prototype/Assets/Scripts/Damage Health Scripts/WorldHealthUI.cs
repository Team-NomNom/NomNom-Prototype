using UnityEngine;
using UnityEngine.UI;

public class WorldHealthUI : MonoBehaviour
{
    [SerializeField] private float yOffset = 2f; // height above tank
    [SerializeField] private Image radialImage; // assign in prefab

    private Transform followTarget;
    private Camera mainCam;

    // Called right after instantiate
    public void Init(Health health, Transform target)
    {
        followTarget = target;
        mainCam = Camera.main;
        health.SetRadialImage(radialImage); // ties fillAmount to HP
    }

    void LateUpdate()
    {
        if (followTarget == null) return;

        // Keep over tank & face camera
        transform.position = followTarget.position + Vector3.up * yOffset;
        if (mainCam != null) transform.LookAt(mainCam.transform);
    }
}
