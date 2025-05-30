using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject player;
    [SerializeField] private Vector3 offset = new(0, 5, -7);
    [SerializeField] private float cameraAngle = 30;
    
    // Start is called once before the firdst execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.Rotate(Vector3.right, cameraAngle);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = player.transform.position + offset;
    }
}
