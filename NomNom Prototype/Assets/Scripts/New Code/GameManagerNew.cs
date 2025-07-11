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

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> team0Spawns = new();
    [SerializeField] private List<Transform> team1Spawns = new();

    [Header("UI")]
    [SerializeField] private GameObject startGameButtonGO;

    // ───────── Net-synced phase
    public readonly NetworkVariable<GameState> CurrentGameState =
        new(GameState.Lobby, NetworkVariableReadPermission.Everyone,
                           NetworkVariableWritePermission.Server);

    // ───────── Server-only state
    private readonly Dictionary<ulong, int> teamByClient = new(); // 0 blue, 1 red
    private readonly Dictionary<ulong, int> selectedTankByClient = new(); // client → tankIdx

    private Coroutine draftTimerCo;

    // ───────── Mono
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }
    private void Start() => RefreshStartButtonVisibility();
    public void RefreshStartButtonVisibility() =>
        startGameButtonGO?.SetActive(NetworkManager.Singleton.IsHost);

    // ─────────────────────────────────────────────
    //  Server RPCs —  UI calls
    // ─────────────────────────────────────────────
    #region RPC_API
    [ServerRpc(RequireOwnership = false)]
    public void StartDraftPhaseServerRpc()
    {
        if (CurrentGameState.Value != GameState.Lobby) return;

        AutoAssignTeams();
        CurrentGameState.Value = GameState.Draft;

        foreach (var kv in teamByClient)
        {
            var toClient = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { kv.Key } }
            };
            ReceiveTeamClientRpc(kv.Value, toClient);
        }

        DraftUIShowClientRpc();
        if (draftTimerCo != null) StopCoroutine(draftTimerCo);
        draftTimerCo = StartCoroutine(DraftTimeoutCoroutine());
    }

    /// Player requests a tank. Server grants only if NOT taken by same team.
    [ServerRpc(RequireOwnership = false)]
    public void RequestTankSelectServerRpc(int tankIdx, ServerRpcParams rpc = default)
    {
        ulong cid = rpc.Receive.SenderClientId;
        int team = teamByClient[cid];

        // already picked by teammate?
        bool taken = selectedTankByClient.Any(kv =>
            kv.Key != cid && teamByClient[kv.Key] == team && kv.Value == tankIdx);

        if (taken)
        {
            // Reject back to that client
            SelectionRejectedClientRpc(tankIdx,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { cid } } });
            return;
        }

        // Release previous pick (if any) for this client
        if (selectedTankByClient.TryGetValue(cid, out int prev))
        {
            if (prev == tankIdx) return; // same pick -> ignore
            BroadcastTaken(team, prev, false);
        }

        selectedTankByClient[cid] = tankIdx;
        BroadcastTaken(team, tankIdx, true);
    }

    /// Called when player presses “Lock In”
    [ServerRpc(RequireOwnership = false)]
    public void AllPlayersLockedInServerRpc()
    {
        if (CurrentGameState.Value != GameState.Draft) return;
        StartMatch();
    }
    #endregion

    // ───────── Helper:  broadcast taken/free to team
    private void BroadcastTaken(int team, int tankIdx, bool nowTaken)
    {
        var teamClients = teamByClient
            .Where(kv => kv.Value == team)
            .Select(kv => kv.Key)
            .ToArray();

        TeamTankTakenClientRpc(tankIdx, nowTaken,
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = teamClients } });
    }

    // ───────── Team assignment
    private void AutoAssignTeams()
    {
        teamByClient.Clear();
        bool toggle = false;
        foreach (var cid in NetworkManager.ConnectedClientsIds.OrderBy(id => id))
        {
            teamByClient[cid] = toggle ? 1 : 0; toggle = !toggle;
        }
    }

    // ───────── Draft timeout
    private IEnumerator DraftTimeoutCoroutine()
    {
        yield return new WaitForSeconds(draftDuration);
        if (CurrentGameState.Value == GameState.Draft) StartMatch();
    }

    // ───────── Match start
    private void StartMatch()
    {
        if (CurrentGameState.Value != GameState.Draft) return;
        if (draftTimerCo != null) StopCoroutine(draftTimerCo);

        Debug.Log("[GameManager] Starting match...");

        // ───────── Auto-assign tanks to unready players ─────────
        foreach (var cid in NetworkManager.ConnectedClientsIds)
        {
            if (selectedTankByClient.ContainsKey(cid)) continue;

            int fallback = TryUseCurrentSelection(cid);

            if (fallback == -1) fallback = PickRandomAvailableTank(cid);

            selectedTankByClient[cid] = fallback;

            Debug.Log($"[AutoPick] Assigned tank {fallback} to player {cid}");
        }

        // ───────── Spawn Tanks ─────────
        CurrentGameState.Value = GameState.InGame;
        DraftUIHideClientRpc();

        foreach (var cid in NetworkManager.ConnectedClientsIds)
        {
            int tankIdx = selectedTankByClient[cid];
            int team = teamByClient[cid];

            Vector3 spawnPos = TeamSpawnManagerNew.Instance.GetSpawnPoint(team);
            var tank = Instantiate(tankPrefabs[tankIdx], spawnPos, Quaternion.identity);
            tank.GetComponent<NetworkObject>().SpawnWithOwnership(cid);
        }

        Debug.Log("[GameManager] Match started.");
    }

    /// Try to keep their *current preview selection* if still valid.
    /// Returns -1 if invalid or unknown.
    private int TryUseCurrentSelection(ulong clientId)
    {
        // Ask UI what the player was last hovering on
        if (DraftUINew.Instance == null) return -1;

        int chosen = DraftUINew.Instance.GetLastSelectionForClient(clientId);
        if (chosen < 0) return -1;

        int team = teamByClient[clientId];

        // Check if any teammate already locked it
        bool taken = selectedTankByClient.Any(kvp =>
            teamByClient[kvp.Key] == team && kvp.Value == chosen);

        return taken ? -1 : chosen;
    }

    private int PickRandomAvailableTank(ulong clientId)
    {
        int myTeam = teamByClient[clientId];
        var all = Enumerable.Range(0, tankPrefabs.Count).ToList();

        var taken = selectedTankByClient
            .Where(kvp => teamByClient[kvp.Key] == myTeam)
            .Select(kvp => kvp.Value)
            .ToHashSet();

        var available = all.Except(taken).ToList();

        if (available.Count == 0)
        {
            Debug.LogWarning($"[AutoPick] No tanks left for team {myTeam}, fallback to 0.");
            return 0;
        }

        return available[Random.Range(0, available.Count)];
    }



    // ───────── Client RPCs
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
