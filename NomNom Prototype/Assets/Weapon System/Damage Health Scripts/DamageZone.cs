using UnityEngine;
using System.Collections.Generic;

public class DamageZone : MonoBehaviour
{
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private float damageDelaySeconds = 0.5f; // NEW → delay before starting damage

    // Track entry time per collider
    private Dictionary<Health, float> tankEntryTimes = new Dictionary<Health, float>();

    private void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponent<Health>();
        if (health != null && !tankEntryTimes.ContainsKey(health))
        {
            // Record time of entry
            tankEntryTimes[health] = Time.time;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        var health = other.GetComponent<Health>();
        if (health != null)
        {
            if (!health.IsAlive)
                return;

            // Check how long this tank has been inside the zone
            if (tankEntryTimes.TryGetValue(health, out float entryTime))
            {
                if (Time.time - entryTime >= damageDelaySeconds)
                {
                    // Start applying damage
                    health.TakeDamage(damagePerSecond * Time.deltaTime);
                }
            }
            else
            {
                // Edge case: somehow OnTriggerStay called before OnTriggerEnter
                tankEntryTimes[health] = Time.time;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var health = other.GetComponent<Health>();
        if (health != null && tankEntryTimes.ContainsKey(health))
        {
            // Reset timer when leaving zone
            tankEntryTimes.Remove(health);
        }
    }
}
