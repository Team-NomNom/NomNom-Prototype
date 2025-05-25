using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artngame.LUMINA;

namespace Artngame.LUMINA.Utility
{
    public class toggleCameraLUMINA : MonoBehaviour
    {
        public List<GameObject> cameras = new List<GameObject>();
        public List<Camera> camerasActual = new List<Camera>();

        public bool disableSun = true;
        public bool cycleAllAtStart = false;

        // Start is called before the first frame update
        void Start()
        {
            if (useAdvancedDisable)
            {
                current_Camera = 0;
                //enable all and disable camera, lumina update and GI, but keep script alive to avoid flashes
                for (int i = 0; i < camerasActual.Count; i++)
                {
                    if (i == current_Camera)
                    {
                        camerasActual[i].enabled = true;
                        camerasActual[i].transform.parent.gameObject.SetActive(true);
                        camerasActual[i].GetComponent<LUMINA>().disableGI = false;
                        camerasActual[i].GetComponent<LUMINA>().updateGI = true;
                        if (disableSun)
                        {
                            camerasActual[i].GetComponent<LUMINA>().sun.gameObject.SetActive(true); //enable sun
                        }
                    }
                    else
                    {
                        camerasActual[i].enabled = false;
                        camerasActual[i].transform.parent.gameObject.SetActive(true);
                        camerasActual[i].GetComponent<LUMINA>().disableGI = true;
                        camerasActual[i].GetComponent<LUMINA>().updateGI = false;
                        if (disableSun)
                        {
                            camerasActual[i].GetComponent<LUMINA>().sun.gameObject.SetActive(false); //disable sun
                        }
                    }
                }
                current_Camera = 1;



            }
        }
        int current_Camera = 1;

        public bool useAdvancedDisable = false;

        int startCycle = 0;
        public float cycleDelay = 0;
        float cycleTime = 0;
        // Update is called once per frame
        void Update()
        {
            if (cycleAllAtStart && startCycle < camerasActual.Count && Time.fixedTime - cycleTime > cycleDelay)
            {
                cycleTime = Time.fixedTime;
                //for (int j = 0; j < camerasActual.Count; j++)
                //{
                for (int i = 0; i < camerasActual.Count; i++)
                {
                    if (i == startCycle)
                    {
                        camerasActual[i].enabled = true;
                        camerasActual[i].GetComponent<LUMINA>().disableGI = false;
                        camerasActual[i].GetComponent<LUMINA>().updateGI = true;
                        if (disableSun)
                        {
                            camerasActual[i].GetComponent<LUMINA>().sun.gameObject.SetActive(true); //enable sun
                        }
                    }
                    else
                    {
                        camerasActual[i].enabled = false;
                        camerasActual[i].GetComponent<LUMINA>().disableGI = true;
                        camerasActual[i].GetComponent<LUMINA>().updateGI = false;
                        if (disableSun)
                        {
                            camerasActual[i].GetComponent<LUMINA>().sun.gameObject.SetActive(false); //disable sun
                        }
                    }
                }
                startCycle++;
                //}
            }
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 30), "Toggle Camera:" + current_Camera))
            {
                if (!useAdvancedDisable)
                {
                    for (int i = 0; i < cameras.Count; i++)
                    {
                        if (i == current_Camera)
                        {
                            cameras[i].SetActive(true);
                        }
                        else
                        {
                            cameras[i].SetActive(false);
                        }
                    }
                    current_Camera++;
                    if (current_Camera >= cameras.Count)
                    {
                        current_Camera = 0;
                    }
                }
                else
                {
                    ///enable all and disable camera, lumina update and GI, but keep script alive to avoid flashes
                    for (int i = 0; i < camerasActual.Count; i++)
                    {
                        if (i == current_Camera)
                        {
                            camerasActual[i].enabled = true;
                            camerasActual[i].GetComponent<LUMINA>().disableGI = false;
                            camerasActual[i].GetComponent<LUMINA>().updateGI = true;
                            if (disableSun)
                            {
                                camerasActual[i].GetComponent<LUMINA>().sun.gameObject.SetActive(true); //enable sun
                            }
                        }
                        else
                        {
                            camerasActual[i].enabled = false;
                            camerasActual[i].GetComponent<LUMINA>().disableGI = true;
                            camerasActual[i].GetComponent<LUMINA>().updateGI = false;
                            if (disableSun)
                            {
                                camerasActual[i].GetComponent<LUMINA>().sun.gameObject.SetActive(false); //disable sun
                            }
                        }
                    }
                    current_Camera++;
                    if (current_Camera >= camerasActual.Count)
                    {
                        current_Camera = 0;
                    }
                }

            }
        }
    }
}