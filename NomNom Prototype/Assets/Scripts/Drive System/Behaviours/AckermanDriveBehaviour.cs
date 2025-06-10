using UnityEngine;
using System;
using Unity.Netcode;

public class AckermanDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Distance between front and rear axles")]
    public float wheelBase = 2f;

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

        // Determine desired linear acceleration
        float targetSpeed = input.forward * profile.maxSpeed;
        float desiredAccel = (input.forward >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                .Evaluate(Mathf.Abs(input.forward)) * targetSpeed;
        float newAccel = tc.ConstrainLinear(desiredAccel, profile, deltaTime);

        // Steering angle
        float steerAngle = input.turn * profile.steeringAngleAckerman;

        // Move tank forward
        rb.MovePosition(rb.position + transform.forward * newAccel * deltaTime);

        // Rotate based on steering
        if (Mathf.Abs(steerAngle) > 0.01f)
        {
            float radius = wheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad);
            float angularVelDeg = newAccel / radius * Mathf.Rad2Deg;
            float yawDegreesThisFrame = angularVelDeg * deltaTime;

            // Use shared ground-aligned rotation helper
            GroundAlignedRotationHelper.ApplyRotation(rb, transform, yawDegreesThisFrame, 1.5f);
        }
    }
}
