using System;
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

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private InputField joinCodeInputField;
    [SerializeField] private Text lobbyCodeText;
    [SerializeField] private Button copyLobbyCodeButton;

    private Lobby currentLobby;
    private string currentLobbyCode;
    private const int MaxPlayers = 10;

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

        if (copyLobbyCodeButton != null)
            copyLobbyCodeButton.onClick.AddListener(CopyLobbyCodeToClipboard);
    }

    public async void CreateLobby()
    {
        try
        {
            // Allocate Relay server first
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Create Lobby and embed the Relay join code
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "RelayJoinCode",
                        new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode)
                    }
                }
            };

            currentLobby = await Lobbies.Instance.CreateLobbyAsync("MyLobby", MaxPlayers, options);

            // Fetch the lobby again to get the LobbyCode
            var lobbyInfo = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
            currentLobbyCode = lobbyInfo.LobbyCode;

            Debug.Log($"Lobby created! Lobby ID: {currentLobby.Id}, Lobby Code: {currentLobbyCode}, RelayJoinCode: {relayJoinCode}");

            // Update UI
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = $"{currentLobbyCode}";
            }

            // Setup Relay for Host
            SetRelayTransportAsHost(allocation);

            // Start Host
            NetworkManager.Singleton.StartHost();
            Debug.Log("Host started using Relay.");
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

            currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode);
            currentLobbyCode = lobbyCode; // Save entered code

            Debug.Log($"Joined Lobby! Lobby ID: {currentLobby.Id}");

            // Fetch Relay join code from lobby data
            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;

            // Join Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            SetRelayTransportAsClient(joinAllocation);

            // Update UI for client too
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = $"{currentLobbyCode}";
            }

            // Start Client
            NetworkManager.Singleton.StartClient();
            Debug.Log("Client started using Relay.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e}");
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
}
