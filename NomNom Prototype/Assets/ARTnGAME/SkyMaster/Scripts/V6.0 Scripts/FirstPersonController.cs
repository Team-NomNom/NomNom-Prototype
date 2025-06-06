using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.GIPROXY
{
    public class FirstPersonController : MonoBehaviour
    {
        public bool setResolution = false;
        public int resX = 1280;
        public int resY = 720;

        //FPS control
        public bool setFrameRateMobile = false;
        public int frameRate = 60;
        public float moveUpFactor = 0;
        // References
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private CharacterController characterController;

        // Player settings
        [SerializeField] private float cameraSensitivity;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float moveInputDeadZone;

        // Touch detection
        private int leftFingerId, rightFingerId;
        private float halfScreenWidth;

        // Camera control
        private Vector2 lookInput;
        private float cameraPitch;

        // Player movement
        private Vector2 moveTouchStartPosition;
        private Vector2 moveInput;

        // Start is called before the first frame update
        void Start()
        {
            if (setFrameRateMobile)
            {
                Application.targetFrameRate = frameRate;
            }
            if (setResolution)
            {
                Screen.SetResolution(resX, resY, true);
            }

            // id = -1 means the finger is not being tracked
            leftFingerId = -1;
            rightFingerId = -1;

            // only calculate once
            halfScreenWidth = Screen.width / 2;

            // calculate the movement input dead zone
            moveInputDeadZone = Mathf.Pow(Screen.height / moveInputDeadZone, 2);
        }

        // Update is called once per frame
        void Update()
        {
            // Handles input
            GetTouchInput();


            if (rightFingerId != -1)
            {
                // Ony look around if the right finger is being tracked
                //        Debug.Log("Rotating");
                LookAround();
            }

            if (leftFingerId != -1)
            {
                // Ony move if the left finger is being tracked
                //        Debug.Log("Moving");
                Move();
            }
        }

        void GetTouchInput()
        {
            // Iterate through all the detected touches
            for (int i = 0; i < Input.touchCount; i++)
            {

                Touch t = Input.GetTouch(i);

                // Check each touch's phase
                switch (t.phase)
                {
                    case TouchPhase.Began:

                        if (t.position.x < halfScreenWidth && leftFingerId == -1)
                        {
                            // Start tracking the left finger if it was not previously being tracked
                            leftFingerId = t.fingerId;

                            // Set the start position for the movement control finger
                            moveTouchStartPosition = t.position;
                        }
                        else if (t.position.x > halfScreenWidth && rightFingerId == -1)
                        {
                            // Start tracking the rightfinger if it was not previously being tracked
                            rightFingerId = t.fingerId;
                        }

                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:

                        if (t.fingerId == leftFingerId)
                        {
                            // Stop tracking the left finger
                            leftFingerId = -1;
                            //                    Debug.Log("Stopped tracking left finger");
                        }
                        else if (t.fingerId == rightFingerId)
                        {
                            // Stop tracking the right finger
                            rightFingerId = -1;
                            //                   Debug.Log("Stopped tracking right finger");
                        }

                        break;
                    case TouchPhase.Moved:

                        // Get input for looking around
                        if (t.fingerId == rightFingerId)
                        {
                            lookInput = t.deltaPosition * cameraSensitivity * Time.deltaTime;
                        }
                        else if (t.fingerId == leftFingerId)
                        {

                            // calculating the position delta from the start position
                            moveInput = t.position - moveTouchStartPosition;
                        }

                        break;
                    case TouchPhase.Stationary:
                        // Set the look input to zero if the finger is still
                        if (t.fingerId == rightFingerId)
                        {
                            lookInput = Vector2.zero;
                        }
                        break;
                }
            }
        }

        void LookAround()
        {

            // vertical (pitch) rotation
            cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

            // horizontal (yaw) rotation
            transform.Rotate(transform.up, lookInput.x);
        }

        void Move()
        {

            // Don't move if the touch delta is shorter than the designated dead zone
            if (moveInput.sqrMagnitude <= moveInputDeadZone) return;

            // Multiply the normalized direction by the speed
            Vector2 movementDirection = moveInput.normalized * moveSpeed * Time.deltaTime;
            // Move relatively to the local transform's direction
            characterController.Move(transform.right * movementDirection.x + transform.forward * movementDirection.y + transform.up * movementDirection.y*moveUpFactor);
        }

    }
}