using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

public class DraftUINew : MonoBehaviour
{
    public static DraftUINew Instance { get; private set; }

    [Header("Root / Main")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button lockInButton;
    [SerializeField] private Image teamBanner;

    [Header("Tank Buttons (order MUST match both prefab lists)")]
    [SerializeField] private List<Button> tankButtons;
    [SerializeField] private List<GameObject> tankPreviewPrefabs;

    [Header("Highlight Colors")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Team Colors")]
    [SerializeField] private Color team0Color = Color.cyan;
    [SerializeField] private Color team1Color = Color.red;

    [Header("Draft Timer UI")]
    [SerializeField] private Text draftTimerText;

    private int chosen = -1;
    private bool locked = false;

    private float timeRemaining = 0f;
    private bool isDraftTimerActive = false;

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

        if (draftTimerText != null)
            draftTimerText.text = "";
    }

    private void Update()
    {
        if (!isDraftTimerActive || draftTimerText == null) return;

        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(0, timeRemaining);
        draftTimerText.text = $"{timeRemaining:F1}s";

        if (timeRemaining <= 0)
        {
            isDraftTimerActive = false;
        }
    }

    public void StartTimer(float duration)
    {
        timeRemaining = duration;
        isDraftTimerActive = true;
        if (draftTimerText != null)
            draftTimerText.gameObject.SetActive(true);
    }

    public void StopTimer()
    {
        isDraftTimerActive = false;
        if (draftTimerText != null)
        {
            draftTimerText.text = "";
            draftTimerText.gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        rootPanel.SetActive(true);
        ResetPanel();
        TankPreviewNew.Instance?.ClearPreview();
        StartTimer(GameManagerNew.Instance != null ? GameManagerNew.Instance.DraftDuration : 30f);
    }

    public void Hide()
    {
        rootPanel.SetActive(false);
        TankPreviewNew.Instance?.ClearPreview();
        StopTimer();
    }

    public void SetTeam(int id)
    {
        teamBanner.color = id == 0 ? team0Color : team1Color;
    }

    public void UpdateTaken(int idx, bool isTaken)
    {
        if (idx < 0 || idx >= tankButtons.Count) return;

        if (isTaken && idx != chosen)
            tankButtons[idx].interactable = false;

        if (!isTaken)
            tankButtons[idx].interactable = true;
    }

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

        chosen = idx;
        Highlight();
        lockInButton.interactable = true;

        if (idx < tankPreviewPrefabs.Count && tankPreviewPrefabs[idx] != null)
            TankPreviewNew.Instance?.ShowTankPreview(tankPreviewPrefabs[idx]);
        else
            Debug.LogWarning($"[DraftUI] No preview prefab for index {idx}");

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

    public void OnSelectionRejected(int idx)
    {
        if (chosen == idx && !locked)
        {
            chosen = -1;
            Highlight();
            lockInButton.interactable = false;
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
