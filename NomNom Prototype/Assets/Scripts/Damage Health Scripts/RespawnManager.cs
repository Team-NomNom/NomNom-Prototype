using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Tank Prefab")]
    [SerializeField] private GameObject tankPrefab;

    [Header("Game Manager Reference")]
    [SerializeField] private GameManager gameManager;

    public float RespawnDelay => respawnDelay;

    public void RespawnTank(GameObject oldTankObject, ulong ownerClientId)
    {
        Debug.Log($"[RespawnManager] RespawnTank → OwnerClientId: {ownerClientId}");

        StartCoroutine(RespawnTankCoroutine(oldTankObject, ownerClientId));
    }

    private IEnumerator RespawnTankCoroutine(GameObject oldTankObject, ulong ownerClientId)
    {
        yield return new WaitForSeconds(respawnDelay);

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Debug.Log($"[RespawnManager] RespawnTankCoroutine → Using spawn point: {spawnPoint.position}, rotation Y: {spawnPoint.rotation.eulerAngles.y}");

        var netObj = oldTankObject.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            Debug.Log($"[RespawnManager] Despawning and destroying old tank: {oldTankObject.name} (OwnerClientId: {ownerClientId})");

            netObj.Despawn(true);
        }
        else
        {
            Debug.LogWarning($"[RespawnManager] Tried to despawn tank but NetworkObject was not spawned or missing.");
        }

        yield return new WaitForSeconds(0.1f);

        GameObject newTankInstance = Instantiate(tankPrefab, spawnPoint.position, spawnPoint.rotation);

        Debug.Log($"[RespawnManager] Instantiate SUCCESS → newTankInstance name: {newTankInstance.name}");

        var newNetObj = newTankInstance.GetComponent<NetworkObject>();

        if (newNetObj == null)
        {
            Debug.LogError("[RespawnManager] New tank is missing NetworkObject!");
            yield break;
        }

        newNetObj.SpawnWithOwnership(ownerClientId);
        Debug.Log($"[RespawnManager] Spawned new tank for client {ownerClientId} at spawn point {spawnPoint.position}");

        // Wait small delay -> ensure clients are fully synced
        yield return new WaitForSeconds(0.1f);

        var newTankHealth = newTankInstance.GetComponent<Health>();
        if (newTankHealth == null)
        {
            Debug.LogError($"[RespawnManager] New tank {newTankInstance.name} is missing Health component!");
        }
        else
        {
            // ForceSetInvincible and delayed clear
            newTankHealth.ForceSetInvincible(true);
            Debug.Log($"[RespawnManager] ForceSetInvincible(true) called on new tank {newTankInstance.name}");

            StartCoroutine(DelayedClearInvincible(newTankHealth));
        }
    }

    private IEnumerator DelayedClearInvincible(Health health)
    {
        yield return new WaitForSeconds(health.InvincibilityDuration);
        health.ForceSetInvincible(false);
        Debug.Log($"[RespawnManager] ForceSetInvincible(false) called after invincibility duration for tank {health.gameObject.name}");
    }

}
