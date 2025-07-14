using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class LobbyMenuUI : MonoBehaviour
{
    [Header("Buttons & Inputs")]
    [SerializeField] private Button hostPublicBtn;
    [SerializeField] private Button hostPrivateBtn;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private InputField codeField;
    [SerializeField] private Button joinCodeBtn;
    [SerializeField] private Button leaveBtn;
    [SerializeField] private Button startGameBtn;      // NEW

    [Header("Server List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private GameObject serverEntryPrefab;

    [Header("Status Texts")]
    [SerializeField] private Text lobbyCodeTxt;
    [SerializeField] private Text pingTxt;
    [SerializeField] private Text errorTxt;

    private NetworkBootstrapNew bootstrap;

    private void Awake()
    {
        bootstrap = FindObjectOfType<NetworkBootstrapNew>();

        /* Button wiring */
        hostPublicBtn.onClick.AddListener(() => bootstrap.HostPublic());
        hostPrivateBtn.onClick.AddListener(() => bootstrap.HostPrivate());
        refreshBtn.onClick.AddListener(() => bootstrap.RefreshServers());
        joinCodeBtn.onClick.AddListener(() => bootstrap.JoinByCode(codeField.text.Trim()));
        leaveBtn.onClick.AddListener(() => bootstrap.LeaveLobby());
        startGameBtn.onClick.AddListener(() => bootstrap.BeginMatch());

        startGameBtn.gameObject.SetActive(false);          // hidden by default
    }

    private void OnEnable() => StartCoroutine(SubscribeOnceReady());

    private IEnumerator SubscribeOnceReady()
    {
        while (LobbyService.Instance == null)
            yield return null;

        var svc = LobbyService.Instance;

        svc.OnServerListRefreshed += BuildServerList;

        svc.OnJoinedLobby += lobby =>
        {
            lobbyCodeTxt.text = lobby.LobbyCode;

            bool iAmHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            startGameBtn.gameObject.SetActive(iAmHost);
        };

        svc.OnLeftLobby += () =>
        {
            lobbyCodeTxt.text = "";
            pingTxt.text = "Ping: -";
            errorTxt.text = "";
            ClearList();
            listContent.gameObject.SetActive(false);
            startGameBtn.gameObject.SetActive(false);
        };

        svc.OnPingUpdated += ms => pingTxt.text =
            ms < 0 ? "Ping: -" : $"{ms:0} ms";

        svc.OnError += msg => errorTxt.text = msg;
    }

    /* ───────── list helpers ───────── */

    private void BuildServerList(List<Lobby> lobbies)
    {
        listContent.gameObject.SetActive(true);
        ClearList();

        foreach (var lob in lobbies)
        {
            var row = Instantiate(serverEntryPrefab, listContent);
            var texts = row.GetComponentsInChildren<Text>();
            texts[0].text = lob.Name;
            texts[1].text = $"{lob.Players.Count}/{lob.MaxPlayers}";

            string joinKey = string.IsNullOrWhiteSpace(lob.LobbyCode) ? lob.Id : lob.LobbyCode;
            row.GetComponentInChildren<Button>()
                .onClick.AddListener(() => bootstrap.JoinByCode(joinKey));
        }
    }

    private void ClearList()
    {
        foreach (Transform c in listContent)
            Destroy(c.gameObject);
    }
}
