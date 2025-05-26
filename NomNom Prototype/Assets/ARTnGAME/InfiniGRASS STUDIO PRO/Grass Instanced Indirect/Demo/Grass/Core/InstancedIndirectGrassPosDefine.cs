using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.GIBLI
{
    [ExecuteAlways]
    public class InstancedIndirectGrassPosDefine : MonoBehaviour
    {
        //v1.2
        public bool recreateGrass = false;

        //v1.1
        public bool useLocalInstance = false;

        [Range(1, 40000000)]
        public int instanceCount = 1000000;
        public float drawDistance = 125;

        private int cacheCount = -1;

        // Start is called before the first frame update
        void Start()
        {
            UpdatePosIfNeeded();
        }
        private void Update()
        {
            UpdatePosIfNeeded();
        }

        //v0.1
        public bool enableGUI = true;

        private void OnGUI()
        {
            if (enableGUI)//v0.1
            {
                GUI.Label(new Rect(300, 50, 200, 30), "Instance Count: " + instanceCount / 1000000 + "Million");
                instanceCount = Mathf.Max(1, (int)(GUI.HorizontalSlider(new Rect(300, 100, 200, 30), instanceCount / 1000000f, 1, 10)) * 1000000);

                GUI.Label(new Rect(300, 150, 200, 30), "Draw Distance: " + drawDistance);
                drawDistance = Mathf.Max(1, (int)(GUI.HorizontalSlider(new Rect(300, 200, 200, 30), drawDistance / 25f, 1, 8)) * 25);
                InstancedIndirectGrassRenderer.instance.drawDistance = drawDistance;
            }
        }

        //v0.1
        public float scaleGrassField = 1;

        private void UpdatePosIfNeeded()
        {
            if (instanceCount == cacheCount && !recreateGrass)//v1.2
                return;


            if (recreateGrass)
            {
                InstancedIndirectGrassRenderer thatONE = GetComponent<InstancedIndirectGrassRenderer>();
                thatONE.Start(); thatONE.LateUpdate();
            }

            //Debug.Log("UpdatePos (Slow)");
            recreateGrass = false;//v1.2

            //same seed to keep grass visual the same
            UnityEngine.Random.InitState(123);

            //auto keep density the same
            float scale = scaleGrassField * Mathf.Sqrt((instanceCount / 4)) / 2f;
            transform.localScale = new Vector3(scale, transform.localScale.y, scale);

            //////////////////////////////////////////////////////////////////////////
            //can define any posWS in this section, random is just an example
            //////////////////////////////////////////////////////////////////////////
            List<Vector3> positions = new List<Vector3>(instanceCount);
            for (int i = 0; i < instanceCount; i++)
            {
                Vector3 pos = Vector3.zero;

                pos.x = UnityEngine.Random.Range(-1f, 1f) * transform.lossyScale.x;
                pos.z = UnityEngine.Random.Range(-1f, 1f) * transform.lossyScale.z;

                //transform to posWS in C#
                pos += transform.position;

                positions.Add(new Vector3(pos.x, pos.y, pos.z));
            }

            //send all posWS to renderer //v1.1            
            if (useLocalInstance)
            {
                InstancedIndirectGrassRenderer thatONE = GetComponent<InstancedIndirectGrassRenderer>();
                thatONE.allGrassPos = positions;
            }
            else
            {
                InstancedIndirectGrassRenderer.instance.allGrassPos = positions;
            }
            cacheCount = positions.Count;
        }

    }
}