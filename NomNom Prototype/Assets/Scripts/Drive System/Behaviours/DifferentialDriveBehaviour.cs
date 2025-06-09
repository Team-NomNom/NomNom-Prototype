using Unity.Netcode;
using UnityEngine;

public class DifferentialDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Distance between left and right tracks")]
    public float trackWidth = 1f;

    private NetworkObject netObj;

    void Awake()
    {
        netObj = GetComponent<NetworkObject>();
    }
    public void HandleDrive(Rigidbody rb, DriveInput input, DriveProfile profile, float deltaTime)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        TankController tc = rb.GetComponent<TankController>();

        // Translation: independent of rotation
        float desiredTransSpeed = input.forward * profile.maxSpeed;
        float transAccel = (input.forward >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                .Evaluate(Mathf.Abs(input.forward)) * desiredTransSpeed;
        float newTransAccel = tc.ConstrainLinear(transAccel, profile, deltaTime);
        rb.MovePosition(rb.position + transform.forward * newTransAccel * deltaTime);

        // Rotation: independent maxRotationSpeed
        float desiredRot = input.turn * profile.maxRotationSpeed;
        float rotAccel = (input.turn >= 0 ? profile.rotationAccelerationCurve : profile.rotationDecelerationCurve)
                                .Evaluate(Mathf.Abs(input.turn)) * desiredRot;
        float newRotAccel = tc.ConstrainAngular(rotAccel, profile, deltaTime);
        // rb.MoveRotation(rb.rotation * Quaternion.Euler(0, newRotAccel * deltaTime, 0));
        float yawDegreesThisFrame = newRotAccel * deltaTime;

        // Ray origin: slightly above the tank’s position
        Vector3 rayOrigin = rb.position + Vector3.up * 0.5f;
        Vector3 rayDir = Vector3.down;
        float rayLength = 1.5f;

        // Draw the ray in the Scene view (yellow color)
        Debug.DrawRay(rayOrigin, rayDir * rayLength, Color.yellow);

        Ray ray = new Ray(rb.position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hitInfo, rayLength))
        {
            // Grab the ground normal
            Vector3 groundNormal = hitInfo.normal;

            // Project the tank’s forward onto the plane defined by groundNormal
            Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;

            Quaternion slopeAlignment = Quaternion.LookRotation(slopeForward, groundNormal);

            Quaternion yawQuat = Quaternion.AngleAxis(yawDegreesThisFrame, groundNormal);

            Quaternion finalRotation = slopeAlignment * yawQuat;

            rb.MoveRotation(finalRotation);
        }
        else
        {
            // If there’s no ground within 1.5 units (ex. falling or off a ledge), just do a pure yaw around world‐up to keep turning while airborne.
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDegreesThisFrame, 0f));
        }
    }
}
