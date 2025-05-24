using System;
using UnityEngine;

public class TestFlag : MonoBehaviour
{
    [SerializeField]
    public Constants.Colors color = Constants.Colors.Red;

    private bool held = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter(Collider collider)
    {
        if (held) return;
        // Checks that object is player and that player is on opposing team
        if (collider.gameObject.name == "Player" && collider.gameObject.GetComponent<TestTankController>().teamColor != color)
        {
            if (color == Constants.Colors.Red)
            {
                Debug.Log("Red flag stolen");
                TestGameManager.showRedFlagStolen = true;
            }
            else
            {
                Debug.Log("Blue flag stolen");
                TestGameManager.showBlueFlagStolen = true;
            }
            // Attaches flag to player
            held = true;
            transform.SetParent(collider.gameObject.transform);
            transform.localPosition = new Vector3(0, 0.84f, 0);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);

        }
    }
}
