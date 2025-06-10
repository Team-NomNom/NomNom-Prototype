using UnityEngine;
using System;
using Unity.Netcode;

public class SwerveDriveBehaviour : MonoBehaviour, IDriveBehaviour
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

        // Forward movement (field-oriented)
        float targetForwardSpeed = input.forward * profile.maxSpeed;
        float desiredForwardAccel = (input.forward >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                        .Evaluate(Mathf.Abs(input.forward)) * targetForwardSpeed;
        float newForwardAccel = tc.ConstrainLinear(desiredForwardAccel, profile, deltaTime);

        // Strafe movement (field-oriented) — simple linear mapping
        float targetStrafeSpeed = input.strafe * maxStrafeSpeed;
        float desiredStrafeAccel = Mathf.Abs(input.strafe) * targetStrafeSpeed;
        float newStrafeAccel = Mathf.Lerp(0f, targetStrafeSpeed, Mathf.Abs(input.strafe)); // No profile constraint needed

        // Combine into world-space move vector:
        Vector3 worldForward = Vector3.ProjectOnPlane(Vector3.forward, Vector3.up).normalized; // world Z+
        Vector3 worldRight = Vector3.ProjectOnPlane(Vector3.right, Vector3.up).normalized;     // world X+

        Vector3 moveVector = (worldForward * newForwardAccel + worldRight * newStrafeAccel) * deltaTime;

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
