// ✅ GameManagerNew.cs (with safe RegisterTank, unique per client)

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum GameState { Lobby, Draft, InGame }

public class GameManagerNew : NetworkBehaviour
{
    public static GameManagerNew Instance { get; private set; }

    [Header("Draft Settings")]
    [SerializeField] private float draftDuration = 30f;
    public float DraftDuration => draftDuration;

    [Header("Tank Prefabs (Selection Order)")]
    [SerializeField] private List<GameObject> tankPrefabs = new();

    [Header("UI")]
    [SerializeField] private GameObject startGameButtonGO;

    public static ProjectileFactory LocalPlayerFactory;
    public static System.Action OnLocalPlayerFactoryAssigned;

    public readonly NetworkVariable<GameState> CurrentGameState =
        new(GameState.Lobby, NetworkVariableReadPermission.Everyone,
                           NetworkVariableWritePermission.Server);

    private readonly Dictionary<ulong, int> teamByClient = new();
    private readonly Dictionary<ulong, int> selectedTankByClient = new();
    private Dictionary<ulong, GameObject> tankByClient = new();
    private Coroutine draftTimerCo;

    private readonly Dictionary<ulong, int> kills = new();
    private readonly Dictionary<ulong, int> deaths = new();


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => RefreshStartButtonVisibility();
    public void RefreshStartButtonVisibility() =>
        startGameButtonGO?.SetActive(NetworkManager.Singleton.IsHost);

