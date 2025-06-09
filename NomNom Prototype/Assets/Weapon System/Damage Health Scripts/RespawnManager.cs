using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform[] spawnPoints; // Assign spawn points in inspector
    [SerializeField] private float respawnDelay = 3f;

    [Header("Tank Prefab")]
    [SerializeField] private GameObject tankPrefab; // You MUST assign this in inspector → same prefab used by GameManager

    [Header("Game Manager Reference")]
    [SerializeField] private GameManager gameManager; // Assigned in inspector → used for clarity / possible future needs

    public void RespawnTank(GameObject oldTankObject, ulong ownerClientId)
    {
        Debug.Log($"[RespawnManager] RespawnTank → OwnerClientId: {ownerClientId}");

        StartCoroutine(RespawnTankCoroutine(oldTankObject, ownerClientId));
    }

    private IEnumerator RespawnTankCoroutine(GameObject oldTankObject, ulong ownerClientId)
    {
        // Wait for respawn delay
        yield return new WaitForSeconds(respawnDelay);

        // Pick a spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Debug.Log($"[RespawnManager] RespawnTankCoroutine → Using spawn point: {spawnPoint.position}, rotation Y: {spawnPoint.rotation.eulerAngles.y}");

        // Despawn and destroy old tank (server authoritative)
        var netObj = oldTankObject.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            Debug.Log($"[RespawnManager] Despawning and destroying old tank: {oldTankObject.name} (OwnerClientId: {ownerClientId})");

            netObj.Despawn(true); // 🚀 destroy old tank fully → prevents duplicates
        }
        else
        {
            Debug.LogWarning($"[RespawnManager] Tried to despawn tank but NetworkObject was not spawned or missing.");
        }

        // Small delay to ensure clients process despawn cleanly
        yield return new WaitForSeconds(0.1f);

        // Instantiate new tank instance
        GameObject newTankInstance = Instantiate(tankPrefab, spawnPoint.position, spawnPoint.rotation);

        Debug.Log($"[RespawnManager] Instantiate SUCCESS → newTankInstance name: {newTankInstance.name}");

        // Get NetworkObject
        var newNetObj = newTankInstance.GetComponent<NetworkObject>();

        if (newNetObj == null)
        {
            Debug.LogError("[RespawnManager] New tank is missing NetworkObject!");
            yield break;
        }

        // SPAWN WITH OWNERSHIP → KEY STEP
        newNetObj.SpawnWithOwnership(ownerClientId);
        Debug.Log($"[RespawnManager] Spawned new tank for client {ownerClientId} at spawn point {spawnPoint.position}");

        // 🚀 Now WAIT to ensure NetworkVariable sync is complete
        yield return new WaitForSeconds(0.1f);

        // ResetHealth AFTER proper sync → correct invincibility flow
        var newTankHealth = newTankInstance.GetComponent<Health>();
        if (newTankHealth != null)
        {
            newTankHealth.ResetHealth();
            Debug.Log($"[RespawnManager] ResetHealth called for new tank {newTankInstance.name}");
        }
        else
        {
            Debug.LogError($"[RespawnManager] New tank {newTankInstance.name} is missing Health component!");
        }
    }
}
