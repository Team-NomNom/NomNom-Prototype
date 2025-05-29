using UnityEngine;

public class DifferentialDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Distance between left and right tracks")]
    public float trackWidth = 1f;

    public void HandleDrive(Rigidbody rb, DriveInput input, DriveProfile profile, float deltaTime)
    {
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
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, newRotAccel * deltaTime, 0));
    }
}
