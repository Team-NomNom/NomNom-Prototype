using UnityEngine;

public static class GroundAlignedRotationHelper
{
    /// <summary>
    /// Performs ground-aligned rotation for a tank, with fallback to world-up if no ground detected.
    /// </summary>
    public static void ApplyRotation(Rigidbody rb, Transform tankTransform, float yawDegreesThisFrame, float rayLength = 1.5f)
    {
        Vector3 rayOrigin = rb.position + Vector3.up * 0.5f;
        Vector3 rayDir = Vector3.down;

        Debug.DrawRay(rayOrigin, rayDir * rayLength, Color.magenta); // visual debugging

        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hitInfo, rayLength))
        {
            Vector3 groundNormal = hitInfo.normal;

            Vector3 slopeForward = Vector3.ProjectOnPlane(tankTransform.forward, groundNormal).normalized;

            Quaternion slopeAlignment = Quaternion.LookRotation(slopeForward, groundNormal);

            Quaternion yawQuat = Quaternion.AngleAxis(yawDegreesThisFrame, groundNormal);

            Quaternion finalRotation = slopeAlignment * yawQuat;

            rb.MoveRotation(finalRotation);
        }
        else
        {
            // No ground within rayLength → pure yaw around world-up
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDegreesThisFrame, 0f));
        }
    }

    /// <summary>
    /// Returns true if tank is grounded within the given rayLength. Outputs RaycastHit info.
    /// </summary>
    public static bool IsGrounded(Rigidbody rb, out RaycastHit hitInfo, float rayLength = 1.5f)
    {
        Vector3 rayOrigin = rb.position + Vector3.up * 0.5f;
        Vector3 rayDir = Vector3.down;

        Debug.DrawRay(rayOrigin, rayDir * rayLength, Color.green); // visual debugging

        return Physics.Raycast(rayOrigin, rayDir, out hitInfo, rayLength);
    }
}
