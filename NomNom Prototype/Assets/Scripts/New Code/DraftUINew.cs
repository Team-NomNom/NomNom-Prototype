using UnityEngine;
using UnityEngine.UI;

public class DraftUINew : MonoBehaviour
{
    public static DraftUINew Instance { get; private set; }

    [Header("UI References")]
    public GameObject rootPanel;
    public Button lockInButton;

    private bool isLockedIn = false;

    private void Awake()
    {
        Instance = this;
        rootPanel.SetActive(false); // hidden by default
        lockInButton.onClick.AddListener(OnLockInPressed);
    }

    private void OnLockInPressed()
    {
        if (isLockedIn) return;

        isLockedIn = true;
        lockInButton.interactable = false;

        Debug.Log("[DraftUINew] Lock-in clicked.");
        GameManagerNew.Instance?.AllPlayersLockedInServerRpc();
    }

    public void Show()
    {
        isLockedIn = false;
        rootPanel.SetActive(true);
        lockInButton.interactable = true;
        Debug.Log("[DraftUINew] Show()");
    }

    public void Hide()
    {
        rootPanel.SetActive(false);
        Debug.Log("[DraftUINew] Hide()");
    }
}
