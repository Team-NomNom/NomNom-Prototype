using UnityEngine;

public interface IDriveBehaviour
{
    void HandleDrive(Rigidbody rb, DriveInput input, DriveProfile profile, float deltaTime);
}
