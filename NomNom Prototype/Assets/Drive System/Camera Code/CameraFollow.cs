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

    [Header("Dynamic Follow Speed")]
    [Tooltip("Multiplier applied to the target's speed to get camera move speed")]
    [SerializeField] private float speedMultiplier = 1f;
    [Tooltip("Optional minimum follow speed, in case the tank is almost stopped")]
    [SerializeField] private float minFollowSpeed = 0.5f;

    [Header("Level Bounds")]
    [Tooltip("Minimum X,Z the camera can go")]
    public Vector2 minBounds;
    [Tooltip("Maximum X,Z the camera can go")]
    public Vector2 maxBounds;

    [Header("Freeze Zones")]
    [Tooltip("Assign BoxColliders (IsTrigger) here to freeze camera when player enters")]
    public List<Collider> freezeZones = new List<Collider>();
    private bool followEnabled = true;

    // Cached Rigidbody from the target
    private Rigidbody targetRb;

    // New → force snap flag
    private bool forceSnapNextFrame = false;

    void Start()
    {
        // Tilt camera down
        transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);

        if (target != null)
            targetRb = target.GetComponent<Rigidbody>();

        AssignTargetRigidbody();
    }

    void FixedUpdate()
    {
        if (!followEnabled || target == null) return;

        if (targetRb == null)
            AssignTargetRigidbody();

        // Compute the raw desired position
        Vector3 desiredPos = target.position + offset;
        Vector3 cur = transform.position;

        // Dead-zone on X
        float x = cur.x;
        if (desiredPos.x > cur.x + deadZone.x) x = desiredPos.x - deadZone.x;
        else if (desiredPos.x < cur.x - deadZone.x) x = desiredPos.x + deadZone.x;

        // Dead-zone on Z
        float z = cur.z;
        if (desiredPos.z > cur.z + deadZone.y) z = desiredPos.z - deadZone.y;
        else if (desiredPos.z < cur.z - deadZone.y) z = desiredPos.z + deadZone.y;

        // Keep height from desiredPos.y
        Vector3 nextPos = new Vector3(x, desiredPos.y, z);

        // Clamp to level bounds
        nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
        nextPos.z = Mathf.Clamp(nextPos.z, minBounds.y, maxBounds.y);

        // Dynamic speed
        float dynamicSpeed = speedMultiplier;
        if (targetRb != null)
            dynamicSpeed = Mathf.Max(targetRb.linearVelocity.magnitude * speedMultiplier, minFollowSpeed);

        // Move the camera
        if (forceSnapNextFrame)
        {
            transform.position = nextPos;
            forceSnapNextFrame = false;
            Debug.Log("[CameraFollow] ForceSnap → camera snapped to target.");
        }
        else
        {
            transform.position = Vector3.MoveTowards(cur, nextPos, dynamicSpeed * Time.deltaTime);
        }
    }

    private void AssignTargetRigidbody()
    {
        if (target != null)
            targetRb = target.GetComponent<Rigidbody>();
    }

    // Call these from zone triggers
    public void FreezeFollow() => followEnabled = false;
    public void UnfreezeFollow() => followEnabled = true;

    // Force camera snap
    public void ForceSnap() => forceSnapNextFrame = true;
}
