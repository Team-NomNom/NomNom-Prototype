//see this for ref: https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
namespace Artngame.GIBLI
{
    [ExecuteAlways]
    public class InstancedIndirectGrassRenderer : MonoBehaviour
    {
        //v1.3 depth cutoff
        public bool depthCutoff = false;
        public float depthCutoffHeigth = 200;//objects above 200m will occlude grass below

        //v1.2
        public bool ReadMaterialProps = false;
        public bool UpdateMaterialProps = false;
        public Color _BaseColor = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
        public Texture2D _BaseColorTexture; //GRASS TEXTURE
        public Color _GroundColor = new Color(56.0f / 255.0f, 118.0f / 255.0f, 66.0f / 255.0f);
        // [Header(Grass Shape)]
        public float _GrassWidth = 0.61f;
        public float _GrassHeight = 0.52f;
        //[Header(Wind)]
        public float _WindAIntensity = 1;
        public float _WindAFrequency = 1;
        public Vector3 _WindATiling = new Vector3(0.82f, 0.62f, 0);
        public Vector3 _WindAWrap = new Vector3(0.42f, 0.5f, 0);

        public float _WindBIntensity = 1;
        public float _WindBFrequency = 1;
        public Vector3 _WindBTiling = new Vector3(3.43f, 3.0f, 0);
        public Vector3 _WindBWrap = new Vector3(0.5f, 0.5f, 0);

        public float _WindCIntensity = 0.59f;
        public float _WindCFrequency = 2;
        public Vector3 _WindCTiling = new Vector3(1.31f, 3, 0);
        public Vector3 _WindCWrap = new Vector3(0.5f, 0.5f, 0);

