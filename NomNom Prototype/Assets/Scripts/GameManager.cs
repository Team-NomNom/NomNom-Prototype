using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [Header("Tank Prefab")]
    [SerializeField] private GameObject tankPrefab;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints;

    public static ProjectileFactory LocalPlayerFactory { get; set; }
    public static System.Action OnLocalPlayerFactoryAssigned;

    public static GameManager Instance { get; private set; }

    private RespawnManager respawnManager;

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

            Debug.Log("OMG ITS OMG OMG IGS OMG");
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
        // Only server spawns tanks
        if (IsServer)
        {
            SpawnTankForClient(clientId);
            Debug.Log("OMG ITS OMG OMG IGS OMG");
        }
    }

    public void SpawnTankForClient(ulong clientId)
    {
        Debug.Log("[GameManager] SpawnTankForClient STARTED.");

        int spawnIndex = (int)(clientId % (ulong)spawnPoints.Count);
        Transform spawnPoint = spawnPoints[spawnIndex];

        GameObject tankInstance = Instantiate(tankPrefab, spawnPoint.position, spawnPoint.rotation);
        tankInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        Debug.Log($"[GameManager] Checking LocalPlayerFactory assignment → clientId={clientId}, LocalClientId={NetworkManager.Singleton.LocalClientId}");

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            LocalPlayerFactory = tankInstance.GetComponent<ProjectileFactory>();
            Debug.Log("[GameManager] LocalPlayerFactory assigned.");
            OnLocalPlayerFactoryAssigned?.Invoke();
        }

        RegisterTank(tankInstance);

        Debug.Log($"Spawned tank for client {clientId} at spawn {spawnIndex}");
    }

    public void RegisterTank(GameObject tank)
    {
        var health = tank.GetComponent<Health>();

        if (health == null)
        {
            Debug.LogError($"[GameManager] RegisterTank → Tank {tank.name} is missing Health component!");
            return;
        }

        Debug.Log($"[GameManager] RegisterTank → Tank={tank.name}, OwnerClientId={tank.GetComponent<NetworkObject>()?.OwnerClientId}");

        // Safe pattern → use OnTankDeath method instead of inline lambda
        health.OnDeath += OnTankDeath;

        Debug.Log($"[GameManager] Registered OnDeath for tank {tank.name}, OwnerClientId: {tank.GetComponent<NetworkObject>()?.OwnerClientId}");
    }

    private void OnTankDeath(Health h)
    {
        Debug.Log($"[GameManager] OnDeath triggered for tank {h.gameObject.name}, OwnerClientId: {h.OwnerClientId} → calling RespawnTank");

        // Start RespawnTankCoroutine safely
        respawnManager.StartCoroutine(respawnManager.RespawnTankCoroutine(h.gameObject, h.OwnerClientId));
    }
}
