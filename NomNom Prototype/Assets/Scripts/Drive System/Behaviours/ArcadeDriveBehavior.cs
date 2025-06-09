using UnityEngine;
using System;
// [RequireComponent(typeof(TankController))]
public class ArcadeDriveBehaviour : MonoBehaviour, IDriveBehaviour
{
    [Tooltip("Distance between left and right tracks")]
    public float trackWidth = 2f;

    public void HandleDrive(Rigidbody rb, DriveInput input, DriveProfile profile, float deltaTime)
    {
        TankController tc = rb.GetComponent<TankController>();

        // figure out the values for left and right acceleration
        float leftPower = Math.Clamp(input.forward + input.turn, -1f, 1f);
        float rightPower = Math.Clamp(input.forward - input.turn, -1f, 1f);
        float leftDesiredAccel = (leftPower >= 0 ? profile.accelerationCurve : profile.decelerationCurve).Evaluate(Mathf.Abs(leftPower)) * leftPower * profile.maxSpeed;
        float rightDesiredAccel = (rightPower >= 0 ? profile.accelerationCurve : profile.decelerationCurve).Evaluate(Mathf.Abs(rightPower)) * rightPower * profile.maxSpeed;
        float leftNewAccel = tc.ConstrainLinear(leftDesiredAccel, profile, deltaTime);
        float rightNewAccel = tc.ConstrainLinear2(rightDesiredAccel, profile, deltaTime);

        // use above values to figure out the linear/rotation acceleration
        float newAccel = (rightNewAccel + leftNewAccel) / 2.0f;
        float newRotAccel = Mathf.Rad2Deg * (leftNewAccel - rightNewAccel) / (trackWidth / 2);
        //print(leftPower + ", " + rightPower + ", " + leftNewAccel + ", " + rightNewAccel + ", " + newAccel + ", " + newRotAccel + ", " + (leftNewAccel - rightNewAccel));

        // Move and rotate tank
        rb.MovePosition(rb.position + newAccel * deltaTime * transform.forward);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, newRotAccel * deltaTime, 0));
    }
}