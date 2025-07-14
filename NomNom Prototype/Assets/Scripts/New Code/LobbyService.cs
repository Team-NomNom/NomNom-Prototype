using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay; // transport relay helpers
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Pure logic wrapper for Unity Lobby + Relay.
/// - Creates / joins / leaves lobbies
/// - Exposes C# events for UI
/// - Sets Relay data, then calls NetworkBootstrapNew.StartHost/Client().
/// </summary>
public class LobbyService : MonoBehaviour
{
    /* --------------- singleton --------------- */
    public static LobbyService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* --------------- public events --------------- */
    public event Action<List<Lobby>> OnServerListRefreshed;
    public event Action<Lobby> OnJoinedLobby;
    public event Action OnLeftLobby;
    public event Action<string> OnError;             // simple error channel
    public event Action<float> OnPingUpdated;

    /* --------------- config --------------- */
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private float pollInterval = 3f;
    [SerializeField] private float heartbeatInterval = 15f;

    /* --------------- state --------------- */
    private Lobby lobby;
    private Coroutine pollCo, heartbeatCo, pingCo;

    /* --------------- public API --------------- */

    /// <summary>Create a Host lobby.  isPrivate = true hides the lobby from the public list.</summary>
    public async Task HostAsync(bool isPrivate = false)
    {
        try
        {
            await EnsureServicesSignedIn();

            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            lobby = await Lobbies.Instance.CreateLobbyAsync(
                "Lobby_" + UnityEngine.Random.Range(1000, 9999),
                maxPlayers,
                new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Player = CreatePlayerData(),
                    Data = new Dictionary<string, DataObject> {
                        { "Relay", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                });

            SetRelayDataHost(alloc);

            FindObjectOfType<NetworkBootstrapNew>().StartHost();
            StartCoroutines();
            OnJoinedLobby?.Invoke(lobby);
        }
        catch (Exception e) { ReportError(e); }
    }

    /// <summary>Join a private lobby by exact code.</summary>
    public async Task JoinByCodeAsync(string code)
    {
        try
        {
            await EnsureServicesSignedIn();

            lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(
                code, new JoinLobbyByCodeOptions { Player = CreatePlayerData() });

            string joinCode = lobby.Data["Relay"].Value;
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            SetRelayDataClient(joinAlloc);

            FindObjectOfType<NetworkBootstrapNew>().StartClient();
            StartCoroutines();
            OnJoinedLobby?.Invoke(lobby);
        }
        catch (Exception e) { ReportError(e); }
    }

    /// <summary>Fetch all public lobbies (IsPrivate == false).</summary>
    public async Task RefreshPublicListAsync()
    {
        try
        {
            await EnsureServicesSignedIn();

            var lobbies = await Lobbies.Instance.QueryLobbiesAsync
            (
                new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter> {
                        new QueryFilter(
                            field: QueryFilter.FieldOptions.IsLocked,
                            op:    QueryFilter.OpOptions.EQ,
                            value: "0")
                    }
                });
            OnServerListRefreshed?.Invoke(lobbies.Results);
        }
        catch (Exception e) { ReportError(e); }
    }

    /// <summary>Leave or delete the current lobby and shut down Netcode.</summary>
    public async Task LeaveAsync()
    {
        try
        {
            if (lobby != null)
            {
                if (NetworkManager.Singleton.IsHost)
                    await Lobbies.Instance.DeleteLobbyAsync(lobby.Id);
                else
                    await Lobbies.Instance.RemovePlayerAsync(
                        lobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e) { Debug.LogWarning(e); }

        lobby = null;
        StopCoroutines();
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        OnLeftLobby?.Invoke();
    }

    /* --------------- internal helpers --------------- */

    private async Task EnsureServicesSignedIn()
    {
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private Player CreatePlayerData()
    {
        string name = PlayerPrefs.GetString("PlayerName",
                       "Player_" + UnityEngine.Random.Range(1000, 9999));
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                { "DisplayName",
                  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) }
            }
        };
    }

    private void SetRelayDataHost(Allocation alloc)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(new RelayServerData(alloc, "dtls"));
    }
    private void SetRelayDataClient(JoinAllocation join)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(new RelayServerData(join, "dtls"));
    }

    /* --------------- error wrapper --------------- */
    private void ReportError(Exception e)
    {
        Debug.LogError(e);
        OnError?.Invoke(e.Message);
    }

    /* --------------- heartbeat, poll, ping coroutines --------------- */

    private void StartCoroutines()
    {
        StopCoroutines();
        if (NetworkManager.Singleton.IsHost)
            heartbeatCo = StartCoroutine(Heartbeat());
        pollCo = StartCoroutine(PollLobby());
        pingCo = StartCoroutine(PingLoop());
    }

    private void StopCoroutines()
    {
        if (heartbeatCo != null) StopCoroutine(heartbeatCo);
        if (pollCo != null) StopCoroutine(pollCo);
        if (pingCo != null) StopCoroutine(pingCo);
        heartbeatCo = pollCo = pingCo = null;
    }

    private IEnumerator Heartbeat()
    {
        while (lobby != null)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobby.Id);
            yield return new WaitForSeconds(heartbeatInterval);
        }
    }

    private IEnumerator PollLobby()
    {
        while (lobby != null)
        {
            var task = Lobbies.Instance.GetLobbyAsync(lobby.Id);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Status == TaskStatus.RanToCompletion)
            {
                lobby = task.Result;
                OnJoinedLobby?.Invoke(lobby);   // reuse event as "lobby updated"
            }
            else if (task.IsFaulted)
            {
                ReportError(task.Exception);
            }
            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator PingLoop()
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        while (true)
        {
            ulong target = NetworkManager.Singleton.IsHost
                         ? NetworkManager.Singleton.LocalClientId
                         : NetworkManager.ServerClientId;
            OnPingUpdated?.Invoke(utp.GetCurrentRtt(target));
            yield return new WaitForSeconds(0.5f);
        }
    }
}
