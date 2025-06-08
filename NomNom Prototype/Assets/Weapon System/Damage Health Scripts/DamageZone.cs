using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;

    private void OnTriggerEnter(Collider other)
    {
        IDamagable damagable = other.GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            Debug.Log($"[DamageZone] OnTriggerEnter → {other.name} entered the zone.");
            // Optional → apply instant damage here if you want
            // damagable.TakeDamage(initialDamageAmount);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        IDamagable damagable = other.GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            // Check if it's a Health component → skip dead tanks
            Health health = other.GetComponentInParent<Health>();
            if (health != null && !health.IsAlive)
            {
                // Skip dead tanks → no damage applied
                return;
            }

            // Apply damage
            damagable.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IDamagable damagable = other.GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            Debug.Log($"[DamageZone] OnTriggerExit → {other.name} left the zone.");
        }
    }
}
