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

    public IEnumerator RespawnTankCoroutine(GameObject tankObject, ulong ownerClientId)
    {
        // Wait for respawn delay
        yield return new WaitForSeconds(respawnDelay);

        // Pick a spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Debug.Log($"[RespawnManager] RespawnTankCoroutine → Using spawn point: {spawnPoint.position}, rotation Y: {spawnPoint.rotation.eulerAngles.y}");

        // 🚀 Clean up old tank (optional → recommended to avoid "ghost" tanks)
        if (tankObject != null && tankObject.GetComponent<NetworkObject>() != null && tankObject.GetComponent<NetworkObject>().IsSpawned)
        {
            tankObject.GetComponent<NetworkObject>().Despawn(true);  // true = destroy GameObject
            Debug.Log($"[RespawnManager] Despawned old tank for client {ownerClientId}");
        }

        // Spawn new tank
        GameObject newTankInstance = Instantiate(tankPrefab, spawnPoint.position, spawnPoint.rotation);
        newTankInstance.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);

        // 🚀 Force invincibility ON, then start delayed clear
        var health = newTankInstance.GetComponent<Health>();
        health.ForceSetInvincible(true);
        StartCoroutine(DelayedClearInvincible(health));

        Debug.Log($"[RespawnManager] Spawned tank → NetworkObjectId={newTankInstance.GetComponent<NetworkObject>().NetworkObjectId}, OwnerClientId={newTankInstance.GetComponent<NetworkObject>().OwnerClientId}, IsSpawned={newTankInstance.GetComponent<NetworkObject>().IsSpawned}");

        // Re-register OnDeath for new tank
        GameManager.Instance.RegisterTank(newTankInstance);

        // If this is the local player → assign LocalPlayerFactory → keep Ammo UI correct
        if (ownerClientId == NetworkManager.Singleton.LocalClientId)
        {
            GameManager.LocalPlayerFactory = newTankInstance.GetComponent<ProjectileFactory>();
            GameManager.OnLocalPlayerFactoryAssigned?.Invoke();
            Debug.Log("[RespawnManager] Reassigned LocalPlayerFactory after respawn.");
        }
    }

    private IEnumerator DelayedClearInvincible(Health health)
    {
        yield return new WaitForSeconds(health.InvincibilityDuration);
        health.ForceSetInvincible(false);
        Debug.Log($"[RespawnManager] ForceSetInvincible(false) called after invincibility duration for tank {health.gameObject.name}");
    }
}
