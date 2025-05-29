using UnityEngine;
using System;

public class TankController : MonoBehaviour
{
    public DriveProfile profile;
    [Tooltip("Assign one component that implements IDriveBehaviour (e.g. AckermanDriveBehaviour)")]
    public MonoBehaviour driveBehaviourMono;

    private IDriveBehaviour driveBehaviour;
    private Rigidbody rb;

    // Internal state for constraints
    private float prevAcceleration = 0f;
    private float prevJerk = 0f;
    private float prevRotationAcceleration = 0f;
    private float prevRotationJerk = 0f;

    // Kludgy fix to allow for 2 acceleration curves at the same time (eg. arcade drive)
    private float prevAcceleration2 = 0f;
    private float prevJerk2 = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (profile.preventFlips)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        driveBehaviour = driveBehaviourMono as IDriveBehaviour;
        if (driveBehaviour == null)
            Debug.LogError("DriveBehaviourMono does not implement IDriveBehaviour");
    }

    void FixedUpdate()
    {
        DriveInput input = new DriveInput
        {
            forward = Input.GetAxis(profile.forwardAxis),
            strafe = Input.GetAxis(profile.strafeAxis),
            turn = Input.GetAxis(profile.turnAxis)
        };

        if (driveBehaviour != null)
            driveBehaviour.HandleDrive(rb, input, profile, Time.fixedDeltaTime);
    }

    /// Constrains linear acceleration with jerk and snap limits, updates state, and returns the clamped acceleration.
    public float ConstrainLinear(float desiredAccel, DriveProfile p, float deltaTime)
    {
        float result = ApplyConstraints(prevAcceleration, prevJerk,
                                        desiredAccel, p.maxJerk, p.maxSnap,
                                        deltaTime, out float newJerk);
        prevAcceleration = result;
        prevJerk = newJerk;
        return result;
    }

    // Kludgy fix to allow for 2 acceleration curves at the same time (eg. arcade drive)
    public float ConstrainLinear2(float desiredAccel, DriveProfile p, float deltaTime)
    {
        float result = ApplyConstraints(prevAcceleration2, prevJerk2,
                                        desiredAccel, p.maxJerk, p.maxSnap,
                                        deltaTime, out float newJerk);
        prevAcceleration2 = result;
        prevJerk2 = newJerk;
        return result;
    }

    /// Constrains angular acceleration with jerk and snap limits, updates state, and returns the clamped angular acceleration.
    public float ConstrainAngular(float desiredRotAccel, DriveProfile p, float deltaTime)
    {
        float result = ApplyConstraints(prevRotationAcceleration, prevRotationJerk,
                                        desiredRotAccel, p.maxRotationJerk, p.maxRotationSnap,
                                        deltaTime, out float newRotJerk);
        prevRotationAcceleration = result;
        prevRotationJerk = newRotJerk;
        return result;
    }

    // Internal helper method
    private float ApplyConstraints(float prevAccel, float prevJerk,
                                   float desiredAccel, float maxJerk, float maxSnap,
                                   float dt, out float newJerk)
    {
        float jerk = (desiredAccel - prevAccel) / dt;
        float clampedJerk = Mathf.Clamp(jerk, -maxJerk, maxJerk);
        float snap = (clampedJerk - prevJerk) / dt;
        float clampedSnap = Mathf.Clamp(snap, -maxSnap, maxSnap);
        newJerk = prevJerk + clampedSnap * dt;
        return prevAccel + clampedJerk * dt;
    }
}