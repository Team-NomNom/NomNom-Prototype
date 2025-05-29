using UnityEngine;
using System;


[CreateAssetMenu(menuName = "Drive System/Drive Profile", fileName = "New DriveProfile")]
public class DriveProfile : ScriptableObject

{
    [Header("Input Axes")]

    [Tooltip("Axis name for forward/back movement")]
    public string forwardAxis = "Vertical";
    [Tooltip("Axis name for strafing (holonomic)")]
    public string strafeAxis = "Horizontal2";
    [Tooltip("Axis name for turning")]
    public string turnAxis = "Horizontal";


    [Header("General")]
    public float maxSpeed = 10f;
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve decelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float maxJerk = 10f;
    public float maxSnap = 50f;

    [Header("Rotation")]
    public float maxRotationSpeed = 100f;
    public AnimationCurve rotationAccelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve rotationDecelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float maxRotationJerk = 360f;
    public float maxRotationSnap = 720f;

    [Header("Flip Prevention")]
    public bool preventFlips = true;

    [Header("Ackerman Specefic Values")]
    public float steeringAngleAckerman = 45f;
}
