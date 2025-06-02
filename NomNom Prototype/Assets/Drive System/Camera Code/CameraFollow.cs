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

    // cached Rigidbody from the target
    private Rigidbody targetRb;

    void Start()
    {
        // tilt camera down
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

        // compute the raw desired position
        Vector3 desiredPos = target.position + offset;
        Vector3 cur = transform.position;

        // dead-zone on X
        float x = cur.x;
        if (desiredPos.x > cur.x + deadZone.x) x = desiredPos.x - deadZone.x;
        else if (desiredPos.x < cur.x - deadZone.x) x = desiredPos.x + deadZone.x;

        // dead-zone on Z (forward/back)
        float z = cur.z;
        if (desiredPos.z > cur.z + deadZone.y) z = desiredPos.z - deadZone.y;
        else if (desiredPos.z < cur.z - deadZone.y) z = desiredPos.z + deadZone.y;

        // Keep the height from desiredPos.y (so offset.y always applies) -> builds the next cam position
        Vector3 nextPos = new Vector3(x, desiredPos.y, z);

        // 5) clamp to level bounds
        nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
        nextPos.z = Mathf.Clamp(nextPos.z, minBounds.y, maxBounds.y);

        // 6) move based on target velocity?
        float dynamicSpeed = speedMultiplier;
        if (targetRb != null)
            dynamicSpeed = Mathf.Max(targetRb.linearVelocity.magnitude * speedMultiplier, minFollowSpeed);

        // 7) move the camera toward nextPos at that speed
        transform.position = Vector3.MoveTowards(cur, nextPos, dynamicSpeed * Time.deltaTime);
    }
    private void AssignTargetRigidbody()
    {
        if (target != null)
            targetRb = target.GetComponent<Rigidbody>();
    }

    // call these from zone triggers
    public void FreezeFollow() => followEnabled = false;
    public void UnfreezeFollow() => followEnabled = true;
}
