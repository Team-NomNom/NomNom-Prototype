using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class RespawnManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    public List<Transform> spawnPoints;

    public void RespawnTank(GameObject tank, ulong clientId)
    {
        StartCoroutine(RespawnTankRoutine(tank, clientId));
    }

    private IEnumerator RespawnTankRoutine(GameObject tank, ulong clientId)
    {
        float respawnDelay = 3f;
        Debug.Log($"Respawning tank {clientId} in {respawnDelay} seconds...");

        yield return new WaitForSeconds(respawnDelay);

        Transform spawnPoint = GetRandomSpawnPoint();

        tank.transform.position = spawnPoint.position;
        tank.transform.rotation = spawnPoint.rotation;

        if (!tank.GetComponent<NetworkObject>().IsSpawned)
        {
            tank.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        }

        tank.SetActive(true);

        var health = tank.GetComponent<Health>();
        health.ResetHealth();

        Debug.Log($"Tank {clientId} respawned!");
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return null;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
}
