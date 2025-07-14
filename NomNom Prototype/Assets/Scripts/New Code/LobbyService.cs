using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using System.Linq;  
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;     // RelayServerData

public class LobbyService : MonoBehaviour
{
    /* ───────── singleton ───────── */
    public static LobbyService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* ───────── events ───────── */
    public event Action<List<Lobby>> OnServerListRefreshed;
    public event Action<Lobby> OnJoinedLobby;   // also fires on lobby update
    public event Action OnLeftLobby;
    public event Action<float> OnPingUpdated;
    public event Action<string> OnError;

    /* ───────── config ───────── */
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private float pollInterval = 3f;
    [SerializeField] private float heartbeatInterval = 15f;

    /* ───────── state ───────── */
    private Lobby lobby;
    private Coroutine heartbeatCo, pollCo, pingCo;

    /* ═════════ PUBLIC API ═════════ */

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

    /// <summary>Accepts either a 6-char join-code **or** a full lobbyId GUID.</summary>
    public async Task JoinByCodeAsync(string key)
    {
        try
        {
            await EnsureServicesSignedIn();

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Join key is empty!");

            // Decide whether it's a code (6 chars) or an Id
            if (key.Length == 6)
            {
                lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(
                    key, new JoinLobbyByCodeOptions { Player = CreatePlayerData() });
            }
            else
            {
                lobby = await Lobbies.Instance.JoinLobbyByIdAsync(
                    key, new JoinLobbyByIdOptions { Player = CreatePlayerData() });
            }

            string relayJoinCode = lobby.Data["Relay"].Value;
            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            SetRelayDataClient(join);

            FindObjectOfType<NetworkBootstrapNew>().StartClient();
            StartCoroutines();
            OnJoinedLobby?.Invoke(lobby);
        }
        catch (Exception e) { ReportError(e); }
    }

    public async Task RefreshPublicListAsync()
    {
        try
        {
            await EnsureServicesSignedIn();

            var list = await Lobbies.Instance.QueryLobbiesAsync(
                new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter> {
            // show only unlocked, public lobbies
            new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                    }
                });


            OnServerListRefreshed?.Invoke(list.Results);
        }
        catch (Exception e) { ReportError(e); }
    }


    public async Task LeaveAsync()
    {
        try
        {
            if (lobby != null)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                    await Lobbies.Instance.DeleteLobbyAsync(lobby.Id);
                else if (lobby != null)
                    await Lobbies.Instance.RemovePlayerAsync(
                        lobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e) { Debug.LogWarning(e); }

        lobby = null;
        StopCoroutines();

        /* full Netcode cleanup */
        if (NetworkManager.Singleton != null)
        {
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToArray())
                obj.Despawn(true);

            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }

        OnLeftLobby?.Invoke();
    }

    /// <summary>
    /// Called by clients when their connection to the host drops.
    /// Cleans up Netcode and fires OnLeftLobby so the UI resets.
    /// </summary>
    public void HandleDisconnectFromHost()
    {
        lobby = null;
        StopCoroutines();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }

        OnLeftLobby?.Invoke();
    }


    /* ═════════ INTERNAL HELPERS ═════════ */

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

    private void ReportError(Exception e)
    {
        Debug.LogError(e);
        OnError?.Invoke(e.Message);
    }

    /* ═════════ COROUTINES ═════════ */

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
                OnJoinedLobby?.Invoke(lobby);
            }
            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator PingLoop()
    {
        while (true)
        {
            if (NetworkManager.Singleton == null)
            {
                OnPingUpdated?.Invoke(-1);
                yield break;
            }

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (utp != null)
            {
                ulong target = NetworkManager.Singleton.IsHost
                             ? NetworkManager.Singleton.LocalClientId
                             : NetworkManager.ServerClientId;
                OnPingUpdated?.Invoke(utp.GetCurrentRtt(target));
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
