using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class ClientInitializer : MonoBehaviour
{
    [SerializeField] private InputField ipInputField;
    [SerializeField] private Button connectButton;

    void Start()
    {
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    private void OnConnectClicked()
    {
        string hostAddress = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("Please enter a valid IP address.");
            return;
        }

        // Grab the UnityTransport component on the NetworkManager
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

        // Set the connection data to the address the player typed
        transport.ConnectionData.Address = hostAddress;
        transport.ConnectionData.Port = 7777;

        // Now start the client
        NetworkManager.Singleton.StartClient();
        Debug.Log($"Attempting to connect to {hostAddress}:7777");
    }
}
