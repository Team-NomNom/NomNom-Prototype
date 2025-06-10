using UnityEngine;
using System;
using Unity.Netcode;

public class OmniDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Max strafe speed (units/sec)")]
    public float maxStrafeSpeed = 10f;

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

        // Forward movement
        float targetForwardSpeed = input.forward * profile.maxSpeed;
        float desiredForwardAccel = (input.forward >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                        .Evaluate(Mathf.Abs(input.forward)) * targetForwardSpeed;
        float newForwardAccel = tc.ConstrainLinear(desiredForwardAccel, profile, deltaTime);

        // Strafe movement (sideways)
        float targetStrafeSpeed = input.strafe * maxStrafeSpeed;
        // Can add a separate strafe acceleration curve if you want, or reuse existing
        float desiredStrafeAccel = Mathf.Abs(input.strafe) * targetStrafeSpeed; // Simple linear map
        float newStrafeAccel = Mathf.Lerp(0f, targetStrafeSpeed, Mathf.Abs(input.strafe)); // Simple no-constraint version (Can add ConstrainLinear2 if desired)

        // Combine forward + strafe into single movement vector
        Vector3 moveVector = (transform.forward * newForwardAccel + transform.right * newStrafeAccel) * deltaTime;

        rb.MovePosition(rb.position + moveVector);

        // Rotation
        float desiredRot = input.turn * profile.maxRotationSpeed;
        float rotAccel = (input.turn >= 0 ? profile.rotationAccelerationCurve : profile.rotationDecelerationCurve)
                                .Evaluate(Mathf.Abs(input.turn)) * desiredRot;
        float newRotAccel = tc.ConstrainAngular(rotAccel, profile, deltaTime);

        float yawDegreesThisFrame = newRotAccel * deltaTime;

        // Ground-aligned rotation
        GroundAlignedRotationHelper.ApplyRotation(rb, transform, yawDegreesThisFrame, 1.5f);
    }
}
