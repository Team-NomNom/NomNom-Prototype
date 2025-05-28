using UnityEngine;
using System;
// [RequireComponent(typeof(TankController))]
public class AckermanDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Distance between front and rear axles")]
    public float wheelBase = 2f;

    public void HandleDrive(Rigidbody rb, DriveInput input, DriveProfile profile, float deltaTime)
    {
        TankController tc = rb.GetComponent<TankController>();

        // Determine desired linear acceleration
        float targetSpeed = input.forward * profile.maxSpeed;
        float desiredAccel = (input.forward >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                .Evaluate(Mathf.Abs(input.forward)) * targetSpeed;
        float newAccel = tc.ConstrainLinear(desiredAccel, profile, deltaTime);

        // Steering angle
        float steerAngle = input.turn * 45f;

        // Move tank forward
        rb.MovePosition(rb.position + transform.forward * newAccel * deltaTime);

        // Rotate based on steering
        if (Mathf.Abs(steerAngle) > 0.01f)
        {
            float radius = wheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad);
            float angularVelDeg = newAccel / radius * Mathf.Rad2Deg;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, angularVelDeg * deltaTime, 0));
        }
    }
}
