using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.LUMINA {
    public class RunTimeLUMINA_FAR : MonoBehaviour
    {
        public LUMINA Lumina_NO_SASCADE;

        public LUMINA_FAR Lumina;
        public LUMINA_FAR_A Lumina_FAR;
        public LUMINA_FAR_B Lumina_FAR_B;
        public Transform sun;

        public Light pointLight;
        public GameObject Particles;

        bool Lumina_CLOSE_disabled = false;
        bool Lumina_FAR_disabled = false;
        bool Lumina_FAR_B_disabled = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        void LateUpdate()
        {
            if (!Lumina_FAR.enabled && Time.fixedTime > 2)
            {
                Lumina_FAR.updateGI = true;
                Lumina_FAR.disableGI = false;
                Lumina_FAR.enabled = true;
            }
        }
        // Update is called once per frame
        void Update()
        {
            if(Lumina_NO_SASCADE != null && Time.fixedTime > 1)
            {
                Lumina_NO_SASCADE.enabled = false;
            }
        }
        public int luminaChoise = 0;
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

            string lumina_close = "ON";
            string lumina_far = "ON";
            string lumina_far_b = "ON";
            if (Lumina_CLOSE_disabled)
            {
                lumina_close = "OFF";
            }
            if (Lumina_FAR_disabled)
            {
                lumina_far = "OFF";
            }
            if (Lumina_FAR_B_disabled)
            {
                lumina_far_b = "OFF";
            }

            string enable_string = "Toggle Close (0:" + lumina_close + ") - Far (1:" + lumina_far + "):" + luminaChoise;

            if (Lumina_FAR_B != null)
            {
                enable_string = "Toggle Close (0:" + lumina_close + ") - Far (1:" + lumina_far + ") - Furthest (1:" + lumina_far_b + "):" + luminaChoise;
            }

            if (GUI.Button(new Rect(10+100, 600, 360, 27), enable_string))
            {
                if (Lumina_FAR_B != null)
                {
                    if (luminaChoise == 0)
                    {
                        luminaChoise = 1;
                    }
                    else if (luminaChoise == 1)
                    {
                        luminaChoise = 2;
                    }
                    else{
                        luminaChoise = 0;
                    }
                }
                else
                {
                    if (luminaChoise == 0)
                    {
                        luminaChoise = 1;
                    }
                    else
                    {
                        luminaChoise = 0;
                    }
                }
            }

            if (luminaChoise == 0)
            {
                if (enableGUI)
                {
                    if (GUI.Button(new Rect(10, 10, 100, 30), "Toggle GI"))
                    {
                        if (Lumina.disableGI)
                        {
                            Lumina.disableGI = false;

                            Lumina_CLOSE_disabled = false;
                        }
                        else
                        {
                            Lumina.disableGI = true;

                            Lumina_CLOSE_disabled = true;
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

                    GUI.Label(new Rect(50, 120, 400, 30), "Particles Glow Power");
                    glowIntensity = GUI.HorizontalSlider(new Rect(50, 150, 400, 30), glowIntensity, 0, 1000);

                    glowMaterialPaerticles.SetVector("_EmissionColor", glowColor * glowIntensity);

                    GUI.Label(new Rect(50, 170, 400, 30), "Global Illumination Power");
                    Lumina.giGain = GUI.HorizontalSlider(new Rect(50, 200, 400, 30), Lumina.giGain, 0, 15);

                    //v0.1
                    if (useSeparateIntensities)
                    {
                        GUI.Label(new Rect(50, 230, 400, 30), "Near Illumination Power");
                        Lumina.nearLightGain = GUI.HorizontalSlider(new Rect(50, 260, 400, 30), Lumina.nearLightGain, 0, 2);
                        GUI.Label(new Rect(50, 290, 400, 30), "Secondary Illumination Power");
                        Lumina.secondaryBounceGain = GUI.HorizontalSlider(new Rect(50, 310, 400, 30), Lumina.secondaryBounceGain, 0, 15);
                        //Lumina.giGain = GIIntensity;
                       // Lumina.secondaryBounceGain = secondaryIntensity;
                        //Lumina.nearLightGain = localLightIntensity;
                    }
                    else
                    {
                        Lumina.giGain = GIIntensity + 1;
                        Lumina.secondaryBounceGain = GIIntensity;
                        Lumina.nearLightGain = GIIntensity;
                    }

                    GUI.Label(new Rect(50, 310 + 30, 400, 30), "Sun Power");
                    sunLight.intensity = GUI.HorizontalSlider(new Rect(50, 310 + 60, 400, 30), sunLight.intensity, 0, 25);

                    GUI.Label(new Rect(50, 370 + 30, 400, 30), "Voxels Space Size");
                    Lumina.voxelSpaceSize = GUI.HorizontalSlider(new Rect(50, 370 + 60, 400, 30), Lumina.voxelSpaceSize, 30, 500);
                }
            }
            else if(luminaChoise == 1)
            {
                if (enableGUI)
                {
                    if (GUI.Button(new Rect(10, 10, 100, 30), "Toggle GI"))
                    {
                        if (Lumina_FAR.disableGI)
                        {
                            Lumina_FAR.disableGI = false;

                            Lumina_FAR_disabled = false;
                        }
                        else
                        {
                            Lumina_FAR.disableGI = true;

                            Lumina_FAR_disabled = true;
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
                        if (Lumina_FAR.visualizeVoxels)
                        {
                            Lumina_FAR.visualizeVoxels = false;
                        }
                        else
                        {
                            Lumina_FAR.visualizeVoxels = true;
                        }
                    }

                    GUI.Label(new Rect(60, 490 + 35 , 150, 30), "Blend far cascade");
                    Lumina_FAR.DitherControl.w = GUI.HorizontalSlider(new Rect(60, 490+35 +20, 150, 30), Lumina_FAR.DitherControl.w, -2, 25);

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
                    Lumina_FAR.giGain = GUI.HorizontalSlider(new Rect(50, 200, 400, 30), Lumina_FAR.giGain, 0, 15);

                    //v0.1
                    if (useSeparateIntensities)
                    {
                        GUI.Label(new Rect(50, 230, 400, 30), "Near Illumination Power");
                        Lumina_FAR.nearLightGain = GUI.HorizontalSlider(new Rect(50, 260, 400, 30), Lumina_FAR.nearLightGain, 0, 2);
                        GUI.Label(new Rect(50, 290, 400, 30), "Secondary Illumination Power");
                        Lumina_FAR.secondaryBounceGain =  GUI.HorizontalSlider(new Rect(50, 310, 400, 30), Lumina_FAR.secondaryBounceGain, 0, 15);
                        //Lumina_FAR.giGain = GIIntensity;
                        //Lumina_FAR.secondaryBounceGain = secondaryIntensity;
                        //Lumina_FAR.nearLightGain = localLightIntensity;
                    }
                    else
                    {
                        Lumina_FAR.giGain = GIIntensity + 1;
                        Lumina_FAR.secondaryBounceGain = GIIntensity;
                        Lumina_FAR.nearLightGain = GIIntensity;
                    }

                    GUI.Label(new Rect(50, 310 + 30, 400, 30), "Sun Power");
                    sunLight.intensity = GUI.HorizontalSlider(new Rect(50, 310 + 60, 400, 30), sunLight.intensity, 0, 25);

                    GUI.Label(new Rect(50, 370 + 30, 400, 30), "Voxels Space Size");
                    Lumina_FAR.voxelSpaceSize = GUI.HorizontalSlider(new Rect(50, 370 + 60, 400, 30), Lumina_FAR.voxelSpaceSize, 30, 500);
                }

            }
            else if (luminaChoise == 2)
            {
                if (enableGUI)
                {
                    if (GUI.Button(new Rect(10, 10, 100, 30), "Toggle GI"))
                    {
                        if (Lumina_FAR_B.disableGI)
                        {
                            Lumina_FAR_B.disableGI = false;

                            Lumina_FAR_B_disabled = false;
                        }
                        else
                        {
                            Lumina_FAR_B.disableGI = true;

                            Lumina_FAR_B_disabled = true;
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
                        if (Lumina_FAR_B.visualizeVoxels)
                        {
                            Lumina_FAR_B.visualizeVoxels = false;
                        }
                        else
                        {
                            Lumina_FAR_B.visualizeVoxels = true;
                        }
                    }

                    GUI.Label(new Rect(60, 490 + 35, 150, 30), "Blend far cascade");
                    Lumina_FAR_B.DitherControl.w = GUI.HorizontalSlider(new Rect(60, 490 + 35 + 20, 150, 30), Lumina_FAR_B.DitherControl.w, -2, 25);

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

                    GUI.Label(new Rect(50, 120, 400, 30), "Particles Glow Power");
                    glowIntensity = GUI.HorizontalSlider(new Rect(50, 150, 400, 30), glowIntensity, 0, 1000);

                    glowMaterialPaerticles.SetVector("_EmissionColor", glowColor * glowIntensity);

                    GUI.Label(new Rect(50, 170, 400, 30), "Global Illumination Power");
                    Lumina_FAR_B.giGain = GUI.HorizontalSlider(new Rect(50, 200, 400, 30), Lumina_FAR_B.giGain, 0, 15);

                    //v0.1
                    if (useSeparateIntensities)
                    {
                        GUI.Label(new Rect(50, 230, 400, 30), "Near Illumination Power");
                        Lumina_FAR_B.nearLightGain = GUI.HorizontalSlider(new Rect(50, 260, 400, 30), Lumina_FAR_B.nearLightGain, 0, 2);
                        GUI.Label(new Rect(50, 290, 400, 30), "Secondary Illumination Power");
                        Lumina_FAR_B.secondaryBounceGain = GUI.HorizontalSlider(new Rect(50, 310, 400, 30), Lumina_FAR_B.secondaryBounceGain, 0, 15);
                    }
                    else
                    {
                        Lumina_FAR_B.giGain = GIIntensity + 1;
                        Lumina_FAR_B.secondaryBounceGain = GIIntensity;
                        Lumina_FAR_B.nearLightGain = GIIntensity;
                    }

                    GUI.Label(new Rect(50, 310 + 30, 400, 30), "Sun Power");
                    sunLight.intensity = GUI.HorizontalSlider(new Rect(50, 310 + 60, 400, 30), sunLight.intensity, 0, 25);

                    GUI.Label(new Rect(50, 370 + 30, 400, 30), "Voxels Space Size");
                    Lumina_FAR_B.voxelSpaceSize = GUI.HorizontalSlider(new Rect(50, 370 + 60, 400, 30), Lumina_FAR_B.voxelSpaceSize, 30, 500);
                }

            }
        }

        public Color glowColor = new Color(191f / 255f, 9f / 255f, 0);
        public float glowIntensity = 20;
        public float GIIntensity = 2;

        //v0.1
        public bool useSeparateIntensities = true;
        public float localLightIntensity = 0.1f;
        public float secondaryIntensity = 8;

        public Material glowMaterialPaerticles;
        public Light sunLight;
        public bool enableGUI = true;
    }
}
