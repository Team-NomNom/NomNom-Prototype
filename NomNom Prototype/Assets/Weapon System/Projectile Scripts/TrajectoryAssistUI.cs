// TrajectoryAssistUI.cs
// Provides a visible in-game aim assist line and predicted impact indicator using a LineRenderer and Decal

using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryAssistUI : MonoBehaviour
{
    [Header("References")]
    public Transform muzzleTransform;
    public GameObject impactMarkerPrefab;

    [Header("Trajectory Settings")]
    public float initialSpeed = 15f;
    [Range(0f, 90f)] public float launchAngle = 45f;
    public int steps = 30;
    public float timestep = 0.1f;
    public LayerMask collisionMask;

    private LineRenderer lineRenderer;
    private GameObject impactMarker;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        if (impactMarkerPrefab != null)
            impactMarker = Instantiate(impactMarkerPrefab);

        if (impactMarker != null)
            impactMarker.SetActive(false);
    }

    void Update()
    {
        if (muzzleTransform == null) return;
        Vector3 start = muzzleTransform.position;
        Vector3 velocity = CalculateInitialVelocity();

        lineRenderer.positionCount = steps;

        Vector3 prev = start;
        for (int i = 0; i < steps; i++)
        {
            float t = timestep * i;
            Vector3 point = start + velocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);

            // Collision prediction (first hit only)
            if (i > 0 && Physics.Linecast(prev, point, out RaycastHit hit, collisionMask))
            {
                if (impactMarker != null)
                {
                    impactMarker.transform.position = hit.point;
                    impactMarker.transform.rotation = Quaternion.LookRotation(hit.normal);
                    impactMarker.SetActive(true);
                }
                lineRenderer.positionCount = i + 1;
                break;
            }
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