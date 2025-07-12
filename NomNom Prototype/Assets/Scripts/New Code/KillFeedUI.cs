using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillFeedUI : MonoBehaviour
{
    public static KillFeedUI Instance { get; private set; }

    [SerializeField] private GameObject entryPrefab;   // a Text or TMP prefab
    [SerializeField] private Transform entriesRoot;   // Vertical Layout Group
    [SerializeField] private int maxEntries = 5;
    [SerializeField] private float lifetime = 4f;

    private readonly Queue<GameObject> live = new();

    void Awake() => Instance = this;

    public void PushEntry(string killer, string victim, bool isSuicide)
    {
        var go = Instantiate(entryPrefab, entriesRoot);
        var txt = go.GetComponent<Text>();
        txt.text = isSuicide
    ? $"<color=#F1948A>{victim}</color> <color=#AAAAAA> Alt F4ed</color>"
    : $"<color=#F9E79F>{killer}</color> ➜ <color=#F1948A>{victim}</color>";


        live.Enqueue(go);
        if (live.Count > maxEntries) Destroy(live.Dequeue());

        StartCoroutine(Fade(go));
    }

    private IEnumerator Fade(GameObject go)
    {
        float t = lifetime;
        var txt = go.GetComponent<Text>();
        Color col = txt.color;

        while (t > 0f)
        {
            t -= Time.deltaTime;
            col.a = t / lifetime;
            txt.color = col;
            yield return null;
        }
        live.Dequeue();
        Destroy(go);
    }
}
