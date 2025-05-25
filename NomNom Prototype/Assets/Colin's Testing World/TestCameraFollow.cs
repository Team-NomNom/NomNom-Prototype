using UnityEngine;

public class TestCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 posOffset = new Vector3(0f, 6f, -7f);
    public Quaternion rotOffset = Quaternion.Euler(60, 0, 0);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 rotatedOffset = target.rotation * posOffset;
        Vector3 desiredPosition = target.position + rotatedOffset;
        // The lerp effects make the camera smoother but they're unnecessary
        transform.position = desiredPosition; //Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = target.rotation * rotOffset; //Quaternion.Lerp(transform.rotation,target.rotation * rotOffset,smoothSpeed * Time.deltaTime);
    }
}
