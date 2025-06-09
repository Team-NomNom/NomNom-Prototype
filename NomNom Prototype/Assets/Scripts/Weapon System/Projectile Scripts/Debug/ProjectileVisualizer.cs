// ProjectileVisualizer.cs
// Shows arc prediction for grenades or other parabolic projectiles during aiming
// Optionally extendable to show homing paths or impact predictions

using UnityEngine;

[ExecuteInEditMode]
public class ProjectileVisualizer : MonoBehaviour
{
    [Header("Projectile Info")]
    public Transform muzzleTransform;
    public float initialSpeed = 15f;
    [Range(0f, 90f)] public float launchAngle = 45f;

    [Header("Arc Settings")]
    public bool showArc = true;
    [Tooltip("How many segments in the arc line.")]
    public int steps = 30;
    [Tooltip("Time interval between points.")]
    public float timestep = 0.1f;

    [Header("Editor Visualization")]
    public Color arcColor = Color.cyan;
    public float markerRadius = 0.05f;

    private void Update()
    {
        if (Application.isPlaying && showArc && muzzleTransform != null)
        {
            DrawArcRuntime();
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && showArc && muzzleTransform != null)
        {
            DrawArcGizmos();
        }
    }

    private void DrawArcRuntime()
    {
        Vector3 start = muzzleTransform.position;
        Vector3 velocity = CalculateInitialVelocity();
        Vector3 prev = start;

        for (int i = 1; i <= steps; i++)
        {
            float t = timestep * i;
            Vector3 point = start + velocity * t + 0.5f * Physics.gravity * t * t;
            Debug.DrawLine(prev, point, arcColor);
            prev = point;
        }
    }

    private void DrawArcGizmos()
    {
        Vector3 start = muzzleTransform.position;
        Vector3 velocity = CalculateInitialVelocity();
        Vector3 prev = start;

        Gizmos.color = arcColor;
        for (int i = 1; i <= steps; i++)
        {
            float t = timestep * i;
            Vector3 point = start + velocity * t + 0.5f * Physics.gravity * t * t;
            Gizmos.DrawLine(prev, point);
            Gizmos.DrawSphere(point, markerRadius);
            prev = point;
        }
    }

    private Vector3 CalculateInitialVelocity()
    {
        float rad = launchAngle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(muzzleTransform.forward.x, 0f, muzzleTransform.forward.z).normalized;
        float vY = initialSpeed * Mathf.Sin(rad);
        float vH = initialSpeed * Mathf.Cos(rad);
        return dir * vH + Vector3.up * vY;
    }
}
