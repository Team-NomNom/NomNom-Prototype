using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Small facade the UI talks to.
/// – Ensures NetworkManager prefab exists
/// – Delegates lobby actions to LobbyService
/// – Starts Host / Client when LobbyService finishes wiring Relay
/// – Hosts can click “Start Game” to load the gameplay scene for everyone.
/// </summary>
public class NetworkBootstrapNew : MonoBehaviour
{
    [Header("Prefab with NetworkManager + UnityTransport")]
    [SerializeField] private GameObject networkManagerPrefab;

    private void Awake()
    {
        // Guarantee LobbyService singleton exists
        if (LobbyService.Instance == null)
            new GameObject("LobbyService").AddComponent<LobbyService>();
    }

    /* ───────────── UI entry points ───────────── */

    public async void HostPublic() { EnsureNetworkManager(); await LobbyService.Instance.HostAsync(false); }
    public async void HostPrivate() { EnsureNetworkManager(); await LobbyService.Instance.HostAsync(true); }
    public async void JoinByCode(string code) { EnsureNetworkManager(); await LobbyService.Instance.JoinByCodeAsync(code); }
    public async void RefreshServers() => await LobbyService.Instance.RefreshPublicListAsync();
    public async void LeaveLobby() => await LobbyService.Instance.LeaveAsync();

    /* ───────────── Begin Match (host only) ───────────── */

    public void BeginMatch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("[Bootstrap] Only the host can start the match.");
            return;
        }

        string gameSceneName = "TestGameScene";   // <-- change if your gameplay scene has a different name
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single);

        Debug.Log("[Bootstrap] Host loading Game scene for all clients.");
    }

    /* ───────────── called BY LobbyService ───────────── */

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
            Debug.LogError("[Bootstrap] NetworkManager prefab reference missing!");
            return;
        }

        var nm = Instantiate(networkManagerPrefab);
        if (nm.GetComponent<UnityTransport>() == null)
            Debug.LogWarning("[Bootstrap] Prefab lacks UnityTransport – Relay will fail.");
    }
}
