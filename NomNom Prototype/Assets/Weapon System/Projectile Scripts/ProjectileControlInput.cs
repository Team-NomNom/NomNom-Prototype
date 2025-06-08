using UnityEngine;

[RequireComponent(typeof(ProjectileFactory))]
public class ProjectileControlInput : MonoBehaviour
{
    [Header("Fire Keys / Buttons")]
    [SerializeField] private KeyCode fireSimpleKey = KeyCode.Mouse0;     // Also supports controller button via axis
    [SerializeField] private KeyCode fireHomingKey = KeyCode.Q;
    [SerializeField] private KeyCode fireArcKey = KeyCode.E;
    [SerializeField] private string fireSimpleAxis = "Fire1"; // Mapped to controller input (e.g., RT)
    [SerializeField] private string fireHomingAxis = "Fire2"; // (e.g., X or L1)
    [SerializeField] private string fireArcAxis = "Fire3";    // (e.g., Y or R1)

    [Header("Turret Rotation")]
    [Tooltip("Enable turret to rotate independently of tank base.")]
    [SerializeField] private bool movesIndependently = true;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private float turretRotateSpeed = 100f;
    [SerializeField] private string turretHorizontalAxis = "Mouse X"; // or "RightStickHorizontal"
    [SerializeField] private string turretVerticalAxis = "Mouse Y";   // or "RightStickVertical"

    private ProjectileFactory factory;
    private Health health; // cache Health reference

    private void Awake()
    {
        factory = GetComponent<ProjectileFactory>();
        health = GetComponent<Health>(); // cache Health once
    }

    private void Update()
    {
        // Prevent firing & turret rotation if tank is dead
        if (health != null && !health.IsAlive)
            return; // skip firing & turret rotation if dead

        HandleFiring();
        HandleTurretRotation();
    }

    private void HandleFiring()
    {
        if (Input.GetKeyDown(fireSimpleKey) || Input.GetButtonDown(fireSimpleAxis))
        {
            factory.FireSimpleProjectile();
        }
        if (Input.GetKeyDown(fireHomingKey) || Input.GetButtonDown(fireHomingAxis))
        {
            factory.FireHomingMissile();
        }
        if (Input.GetKeyDown(fireArcKey) || Input.GetButtonDown(fireArcAxis))
        {
            factory.FireArcGrenade();
        }
    }

    private void HandleTurretRotation()
    {
        if (!movesIndependently || turretTransform == null) return;

        float x = Input.GetAxis(turretHorizontalAxis);
        float y = Input.GetAxis(turretVerticalAxis);
        Vector2 input = new Vector2(x, y);

        if (input.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0, angle, 0);
            turretTransform.rotation = Quaternion.RotateTowards(
                turretTransform.rotation,
                targetRot,
                turretRotateSpeed * Time.deltaTime
            );
        }
    }
}
