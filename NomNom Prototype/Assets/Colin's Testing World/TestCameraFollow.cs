using UnityEngine;

public class TestCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 posOffset = new Vector3(0f, 5f, -7f);
    public Quaternion rotOffset = Quaternion.Euler(30, 0, 0);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 rotatedOffset = target.rotation * posOffset;
        Vector3 desiredPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation,target.rotation * rotOffset,smoothSpeed * Time.deltaTime);
    }
}
