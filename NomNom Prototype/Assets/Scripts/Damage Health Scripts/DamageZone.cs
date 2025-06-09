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
        }
    }

    private void OnTriggerStay(Collider other)
    {
        IDamagable damagable = other.GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health != null && (!health.IsAlive || health.IsInvincible))
            {
                // Skip dead tanks or invincible tanks → no damage applied
                return;
            }

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
