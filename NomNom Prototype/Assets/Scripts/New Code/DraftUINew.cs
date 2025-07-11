using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// Draft UI:   • shows team banner
///             • enforces unique-per-team tank locking
///             • spawns lightweight preview models
/// </summary>
public class DraftUINew : MonoBehaviour
{
    public static DraftUINew Instance { get; private set; }

    // ──────────────  UI REFS  ────────────────────────────────────────────────
    [Header("Root / Main")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button lockInButton;
    [SerializeField] private Image teamBanner;

    [Header("Tank Buttons  (order MUST match both prefab lists)")]
    [SerializeField] private List<Button> tankButtons;        // assign in Inspector
    [SerializeField] private List<GameObject> tankPreviewPrefabs; // NEW: model-only prefabs

    [Header("Highlight Colors")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Team Colors")]
    [SerializeField] private Color team0Color = Color.cyan;
    [SerializeField] private Color team1Color = Color.red;

    // ──────────────  STATE  ─────────────────────────────────────────────────
    private int chosen = -1;
    private bool locked = false;

    // ──────────────  UNITY  ────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        rootPanel.SetActive(false);

        // Hook up each button → TryPick(idx)
        for (int i = 0; i < tankButtons.Count; i++)
        {
            int idx = i;
            tankButtons[i].onClick.AddListener(() => TryPick(idx));
        }

        lockInButton.onClick.AddListener(LockIn);
        lockInButton.interactable = false;
    }

    // ──────────────  CALLED FROM GameManager via RPCs  ──────────────────────
    public void Show()
    {
        rootPanel.SetActive(true);
        ResetPanel();
        TankPreviewNew.Instance?.ClearPreview();
    }

    public void Hide()
    {
        rootPanel.SetActive(false);
        TankPreviewNew.Instance?.ClearPreview();
    }

    public void SetTeam(int id)
    {
        teamBanner.color = id == 0 ? team0Color : team1Color;
    }

    /// Server broadcast whenever a teammate takes OR frees a tank index
    public void UpdateTaken(int idx, bool isTaken)
    {
        if (idx < 0 || idx >= tankButtons.Count) return;

        // Disable button for teammates who didn't pick this tank
        if (isTaken && idx != chosen)
            tankButtons[idx].interactable = false;

        // If a teammate frees it, re-enable
        if (!isTaken)
            tankButtons[idx].interactable = true;

        // *** DO NOT clear preview if this is my own choice ***
        if (isTaken && idx == chosen && !locked)
        {
            // keep my preview; nothing to do
        }
    }


    // ──────────────  INTERNALS  ─────────────────────────────────────────────
    private void ResetPanel()
    {
        locked = false;
        chosen = -1;

        foreach (var btn in tankButtons)
            btn.interactable = true;

        Highlight();
        lockInButton.interactable = false;
    }

    private void TryPick(int idx)
    {
        if (locked || !tankButtons[idx].interactable) return;

        // 1. Local optimistic UI
        chosen = idx;
        Highlight();
        lockInButton.interactable = true;

        // 2. Show preview (model-only prefab)
        if (idx < tankPreviewPrefabs.Count && tankPreviewPrefabs[idx] != null)
            TankPreviewNew.Instance?.ShowTankPreview(tankPreviewPrefabs[idx]);
        else
            Debug.LogWarning($"[DraftUI] No preview prefab for index {idx}");

        // 3. Notify server for uniqueness check & record
        GameManagerNew.Instance?.RequestTankSelectServerRpc(idx);
    }

    private void Highlight()
    {
        for (int i = 0; i < tankButtons.Count; i++)
        {
            var colors = tankButtons[i].colors;
            colors.normalColor = (i == chosen) ? selectedColor : normalColor;
            tankButtons[i].colors = colors;
        }
    }

    private void LockIn()
    {
        if (locked || chosen < 0) return;

        locked = true;
        lockInButton.interactable = false;
        GameManagerNew.Instance?.AllPlayersLockedInServerRpc();
    }

    /// <summary>
    /// Called only on the client whose pick was rejected because a
    /// teammate locked the same tank first.
    /// </summary>
    public void OnSelectionRejected(int idx)
    {
        if (chosen == idx && !locked)
        {
            chosen = -1;
            Highlight();                         // refresh button colours
            lockInButton.interactable = false;   // must pick again
            TankPreviewNew.Instance?.ClearPreview();
        }

        Debug.Log("[DraftUI] Pick rejected – teammate already locked that tank.");
    }

    public int GetLastSelectionForClient(ulong clientId)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient) return -1;
        if (clientId != NetworkManager.Singleton.LocalClientId) return -1;
        return chosen;
    }


}
