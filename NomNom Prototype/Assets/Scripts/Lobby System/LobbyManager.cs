﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using System.Linq;
using Unity.Collections;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private InputField joinCodeInputField;
    [SerializeField] private Text lobbyCodeText;
    [SerializeField] private Button copyLobbyCodeButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private InputField playerNameInputField;
    [SerializeField] private Text pingText;

    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListEntryPrefab;

    [Header("Panels")]
    [SerializeField] private GameObject preLobbyPanel;
    [SerializeField] private GameObject inLobbyPanel;

    [Header("Prefabs")]
    [SerializeField] private GameObject networkManagerPrefab;


    private Lobby currentLobby;
    private string currentLobbyCode;
    private const int MaxPlayers = 4;

    private float heartbeatInterval = 15f;
    private float lobbyPollInterval = 3f;
    private Coroutine heartbeatCoroutine;
    private Coroutine lobbyPollCoroutine;
    private Coroutine pingCoroutine;

    // Map playerId -> clientId for tracking connected players
    private Dictionary<string, ulong> playerIdToClientId = new Dictionary<string, ulong>();

    // Track playerIds that are currently disconnected (cleaner version -> true/false map)
    private Dictionary<string, bool> playerIdIsConnected = new Dictionary<string, bool>();


    private async void Awake()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in anonymously as {AuthenticationService.Instance.PlayerId}");
        }

        createLobbyButton.onClick.AddListener(() => CreateLobby());
        joinLobbyButton.onClick.AddListener(() => JoinLobby(joinCodeInputField.text.Trim()));
        leaveLobbyButton.onClick.AddListener(() => LeaveLobby());

        if (copyLobbyCodeButton != null)
            copyLobbyCodeButton.onClick.AddListener(CopyLobbyCodeToClipboard);

        // Load player name if saved
        if (PlayerPrefs.HasKey("PlayerName") && playerNameInputField != null)
        {
            playerNameInputField.text = PlayerPrefs.GetString("PlayerName");
        }

        // Start in pre-lobby state
        UpdateUIState(false);

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerJoin", OnPlayerJoinMessageReceived);
    }

    public async void CreateLobby()
    {

        if (NetworkManager.Singleton == null)
        {
            Debug.Log("[LobbyManager] NetworkManager was destroyed → recreating.");
            Instantiate(networkManagerPrefab);
        }

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = CreateLocalPlayerObject(),
                Data = new Dictionary<string, DataObject>
            {
                {
                    "RelayJoinCode",
                    new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode)
                }
            }
            };

            currentLobby = await Lobbies.Instance.CreateLobbyAsync("MyLobby", MaxPlayers, options);

            var lobbyInfo = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
            currentLobbyCode = lobbyInfo.LobbyCode;

            Debug.Log($"Lobby created! Lobby ID: {currentLobby.Id}, Lobby Code: {currentLobbyCode}, RelayJoinCode: {relayJoinCode}");

            if (lobbyCodeText != null)
                lobbyCodeText.text = $"{currentLobbyCode}";

            GUIUtility.systemCopyBuffer = currentLobbyCode;
            Debug.Log($"Copied Lobby Code to clipboard: {currentLobbyCode}");

            SavePlayerName();

            SetRelayTransportAsHost(allocation);

            // Start Host → no PlayerPrefab → tank will not spawn automatically → expected
            NetworkManager.Singleton.StartHost();
            Debug.Log("[LobbyManager] StartHost() called.");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            // 🚀 Fallback → guaranteed reliable → spawn Host tank after slight delay
            StartCoroutine(SpawnHostTankDelayed());

            Debug.Log("Host started using Relay.");

            StartLobbyHeartbeat();
            StartLobbyPolling();
            StartPingCoroutine();

            UpdateUIState(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
    }

    private IEnumerator SpawnHostTankDelayed()
    {
        // Wait 1 frame to ensure NetworkManager is ready
        yield return null;

        // Wait slight delay to ensure full server startup
        yield return new WaitForSeconds(0.1f);

        Debug.Log("[LobbyManager] SpawnHostTankDelayed → spawning Host tank manually.");

        GameManager.Instance.SpawnTankForClient(NetworkManager.Singleton.LocalClientId);
    }

    public async void JoinLobby(string lobbyCode)
    {

        if (NetworkManager.Singleton == null)
        {
            Debug.Log("[LobbyManager] NetworkManager was destroyed → recreating.");
            Instantiate(networkManagerPrefab);
        }

        try
        {
            if (string.IsNullOrEmpty(lobbyCode))
            {
                Debug.LogError("Please enter a valid Lobby Code.");
                return;
            }

            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = CreateLocalPlayerObject()
            };

            currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            currentLobbyCode = lobbyCode;

            Debug.Log($"Joined Lobby! Lobby ID: {currentLobby.Id}");

            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            SetRelayTransportAsClient(joinAllocation);

            if (lobbyCodeText != null)
                lobbyCodeText.text = $"{currentLobbyCode}";

            // Save player name
            SavePlayerName();

            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            // Setup OnClientConnectedCallback -> send PlayerJoinMessage:
            NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
            {
                if (!NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
                {
                    var msg = new PlayerJoinMessage
                    {
                        PlayerId = AuthenticationService.Instance.PlayerId
                    };

                    using (var writer = new FastBufferWriter(sizeof(int) + 64, Allocator.Temp))
                    {
                        writer.WriteValueSafe(msg);

                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("PlayerJoin", NetworkManager.ServerClientId, writer);

                        Debug.Log($"[Client] Sent PlayerJoin message → PlayerId {msg.PlayerId}");
                    }
                }
            };

            Debug.Log("Client started using Relay.");

            StartLobbyPolling();
            StartPingCoroutine();

            // Switch to in-lobby UI
            UpdateUIState(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e}");
        }
    }

    private Player CreateLocalPlayerObject()
    {
        string playerName = playerNameInputField != null && !string.IsNullOrWhiteSpace(playerNameInputField.text)
            ? playerNameInputField.text.Trim()
            : $"Player_{UnityEngine.Random.Range(1000, 9999)}";

        Debug.Log($"Using Player Name: {playerName}");

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
        {
            { "DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
            { "ClientId", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, NetworkManager.Singleton.LocalClientId.ToString()) }
        }
        };
    }

    private void SavePlayerName()
    {
        if (playerNameInputField != null && !string.IsNullOrWhiteSpace(playerNameInputField.text))
        {
            PlayerPrefs.SetString("PlayerName", playerNameInputField.text.Trim());
            PlayerPrefs.Save();
            Debug.Log($"Saved Player Name: {playerNameInputField.text.Trim()}");
        }
    }

    private void SetRelayTransportAsHost(Allocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
    }

    private void SetRelayTransportAsClient(JoinAllocation joinAllocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
    }

    private void CopyLobbyCodeToClipboard()
    {
        if (!string.IsNullOrEmpty(currentLobbyCode))
        {
            GUIUtility.systemCopyBuffer = currentLobbyCode;
            Debug.Log($"Copied Lobby Code to clipboard: {currentLobbyCode}");
        }
        else
        {
            Debug.LogWarning("No Lobby Code available to copy.");
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            if (currentLobby != null)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id);
                    Debug.Log("Host deleted the Lobby.");
                }
                else
                {
                    await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                    Debug.Log("Client left the Lobby.");
                }

                currentLobby = null;
            }

            // Shutdown network and despawn local owned objects
            NetworkManager.Singleton.Shutdown();

            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList())
            {
                if (obj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                {
                    obj.Despawn(true);
                    Debug.Log($"[LeaveLobby] Despawned local owned object: {obj.name}");
                }
            }

            StopLobbyHeartbeat();
            StopLobbyPolling();
            StopPingCoroutine();

            if (lobbyCodeText != null)
                lobbyCodeText.text = "(none)";
            if (pingText != null)
                pingText.text = "Ping: -";
            ClearPlayerListUI();

            Debug.Log("Left Lobby and shutdown network.");

            UpdateUIState(false);

            // Ultimate fix → destroy NetworkManager → force clean state next time
            Destroy(NetworkManager.Singleton.gameObject);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave lobby: {e}");
        }
    }



    public async void KickPlayer(string playerId)
    {
        if (currentLobby != null && NetworkManager.Singleton.IsHost)
        {
            try
            {
                await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                Debug.Log($"Kicked player {playerId}");

                // Despawn tank for kicked player
                if (playerIdToClientId.TryGetValue(playerId, out ulong kickedClientId))
                {
                    GameManager.Instance.DespawnTankForClient(kickedClientId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to kick player: {e}");
            }
        }
    }


    private void StartLobbyHeartbeat()
    {
        if (heartbeatCoroutine != null)
            StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
    }

    private void StopLobbyHeartbeat()
    {
        if (heartbeatCoroutine != null)
            StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = null;
    }

    private IEnumerator HeartbeatCoroutine()
    {
        while (currentLobby != null && NetworkManager.Singleton.IsHost)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            Debug.Log("Sent Lobby heartbeat.");
            yield return new WaitForSeconds(heartbeatInterval);
        }
    }

    private void StartLobbyPolling()
    {
        if (lobbyPollCoroutine != null)
            StopCoroutine(lobbyPollCoroutine);
        lobbyPollCoroutine = StartCoroutine(LobbyPollCoroutine());
    }

    private void StopLobbyPolling()
    {
        if (lobbyPollCoroutine != null)
            StopCoroutine(lobbyPollCoroutine);
        lobbyPollCoroutine = null;
    }

    private IEnumerator LobbyPollCoroutine()
    {
        while (currentLobby != null)
        {
            yield return new WaitForSeconds(lobbyPollInterval);

            Task<Lobby> getLobbyTask = Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
            yield return new WaitUntil(() => getLobbyTask.IsCompleted);

            try
            {
                if (getLobbyTask.Exception != null)
                {
                    throw getLobbyTask.Exception;
                }

                currentLobby = getLobbyTask.Result;
                UpdatePlayerListUI();

                // Detect if I was kicked:
                bool isInLobby = currentLobby.Players.Any(p => p.Id == AuthenticationService.Instance.PlayerId);
                if (!isInLobby)
                {
                    Debug.LogWarning("You were kicked from the lobby!");

                    // Perform Leave logic:
                    NetworkManager.Singleton.Shutdown();
                    StopLobbyPolling();
                    StopLobbyHeartbeat();
                    StopPingCoroutine();
                    currentLobby = null;

                    // Update UI
                    UpdateUIState(false);

                    // Early exit → no need to process further
                    yield break;
                }

                UpdatePlayerListUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to poll lobby: {e}");

                // Check if exception means lobby was deleted (host ended game)
                if (e is LobbyServiceException lobbyEx &&
                    (lobbyEx.Reason == LobbyExceptionReason.Forbidden || lobbyEx.Reason == LobbyExceptionReason.LobbyNotFound))
                {
                    Debug.LogWarning("Lobby was deleted (host ended the game). Leaving lobby.");

                    // Perform Leave logic:
                    NetworkManager.Singleton.Shutdown();
                    StopLobbyPolling();
                    StopLobbyHeartbeat();
                    StopPingCoroutine();
                    currentLobby = null;

                    // Update UI
                    UpdateUIState(false);

                    yield break;
                }
            }
        }
    }

    private void UpdatePlayerListUI()
    {
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in currentLobby.Players)
        {
            GameObject entryGO = Instantiate(playerListEntryPrefab, playerListContent);
            PlayerListEntry entry = entryGO.GetComponent<PlayerListEntry>();

            string displayName = player.Data != null && player.Data.ContainsKey("DisplayName")
                ? player.Data["DisplayName"].Value
                : player.Id;

            if (currentLobby.HostId == player.Id)
                displayName += " (Host)";

            if (NetworkManager.Singleton.IsHost && player.Id != AuthenticationService.Instance.PlayerId)
            {
                bool isDisconnected = playerIdIsConnected.ContainsKey(player.Id) && !playerIdIsConnected[player.Id];

                if (isDisconnected)
                {
                    displayName += " (Disconnected)";
                    Debug.Log($"Showing playerId {player.Id} as (Disconnected) in UI");
                }
            }

            bool isHost = NetworkManager.Singleton.IsHost;
            bool isSelf = player.Id == AuthenticationService.Instance.PlayerId;

            entry.Setup(displayName, player.Id, isHost, isSelf, this);
        }
    }


    private void ClearPlayerListUI()
    {
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void StartPingCoroutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);
        pingCoroutine = StartCoroutine(PingCoroutine());
    }

    private void StopPingCoroutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);
        pingCoroutine = null;
    }

    private IEnumerator PingCoroutine()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        while (true)
        {
            ulong targetClientId = NetworkManager.Singleton.IsHost
                ? NetworkManager.Singleton.LocalClientId
                : NetworkManager.ServerClientId;

            float pingMs = transport.GetCurrentRtt(targetClientId);

            if (pingText != null)
                pingText.text = $"Ping: {Mathf.RoundToInt(pingMs)} ms";

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.LogWarning($"Client disconnected from server: {clientId}");

        if (NetworkManager.Singleton.IsServer)
        {
            // Despawn tank for the disconnected client
            GameManager.Instance.DespawnTankForClient(clientId);
        }

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("Local player disconnected → performing Leave cleanup.");

            StopLobbyPolling();
            StopLobbyHeartbeat();
            StopPingCoroutine();
            currentLobby = null;

            UpdateUIState(false);

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
        else
        {
            Debug.Log($"Another client ({clientId}) disconnected. Host stays in Lobby.");

            // Update disconnected UI status
            string playerIdToMark = playerIdToClientId.FirstOrDefault(kv => kv.Value == clientId).Key;

            if (!string.IsNullOrEmpty(playerIdToMark))
            {
                playerIdIsConnected[playerIdToMark] = false;

                Debug.Log($"Marked playerId {playerIdToMark} as Disconnected (playerIdIsConnected = false).");
            }
            else
            {
                Debug.LogWarning($"Could not find playerId for clientId {clientId} → PlayerJoinMessage may not have arrived.");
            }

            UpdatePlayerListUI();
        }
    }



    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");

        if (NetworkManager.Singleton.IsServer)
        {
            // Host should spawn tank for newly connected clients (but not again for host)
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"[LobbyManager] Spawning tank for connected client {clientId}");
                GameManager.Instance.SpawnTankForClient(clientId);
            }
        }

        UpdatePlayerListUI();
    }


    private void UpdateUIState(bool inLobby)
    {
        if (preLobbyPanel != null)
            preLobbyPanel.SetActive(!inLobby);

        if (inLobbyPanel != null)
            inLobbyPanel.SetActive(inLobby);
    }

    private void OnPlayerJoinMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        var msg = new PlayerJoinMessage();
        reader.ReadValueSafe(out msg);

        playerIdToClientId[msg.PlayerId.ToString()] = senderClientId;
        playerIdIsConnected[msg.PlayerId.ToString()] = true;

        Debug.Log($"[OnPlayerJoinMessageReceived] Mapped playerId {msg.PlayerId} → clientId {senderClientId} (via message)");

        UpdatePlayerListUI();
    }


}

public struct PlayerJoinMessage : INetworkSerializable
{
    public FixedString64Bytes PlayerId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
    }
}
