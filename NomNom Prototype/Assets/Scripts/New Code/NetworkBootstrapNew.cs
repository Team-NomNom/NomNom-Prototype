using Unity.Netcode;
using UnityEngine;

public class NetworkBootstrapNew : MonoBehaviour
{
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        GameManagerNew.Instance?.RefreshStartButtonVisibility();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
