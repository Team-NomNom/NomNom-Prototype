using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Pure UI glue for the Lobby menu.
/// – Wires button clicks to NetworkBootstrapNew
/// – Rebuilds server list & status texts when LobbyService raises events.
/// </summary>
public class LobbyMenuUI : MonoBehaviour
{
    [Header("Buttons / Inputs")]
    [SerializeField] private Button hostPublicBtn;
    [SerializeField] private Button hostPrivateBtn;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private InputField codeField;
    [SerializeField] private Button joinCodeBtn;
    [SerializeField] private Button leaveBtn;

    [Header("Server List")]
    [SerializeField] private Transform listContent;          // ScrollView Content
    [SerializeField] private GameObject serverEntryPrefab;    // Prefab with Name/Players/Join

    [Header("Status Texts")]
    [SerializeField] private Text lobbyCodeTxt;
    [SerializeField] private Text pingTxt;
    [SerializeField] private Text errorTxt;

    private NetworkBootstrapNew bootstrap;

    private void Awake()
    {
        bootstrap = FindObjectOfType<NetworkBootstrapNew>();

        /* button wiring */
        hostPublicBtn.onClick.AddListener(() => bootstrap.HostPublic());
        hostPrivateBtn.onClick.AddListener(() => bootstrap.HostPrivate());
        refreshBtn.onClick.AddListener(() => bootstrap.RefreshServers());
        joinCodeBtn.onClick.AddListener(() =>
         bootstrap.JoinByCode(codeField.text.Trim()));
        leaveBtn.onClick.AddListener(() => bootstrap.LeaveLobby());
    }

    private void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    private IEnumerator WaitAndSubscribe()
    {
        /* ensure LobbyService singleton exists */
        while (LobbyService.Instance == null)
            yield return null;

        var svc = LobbyService.Instance;

        svc.OnServerListRefreshed += BuildServerList;
        svc.OnJoinedLobby += lob => lobbyCodeTxt.text = lob.LobbyCode;
        svc.OnLeftLobby += () => { lobbyCodeTxt.text = ""; ClearList(); };
        svc.OnPingUpdated += ms => pingTxt.text = $"Ping: {ms:0} ms";
        svc.OnError += msg => errorTxt.text = msg;
    }

    /* ------------------ server list helpers ------------------ */

    private void BuildServerList(List<Lobby> lobbies)
    {
        ClearList();

        foreach (var lob in lobbies)
        {
            var row = Instantiate(serverEntryPrefab, listContent);

            var texts = row.GetComponentsInChildren<Text>();
            texts[0].text = lob.Name;
            texts[1].text = $"{lob.Players.Count}/{lob.MaxPlayers}";

            var joinBtn = row.GetComponentInChildren<Button>();
            joinBtn.onClick.AddListener(() => bootstrap.JoinByCode(lob.LobbyCode));
        }
    }

    private void ClearList()
    {
        foreach (Transform c in listContent) Destroy(c.gameObject);
    }
}