        public float _RandomNormal = 0.05f;
        //make SRP batcher happy
        //_PivotPosWS("_PivotPosWS", Vector) = (0,0,0,0)
        //_BoundSize("_BoundSize", Vector) = (1,1,0)
        //v0.1
        //TERRAIN DEPTH
        //_DepthCameraPos("Depth Camera Pos", Vector) = (240, 100, 250, 3.0)
        //_ShoreContourTex("_ShoreContourTex", 2D) = "white" {}
        //_ShoreFadeFactor("_ShoreFadeFactor", Float) = 1.0
        //_TerrainScale("Terrain Scale", Float) = 1000.0
        //v2.0.7
        public float _grassHeight = 1;
        //_grassNormal("grassNormal", Float) = 3
        public float _respectHeight = 0.6f;
        //v2.0.8
        //_InteractTexture("_Interact Texture", 2D) = "white" {}
        //_InteractTexturePos("Interact Texture Pos", Vector) = (0, 5000, 0, 0)
        //_InteractTexturePosY("Interact Texture Pos Y Shadows", Float) = 5000
        //v2.1.1
        //_shapeOnlyHeight("Shape Only Height", Float) = 0
        //v2.1.11
        public float _BaseHeight = -0.4f;
        //v2.1.20 - //INFINIGRASS - DENSITY EMULATING GRASS
        public float _NoiseAmplitude = 0.1f;
        public float _NoiseFreqX = 64.73f;
        public float _NoiseFreqZ = 2.23f;
        public float _NoiseOffsetY = 0.1f;
        //v2.1.20b
        public float _textureFeed = 1; //default is on
        //END INFINIGRASS 3
        //INFINIGRASS 5 - GRASS BURN
        //BURN 2.1.14
        //_NoiseTexture("Noise Texture", 2D) = "white" {}
        //_Burnfactor("Burn factor", Range(-1, 1)) = -1//6
        //_Burnramp("Burn ramp", 2D) = "white" {}
        //_BurnCenter("Ocean Center", Vector) = (0, 0, 0, 0)
        //END INFINIGRASS 5 - GRASS BURN
        public void UpdateMaterial(Material grassMaterial)
        {
            //v0.3
            if (grassMaterial != null)
            {
                grassMaterial.SetColor("_BaseColor", _BaseColor);
                if (_BaseColorTexture != null)
                {
                    grassMaterial.SetTexture("_BaseColorTexture", _BaseColorTexture);
                }
                grassMaterial.SetColor("_GroundColor", _GroundColor);
                grassMaterial.SetFloat("_GrassWidth", _GrassWidth);
                grassMaterial.SetFloat("_GrassHeight", _GrassHeight);
                grassMaterial.SetFloat("_WindAIntensity", _WindAIntensity);
                grassMaterial.SetFloat("_WindAFrequency", _WindAFrequency);

                grassMaterial.SetVector("_WindATiling", _WindATiling);
                grassMaterial.SetVector("_WindAWrap", _WindAWrap);

                grassMaterial.SetFloat("_WindBIntensity", _WindBIntensity);
                grassMaterial.SetFloat("_WindBFrequency", _WindBFrequency);

                grassMaterial.SetVector("_WindBTiling", _WindBTiling);
                grassMaterial.SetVector("_WindBWrap", _WindBWrap);

                grassMaterial.SetFloat("_WindCIntensity", _WindCIntensity);
                grassMaterial.SetFloat("_WindCFrequency", _WindCFrequency);

                grassMaterial.SetVector("_WindCTiling", _WindCTiling);
                grassMaterial.SetVector("_WindCWrap", _WindCWrap);

                grassMaterial.SetFloat("_RandomNormal", _RandomNormal);
                grassMaterial.SetFloat("_grassHeight", _grassHeight);
                grassMaterial.SetFloat("_respectHeight", _respectHeight);
                grassMaterial.SetFloat("_BaseHeight", _BaseHeight);
                grassMaterial.SetFloat("_NoiseAmplitude", _NoiseAmplitude);
                grassMaterial.SetFloat("_NoiseFreqX", _NoiseFreqX);
                grassMaterial.SetFloat("_NoiseFreqZ", _NoiseFreqZ);
                grassMaterial.SetFloat("_NoiseOffsetY", _NoiseOffsetY);
                grassMaterial.SetFloat("_textureFeed", _textureFeed);
            }
        }
        public void ReadMaterial(Material grassMaterial)
        {
            //v0.3
            if (grassMaterial != null)
            {
                _BaseColor = grassMaterial.GetColor("_BaseColor");
                //if (_BaseColorTexture != null)
                //{
                _BaseColorTexture = (Texture2D)grassMaterial.GetTexture("_BaseColorTexture");
                //}
                _GroundColor = grassMaterial.GetColor("_GroundColor");
                _GrassWidth = grassMaterial.GetFloat("_GrassWidth");
                _GrassHeight = grassMaterial.GetFloat("_GrassHeight");
                _WindAIntensity = grassMaterial.GetFloat("_WindAIntensity");
                _WindAFrequency = grassMaterial.GetFloat("_WindAFrequency");

                _WindATiling = grassMaterial.GetVector("_WindATiling");
                _WindAWrap = grassMaterial.GetVector("_WindAWrap");

                _WindBIntensity = grassMaterial.GetFloat("_WindBIntensity");
                _WindBFrequency = grassMaterial.GetFloat("_WindBFrequency");

                _WindBTiling = grassMaterial.GetVector("_WindBTiling");
                _WindBWrap = grassMaterial.GetVector("_WindBWrap");

                _WindCIntensity = grassMaterial.GetFloat("_WindCIntensity");
                _WindCFrequency = grassMaterial.GetFloat("_WindCFrequency");

                _WindCTiling = grassMaterial.GetVector("_WindCTiling");
                _WindCWrap = grassMaterial.GetVector("_WindCWrap");

                _RandomNormal = grassMaterial.GetFloat("_RandomNormal");
                _grassHeight = grassMaterial.GetFloat("_grassHeight");
                _respectHeight = grassMaterial.GetFloat("_respectHeight");
                _BaseHeight = grassMaterial.GetFloat("_BaseHeight");
                _NoiseAmplitude = grassMaterial.GetFloat("_NoiseAmplitude");
                _NoiseFreqX = grassMaterial.GetFloat("_NoiseFreqX");
                _NoiseFreqZ = grassMaterial.GetFloat("_NoiseFreqZ");
                _NoiseOffsetY = grassMaterial.GetFloat("_NoiseOffsetY");
                _textureFeed = grassMaterial.GetFloat("_textureFeed");
            }
        }

        //v1.2
        public bool enableInteractHere = true;

        //v0.4
        public bool endlessMode = false;
        public float scaleGrassHolder = 450;
        public bool autoAdjustScale = true;

        //v0.3
        public bool computeFeedVertices = false;

        //v0.1
        public float cameraFrustumPlanesLeft = 0;
        public float cameraFrustumPlanesRight = 0;
        public float cameraFrustumPlanesDownY = 0;
        public float cameraFrustumPlanesUpY = 0;
        public float cameraFrustumPlanesFarY = 0;
        public float cameraFrustumPlanesCloseY = 0;
        public float minY = 0;
        public float maxY = 0;
        public float offsetGPUcullX = 0;
        public float offsetGPUcullY = 0;


        [Header("Settings")]
        public float drawDistance = 125;//this setting will affect performance a lot!
        public Material instanceMaterial;

        [Header("Internal")]
        public ComputeShader cullingComputeShader;

        public ComputeShader growthComputeShader;//v0.2

        [NonSerialized]
        public List<Vector3> allGrassPos = new List<Vector3>();//user should update this list using C#
                                                               //=====================================================
        [HideInInspector]
        public static InstancedIndirectGrassRenderer instance;// global ref to this script

        private int cellCountX = -1;
        private int cellCountZ = -1;
        private int dispatchCount = -1;

        //smaller the number, CPU needs more time, but GPU is faster
        private float cellSizeX = 10; //unity unit (m)
        private float cellSizeZ = 10; //unity unit (m)

        private int instanceCountCache = -1;
        private Mesh cachedGrassMesh;

