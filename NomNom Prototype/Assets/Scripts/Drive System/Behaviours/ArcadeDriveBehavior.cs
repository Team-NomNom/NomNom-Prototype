using UnityEngine;
using System;
using Unity.Netcode;

public class ArcadeDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Distance between left and right tracks")]
    public float trackWidth = 2f;

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

        // Left/right track power values
        float leftPower = Math.Clamp(input.forward + input.turn, -1f, 1f);
        float rightPower = Math.Clamp(input.forward - input.turn, -1f, 1f);

        // Desired acceleration for each side
        float leftDesiredAccel = (leftPower >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                    .Evaluate(Mathf.Abs(leftPower)) * leftPower * profile.maxSpeed;
        float rightDesiredAccel = (rightPower >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                    .Evaluate(Mathf.Abs(rightPower)) * rightPower * profile.maxSpeed;

        // Apply acceleration constraints
        float leftNewAccel = tc.ConstrainLinear(leftDesiredAccel, profile, deltaTime);
        float rightNewAccel = tc.ConstrainLinear2(rightDesiredAccel, profile, deltaTime);

        // Final linear and rotational acceleration
        float newAccel = (rightNewAccel + leftNewAccel) / 2.0f;
        float newRotAccel = Mathf.Rad2Deg * (leftNewAccel - rightNewAccel) / (trackWidth / 2);

        float yawDegreesThisFrame = newRotAccel * deltaTime;

        // Move forward
        rb.MovePosition(rb.position + newAccel * deltaTime * transform.forward);

        // Apply rotation via shared ground-aligned helper
        GroundAlignedRotationHelper.ApplyRotation(rb, transform, yawDegreesThisFrame, 1.5f);
    }
}
