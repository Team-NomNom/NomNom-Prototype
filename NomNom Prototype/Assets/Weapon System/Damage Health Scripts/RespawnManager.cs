using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform[] spawnPoints; // Assign spawn points in inspector
    [SerializeField] private float respawnDelay = 3f;

    public void RespawnTank(GameObject tankObject, ulong ownerClientId)
    {
        Debug.Log($"[RespawnManager] RespawnTank → OwnerClientId: {ownerClientId}");

        StartCoroutine(RespawnTankCoroutine(tankObject, ownerClientId));
    }

    private IEnumerator RespawnTankCoroutine(GameObject tankObject, ulong ownerClientId)
    {
        // Wait for respawn delay
        yield return new WaitForSeconds(respawnDelay);

        // Pick a spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Debug.Log($"[RespawnManager] RespawnTankCoroutine → Using spawn point: {spawnPoint.position}, rotation Y: {spawnPoint.rotation.eulerAngles.y}");

        // Move tank safely → using coroutine
        Vector3 spawnPos = spawnPoint.position + Vector3.up * 0.5f; // optional small Y offset
        float spawnRotationY = spawnPoint.rotation.eulerAngles.y;

        yield return StartCoroutine(SetTankPositionSafe(tankObject, spawnPos, spawnRotationY));

        // Reset health
        Health health = tankObject.GetComponent<Health>();
        if (health != null)
        {
            // Disable collider FIRST
            Collider col = tankObject.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
                Debug.Log($"[RespawnManager] Tank {tankObject.name} collider DISABLED during invincibility.");
            }

            // Reset health and trigger invincibility
            health.ResetHealth();

            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
                cam.ForceSnap();

            Debug.Log($"[RespawnManager] Tank {tankObject.name} health reset.");

            // Wait for invincibility window
            yield return new WaitForSeconds(health.InvincibilityDuration);

            // Re-enable collider AFTER invincibility ends
            if (col != null)
            {
                col.enabled = true;
                Debug.Log($"[RespawnManager] Tank {tankObject.name} collider RE-ENABLED after invincibility.");
            }
        }
        else
        {
            Debug.LogError($"[RespawnManager] Tank {tankObject.name} is missing Health component!");
        }
    }


    private IEnumerator SetTankPositionSafe(GameObject tankObject, Vector3 newPosition, float spawnRotationY)
    {
        Rigidbody rb = tankObject.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"[RespawnManager] SetTankPositionSafe -> Tank {tankObject.name} is missing Rigidbody!");
            yield break;
        }

        // TEMP disable physics
        rb.isKinematic = true;

        // Move tank
        tankObject.transform.position = newPosition;

        // Set tank rotation to match spawn point Y rotation
        Quaternion targetRotation = Quaternion.Euler(0f, spawnRotationY, 0f);
        tankObject.transform.rotation = targetRotation;

        // Reset velocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[RespawnManager] SetTankPositionSafe → Tank {tankObject.name} moved to {newPosition} with Y rotation: {spawnRotationY}");

        // Wait 1 physics frame → let Physics resolve any overlap
        yield return null;

        // Re-enable physics
        rb.isKinematic = false;

        Debug.Log($"[RespawnManager] Rigidbody isKinematic AFTER: {rb.isKinematic}, velocity: {rb.linearVelocity}, angularVelocity: {rb.angularVelocity}");
    }
}
