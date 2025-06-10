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

        // Translation
        float desiredTransSpeed = input.forward * profile.maxSpeed;
        float transAccel = (input.forward >= 0 ? profile.accelerationCurve : profile.decelerationCurve)
                                .Evaluate(Mathf.Abs(input.forward)) * desiredTransSpeed;
        float newTransAccel = tc.ConstrainLinear(transAccel, profile, deltaTime);

        rb.MovePosition(rb.position + transform.forward * newTransAccel * deltaTime);

        // Rotation
        float desiredRot = input.turn * profile.maxRotationSpeed;
        float rotAccel = (input.turn >= 0 ? profile.rotationAccelerationCurve : profile.rotationDecelerationCurve)
                                .Evaluate(Mathf.Abs(input.turn)) * desiredRot;
        float newRotAccel = tc.ConstrainAngular(rotAccel, profile, deltaTime);

        float yawDegreesThisFrame = newRotAccel * deltaTime;

        // Use shared ground-aligned rotation helper
        GroundAlignedRotationHelper.ApplyRotation(rb, transform, yawDegreesThisFrame, 1.5f);
    }
}
