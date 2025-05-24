using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TestGameManager : MonoBehaviour
{
    [SerializeField]
    static int gameDuration = 300;

    [SerializeField]
    private Text countdownText;

    public static bool showRedFlagStolen = false;
    public static bool showBlueFlagStolen = false;

    [SerializeField]
    private UnityEngine.UI.RawImage redFlagStolen;
    [SerializeField]
    private UnityEngine.UI.RawImage blueFlagStolen;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Timer());
    }

    // Update is called once per frame
    void Update()
    {
        redFlagStolen.enabled = showRedFlagStolen;
        blueFlagStolen.enabled = showBlueFlagStolen;
    }

    IEnumerator Timer()
    {
        while (gameDuration > 0)
        {
            // Converts number of seconds into actual time format
            TimeSpan time = TimeSpan.FromSeconds(gameDuration);
            countdownText.text = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            yield return new WaitForSeconds(1f);
            gameDuration--;
        }

        // Make the game stop here? idk
    }
}