    [ServerRpc(RequireOwnership = false)]
    public void StartDraftPhaseServerRpc()
    {
        if (CurrentGameState.Value != GameState.Lobby) return;
        AutoAssignTeams();
        CurrentGameState.Value = GameState.Draft;

        foreach (var kv in teamByClient)
        {
            ReceiveTeamClientRpc(kv.Value, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { kv.Key } }
            });
        }

        DraftUIShowClientRpc();
        if (draftTimerCo != null) StopCoroutine(draftTimerCo);
        draftTimerCo = StartCoroutine(DraftTimeoutCoroutine());
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTankSelectServerRpc(int tankIdx, ServerRpcParams rpc = default)
    {
        ulong cid = rpc.Receive.SenderClientId;
        int team = teamByClient[cid];

        bool taken = selectedTankByClient.Any(kv =>
            kv.Key != cid && teamByClient[kv.Key] == team && kv.Value == tankIdx);

        if (taken)
        {
            SelectionRejectedClientRpc(tankIdx,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { cid } } });
            return;
        }

        if (selectedTankByClient.TryGetValue(cid, out int prev))
        {
            if (prev == tankIdx) return;
            BroadcastTaken(team, prev, false);
        }

        selectedTankByClient[cid] = tankIdx;
        BroadcastTaken(team, tankIdx, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AllPlayersLockedInServerRpc()
    {
        if (CurrentGameState.Value != GameState.Draft) return;
        StartMatch();
    }

    private void BroadcastTaken(int team, int tankIdx, bool nowTaken)
    {
        var teamClients = teamByClient.Where(kv => kv.Value == team).Select(kv => kv.Key).ToArray();
        TeamTankTakenClientRpc(tankIdx, nowTaken,
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = teamClients } });
    }

    private void AutoAssignTeams()
    {
        teamByClient.Clear();
        bool toggle = false;
        foreach (var cid in NetworkManager.ConnectedClientsIds.OrderBy(id => id))
        {
            teamByClient[cid] = toggle ? 1 : 0;
            toggle = !toggle;
        }
    }

    private IEnumerator DraftTimeoutCoroutine()
    {
        yield return new WaitForSeconds(draftDuration);
        if (CurrentGameState.Value == GameState.Draft) StartMatch();
    }

    private void StartMatch()
    {
        if (CurrentGameState.Value != GameState.Draft) return;
        if (draftTimerCo != null) StopCoroutine(draftTimerCo);

        foreach (var cid in NetworkManager.ConnectedClientsIds)
        {
            if (selectedTankByClient.ContainsKey(cid)) continue;

            int fallback = TryUseCurrentSelection(cid);
            if (fallback == -1) fallback = PickRandomAvailableTank(cid);
            selectedTankByClient[cid] = fallback;
        }

        CurrentGameState.Value = GameState.InGame;
        DraftUIHideClientRpc();

        foreach (var cid in NetworkManager.ConnectedClientsIds)
        {
            int tankIdx = selectedTankByClient[cid];
            int team = teamByClient[cid];
            Vector3 spawnPos = TeamSpawnManagerNew.Instance.GetSpawnPoint(team);
            var tank = Instantiate(tankPrefabs[tankIdx], spawnPos, Quaternion.identity);
            tank.GetComponent<NetworkObject>().SpawnWithOwnership(cid);

            RegisterTank(tank);

            if (cid == NetworkManager.Singleton.LocalClientId)
            {
                LocalPlayerFactory = tank.GetComponent<ProjectileFactory>();
                OnLocalPlayerFactoryAssigned?.Invoke();
            }
        }
    }

    private int TryUseCurrentSelection(ulong clientId)
    {
        if (DraftUINew.Instance == null) return -1;
        int chosen = DraftUINew.Instance.GetLastSelectionForClient(clientId);
        if (chosen < 0) return -1;

        int team = teamByClient[clientId];
        bool taken = selectedTankByClient.Any(kvp => teamByClient[kvp.Key] == team && kvp.Value == chosen);
        return taken ? -1 : chosen;
    }

    private int PickRandomAvailableTank(ulong clientId)
    {
        int myTeam = teamByClient[clientId];
        var all = Enumerable.Range(0, tankPrefabs.Count).ToList();
        var taken = selectedTankByClient
            .Where(kvp => teamByClient[kvp.Key] == myTeam)
            .Select(kvp => kvp.Value).ToHashSet();
        var available = all.Except(taken).ToList();
        return available.Count > 0 ? available[Random.Range(0, available.Count)] : 0;
    }

    public void RegisterTank(GameObject tank)
    {
        ulong clientId = tank.GetComponent<NetworkObject>().OwnerClientId;

        if (tankByClient.TryGetValue(clientId, out var existing))
        {
            if (existing != null && existing == tank)
            {
                Debug.LogWarning($"[GMN] Already registered tank for client {clientId}, skipping.");
                return;
            }
        }

        tankByClient[clientId] = tank;

        var health = tank.GetComponent<Health>();
        if (health == null) return;

        health.OnDeath -= OnTankDeath;
        health.OnDeath += OnTankDeath;

        Debug.Log($"[GMN] RegisterTank → bound to {tank.name}, owner={clientId}");
    }

    private void OnTankDeath(Health h)
    {
        Debug.Log($"[GMN] OnTankDeath → RespawnTank for {h.OwnerClientId}");
        RespawnManagerNew.Instance?.RespawnTank(h.gameObject, h.OwnerClientId);
    }

    public int GetTeam(ulong clientId) =>
        teamByClient.TryGetValue(clientId, out var team) ? team : 0;

    public int GetSelectedTankIndex(ulong clientId) =>
        selectedTankByClient.TryGetValue(clientId, out var idx) ? idx : 0;

    public GameObject GetTankPrefab(int idx)
    {
        if (idx >= 0 && idx < tankPrefabs.Count)
            return tankPrefabs[idx];
        return null;
    }

    public void RegisterKill(ulong killerId, ulong victimId)
    {
        if (killerId == ulong.MaxValue || killerId == victimId) return;

        kills[killerId] = kills.GetValueOrDefault(killerId) + 1;
        deaths[victimId] = deaths.GetValueOrDefault(victimId) + 1;

        KillFeedClientRpc(GetPlayerName(killerId), GetPlayerName(victimId));

        // NEW — broadcast full scoreboard
        var ids = kills.Keys.Union(deaths.Keys).ToArray();
        var kArr = ids.Select(id => kills.GetValueOrDefault(id)).ToArray();
        var dArr = ids.Select(id => deaths.GetValueOrDefault(id)).ToArray();
        ScoreUpdateClientRpc(ids, kArr, dArr);
    }

    private string GetPlayerName(ulong cid)
    {
        // Replace with your actual player-name lookup if you have one.
        return $"Player {cid}";
    }

    [ClientRpc]
    private void KillFeedClientRpc(string killerName, string victimName)
    {
        KillFeedUI.Instance?.PushEntry(killerName, victimName);
    }

    [ClientRpc]
    private void ScoreUpdateClientRpc(ulong[] clientIds, int[] killVals, int[] deathVals)
    {
        if (ScoreboardUI.Instance == null) return;
        for (int i = 0; i < clientIds.Length; i++)
            ScoreboardUI.Instance.SetScore(clientIds[i], killVals[i], deathVals[i]);
    }



    [ClientRpc] private void DraftUIShowClientRpc() => DraftUINew.Instance?.Show();
    [ClientRpc] private void DraftUIHideClientRpc() => DraftUINew.Instance?.Hide();

    [ClientRpc]
    private void ReceiveTeamClientRpc(int teamId, ClientRpcParams p = default) =>
        DraftUINew.Instance?.SetTeam(teamId);

    [ClientRpc]
    private void TeamTankTakenClientRpc(int tankIdx, bool isTaken, ClientRpcParams p = default) =>
        DraftUINew.Instance?.UpdateTaken(tankIdx, isTaken);

    [ClientRpc]
    private void SelectionRejectedClientRpc(int tankIdx, ClientRpcParams p = default) =>
        DraftUINew.Instance?.OnSelectionRejected(tankIdx);
}
