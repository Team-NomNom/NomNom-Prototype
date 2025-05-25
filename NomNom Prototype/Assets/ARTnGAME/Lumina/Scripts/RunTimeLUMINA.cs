using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.LUMINA {
    public class RunTimeLUMINA : MonoBehaviour
    {
        public LUMINA Lumina;
        public Transform sun;

        public Light pointLight;
        public GameObject Particles;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 600, 82, 27), "Toggle GUI"))
            {
                if (enableGUI)
                {
                    enableGUI = false;
                }
                else
                {
                    enableGUI = true;
                }
            }


            if (enableGUI)
            {
                if (GUI.Button(new Rect(10, 10, 100, 30), "Toggle GI"))
                {
                    if (Lumina.disableGI)
                    {
                        Lumina.disableGI = false;
                    }
                    else
                    {
                        Lumina.disableGI = true;
                    }
                }

                if (GUI.Button(new Rect(40, 90, 120, 30), "Toggle Particles"))
                {
                    if (Particles.activeInHierarchy)
                    {
                        Particles.SetActive(false);
                    }
                    else
                    {
                        Particles.SetActive(true);
                    }
                }

                if (GUI.Button(new Rect(60, 490, 150, 30), "Toggle Debug Voxels"))
                {
                    if (Lumina.visualizeVoxels)
                    {
                        Lumina.visualizeVoxels = false;
                    }
                    else
                    {
                        Lumina.visualizeVoxels = true;
                    }
                }

                Vector3 sunRot = sun.eulerAngles;
                float sunRotX = sunRot.y;
                sunRotX = GUI.HorizontalSlider(new Rect(120, 10, 400, 30), sunRotX, -180, 180);
                //sun.eulerAngles = new Vector3(sunRot.x, sunRotX, sunRot.z);
                GUI.Label(new Rect(150, 30, 400, 30), "Rotate Sun Horizontally");

                float sunRotY = sunRot.x;
                sunRotY = GUI.VerticalSlider(new Rect(10, 50, 30, 400), sunRotY, -19, 89);
                sun.eulerAngles = new Vector3(sunRotY, sunRotX, sunRot.z);

                //point light
                GUI.Label(new Rect(50, 50, 400, 30), "Point Light Power");
                pointLight.intensity = GUI.HorizontalSlider(new Rect(50, 70, 400, 30), pointLight.intensity, 0, 5);

                //Vector4 emissionColor = glowMaterialPaerticles.GetVector("_EmmissionColor");
                //self.material.SetVector("_EmmissionColor", ((Vector4)yourColor) * Mathf.Exp(2f, exposure));
                GUI.Label(new Rect(50, 120, 400, 30), "Particles Glow Power");
                glowIntensity = GUI.HorizontalSlider(new Rect(50, 150, 400, 30), glowIntensity, 0, 1000);

                glowMaterialPaerticles.SetVector("_EmissionColor", glowColor * glowIntensity);

                GUI.Label(new Rect(50, 170, 400, 30), "Global Illumination Power");
                GIIntensity = GUI.HorizontalSlider(new Rect(50, 200, 400, 30), GIIntensity, 0, 5);

                //v0.1
                if (useSeparateIntensities)
                {
                    GUI.Label(new Rect(50, 230, 400, 30), "Near Illumination Power");
                    localLightIntensity = GUI.HorizontalSlider(new Rect(50, 260, 400, 30), localLightIntensity, 0, 2);
                    GUI.Label(new Rect(50, 290, 400, 30), "Secondary Illumination Power");
                    secondaryIntensity = GUI.HorizontalSlider(new Rect(50, 310, 400, 30), secondaryIntensity, 0, 15);
                    Lumina.giGain = GIIntensity;
                    Lumina.secondaryBounceGain = secondaryIntensity;
                    Lumina.nearLightGain = localLightIntensity;
                }
                else
                {
                    Lumina.giGain = GIIntensity + 1;
                    Lumina.secondaryBounceGain = GIIntensity;
                    Lumina.nearLightGain = GIIntensity;
                }                

                GUI.Label(new Rect(50, 310+30, 400, 30), "Sun Power");
                sunLight.intensity = GUI.HorizontalSlider(new Rect(50, 310+60, 400, 30), sunLight.intensity, 0, 25);

                GUI.Label(new Rect(50, 370+30, 400, 30), "Voxels Space Size");
                Lumina.voxelSpaceSize = GUI.HorizontalSlider(new Rect(50, 370+60, 400, 30), Lumina.voxelSpaceSize, 30, 500);
            }
        }

        public Color glowColor = new Color(191f / 255f, 9f / 255f, 0);
        public float glowIntensity = 20;
        public float GIIntensity = 2;

        //v0.1
        public bool useSeparateIntensities = false;
        public float localLightIntensity = 0.1f;
        public float secondaryIntensity = 8;

        public Material glowMaterialPaerticles;
        public Light sunLight;
        public bool enableGUI = true;
    }
}
