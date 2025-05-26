using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Artngame.CommonTools
{
    public class sceneFXCyclerSM : MonoBehaviour
    {
        public float offsetA = 0;
        public float offsetB = 0;

        public List<GameObject> cycleObjects = new List<GameObject>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnGUI()
        {
            if (GUI.Button(new Rect(10 + offsetA, 10 + 40, 500 - offsetA, 30), "Cycle Scenes:"))// + SceneManager.GetActiveScene().name))
            {
                if (SceneManager.GetActiveScene().buildIndex + 1 == SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(0);

                }
                else
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                }
            }
            if (GUI.Button(new Rect(10 + 501, 10 + 40, 90, 30), "Back"))
            {
                if (SceneManager.GetActiveScene().buildIndex - 1 == -1)
                {
                    SceneManager.LoadScene(SceneManager.sceneCountInBuildSettings - 1);

                }
                else
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
                }
            }
            if (cycleObjects.Count > 0)
            {
                if (GUI.Button(new Rect(10 + offsetB, 10 + 2 * 40, 200, 30), "Cycle Effects"))
                {
                    currentEffect++;
                    if (currentEffect == cycleObjects.Count)
                    {
                        currentEffect = 0;
                    }

                    if (!cycleObjects[currentEffect].activeInHierarchy)
                    {
                        cycleObjects[currentEffect].SetActive(true);
                    }

                    int prev = currentEffect - 1;
                    if (prev < 0)
                    {
                        prev = cycleObjects.Count - 1;
                    }
                    if (cycleObjects[prev].activeInHierarchy)
                    {
                        cycleObjects[prev].SetActive(false);
                    }
                }
            }
        }
        int currentEffect = -1;


        //public bool updateDepthRendererStep = false; //SOLVE issue when scene is reloaded detph not passed to materials!
        //public float updateDistance = 120;
        //public DepthRendererCT_URP depthRenderer;


        private void Start()
        {
            counter = 0;
            //if (updateDepthRendererStep && depthRenderer != null)
            //{
            //    depthRenderer.updateDistance = 0;
            //    //Debug.Log("RESETED");
            //}
        }


        int counter = 0;

        public bool forceUpdateRate = false;
        public GameObject updateRateObject;
        public float prevUpdateTime = 0;
        public float updateEvery = 0.01f;

        void Update()
        {
            if (forceUpdateRate)
            {
                if (Time.fixedTime - prevUpdateTime > updateEvery)
                {
                    prevUpdateTime = Time.fixedTime;
                    updateRateObject.SetActive(true);
                }
                else
                {
                    updateRateObject.SetActive(false);
                }
            }

            counter++;
            //if (counter > 20 && updateDepthRendererStep && depthRenderer != null)
            //{
            //    depthRenderer.updateDistance = updateDistance;
            //    //Debug.Log("RESETED 1");
            //}


            if (Input.GetKeyDown(KeyCode.Alpha1))
            {

                if (SceneManager.GetActiveScene().buildIndex + 1 == SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(0);

                }
                else
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                }
            }
        }
    }
}