using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardUI : MonoBehaviour
{
    public static ScoreboardUI Instance { get; private set; }

    [SerializeField] private GameObject rowPrefab;   // Text-only row or a nicer layout
    [SerializeField] private Transform tableRoot;   // Vertical Layout Group
    [SerializeField] private GameObject panelRoot;   // whole panel we enable/disable
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    // clientId → (row GameObject, Text refs)
    private readonly Dictionary<ulong, (GameObject go, Text name, Text kill, Text death)> rows = new();

    private void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);
    }

    private void Update()
    {
        bool shouldShow = Input.GetKey(toggleKey);
        if (panelRoot.activeSelf != shouldShow)
            panelRoot.SetActive(shouldShow);
    }

    public void SetScore(ulong clientId, int kills, int deaths)
    {
        if (!rows.TryGetValue(clientId, out var tuple))
        {
            var go = Instantiate(rowPrefab, tableRoot);
            var texts = go.GetComponentsInChildren<Text>();
            var entry = (go: go, name: texts[0], kill: texts[1], death: texts[2]);  
            rows[clientId] = entry;

            entry.name.text = $"Player {clientId}";   // replace later with name lookup
        }

        rows[clientId].kill.text = kills.ToString();
        rows[clientId].death.text = deaths.ToString();
    }
}
