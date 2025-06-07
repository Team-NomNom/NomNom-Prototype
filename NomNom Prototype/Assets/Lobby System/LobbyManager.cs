using System;
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

    // Track playerIds that are currently Disconnected (Host side)
    private HashSet<string> disconnectedPlayerIds = new HashSet<string>();

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

            // Auto-copy Lobby Code
            GUIUtility.systemCopyBuffer = currentLobbyCode;
            Debug.Log($"Copied Lobby Code to clipboard: {currentLobbyCode}");

            // Save player name
            SavePlayerName();

            SetRelayTransportAsHost(allocation);

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            // NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            Debug.Log("Host started using Relay.");

            StartLobbyHeartbeat();
            StartLobbyPolling();
            StartPingCoroutine();

            // Switch to in-lobby UI
            UpdateUIState(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
    }

    public async void JoinLobby(string lobbyCode)
    {
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
                lobbyCodeText.text = $"Lobby Code: {currentLobbyCode}";

            // Save player name
            SavePlayerName();

            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            // NEW → Setup OnClientConnectedCallback → send PlayerJoinMessage:
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

            NetworkManager.Singleton.Shutdown();
            // Force cleanup of any remaining NetworkObjects (fixes host player sticking bug). Update: wasn't the issue but is still good practice.
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList())
            {
                Destroy(obj.gameObject);
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

            // Switch back to pre-lobby UI
            UpdateUIState(false);
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
        // Clear old list
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // Rebuild list from currentLobby.Players
        foreach (var player in currentLobby.Players)
        {
            // NEW: Skip players marked as Disconnected → this hides the row entirely
            if (NetworkManager.Singleton.IsHost && disconnectedPlayerIds.Contains(player.Id))
            {
                Debug.Log($"Skipping Disconnected playerId {player.Id} in UI");
                continue;
            }

            GameObject entryGO = Instantiate(playerListEntryPrefab, playerListContent);
            PlayerListEntry entry = entryGO.GetComponent<PlayerListEntry>();

            string displayName = player.Data != null && player.Data.ContainsKey("DisplayName")
                ? player.Data["DisplayName"].Value
                : player.Id;

            if (currentLobby.HostId == player.Id)
                displayName += " (Host)";

            // NO need to show "(Disconnected)" anymore — we skip showing them entirely now.

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

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("Local player disconnected → performing Leave cleanup.");

            // Perform Leave logic safely:
            StopLobbyPolling();
            StopLobbyHeartbeat();
            StopPingCoroutine();
            currentLobby = null;

            // Update UI
            UpdateUIState(false);

            // Unsubscribe to prevent double call:
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
        else
        {
            // Another client disconnected -> Host stays in Lobby.
            Debug.Log($"Another client ({clientId}) disconnected. Host stays in Lobby.");

            // Try to map playerId -> from playerIdToClientId first
            string playerIdToMark = playerIdToClientId.FirstOrDefault(kv => kv.Value == clientId).Key;

            // Fallback: If playerIdToMark is empty, try to find it from Lobby.Players
            if (string.IsNullOrEmpty(playerIdToMark) && currentLobby != null)
            {
                foreach (var player in currentLobby.Players)
                {
                    if (player.Data != null && player.Data.ContainsKey("ClientId"))
                    {
                        if (ulong.TryParse(player.Data["ClientId"].Value, out ulong lobbyClientId) && lobbyClientId == clientId)
                        {
                            playerIdToMark = player.Id;
                            Debug.Log($"[Fallback] Matched playerId {playerIdToMark} from Lobby.Players → clientId {clientId}");
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(playerIdToMark))
            {
                if (!disconnectedPlayerIds.Contains(playerIdToMark))
                {
                    disconnectedPlayerIds.Add(playerIdToMark);
                    Debug.Log($"Marked playerId {playerIdToMark} as Disconnected.");
                }

                // Force remove from playerIdToClientId → ensures UI hides player even if Lobby.Players lags
                playerIdToClientId.Remove(playerIdToMark);
                Debug.Log($"Removed playerId {playerIdToMark} from playerIdToClientId.");
            }

            // Update UI after marking
            UpdatePlayerListUI();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");

        // Host waits for PlayerJoinMessage to map players → nothing to do here
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
        disconnectedPlayerIds.Remove(msg.PlayerId.ToString());

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