        private ComputeBuffer allInstancesPosWSBuffer;
        private ComputeBuffer visibleInstancesOnlyPosWSIDBuffer;
        private ComputeBuffer argsBuffer;

        private List<Vector3>[] cellPosWSsList; //for binning: binning will put each posWS into correct cell
        private float minX, minZ, maxX, maxZ;
        private List<int> visibleCellIDList = new List<int>();
        private Plane[] cameraFrustumPlanes = new Plane[6];

        bool shouldBatchDispatch = true;
        //=====================================================

        private void OnEnable()
        {
            if (enableInteractHere)
            {
                instance = this; // assign global ref using this script
            }
        }

        //v1.2
        public void Update()
        {
            if (ReadMaterialProps)
            {
                ReadMaterial(instanceMaterial);
                ReadMaterialProps = false;
            }
            if (UpdateMaterialProps)
            {
                UpdateMaterial(instanceMaterial);
            }
        }

        public void LateUpdate()
        {

            //v0.4
            if (endlessMode)
            {
                instanceMaterial.SetVector("_WorldSpaceCameraPosA", Camera.main.transform.position);
                instanceMaterial.SetFloat("scaleGrassHolder", scaleGrassHolder);// transform.localScale.x);
            }
            else
            {
                instanceMaterial.SetFloat("scaleGrassHolder", 0);
            }

    // recreate all buffers if needed
    UpdateAllInstanceTransformBufferIfNeeded();

            if (allGrassPos.Count <= 0)
            {
                return;
            }

            if (Application.isPlaying && computeFeedVertices)
            {
                UpdateComputeVertexFeed();
            }

            //=====================================================================================================
            // rough quick big cell frustum culling in CPU first
            //=====================================================================================================
            visibleCellIDList.Clear();//fill in this cell ID list using CPU frustum culling first
            Camera cam = Camera.main;

            //Do frustum culling using per cell bound
            //https://docs.unity3d.com/ScriptReference/GeometryUtility.CalculateFrustumPlanes.html
            //https://docs.unity3d.com/ScriptReference/GeometryUtility.TestPlanesAABB.html
            float cameraOriginalFarPlane = cam.farClipPlane;
            cam.farClipPlane = drawDistance;//allow drawDistance control    
            GeometryUtility.CalculateFrustumPlanes(cam, cameraFrustumPlanes);//Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
            cam.farClipPlane = cameraOriginalFarPlane;//revert far plane edit

            //slow loop
            //TODO: (A)replace this forloop by a quadtree test?
            //TODO: (B)convert this forloop to job+burst? (UnityException: TestPlanesAABB can only be called from the main thread.)
            Profiler.BeginSample("CPU cell frustum culling (heavy)");
            //Debug.Log("cellPosWSsList.Length="+ cellPosWSsList.Length);
            for (int i = 0; i < cellPosWSsList.Length; i++)
            {
                //create cell bound
                 Vector3 centerPosWS = new Vector3(i % cellCountX + 0.5f, 0, i / cellCountX + 0.5f);
                //Vector3 centerPosWS = new Vector3(i % cellCountX + 0.5f, transform.position.y, i / cellCountX + 0.5f);
                centerPosWS.x = Mathf.Lerp(minX, maxX, centerPosWS.x / cellCountX);
                centerPosWS.z = Mathf.Lerp(minZ, maxZ, centerPosWS.z / cellCountZ);
                Vector3 sizeWS = new Vector3(Mathf.Abs(maxX - minX) / cellCountX, 0, Mathf.Abs(maxX - minX) / cellCountX);
                // Bounds cellBound = new Bounds(centerPosWS + new Vector3(0,50,0), sizeWS);
                Bounds cellBound = new Bounds(centerPosWS, sizeWS);

                cameraFrustumPlanes[2].Translate(new Vector3(0, cameraFrustumPlanesDownY, 0));
                cameraFrustumPlanes[3].Translate(new Vector3(0, cameraFrustumPlanesUpY, 0));
                cameraFrustumPlanes[4].Translate(new Vector3(0, cameraFrustumPlanesFarY, 0));
                cameraFrustumPlanes[5].Translate(new Vector3(0, cameraFrustumPlanesCloseY, 0));
                cameraFrustumPlanes[0].Translate(new Vector3(0, cameraFrustumPlanesLeft, 0));
                cameraFrustumPlanes[1].Translate(new Vector3(0, cameraFrustumPlanesRight, 0));

                //if (GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, cellBound) || endlessMode) //v0.4
                {
                    visibleCellIDList.Add(i);
                }
            }
            Profiler.EndSample();

            //=====================================================================================================
            // then loop though only visible cells, each visible cell dispatch GPU culling job once
            // at the end compute shader will fill all visible instance into visibleInstancesOnlyPosWSIDBuffer
            //=====================================================================================================
            Matrix4x4 v = cam.worldToCameraMatrix;
            Matrix4x4 p = cam.projectionMatrix;
            Matrix4x4 vp = p * v;

            visibleInstancesOnlyPosWSIDBuffer.SetCounterValue(0);

            //v0.4
            if (endlessMode)
            {
                cullingComputeShader.SetVector("_WorldSpaceCameraPosA", Camera.main.transform.position);
                cullingComputeShader.SetFloat("scaleGrassHolder", scaleGrassHolder);// transform.localScale.x);
            }
            else
            {
                cullingComputeShader.SetFloat("scaleGrassHolder", 0);
            }

            //v1.3
            if (depthCutoff && instanceMaterial.GetTexture("_ShoreContourTex") != null)
            {
                cullingComputeShader.SetFloat("depthCutoffHeigth", depthCutoffHeigth);
                cullingComputeShader.SetVector("_DepthCameraPos", instanceMaterial.GetVector("_DepthCameraPos"));
                cullingComputeShader.SetFloat("_ShoreFadeFactor", instanceMaterial.GetFloat("_ShoreFadeFactor"));
                cullingComputeShader.SetFloat("_TerrainScale", instanceMaterial.GetFloat("_TerrainScale"));
                cullingComputeShader.SetTexture(0, "_ShoreContourTex", instanceMaterial.GetTexture("_ShoreContourTex"));
            }

            //set once only
            cullingComputeShader.SetMatrix("_VPMatrix", vp);
            cullingComputeShader.SetFloat("_MaxDrawDistance", drawDistance);

            //v0.1
            cullingComputeShader.SetFloat("offsetGPUcullY", offsetGPUcullY);
            cullingComputeShader.SetFloat("offsetGPUcullX", offsetGPUcullX);

            //dispatch per visible cell
            dispatchCount = 0;
            for (int i = 0; i < visibleCellIDList.Count; i++)
            {
                int targetCellFlattenID = visibleCellIDList[i];
                int memoryOffset = 0;
                for (int j = 0; j < targetCellFlattenID; j++)
                {
                    memoryOffset += cellPosWSsList[j].Count;
                }
                cullingComputeShader.SetInt("_StartOffset", memoryOffset); //culling read data started at offseted pos, will start from cell's total offset in memory
                int jobLength = cellPosWSsList[targetCellFlattenID].Count;

                //============================================================================================
                //batch n dispatchs into 1 dispatch, if memory is continuous in allInstancesPosWSBuffer
                if (shouldBatchDispatch)
                {
                    while ((i < visibleCellIDList.Count - 1) && //test this first to avoid out of bound access to visibleCellIDList
                            (visibleCellIDList[i + 1] == visibleCellIDList[i] + 1))
                    {
                        //if memory is continuous, append them together into the same dispatch call
                        jobLength += cellPosWSsList[visibleCellIDList[i + 1]].Count;
                        i++;
                    }
                }
                //============================================================================================

                cullingComputeShader.Dispatch(0, Mathf.CeilToInt(jobLength / 64f), 1, 1); //disaptch.X division number must match numthreads.x in compute shader (e.g. 64)
                dispatchCount++;
            }

            //====================================================================================
            // Final 1 big DrawMeshInstancedIndirect draw call 
            //====================================================================================
            // GPU per instance culling finished, copy visible count to argsBuffer, to setup DrawMeshInstancedIndirect's draw amount 
            ComputeBuffer.CopyCount(visibleInstancesOnlyPosWSIDBuffer, argsBuffer, 4);

            // Render 1 big drawcall using DrawMeshInstancedIndirect    
            Bounds renderBound = new Bounds();

            //v0.4
            if (endlessMode)
            {
                renderBound.SetMinMax(new Vector3(minX, minY, minZ) + Camera.main.transform.position, new Vector3(maxX, maxY, maxZ) + Camera.main.transform.position);
            }
            else
            {
                renderBound.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));//if camera frustum is not overlapping this bound, DrawMeshInstancedIndirect will not even render
            }
            Graphics.DrawMeshInstancedIndirect(GetGrassMeshCache(), 0, instanceMaterial, renderBound, argsBuffer);
        }

        //v0.1
        public bool enableGUI = true;

        private void OnGUI()
        {
            if (enableGUI)//v0.1
            {
                GUI.contentColor = Color.black;
                GUI.Label(new Rect(200, 0, 400, 60),
                    $"After CPU cell frustum culling,\n" +
                    $"-Visible cell count = {visibleCellIDList.Count}/{cellCountX * cellCountZ}\n" +
                    $"-Real compute dispatch count = {dispatchCount} (saved by batching = {visibleCellIDList.Count - dispatchCount})");

                shouldBatchDispatch = GUI.Toggle(new Rect(400, 400, 200, 100), shouldBatchDispatch, "shouldBatchDispatch");
            }
        }

        void OnDisable()
        {
            //release all compute buffers
            if (allInstancesPosWSBuffer != null)
                allInstancesPosWSBuffer.Release();
            allInstancesPosWSBuffer = null;

            if (visibleInstancesOnlyPosWSIDBuffer != null)
                visibleInstancesOnlyPosWSIDBuffer.Release();
            visibleInstancesOnlyPosWSIDBuffer = null;

            if (argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;

            instance = null;
        }
        public bool recreateMesh = false;
        public int grassType = 0;
        Mesh GetGrassMeshCache()
        {
            if (!cachedGrassMesh || recreateMesh)
            {

                recreateMesh = false;

                //if not exist, create a 3 vertices hardcode triangle grass mesh
                cachedGrassMesh = new Mesh();

                if (grassType == 0)
                {
                    //single grass (vertices)
                    Vector3[] verts = new Vector3[3];
                    verts[0] = new Vector3(-0.25f, 0);
                    verts[1] = new Vector3(+0.25f, 0);
                    verts[2] = new Vector3(-0.0f, 1);
                    //single grass (Triangle index)
                    int[] trinagles = new int[3] { 2, 1, 0, }; //order to fit Cull Back in grass shader

                    cachedGrassMesh.SetVertices(verts);
                    cachedGrassMesh.SetTriangles(trinagles, 0);
                }

                if (grassType == 3) //TEXTURED QUAD
                {
                    //single grass (vertices)
                    Vector3[] verts = new Vector3[4];
                    verts[0] = new Vector3(-0.25f, 0);
                    verts[1] = new Vector3(+0.25f, 0);
                    verts[2] = new Vector3(-0.25f, 1);
                    verts[3] = new Vector3(0.25f, 1);
                    //single grass (Triangle index)
                    int[] trinagles = new int[6] { 2, 1, 0, 0,3,2 }; //order to fit Cull Back in grass shader

                    Vector3[] UVs = new Vector3[4];
                    UVs[0] = new Vector3(0, 0);
                    UVs[1] = new Vector3(1, 0);
                    UVs[2] = new Vector3(0, 1);
                    UVs[3] = new Vector3(1, 1);

                    cachedGrassMesh.SetVertices(verts);
                    cachedGrassMesh.SetUVs(0, UVs);
                    cachedGrassMesh.SetTriangles(trinagles, 0);
                }

                if (grassType == 4) //TEXTURED MULTI QUAD
                {
                    //single grass (vertices)
                    Vector3[] verts = new Vector3[8];
                    verts[0] = new Vector3(-0.25f, 0);
                    verts[1] = new Vector3(+0.25f, 0);
                    verts[2] = new Vector3(-0.25f, 1);
                    verts[3] = new Vector3(0.25f, 1);
                    verts[4] = new Vector3(-0.35f, 0, 0);
                    verts[5] = new Vector3(+0.35f, 0, 0.5f);
                    verts[6] = new Vector3(-0.35f, 1.4f, 1.15f);
                    verts[7] = new Vector3(0.35f, 1,0.15f);
                    //single grass (Triangle index)
                    int[] trinagles = new int[12] { 2, 1, 0, 0, 3, 2,   6, 5, 4, 4, 7, 6}; //order to fit Cull Back in grass shader

                    Vector3[] UVs = new Vector3[8];
                    UVs[0] = new Vector3(0, 0);
                    UVs[1] = new Vector3(1, 0);
                    UVs[2] = new Vector3(0, 1);
                    UVs[3] = new Vector3(1, 1);
                    UVs[4] = new Vector3(0, 0);
                    UVs[5] = new Vector3(1, 0);
                    UVs[6] = new Vector3(0, 1);
                    UVs[7] = new Vector3(1, 1);

                    cachedGrassMesh.SetVertices(verts);
                    cachedGrassMesh.SetUVs(0, UVs);
                    cachedGrassMesh.SetTriangles(trinagles, 0);
                }
                if (grassType == 5) //TEXTURED MULTI QUAD 3
                {
                    //single grass (vertices)
                    Vector3[] verts = new Vector3[12];
                    verts[0] = new Vector3(-0.25f, 0);
                    verts[1] = new Vector3(+0.25f, 0);
                    verts[2] = new Vector3(-0.25f, 1);
                    verts[3] = new Vector3(0.25f, 1);
                    verts[4] = new Vector3(-0.35f, 0, 0);
                    verts[5] = new Vector3(+0.35f, 0, 0.5f);
                    verts[6] = new Vector3(-0.35f, 1.1f, 0.95f);
                    verts[7] = new Vector3(0.35f, 1, 0.15f);
                    verts[8] = new Vector3(-0.25f, 0, 0);
                    verts[9] = new Vector3(+0.55f, 0.5f, 0.5f);
                    verts[10] = new Vector3(-0.35f, 0.4f, -0.85f);
                    verts[11] = new Vector3(0.35f, 0.8f, 0.15f);
                    //single grass (Triangle index)
                    int[] trinagles = new int[18] { 2, 1, 0, 0, 3, 2,    6, 5, 4, 4, 7, 6,    10, 9, 8, 8, 11, 10 }; //order to fit Cull Back in grass shader

                    Vector3[] UVs = new Vector3[12];
                    UVs[0] = new Vector3(0, 0);
                    UVs[1] = new Vector3(1, 0);
                    UVs[2] = new Vector3(0, 1);
                    UVs[3] = new Vector3(1, 1);
                    UVs[4] = new Vector3(0, 0);
                    UVs[5] = new Vector3(1, 0);
                    UVs[6] = new Vector3(0, 1);
                    UVs[7] = new Vector3(1, 1);
                    UVs[8] = new Vector3(0, 0);
                    UVs[9] = new Vector3(1, 0);
                    UVs[10] = new Vector3(0, 1);
                    UVs[11] = new Vector3(1, 1);

                    cachedGrassMesh.SetVertices(verts);
                    cachedGrassMesh.SetUVs(0, UVs);
                    cachedGrassMesh.SetTriangles(trinagles, 0);
                }

                if (grassType == 1)
                {
                    //single grass (vertices)
                    Vector3[] verts = new Vector3[9];
                    verts[0] = new Vector3(-0.25f, 0);
                    verts[1] = new Vector3(+0.25f, 0);

                    verts[2] = new Vector3(-0.2f, 0.25f);
                    verts[3] = new Vector3(+0.2f, 0.25f);

                    verts[4] = new Vector3(-0.15f, 0.5f);
                    verts[5] = new Vector3(+0.15f, 0.5f);

                    verts[6] = new Vector3(-0.05f, 0.75f);
                    verts[7] = new Vector3(+0.05f, 0.75f);

                    verts[8] = new Vector3(-0.0f, 1);
                    //single grass (Triangle index)
                    int[] trinagles = new int[21] { 8, 7, 6, 6, 7, 5, 4, 6, 5, 4, 5, 3, 2, 4, 3, 2, 3, 1, 0, 2, 1 }; //order to fit Cull Back in grass shader

                    Vector3[] UVs = new Vector3[9];
                    //UVs[0] = new Vector3(0, 1);
                    //UVs[1] = new Vector3(1, 1);

                    //UVs[2] = new Vector3(0.05f / 0.25f, 0.75f);
                    //UVs[3] = new Vector3(0.05f / 0.25f, 0.75f);

                    //UVs[4] = new Vector3(0.15f / 0.25f, 0.5f);
                    //UVs[5] = new Vector3(0.15f / 0.25f, 0.5f);

                    //UVs[6] = new Vector3(0.2f / 0.25f, 0.25f);
                    //UVs[7] = new Vector3(0.2f / 0.25f, 0.25f);

                    //UVs[8] = new Vector3(0.5f, 0);
                    UVs[0] = new Vector3(0, 0);
                    UVs[1] = new Vector3(1, 0);

                    UVs[2] = new Vector3(0.05f / 0.25f, 0.25f);
                    UVs[3] = new Vector3(0.05f / 0.25f, 0.25f);

                    UVs[4] = new Vector3(0.15f / 0.25f, 0.5f);
                    UVs[5] = new Vector3(0.15f / 0.25f, 0.5f);

                    UVs[6] = new Vector3(0.2f / 0.25f, 0.75f);
                    UVs[7] = new Vector3(0.2f / 0.25f, 0.75f);

                    UVs[8] = new Vector3(0.5f, 1);

                    //verts[2] = new Vector3(-0.05f, 0.5f);
                    //verts[3] = new Vector3(+0.05f, 0.5f);

                    //verts[4] = new Vector3(-0.0f, 1);
                    ////single grass (Triangle index)
                    //int[] trinagles = new int[9] { 4, 3, 2, 3, 1, 2, 3, 1, 0 }; //order to fit Cull Back in grass shader
                    cachedGrassMesh.SetVertices(verts);
                    cachedGrassMesh.SetUVs(0, UVs);

                    cachedGrassMesh.SetTriangles(trinagles, 0);
                }

                //FLOWERS
                if (grassType == 2)
                {

                    int vertexCount = bladeSegments * 2 + 1 + 3 + 5 * 6;

                    //vertices
                    Vector3[] verts = new Vector3[vertexCount];
                    Vector3[] UVs = new Vector3[vertexCount];

                    for (int i = 0; i < vertexCount; i++)
                    {
                        verts[i] = new Vector3(0, 0);
                        UVs[i] = new Vector3(0, 0);
                    }

                    //verts[0] = new Vector3(-0.25f, 0);
                    //verts[1] = new Vector3(+0.25f, 0);

                    //verts[2] = new Vector3(-0.2f, 0.25f);
                    //verts[3] = new Vector3(+0.2f, 0.25f);

                    //verts[4] = new Vector3(-0.15f, 0.5f);
                    //verts[5] = new Vector3(+0.15f, 0.5f);

                    //verts[6] = new Vector3(-0.05f, 0.75f);
                    //verts[7] = new Vector3(+0.05f, 0.75f);

                    //verts[8] = new Vector3(-0.0f, 1);
                    //single grass (Triangle index)

                    // int[] trinagles = new int[21] { 8, 7, 6, 6, 7, 5, 4, 6, 5, 4, 5, 3, 2, 4, 3, 2, 3, 1, 0, 2, 1 }; //order to fit Cull Back in grass shader
                    int trisCount = 4 * flower_repeats;
                    int offset = 10;
                    int[] trinagles = new int[42] { 8, 7, 6, 6, 7, 5, 4, 6, 5, 4, 5, 3, 2, 4, 3, 2, 3, 1, 0, 2, 1,
                     8+offset, 7+offset, 6+offset, 6+offset, 7+offset, 5+offset, 4+offset, 6+offset, 5+offset, 4+offset, 5+offset, 3+offset, 2+offset, 4+offset, 3+offset, 2+offset, 3+offset, 1+offset, 0+offset, 2+offset, 1+offset
                };

                    //UVs[0] = new Vector3(0, 0);
                    //UVs[1] = new Vector3(1, 0);

                    //UVs[2] = new Vector3(0.05f / 0.25f, 0.25f);
                    //UVs[3] = new Vector3(0.05f / 0.25f, 0.25f);

                    //UVs[4] = new Vector3(0.15f / 0.25f, 0.5f);
                    //UVs[5] = new Vector3(0.15f / 0.25f, 0.5f);

                    //UVs[6] = new Vector3(0.2f / 0.25f, 0.75f);
                    //UVs[7] = new Vector3(0.2f / 0.25f, 0.75f);

                    //UVs[8] = new Vector3(0.5f, 1);
                    Debug.Log(verts.Length);
                    cachedGrassMesh.SetVertices(verts);
                    cachedGrassMesh.SetUVs(0, UVs);
                    cachedGrassMesh.SetTriangles(trinagles, 0);
                }//end type 2

            }

            return cachedGrassMesh;
        }

        public int bladeSegments = 5;
        public int flower_repeats = 2;

        void UpdateAllInstanceTransformBufferIfNeeded()
        {
            //always update
            instanceMaterial.SetVector("_PivotPosWS", transform.position);
            instanceMaterial.SetVector("_BoundSize", new Vector2(transform.localScale.x, transform.localScale.z));

            //early exit if no need to update buffer
            if (instanceCountCache == allGrassPos.Count &&
                argsBuffer != null &&
                allInstancesPosWSBuffer != null &&
                visibleInstancesOnlyPosWSIDBuffer != null)
            {
                return;
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////

            //Debug.Log("UpdateAllInstanceTransformBuffer (Slow)");

            ///////////////////////////
            // allInstancesPosWSBuffer buffer
            ///////////////////////////
            if (allInstancesPosWSBuffer != null)
            {
                allInstancesPosWSBuffer.Release();
            }
            if (allGrassPos.Count <= 0)
            {
                return;
            }

            allInstancesPosWSBuffer = new ComputeBuffer(allGrassPos.Count, sizeof(float) * 3); //float3 posWS only, per grass

            if (visibleInstancesOnlyPosWSIDBuffer != null)
                visibleInstancesOnlyPosWSIDBuffer.Release();
            visibleInstancesOnlyPosWSIDBuffer = new ComputeBuffer(allGrassPos.Count, sizeof(uint), ComputeBufferType.Append); //uint only, per visible grass

            //find all instances's posWS XZ bound min max
            minX = float.MaxValue;
            minZ = float.MaxValue;
            maxX = float.MinValue;
            maxZ = float.MinValue;
            for (int i = 0; i < allGrassPos.Count; i++)
            {
                Vector3 target = allGrassPos[i];
                minX = Mathf.Min(target.x, minX);
                minZ = Mathf.Min(target.z, minZ);
                maxX = Mathf.Max(target.x, maxX);
                maxZ = Mathf.Max(target.z, maxZ);
            }

            //decide cellCountX,Z here using min max
            //each cell is cellSizeX x cellSizeZ
            cellCountX = Mathf.CeilToInt((maxX - minX) / cellSizeX);
            cellCountZ = Mathf.CeilToInt((maxZ - minZ) / cellSizeZ);

            //init per cell posWS list memory
            cellPosWSsList = new List<Vector3>[cellCountX * cellCountZ]; //flatten 2D array
            for (int i = 0; i < cellPosWSsList.Length; i++)
            {
                cellPosWSsList[i] = new List<Vector3>();
            }

            //binning, put each posWS into the correct cell
            for (int i = 0; i < allGrassPos.Count; i++)
            {
                Vector3 pos = allGrassPos[i];

                //find cellID
                int xID = Mathf.Min(cellCountX - 1, Mathf.FloorToInt(Mathf.InverseLerp(minX, maxX, pos.x) * cellCountX)); //use min to force within 0~[cellCountX-1]  
                int zID = Mathf.Min(cellCountZ - 1, Mathf.FloorToInt(Mathf.InverseLerp(minZ, maxZ, pos.z) * cellCountZ)); //use min to force within 0~[cellCountZ-1]

                cellPosWSsList[xID + zID * cellCountX].Add(pos);
            }

            //combine to a flatten array for compute buffer
            int offset = 0;
            Vector3[] allGrassPosWSSortedByCell = new Vector3[allGrassPos.Count];
            for (int i = 0; i < cellPosWSsList.Length; i++)
            {
                for (int j = 0; j < cellPosWSsList[i].Count; j++)
                {
                    allGrassPosWSSortedByCell[offset] = cellPosWSsList[i][j];
                    offset++;
                }
            }

            allInstancesPosWSBuffer.SetData(allGrassPosWSSortedByCell);
            instanceMaterial.SetBuffer("_AllInstancesTransformBuffer", allInstancesPosWSBuffer);
            instanceMaterial.SetBuffer("_VisibleInstanceOnlyTransformIDBuffer", visibleInstancesOnlyPosWSIDBuffer);

            ///////////////////////////
            // Indirect args buffer
            ///////////////////////////
            if (argsBuffer != null)
                argsBuffer.Release();
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            args[0] = (uint)GetGrassMeshCache().GetIndexCount(0);
            args[1] = (uint)allGrassPos.Count;
            args[2] = (uint)GetGrassMeshCache().GetIndexStart(0);
            args[3] = (uint)GetGrassMeshCache().GetBaseVertex(0);
            args[4] = 0;

            argsBuffer.SetData(args);

            ///////////////////////////
            // Update Cache
            ///////////////////////////
            //update cache to prevent future no-op buffer update, which waste performance
            instanceCountCache = allGrassPos.Count;


            //set buffer
            cullingComputeShader.SetBuffer(0, "_AllInstancesPosWSBuffer", allInstancesPosWSBuffer);
            cullingComputeShader.SetBuffer(0, "_VisibleInstancesOnlyPosWSIDBuffer", visibleInstancesOnlyPosWSIDBuffer);
        }


        //v0.2 - Compute shader based feed of vertex positions
        public void Start()
        {
            if (Application.isPlaying && computeFeedVertices && cachedGrassMesh != null)
            {
                initComputeVertexFeed();
            }

        //v0.4
        if(endlessMode && autoAdjustScale)
        {
                scaleGrassHolder = transform.localScale.x / 2;
        }
    }
        public struct vertData
        {
            public uint vid;
            public Vector4 position;
            public Vector3 normal;
            public Vector2 uv;
            public Color color;
            public Vector4 orposition;
            public Vector3 speed;
        }
        private vertData[] meshData;
        private int _kernelVertFeed;
        private ComputeBuffer vertSBUFFER;
        public void initComputeVertexFeed()
        {
            Vector3 vel = Vector3.one;
            meshData = new vertData[cachedGrassMesh.vertexCount];
            for (int i = 0; i < cachedGrassMesh.vertexCount; i++)
            {
                meshData[i].vid = (uint)i;
                meshData[i].position = cachedGrassMesh.vertices[i];
                //meshData[i].normal = cachedGrassMesh.normals[i];
                meshData[i].uv = cachedGrassMesh.uv[i];
                //meshData[i].color = cachedGrassMesh.colors[i];
                meshData[i].orposition = meshData[i].position;
                meshData[i].speed = vel;
            }

            if (vertSBUFFER != null)
            {
                vertSBUFFER.Release();
            }

            //Compute Buffer
            vertSBUFFER = new ComputeBuffer(cachedGrassMesh.vertexCount, 7 * 3 * 4);// size of vertData in bytes
            vertSBUFFER.SetData(meshData);

            //Compute Shader
            _kernelVertFeed = growthComputeShader.FindKernel("CSMain");
            uint threadX = 0;
            uint threadY = 0;
            uint threadZ = 0;
            growthComputeShader.GetKernelThreadGroupSizes(_kernelVertFeed, out threadX, out threadY, out threadZ);
            dispatchCount = Mathf.CeilToInt(meshData.Length / threadX);
            growthComputeShader.SetBuffer(_kernelVertFeed, "vertSBUFFER", vertSBUFFER);
            growthComputeShader.SetInt("vertCount", meshData.Length);

            growthComputeShader.SetInt("flower_repeats", flower_repeats);
            growthComputeShader.SetInt("bladeSegments", bladeSegments);
            growthComputeShader.SetInt("grassType", grassType);

            //Material       
            instanceMaterial.SetBuffer("vertBuffer", vertSBUFFER);
        }
        void UpdateComputeVertexFeed()
        {
            initComputeVertexFeed();
            //Run compute shader
            growthComputeShader.SetFloat("_Time", Time.time);
            growthComputeShader.Dispatch(_kernelVertFeed, dispatchCount, 1, 1);

            
        }
        void OnDestroy()
        {
            if (vertSBUFFER != null)
            {
                vertSBUFFER.Release();
            }
        }
        //////////////////////// END COMPUTE SHADER VERTEX FEED
    }
}