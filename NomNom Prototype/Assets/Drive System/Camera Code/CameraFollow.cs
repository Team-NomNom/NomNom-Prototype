using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -7);
    [SerializeField] private float cameraAngle = 30f;

    [Header("Dead Zone (world units)")]
    [Tooltip("How far the target can move from camera center before the camera follows")]
    [SerializeField] private Vector2 deadZone = new Vector2(1f, 1f);

    [Header("Smoothing")]
    [Tooltip("Time for camera to catch up")]
    // [SerializeField, Range(0.01f, 1f)] private float smoothTime = 0.15f;
    [SerializeField] private float followSpeed = 5f;  // tweak this (when its super high it motion sickness lowers...but then there is no reason to have any other logic then)
    private Vector3 smoothVelocity;

    [Header("Level Bounds")]
    [Tooltip("Minimum X,Z the camera can go")]
    public Vector2 minBounds;
    [Tooltip("Maximum X,Z the camera can go")]
    public Vector2 maxBounds;

    [Header("Freeze Zones")]
    [Tooltip("Assign BoxColliders (IsTrigger) here to freeze camera when player enters")]
    public List<Collider> freezeZones = new List<Collider>();
    private bool followEnabled = true;

    void Start()
    {
        // tilt camera down
        transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
    }

    void LateUpdate()
    {
        if (!followEnabled || target == null) return;

        // desired world-space position based on player + offset
        Vector3 desiredPos = target.position + offset;
        Vector3 cur = transform.position;

        // dead-zone logic on X
        float x = cur.x;
        if (desiredPos.x > cur.x + deadZone.x) x = desiredPos.x - deadZone.x;
        else if (desiredPos.x < cur.x - deadZone.x) x = desiredPos.x + deadZone.x;

        // dead-zone logic on Z (forward/back)
        float z = cur.z;
        if (desiredPos.z > cur.z + deadZone.y) z = desiredPos.z - deadZone.y;
        else if (desiredPos.z < cur.z - deadZone.y) z = desiredPos.z + deadZone.y;

        // keep the height from desiredPos.y (so offset.y always applies)
        Vector3 nextPos = new Vector3(x, desiredPos.y, z);

        // clamp to level bounds
        nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
        nextPos.z = Mathf.Clamp(nextPos.z, minBounds.y, maxBounds.y);

        // move
        transform.position = Vector3.Lerp(transform.position, nextPos, followSpeed * Time.deltaTime);

    }

    // Call these from zone triggers
    public void FreezeFollow() => followEnabled = false;
    public void UnfreezeFollow() => followEnabled = true;
}
