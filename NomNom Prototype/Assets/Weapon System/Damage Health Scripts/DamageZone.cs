using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [Header("Damage Zone Settings")]
    [Tooltip("Damage applied per tick interval to objects inside the zone.")]
    [SerializeField] private float damagePerTick = 10f;

    [Tooltip("How often to apply damage (in seconds).")]
    [SerializeField] private float damageInterval = 0.5f;

    private readonly Dictionary<Health, float> trackedHealths = new Dictionary<Health, float>();

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        Debug.Log("[DamageZone] Awake — ready.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null && !trackedHealths.ContainsKey(health))
        {
            trackedHealths[health] = 0f; // start timer at 0
            Debug.Log($"[DamageZone] {other.gameObject.name} entered zone. Now tracking {health.gameObject.name}.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null && trackedHealths.Remove(health))
        {
            Debug.Log($"[DamageZone] {other.gameObject.name} exited zone. Stopped tracking {health.gameObject.name}.");
        }
    }

    private void Update()
    {
        if (trackedHealths.Count == 0) return;

        List<Health> toRemove = new List<Health>();

        foreach (var pair in trackedHealths)
        {
            Health health = pair.Key;
            float timer = pair.Value;

            if (health == null)
            {
                toRemove.Add(health);
                continue;
            }

            timer += Time.deltaTime;

            if (timer >= damageInterval)
            {
                Debug.Log($"[DamageZone] Damaging {health.gameObject.name} for {damagePerTick}.");
                health.TakeDamage(damagePerTick);
                timer = 0f;
            }

            trackedHealths[health] = timer; 
        }

        foreach (var health in toRemove)
        {
            trackedHealths.Remove(health);
        }
    }
}
