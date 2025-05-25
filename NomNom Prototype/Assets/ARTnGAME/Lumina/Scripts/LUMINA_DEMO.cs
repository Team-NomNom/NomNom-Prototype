using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artngame.LUMINA
{
    public class LUMINA_DEMO : MonoBehaviour
    {
        LUMINA segi;

        public GameObject throwObject;
        List<GameObject> thrownObjects = new List<GameObject>();

        public LayerMask grabMask;

        public Text voxelResolution;
        public Text reflections;
        public Text cones;
        public Text coneTraceSteps;
        public Text infiniteBounces;
        public Text gi;
        public Text fpsCounter;
        public Text spawnedObjects;

        Transform heldObject;
        Transform heldObjectParent;

        public LUMINAPreset low;
        public LUMINAPreset medium;
        public LUMINAPreset high;
        public LUMINAPreset ultra;

        float fps;
        float prevfps;
        int spawnedObjectsCounter;

        public GameObject infoOverlay;

        void Start()
        {
            segi = GetComponent<LUMINA>();
        }

        void UpdateUIText()
        {
            voxelResolution.text = "Voxel Resolution: " + (segi.voxelResolution == LUMINA.VoxelResolution.low ? "128" : "256");
            reflections.text = "Reflections: " + (segi.doReflections ? "On" : "Off");
            cones.text = "Cones: " + segi.cones.ToString();
            coneTraceSteps.text = "Cone Trace Steps: " + segi.coneTraceSteps.ToString();
            infiniteBounces.text = "Infinite Bounces: " + (segi.infiniteBounces ? "On" : "Off");
            gi.text = "GI: " + (segi.enabled ? "On" : "Off");

            fps = Mathf.Lerp(fps, Mathf.Lerp(1.0f / Time.deltaTime, prevfps, 0.5f), 3.0f * Time.deltaTime);

            fpsCounter.text = "FPS: " + Mathf.RoundToInt(fps).ToString();

            spawnedObjects.text = "Spawned Objects: " + spawnedObjectsCounter.ToString();

            prevfps = 1.0f / Time.deltaTime;
        }

        void Update()
        {
            //UpdateUIText();

            if (Input.GetKeyDown(KeyCode.G))
            {
                if (segi.enabled)
                {
                    segi.disableGI = true;
                    gi.text = "GI: Off";
                    //AddBadAmbient();
                }
                else
                {
                    segi.disableGI = false;
                    gi.text = "GI: On";
                    //RemoveBadAmbient();
                }
            }

            //if (Input.GetKeyDown(KeyCode.I))
            //{
            //    segi.infiniteBounces = !segi.infiniteBounces;
            //    infiniteBounces.text = segi.infiniteBounces ? "Infinite Bounces: On" : "Infinite Bounces: Off";
            //}

            //if (Input.GetKeyDown(KeyCode.R))
            //{
            //    if (segi.voxelResolution == LUMINA.VoxelResolution.high)
            //    {
            //        segi.voxelResolution = LUMINA.VoxelResolution.low;
            //        voxelResolution.text = "Voxel Resolution: 128";
            //    }
            //    else
            //    {
            //        segi.voxelResolution = LUMINA.VoxelResolution.high;
            //        voxelResolution.text = "Voxel Resolution: 256";
            //    }
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //    segi.ApplyPreset(low);
            //if (Input.GetKeyDown(KeyCode.Alpha2))
            //    segi.ApplyPreset(medium);
            //if (Input.GetKeyDown(KeyCode.Alpha3))
            //    segi.ApplyPreset(high);
            //if (Input.GetKeyDown(KeyCode.Alpha4))
            //    segi.ApplyPreset(ultra);

            //Throw an object
            if (Input.GetKeyDown(KeyCode.Q))
            {
                GameObject thrownObject = Instantiate(throwObject, transform.position + transform.forward * 4.0f, transform.rotation) as GameObject;
                thrownObject.transform.localScale *= UnityEngine.Random.Range(0.4f,0.7f); 
                Rigidbody thrownRigidbody = thrownObject.GetComponent<Rigidbody>();
                Material mat = thrownObject.GetComponent<MeshRenderer>().material;
                if (UnityEngine.Random.Range(1, 10) == 2) {
                    mat.color = Color.red;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                if (UnityEngine.Random.Range(1, 10) ==3)
                {
                    mat.color = Color.cyan;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                if (UnityEngine.Random.Range(1, 10) == 4)
                {
                    mat.color = Color.green;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                if (UnityEngine.Random.Range(1, 10) == 5)
                {
                    mat.color = Color.blue;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                if (UnityEngine.Random.Range(1, 10) == 6)
                {
                    mat.color = Color.blue;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                if (UnityEngine.Random.Range(1, 10) == 7)
                {
                    mat.color = Color.yellow;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                if (UnityEngine.Random.Range(1, 10) == 8)
                {
                    mat.color = Color.gray;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                else
                {
                    mat.color = Color.magenta;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.6f);
                    mat.SetColor("_EmissionColor", mat.color);
                    mat.SetColor("_BaseColor", mat.color);
                }
                thrownRigidbody.AddForce(transform.forward * 1000.0f);
                thrownObjects.Add(thrownObject);
                spawnedObjectsCounter++;
            }

           

            //Clear thrown objects
            if (Input.GetKeyDown(KeyCode.C))
            {
                foreach (GameObject thrownObject in thrownObjects)
                {
                    Destroy(thrownObject);
                }

                thrownObjects.Clear();
                spawnedObjectsCounter = 0;
            }

            //Grabbing and moving objects
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (heldObject == null)
                {
                    RaycastHit hit;

                    if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit, 50.0f, grabMask))
                    {
                        heldObject = hit.transform;
                        heldObjectParent = heldObject.parent;
                        heldObject.SetParent(transform);
                        Debug.Log("Grabbed " + hit.transform.gameObject.name);
                    }
                }
                else
                {
                    if (heldObjectParent != null)
                    {
                        heldObject.SetParent(heldObjectParent);
                    }
                    else
                    {
                        heldObject.SetParent(null);
                    }
                    heldObject = null;
                }
            }

            //if (Input.GetKey(KeyCode.PageUp))
            //{
            //    segi.sun.intensity += 1.0f * Time.deltaTime;
            //}

            //if (Input.GetKey(KeyCode.PageDown))
            //{
            //    segi.sun.intensity -= 1.0f * Time.deltaTime;
            //}

            //if (Input.GetKey(KeyCode.Home))
            //{
            //    segi.softSunlight += 1.0f * Time.deltaTime;
            //    segi.softSunlight = Mathf.Max(0.0f, segi.softSunlight);
            //}
            //if (Input.GetKey(KeyCode.End))
            //{
            //    segi.softSunlight -= 1.0f * Time.deltaTime;
            //    segi.softSunlight = Mathf.Max(0.0f, segi.softSunlight);
            //}

            //if (Input.GetKey(KeyCode.RightArrow))
            //{
            //    segi.sun.transform.RotateAround(segi.sun.transform.position, Vector3.up, 30.0f * Time.deltaTime);
            //}
            //if (Input.GetKey(KeyCode.LeftArrow))
            //{
            //    segi.sun.transform.RotateAround(segi.sun.transform.position, Vector3.down, 30.0f * Time.deltaTime);
            //}
            //if (Input.GetKey(KeyCode.UpArrow))
            //{
            //    segi.sun.transform.Rotate(Vector3.right * 30.0f * Time.deltaTime);
            //}
            //if (Input.GetKey(KeyCode.DownArrow))
            //{
            //    segi.sun.transform.Rotate(Vector3.left * 30.0f * Time.deltaTime);
            //}

            if (Input.GetKeyDown(KeyCode.H))
            {
                if (infoOverlay.activeSelf)
                {
                    infoOverlay.SetActive(false);
                }
                else
                {
                    infoOverlay.SetActive(true);
                }

            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
        public float yGuiOffset = 0;
        void OnGUI()
        {
            GUI.TextField(new Rect(10, 10+ yGuiOffset, 200, 22), "'Q' to throw objects, 'C' to clear");
        }
        private void RemoveBadAmbient()
        {
            RenderSettings.ambientIntensity = 0.0f;
        }

        private void AddBadAmbient()
        {
            RenderSettings.ambientIntensity = 1.0f;
        }
    }
}