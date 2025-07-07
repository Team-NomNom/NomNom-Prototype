using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraftUINew : MonoBehaviour
{
    public static DraftUINew Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button lockInButton;
    [SerializeField] private Image teamBanner;

    [Header("Tank Buttons (order matches prefabs)")]
    [SerializeField] private List<Button> tankButtons;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Team Colors")]
    [SerializeField] private Color team0Color = Color.cyan;
    [SerializeField] private Color team1Color = Color.red;

    private int chosen = -1;
    private bool locked = false;

    private void Awake()
    {
        Instance = this;
        rootPanel.SetActive(false);

        for (int i = 0; i < tankButtons.Count; i++)
        {
            int idx = i;
            tankButtons[i].onClick.AddListener(() => TryPick(idx));
        }
        lockInButton.onClick.AddListener(LockIn);
        lockInButton.interactable = false;
    }

    // ───────── Public API  (called from GameManagerNew RPCs)
    public void Show()
    {
        rootPanel.SetActive(true);
        ResetPanel();
    }
    public void Hide() => rootPanel.SetActive(false);

    public void SetTeam(int id)
    {
        if (teamBanner != null)
            teamBanner.color = id == 0 ? team0Color : team1Color;
    }

    /// Called when server says a tank became taken/free in my team
    public void UpdateTaken(int idx, bool isTaken)
    {
        if (idx < 0 || idx >= tankButtons.Count) return;

        tankButtons[idx].interactable = !isTaken;

        // If my current choice was just taken by teammate, unselect it
        if (isTaken && chosen == idx && !locked)
        {
            chosen = -1;
            Highlight();
            lockInButton.interactable = false;
        }
    }

    /// Called only on client that was rejected
    public void OnSelectionRejected(int idx)
    {
        if (chosen == idx && !locked)
        {
            chosen = -1;
            Highlight();
            lockInButton.interactable = false;
        }
        Debug.Log("[DraftUI] Pick rejected - teammate already took that tank.");
    }

    // ───────── Internals
    private void ResetPanel()
    {
        locked = false;
        chosen = -1;
        foreach (var btn in tankButtons) btn.interactable = true;
        Highlight();
        lockInButton.interactable = false;
    }

    private void TryPick(int idx)
    {
        if (locked || !tankButtons[idx].interactable) return;

        GameManagerNew.Instance?.RequestTankSelectServerRpc(idx);
        // assume optimistic; will revert if rejected
        chosen = idx;
        Highlight();
        lockInButton.interactable = true;
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
}
