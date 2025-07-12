// ✅ RespawnManagerNew.cs (fully integrated with GameManagerNew + logging)

using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class RespawnManagerNew : MonoBehaviour
{
    public static RespawnManagerNew Instance { get; private set; }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3f;

    [Header("Fallback Tank Prefabs")]
    [SerializeField] private List<GameObject> fallbackTankPrefabs;

    public float RespawnDelay => respawnDelay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RespawnTank(GameObject oldTankObject, ulong ownerClientId)
    {
        Debug.Log($"[RESPAWN] Queued respawn for client {ownerClientId}");
        StartCoroutine(RespawnTankCoroutine(oldTankObject, ownerClientId));
    }

    private IEnumerator RespawnTankCoroutine(GameObject tankObject, ulong ownerClientId)
    {
        Debug.Log($"[RESPAWN] Coroutine start for client {ownerClientId}");
        yield return new WaitForSeconds(respawnDelay);

        if (tankObject != null && tankObject.GetComponent<NetworkObject>()?.IsSpawned == true)
        {
            tankObject.GetComponent<NetworkObject>().Despawn(true);
            Debug.Log($"[RESPAWN] Despawned old tank for client {ownerClientId}");
        }

        int team = GameManagerNew.Instance.GetTeam(ownerClientId);
        int tankIndex = GameManagerNew.Instance.GetSelectedTankIndex(ownerClientId);

        GameObject prefabToUse = GameManagerNew.Instance.GetTankPrefab(tankIndex);
        if (prefabToUse == null && fallbackTankPrefabs.Count > 0)
        {
            Debug.LogWarning($"[RESPAWN] Using fallback prefab for client {ownerClientId}");
            prefabToUse = fallbackTankPrefabs[0];
        }

        Vector3 spawnPos = TeamSpawnManagerNew.Instance.GetSpawnPoint(team);
        GameObject newTank = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
        newTank.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);

        var health = newTank.GetComponent<Health>();
        health.ForceSetInvincible(true);
        StartCoroutine(DelayedClearInvincibility(health));

        GameManagerNew.Instance.RegisterTank(newTank);

        if (ownerClientId == NetworkManager.Singleton.LocalClientId)
        {
            GameManagerNew.LocalPlayerFactory = newTank.GetComponent<ProjectileFactory>();
            GameManagerNew.OnLocalPlayerFactoryAssigned?.Invoke();
        }

        Debug.Log($"[RESPAWN] Spawned tank {tankIndex} for client {ownerClientId}, netID={newTank.GetComponent<NetworkObject>().NetworkObjectId}");
    }

    private IEnumerator DelayedClearInvincibility(Health health)
    {
        yield return new WaitForSeconds(health.InvincibilityDuration);
        health.ForceSetInvincible(false);
        Debug.Log($"[RESPAWN] Invincibility cleared for tank {health.gameObject.name}");
    }
}
