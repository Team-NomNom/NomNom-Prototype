using Unity.Netcode;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("HOST HAS STARTED");
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer() // for a dedicated server build
    {
        NetworkManager.Singleton.StartServer();
    }
}
