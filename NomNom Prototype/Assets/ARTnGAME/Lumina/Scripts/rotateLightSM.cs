using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artngame.SKYMASTER
{
    public class rotateLightSM : MonoBehaviour
    {

        public bool avoid90Deg = false;

        public float speed = 5.0f;

        private Vector3 lastMousePosition;

        void Update()
        {
            Vector3 DTT = (lastMousePosition - Input.mousePosition) * speed * Time.deltaTime ;

            if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))
            {
                transform.Rotate(new Vector3(-DTT.y, -DTT.x, 0));
            }
            if (avoid90Deg)
            {
                if(transform.forward.x == 0 && transform.forward.z == 0)
                {
                    transform.Rotate(new Vector3(0.01f, 0.01f, 0.01f));
                }
            }

            lastMousePosition = Input.mousePosition;
        }
    }
}
