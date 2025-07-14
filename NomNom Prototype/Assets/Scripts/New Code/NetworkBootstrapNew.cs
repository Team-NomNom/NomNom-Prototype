using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Small facade that the UI talks to.
/// – Ensures a NetworkManager prefab exists (with UnityTransport)
/// – Delegates create / join / refresh to LobbyService
/// – Starts Host / Client once LobbyService sets Relay data
/// </summary>
public class NetworkBootstrapNew : MonoBehaviour
{
    [Header("Reference to a prefab that contains NetworkManager + UnityTransport")]
    [SerializeField] private GameObject networkManagerPrefab;

    private void Awake()
    {
        // Guarantee LobbyService singleton exists early.
        if (LobbyService.Instance == null)
            new GameObject("LobbyService").AddComponent<LobbyService>();
    }

    /* ───────────── UI entry points ───────────── */

    public async void HostPublic() { EnsureNetworkManager(); await LobbyService.Instance.HostAsync(isPrivate: false); }
    public async void HostPrivate() { EnsureNetworkManager(); await LobbyService.Instance.HostAsync(isPrivate: true); }
    public async void JoinByCode(string code) { EnsureNetworkManager(); await LobbyService.Instance.JoinByCodeAsync(code); }
    public async void RefreshServers() => await LobbyService.Instance.RefreshPublicListAsync();
    public async void LeaveLobby() => await LobbyService.Instance.LeaveAsync();

    /* ───────────── called BY LobbyService ─────────────
       LobbyService sets Relay data, then invokes these
       methods (it fetches NetworkBootstrapNew via FindObjectOfType). */

    public void StartHost()
    {
        EnsureNetworkManager();
        NetworkManager.Singleton.StartHost();
        GameManagerNew.Instance?.RefreshStartButtonVisibility();
        Debug.Log("[Bootstrap] Host started.");
    }

    public void StartClient()
    {
        EnsureNetworkManager();
        NetworkManager.Singleton.StartClient();
        Debug.Log("[Bootstrap] Client started.");
    }

    /* ───────────── helper ───────────── */

    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null) return;

        if (networkManagerPrefab == null)
        {
            Debug.LogError("[Bootstrap] NetworkManager prefab reference is missing!");
            return;
        }

        var nm = Instantiate(networkManagerPrefab);
        if (nm.GetComponent<UnityTransport>() == null)
            Debug.LogWarning("[Bootstrap] Prefab lacks UnityTransport – Relay will fail.");
    }
}
