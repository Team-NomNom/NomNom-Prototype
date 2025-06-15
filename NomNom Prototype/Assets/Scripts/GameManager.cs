using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [Header("Tank Prefabs")]
    [SerializeField] private List<GameObject> tankPrefabs;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints;

    public static ProjectileFactory LocalPlayerFactory { get; set; }
    public static System.Action OnLocalPlayerFactoryAssigned;

    public static GameManager Instance { get; private set; }

    private RespawnManager respawnManager;

    private Dictionary<ulong, GameObject> clientTanks = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, int> playerTankChoices = new Dictionary<ulong, int>();

    public int AvailableTankPrefabCount => tankPrefabs.Count;

    public GameObject GetTankPrefab(int index)
    {
        return (index >= 0 && index < tankPrefabs.Count) ? tankPrefabs[index] : tankPrefabs[0];
    }

    public bool TryGetTankChoice(ulong clientId, out int index)
    {
        return playerTankChoices.TryGetValue(clientId, out index);
    }

    public void SetTankChoice(ulong clientId, int prefabIndex)
    {
        playerTankChoices[clientId] = prefabIndex;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        respawnManager = FindObjectOfType<RespawnManager>();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Always spawn host tank explicitly
            SpawnTankForClient(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            SpawnTankForClient(clientId);
        }
    }

    public void SpawnTankForClient(ulong clientId)
    {
        Debug.Log("[GameManager] SpawnTankForClient STARTED.");

        int spawnIndex = (int)(clientId % (ulong)spawnPoints.Count);
        Transform spawnPoint = spawnPoints[spawnIndex];

        int tankIndex = 0;
        if (playerTankChoices.TryGetValue(clientId, out int chosenIndex))
        {
            if (chosenIndex >= 0 && chosenIndex < tankPrefabs.Count)
                tankIndex = chosenIndex;
            else
                Debug.LogWarning($"Invalid tank index {chosenIndex} for client {clientId}, defaulting to 0.");
        }

        GameObject prefabToUse = tankPrefabs[tankIndex];
        GameObject tankInstance = Instantiate(prefabToUse, spawnPoint.position, spawnPoint.rotation);
        tankInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            LocalPlayerFactory = tankInstance.GetComponent<ProjectileFactory>();
            OnLocalPlayerFactoryAssigned?.Invoke();
        }

        RegisterTank(tankInstance);
        clientTanks[clientId] = tankInstance;

        Debug.Log($"Spawned tank {tankIndex} for client {clientId} at spawn {spawnIndex}");
    }

    public void RegisterTank(GameObject tank)
    {
        var health = tank.GetComponent<Health>();

        if (health == null)
        {
            Debug.LogError($"[GameManager] RegisterTank → Tank {tank.name} is missing Health component!");
            return;
        }

        health.OnDeath += OnTankDeath;

        Debug.Log($"[GameManager] Registered OnDeath for tank {tank.name}, OwnerClientId: {tank.GetComponent<NetworkObject>()?.OwnerClientId}");
    }

    private void OnTankDeath(Health h)
    {
        Debug.Log($"[GameManager] OnDeath triggered for tank {h.gameObject.name}, OwnerClientId: {h.OwnerClientId} → calling RespawnTank");

        if (respawnManager != null)
        {
            respawnManager.StartCoroutine(respawnManager.RespawnTankCoroutine(h.gameObject, h.OwnerClientId));
        }
    }

    public void DespawnTankForClient(ulong clientId)
    {
        if (clientTanks.TryGetValue(clientId, out GameObject tank))
        {
            if (tank != null && tank.GetComponent<NetworkObject>()?.IsSpawned == true)
            {
                tank.GetComponent<NetworkObject>().Despawn(true);
                Debug.Log($"[GameManager] Despawned tank for client {clientId}");
            }

            clientTanks.Remove(clientId);
        }
        else
        {
            Debug.LogWarning($"[GameManager] DespawnTankForClient → no tank found for client {clientId}");
        }
    }
}
