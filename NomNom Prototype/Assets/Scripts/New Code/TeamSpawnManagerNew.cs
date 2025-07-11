using UnityEngine;

public class TeamSpawnManagerNew : MonoBehaviour
{
    public static TeamSpawnManagerNew Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] redSpawns;
    [SerializeField] private Transform[] blueSpawns;

    private int redIndex = 0;
    private int blueIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public Vector3 GetSpawnPoint(int team)
    {
        if (team == 0 && redSpawns.Length > 0)
        {
            Vector3 pos = redSpawns[redIndex % redSpawns.Length].position;
            redIndex++;
            return pos;
        }
        else if (team == 1 && blueSpawns.Length > 0)
        {
            Vector3 pos = blueSpawns[blueIndex % blueSpawns.Length].position;
            blueIndex++;
            return pos;
        }
        Debug.LogWarning($"[SpawnManager] No spawns available for team {team}!");
        return Vector3.zero;
    }
}
