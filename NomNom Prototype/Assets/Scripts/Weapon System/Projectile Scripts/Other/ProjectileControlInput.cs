using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileFactory))]
public class ProjectileControlInput : MonoBehaviour
{
    [System.Serializable]
    public class WeaponInputMapping
    {
        public int weaponSlotIndex;
        public KeyCode fireKey = KeyCode.None;
        public string fireAxis = "";
    }

    [Header("Weapon Input Mappings")]
    [Tooltip("Define which key/axis fires which weapon slot.")]
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

            if (mapping.fireKey != KeyCode.None && Input.GetKeyDown(mapping.fireKey))
                firePressed = true;

            if (!string.IsNullOrEmpty(mapping.fireAxis) && Input.GetButtonDown(mapping.fireAxis))
                firePressed = true;

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
