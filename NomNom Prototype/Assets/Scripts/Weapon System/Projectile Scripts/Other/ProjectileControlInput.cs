using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileFactory))]
public class ProjectileControlInput : MonoBehaviour
{
    [System.Serializable]
    public class WeaponInputMapping
    {
        public int weaponSlotIndex;

        [Tooltip("Keyboard keys that can fire this weapon.")]
        public List<KeyCode> fireKeys = new List<KeyCode>();

        [Tooltip("Input axis/button names (e.g. 'Fire1', 'JoystickButton0')")]
        public List<string> fireAxes = new List<string>();
    }

    [Header("Weapon Input Mappings")]
    [Tooltip("Define which keys/axes fire which weapon slots.")]
    [SerializeField] private List<WeaponInputMapping> weaponInputs = new List<WeaponInputMapping>();

    [Header("Turret Rotation")]
    [Tooltip("Enable turret to rotate independently of tank base.")]
    [SerializeField] private bool movesIndependently = true;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private float turretRotateSpeed = 100f;
    [SerializeField] private string turretHorizontalAxis = "Mouse X"; // or "RightStickHorizontal"
    [SerializeField] private string turretVerticalAxis = "Mouse Y";   // or "RightStickVertical"

    private ProjectileFactory factory;
    private Health health;

    private void Awake()
    {
        factory = GetComponent<ProjectileFactory>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        if (health != null && !health.IsAlive)
            return;

        HandleFiring();
        HandleTurretRotation();
    }

    private void HandleFiring()
    {
        foreach (var mapping in weaponInputs)
        {
            // Validate weapon index
            if (mapping.weaponSlotIndex < 0 || mapping.weaponSlotIndex >= factory.weaponSlots.Count)
                continue;

            bool firePressed = false;

            // Check all keyboard keys
            foreach (var key in mapping.fireKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    firePressed = true;
                    break;
                }
            }

            // Check all input axes/buttons
            if (!firePressed)
            {
                foreach (var axis in mapping.fireAxes)
                {
                    if (Input.GetButtonDown(axis))
                    {
                        firePressed = true;
                        break;
                    }
                }
            }

            if (firePressed)
            {
                factory.TryFireWeapon(mapping.weaponSlotIndex);
            }
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
