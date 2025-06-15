using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Tank Prefab (default fallback)")]
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
        yield return new WaitForSeconds(respawnDelay);

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Debug.Log($"[RespawnManager] RespawnTankCoroutine → Using spawn point: {spawnPoint.position}, rotation Y: {spawnPoint.rotation.eulerAngles.y}");

        if (tankObject != null && tankObject.GetComponent<NetworkObject>()?.IsSpawned == true)
        {
            tankObject.GetComponent<NetworkObject>().Despawn(true);
            Debug.Log($"[RespawnManager] Despawned old tank for client {ownerClientId}");
        }

        // 🧠 Choose tank prefab from GameManager
        int tankIndex = 0;
        if (GameManager.Instance.TryGetTankChoice(ownerClientId, out int chosenIndex))
        {
            if (chosenIndex >= 0 && chosenIndex < GameManager.Instance.AvailableTankPrefabCount)
                tankIndex = chosenIndex;
            else
                Debug.LogWarning($"[RespawnManager] Invalid tank index {chosenIndex} for client {ownerClientId}, defaulting to 0.");
        }

        GameObject prefabToUse = GameManager.Instance.GetTankPrefab(tankIndex);
        GameObject newTankInstance = Instantiate(prefabToUse, spawnPoint.position, spawnPoint.rotation);
        newTankInstance.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);

        var health = newTankInstance.GetComponent<Health>();
        health.ForceSetInvincible(true);
        StartCoroutine(DelayedClearInvincible(health));

        Debug.Log($"[RespawnManager] Spawned tank {tankIndex} → NetworkObjectId={newTankInstance.GetComponent<NetworkObject>().NetworkObjectId}, OwnerClientId={newTankInstance.GetComponent<NetworkObject>().OwnerClientId}");

        GameManager.Instance.RegisterTank(newTankInstance);

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
