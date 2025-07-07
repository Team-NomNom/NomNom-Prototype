using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq;

public enum GameState
{
    Lobby,   // players roam & shoot in lobby
    Draft,   // team / tank selection
    InGame   // actual match
}

/// <summary>
/// Central authority that drives high-level phase changes.
/// - Keeps a single NetworkVariable<GameState> synced.
/// - Host (or server) is the only one allowed to mutate it.
/// - UI panels listen for state changes on the client.
/// </summary>
public class GameManagerNew : NetworkBehaviour
{
    public static GameManagerNew Instance { get; private set; }

    [Header("Match Settings")]
    [Tooltip("How long the draft phase lasts if not everyone locks in (seconds)")]
    [SerializeField] private float draftDuration = 30f;

    [Header("UI References")]
    [Tooltip("Reference to the Start Game button in UI")]
    [SerializeField] private GameObject startGameButtonGO;

    /// <summary>
    /// Authoritative game state replicated to all clients.
    /// </summary>
    public readonly NetworkVariable<GameState> CurrentGameState =
        new(GameState.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private Coroutine draftTimerCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RefreshStartButtonVisibility();
    }

    public void RefreshStartButtonVisibility()
    {
        if (startGameButtonGO != null)
            startGameButtonGO.SetActive(NetworkManager.Singleton.IsHost);
    }


    // ─────────────────────────────────────────────────────────────────────────────
    // Public RPC API — called by UI buttons or other scripts
    // ─────────────────────────────────────────────────────────────────────────────
    #region RPC API
    /// <summary>
    /// Host triggers this via a “Start Game” button.
    /// Puts everyone into Draft state and starts the timeout.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void StartDraftPhaseServerRpc()
    {
        if (CurrentGameState.Value != GameState.Lobby) return;

        CurrentGameState.Value = GameState.Draft;
        DraftUIShowClientRpc();

        if (draftTimerCo != null) StopCoroutine(draftTimerCo);
        draftTimerCo = StartCoroutine(DraftTimeoutCoroutine());
    }

    /// <summary>
    /// Once every client has locked in, any client can signal we’re ready.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AllPlayersLockedInServerRpc()
    {
        if (CurrentGameState.Value != GameState.Draft) return;
        StartMatch();
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────────
    private IEnumerator DraftTimeoutCoroutine()
    {
        yield return new WaitForSeconds(draftDuration);
        if (CurrentGameState.Value == GameState.Draft)
            StartMatch();
    }

    private void StartMatch()
    {
        if (CurrentGameState.Value != GameState.Draft) return;

        if (draftTimerCo != null) StopCoroutine(draftTimerCo);

        CurrentGameState.Value = GameState.InGame;
        DraftUIHideClientRpc();

        // ⚠️ Actual tank spawning will be handled later once
        //     player-specific data & spawn points are implemented.
        Debug.Log("[GameManager] Match started!");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Client RPCs for showing / hiding the draft panel
    // ─────────────────────────────────────────────────────────────────────────────
    [ClientRpc]
    private void DraftUIShowClientRpc() => DraftUINew.Instance?.Show();

    [ClientRpc]
    private void DraftUIHideClientRpc() => DraftUINew.Instance?.Hide();
}
