using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class PlayerListEntry : MonoBehaviour
{
    [SerializeField] private Text playerNameText;
    [SerializeField] private Button kickButton;

    private LobbyManager lobbyManager;
    private string playerId;

    public void Setup(string displayName, string playerId, bool isHost, bool isSelf, LobbyManager lobbyManager)
    {
        playerNameText.text = displayName;
        this.playerId = playerId;
        this.lobbyManager = lobbyManager;

        // Host can kick others, but not self
        if (isHost && !isSelf)
        {
            kickButton.gameObject.SetActive(true);
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(OnKickClicked);
        }
        else
        {
            kickButton.gameObject.SetActive(false);
        }
    }

    private void OnKickClicked()
    {
        if (lobbyManager != null && !string.IsNullOrEmpty(playerId))
        {
            lobbyManager.KickPlayer(playerId);
        }
    }
}
