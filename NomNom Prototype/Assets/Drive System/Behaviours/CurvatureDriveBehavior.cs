using UnityEngine;
using System;
// [RequireComponent(typeof(TankController))]
public class CurvatureDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    //technically this is an implementation of arcade but whatever
    [Tooltip("Distance between left/right wheels")]
    public float wheelWidth = 2f;

    public void HandleDrive(Rigidbody rb, DriveInput input, DriveProfile profile, float deltaTime)
    {
        TankController tc = rb.GetComponent<TankController>();

        float leftPower = Math.Clamp(input.forward + input.turn, -1f, 1f);
        float rightPower = Math.Clamp(input.forward - input.turn, -1f, 1f);
        float leftDesiredAccel = (leftPower >= 0 ? profile.accelerationCurve : profile.decelerationCurve).Evaluate(Mathf.Abs(leftPower)) * leftPower * profile.maxSpeed;
        float rightDesiredAccel = (rightPower >= 0 ? profile.accelerationCurve : profile.decelerationCurve).Evaluate(Mathf.Abs(rightPower)) * rightPower * profile.maxSpeed;
        float leftNewAccel = tc.ConstrainLinear(leftDesiredAccel, profile, deltaTime);
        float rightNewAccel = tc.ConstrainLinear2(rightDesiredAccel, profile, deltaTime);

        float newAccel = (rightNewAccel + leftNewAccel) / 2.0f;
        float newRotAccel = Mathf.Rad2Deg * (leftNewAccel - rightNewAccel) / (wheelWidth / 2);
        //print(leftPower + ", " + rightPower + ", " + leftNewAccel + ", " + rightNewAccel + ", " + newAccel + ", " + newRotAccel + ", " + (leftNewAccel - rightNewAccel));

        // Move and rotate tank
        rb.MovePosition(rb.position + newAccel * deltaTime * transform.forward);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, newRotAccel * deltaTime, 0));
    }
}