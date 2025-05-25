using UnityEngine;

namespace Artngame.CommonTools
{
    public class dualCameraController : MonoBehaviour
    {
        public Camera Camera1;
        public Camera Camera2;

        public RenderTexture Camera1tex;
        public RenderTexture Camera2tex;

        public RenderTexture dummy;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }
        int Frame = 0;
        // Update is called once per frame
        void Update()
        {
            Frame++;
            if (Frame == 2)
            {
                Frame = 0;
                if (Camera1.tag == "MainCamera")
                {
                    Camera1.tag = "Untagged";
                    Camera2.tag = "MainCamera";
                    Camera1.targetTexture = dummy;
                    Camera2.targetTexture = Camera2tex;
                    //Camera1.SetActive(false);
                    //Camera2.SetActive(true);
                }
                else
                {
                    Camera1.tag = "MainCamera";
                    Camera2.tag = "Untagged";
                    Camera1.targetTexture = Camera1tex;
                    Camera2.targetTexture = dummy;
                    //Camera1.SetActive(true);
                    //Camera2.SetActive(false);
                }
            }

            //Frame++;
            //if (Frame == 6)
            //{
            //    Frame = 0;
            //    if (Camera1.activeInHierarchy)
            //    {
            //        Camera1.SetActive(false);
            //        Camera2.SetActive(true);
            //    }
            //    else
            //    {
            //        Camera1.SetActive(true);
            //        Camera2.SetActive(false);
            //    }
            //}
        }
    }
}