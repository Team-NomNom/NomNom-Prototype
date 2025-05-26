using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.GIBLI.Utilities
{
    public class MoveCamera : MonoBehaviour
    {
        public Camera cameraA;
        public List<Transform> allCameraTransforms = new List<Transform>();

        int index = 0;
        private void OnGUI()
        {
            if (GUI.Button(new Rect(100, 450, 200, 100), "Move camera"))
            {
                index = (index + 1) % allCameraTransforms.Count;
            }
        }

        private void LateUpdate()
        {
            Transform target = allCameraTransforms[index];

            cameraA.transform.position = Vector3.Lerp(cameraA.transform.position, target.position, Time.deltaTime * 2);
            cameraA.transform.rotation = Quaternion.Slerp(cameraA.transform.rotation, target.rotation, Time.deltaTime * 2);
        }
    }
}