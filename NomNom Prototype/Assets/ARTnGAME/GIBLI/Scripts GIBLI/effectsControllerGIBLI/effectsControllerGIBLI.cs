using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering.Universal;//v1.1.8n
using UnityEngine.Rendering;
using System.Reflection;

// v1.1.9f
#if UNITY_EDITOR
using UnityEditor;
#endif

//DEPTH LINE
using Artngame.GIBLI.Toguchi.Rendering;

//CUSTOM EFFECTS
using Artngame.GIBLI.SleeplessOwl.URPPostProcessing;

//RIVER
using Artngame.SplineMesh;

//CAVITY
using Artngame.GIBLI.MalyaWka.ScreenSpaceCavity.Renders;

//GRASS
using Artngame.CommonTools.DepthRenderer;

//TREES
using Artngame.GIBLI.TreeMaker;

//EXTRAS
using Artngame.GIBLI.MetaBall2D;
//using Artngame.GIBLI.SSR;
using Artngame.CommonTools;//v1.5c

namespace Artngame.GIBLI
{
    [ExecuteInEditMode]
    public class effectsControllerGIBLI : MonoBehaviour
    {

        public PostProcessOrderConfig PostProcessOrderConfig;
        public GameObject GIBLI_Effects_Volume_SOURCE;
        public Volume GIBLI_Effects_Volume;
        public Shader depthLineShader;

        public Shader DiffusionShader;
        public Shader FlareShader;
        public Shader GradientFogShader;
        public Shader ShaftsShader;
        public Material PixelateShader;
        public Shader RaymarchShader;
        public Material RippleShader;
        public Material OutlineShader;

        //RIVER
        public GameObject riverSOURCE;

        //CLOUD
        public GameObject cloudSOURCE;
        public GameObject cloudMakerSOURCE;
        public GameObject hatchSOURCE;
        public GameObject treeMakerSOURCE;
        public GameObject treeBlobMakerSOURCE;
        public GameObject grassInstancedSOURCE; public GameObject grassInstanced;
        public GameObject grassGeometrySOURCE;

        //CAVITY
        public GameObject cavitySOURCE;
        public connectVisualFXGIBLI cavityController;
        //public enum CavityType { Both = 0, Curvature = 1, Cavity = 2 }
        public int cavityType = 0;
        //[SerializeField] internal CavityType cavityType = CavityType.Both;
        public bool debugCavity = false;
        public float curvatureScale = 1.0f;
        public float curvatureRidge = 0.25f;
        public float curvatureValley = 0.25f;
        public float cavityDistance = 0.25f;
        public float cavityAttenuation = 0.015625f;
        public float cavityRidge = 1.25f;
        public float cavityValley = 1.25f;
        public int cavitySamples = 4;

        ////////////////////////// CONTROL VARIABLES //////////////////////////
        public float Outline_Depth_Sensitivity = 0.4f;//0.01f;
        public float Outline_Normals_Sensitivity = 0.1f;//7.52f;
        public float Outline_Color_Sensitivity = 4;//3.35f;
        public Color Outline_Color = Color.black;
        public float OutlineThickness = 0.9f;

        public float RippleStrength = 0.0127f;
        public float RippleScale = 22;
        public float RippleFalloff = 20;
        public float RippleNoiseTiling = 1;
        public float RippleNoiseStep = 0.2f;

        //SKYBOX
        public bool skyboxSettings = false;
        public Material skyboxMaterial;
        public Vector3 _ControlVector = new Vector3(0.8f,1,0);
        public float posterizeFactor=0.2f;
        public float posterizeColorNumber=10;
        public Vector3 darkenSun = new Vector3(0,1,1);
        public float SkyExposure=8;     // HDR exposure
        public Color GroundColor = new Color(210.0f / 255.0f, 227.0f / 255.0f, 255.0f/255.0f);
        public float SunSize  = 0.043f;
        public float SunSizeConvergence=5;
        public Color SkyTint = new Color(50.0f / 255.0f, 47.0f / 255.0f, 195.0f / 255.0f);
        public float AtmosphereThickness=0.6f;
        //
        public Color SkyColor1 = new Color(33.0f / 255.0f, 64.0f / 255.0f, 96.0f / 255.0f);
        public Color SkyColor2 = new Color(43.0f / 255.0f,0.0f / 255.0f, 0.0f / 255.0f);
        //public Vector3 UpVector;
        public float SkyIntensity=1;
        public float SkyExponent=1;
        public Color SkyColor3 = new Color(6.0f / 255.0f, 67.0f / 255.0f, 67.0f / 255.0f);
        public float SkyIntensityB=1;
        public float SkyExponent1 = 1;
        public float SkyExponent2 = 1;

        //GRASS
        //v1.2
        public float scaleParameterBounds = 1;
        public bool grassSettings = false;
        public Material grassMaterial;
        public Color _BaseColor = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
        public Texture2D _BaseColorTexture; //GRASS TEXTURE
        public Color _GroundColor = new Color(56.0f / 255.0f, 118.0f / 255.0f, 66.0f / 255.0f);
        // [Header(Grass Shape)]
        public float _GrassWidth = 0.61f;
        public float _GrassHeight = 0.52f;
        //[Header(Wind)]
        public float _WindAIntensity = 1;
        public float _WindAFrequency = 1;
        public Vector3 _WindATiling = new Vector3(0.82f,0.62f,0);
        public Vector3 _WindAWrap = new Vector3(0.42f, 0.5f, 0);

        public float _WindBIntensity = 1;
        public float _WindBFrequency = 1;
        public Vector3 _WindBTiling = new Vector3(3.43f, 3.0f, 0);
        public Vector3 _WindBWrap = new Vector3(0.5f, 0.5f, 0);

        public float _WindCIntensity = 0.59f;
        public float _WindCFrequency = 2;
        public Vector3 _WindCTiling = new Vector3(1.31f, 3, 0);
        public Vector3 _WindCWrap= new Vector3(0.5f, 0.5f, 0);

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



        // Start is called before the first frame update
        void Start()
        {
            checkPipelineFeatures();
        }

        // Update is called once per frame
        void Update()
        {
            //v0.3
            if(grassMaterial != null)
            {
                grassMaterial.SetColor("_BaseColor", _BaseColor);
                if(_BaseColorTexture != null){
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

            //v0.3
            if (skyboxMaterial != null)
            {
                //SKYBOX
                skyboxMaterial.SetVector("_ControlVector", _ControlVector);
                skyboxMaterial.SetFloat("posterizeFactor", posterizeFactor);
                skyboxMaterial.SetFloat("posterizeColorNumber", posterizeColorNumber);
                skyboxMaterial.SetVector("darkenSun", darkenSun);
                skyboxMaterial.SetFloat("_Exposure", SkyExposure);
                skyboxMaterial.SetColor("_GroundColor", GroundColor);
                skyboxMaterial.SetFloat("_SunSize", SunSize);
                skyboxMaterial.SetFloat("_SunSizeConvergence", SunSizeConvergence);
                skyboxMaterial.SetColor("_SkyTint", SkyTint);
                skyboxMaterial.SetFloat("_AtmosphereThickness", AtmosphereThickness);
                skyboxMaterial.SetColor("_Color1", SkyColor1);
                skyboxMaterial.SetColor("_Color2", SkyColor2);
                skyboxMaterial.SetFloat("_Intensity", SkyIntensity);
                skyboxMaterial.SetFloat("_Exponent", SkyExponent);
                skyboxMaterial.SetColor("_Color3", SkyColor3);
                skyboxMaterial.SetFloat("_IntensityB", SkyIntensityB);
                skyboxMaterial.SetFloat("_Exponent1", SkyExponent1);
                skyboxMaterial.SetFloat("_Exponent2", SkyExponent2);
            }

            //v0.2
            if(cavityController != null)
            {
                //update cavity
                ScreenSpaceCavitySettings localSettings = new ScreenSpaceCavitySettings();
                if (cavityType == 0) {
                    localSettings.cavityType = ScreenSpaceCavitySettings.CavityType.Both;
                }
                if (cavityType == 1)
                {
                    localSettings.cavityType = ScreenSpaceCavitySettings.CavityType.Curvature;
                }
                if (cavityType == 2)
                {
                    localSettings.cavityType = ScreenSpaceCavitySettings.CavityType.Cavity;
                }
                if (cavityType == 3)
                {
                    localSettings.cavityType = ScreenSpaceCavitySettings.CavityType.Disable;
                }
                localSettings.debug = debugCavity;
                localSettings.curvatureScale = curvatureScale;
                localSettings.curvatureRidge = curvatureRidge;
                localSettings.curvatureValley = curvatureValley;
                localSettings.cavityDistance = cavityDistance;
                localSettings.cavityAttenuation = cavityAttenuation;
                localSettings.cavityRidge = cavityRidge;
                localSettings.cavityValley = cavityValley;
                localSettings.cavitySamples = cavitySamples;

                cavityController.cavitySettings = localSettings;
            }



            if (OutlineShader != null)
            {
                OutlineShader.SetFloat("Outline_Depth_Sensitivity", Outline_Depth_Sensitivity);
                OutlineShader.SetFloat("Outline_Normals_Sensitivity", Outline_Normals_Sensitivity);
                OutlineShader.SetFloat("Outline_Color_Sensitivity", Outline_Color_Sensitivity);
                OutlineShader.SetColor("Outline_Color", Outline_Color);
                OutlineShader.SetFloat("OutlineThickness", OutlineThickness);
            }
            if (RippleShader != null)
            {
                RippleShader.SetFloat("_RippleStrength", RippleStrength);
                RippleShader.SetFloat("_RippleScale", RippleScale);
                RippleShader.SetFloat("_RippleFalloff", RippleFalloff);
                RippleShader.SetFloat("_NoiseTiling", RippleNoiseTiling);
                RippleShader.SetFloat("_NoiseStep", RippleNoiseStep);
            }
        }

        ///// IMAGE EFFECTS SETUP
        ///////////////////////////////////////// GRASS BENDING SETUP /////////////////////////////////////////
        [HideInInspector]
        public bool setupGrassBending = false;
        [HideInInspector]
        public bool setupRippleFeature = false;
        [HideInInspector]
        public bool setupPixelateFeature = false;
        [HideInInspector]
        public bool setupMatballFeature = false;
        [HideInInspector]
        public bool setupScreenSReflectionFeature = false;
        [HideInInspector]
        public bool setupCustomPostProcessFeature = false;
        [HideInInspector]
        public bool setupStarGlowFeature = false;
        [HideInInspector]
        public bool setupRayMarchFeature = false;
        [HideInInspector]
        public bool setupLightShaftsFeature = false;
        [HideInInspector]
        public bool setupGradientFogFeature = false;
        [HideInInspector]
        public bool setupFlareFeature = false;
        [HideInInspector]
        public bool setupDiffusionFeature = false;
        [HideInInspector]
        public bool setupDepthLineFeature = false;
        [HideInInspector]
        public bool setupOutlineFeature = false;
        [HideInInspector]
        public bool setupDepthNormalsFeature = false;
        [HideInInspector]
        public bool setupGrabScreenFeature = false;
        [HideInInspector]
        public bool setupCavityFeature = false;


        //v0.3
        public void checkPipelineFeatures()
        {
#if (UNITY_EDITOR)   

            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i].GetType() == typeof(GrassBendingRTPrePass))
                {
                    if (features[i].isActive){  setupGrassBending = true;   }
                    else{                       setupGrassBending = false;  }                   
                }

                if (features[i].GetType() == typeof(WorldSpaceRippleFeature))
                {
                    if (features[i].isActive) { setupRippleFeature = true; }
                    else { setupRippleFeature = false; }
                }
                if (features[i].GetType() == typeof(PixelateFeature))
                {
                    if (features[i].isActive) { setupPixelateFeature = true; }
                    else { setupPixelateFeature = false; }
                }
                if (features[i].GetType() == typeof(MetaballRender2D))
                {
                    if (features[i].isActive) { setupMatballFeature = true; }
                    else { setupMatballFeature = false; }
                }
                if (features[i].GetType() == typeof(MobileSSPRRendererFeature))
                {
                    if (features[i].isActive) { setupScreenSReflectionFeature = true; }
                    else { setupScreenSReflectionFeature = false; }
                }
                if (features[i].GetType() == typeof(PostProcessRenderFeature))
                {
                    if (features[i].isActive) { setupCustomPostProcessFeature = true; }
                    else { setupCustomPostProcessFeature = false; }
                }
                if (features[i].GetType() == typeof(StarGlowRenderFeature))
                {
                    if (features[i].isActive) { setupStarGlowFeature = true; }
                    else { setupStarGlowFeature = false; }
                }
                if (features[i].GetType() == typeof(SimpleRaymarchRenderFeature))
                {
                    if (features[i].isActive) { setupRayMarchFeature = true; }
                    else { setupRayMarchFeature = false; }
                }

                if (features[i].GetType() == typeof(LightShaftRenderFeature))
                {
                    if (features[i].isActive) { setupLightShaftsFeature = true; }
                    else { setupLightShaftsFeature = false; }
                }
                if (features[i].GetType() == typeof(GradientFogRenderFeature))
                {
                    if (features[i].isActive) { setupGradientFogFeature = true; }
                    else { setupGradientFogFeature = false; }
                }
                if (features[i].GetType() == typeof(FlareRenderFeature))
                {
                    if (features[i].isActive) { setupFlareFeature = true; }
                    else { setupFlareFeature = false; }
                }

                if (features[i].GetType() == typeof(DiffusionRenderFeature))
                {
                    if (features[i].isActive) { setupDiffusionFeature = true; }
                    else { setupDiffusionFeature = false; }
                }
                if (features[i].GetType() == typeof(DepthLineRenderFeature))
                {
                    if (features[i].isActive) { setupDepthLineFeature = true; }
                    else { setupDepthLineFeature = false; }
                }
                if (features[i].GetType() == typeof(OutlineFeature))
                {
                    if (features[i].isActive) { setupOutlineFeature = true; }
                    else { setupOutlineFeature = false; }
                }

                if (features[i].GetType() == typeof(DepthNormalsFeature))
                {
                    if (features[i].isActive) { setupDepthNormalsFeature = true; }
                    else { setupDepthNormalsFeature = false; }
                }
                if (features[i].GetType() == typeof(GrabScreenFeature))
                {
                    if (features[i].isActive) { setupGrabScreenFeature = true; }
                    else { setupGrabScreenFeature = false; }
                }
                if (features[i].GetType() == typeof(ScreenSpaceCavity))
                {
                    if (features[i].isActive) { setupCavityFeature = true; }
                    else { setupCavityFeature = false; }
                }
                ///////
            }           
#endif
        }


        //v0.2
        public void setupRiver()
        {
            if(riverSOURCE != null)
            {
                GameObject river = Instantiate(riverSOURCE, new Vector3(-6,8.1f,4),Quaternion.identity);
                SplineExtrusion[] extrusions = river.GetComponentsInChildren<SplineExtrusion>(false);
                if (extrusions != null)
                {
                    for (int i = 0; i < extrusions.Length; i++)
                    {
                        extrusions[i].isPrefab = false;
                    }
                }
            }
        }
        public void setupClouds()
        {
            if (cloudSOURCE != null)
            {
                Vector3 camForw = Vector3.zero;
                if (Camera.main != null)
                {
                    camForw = Camera.main.transform.position + Camera.main.transform.forward * 80 + Vector3.up * 100;
                    camForw.y = 100;
                }
                GameObject clouds = Instantiate(cloudSOURCE, camForw, Quaternion.identity);               
            }
        }
        public void setupCloudMaker()
        {
            if (cloudMakerSOURCE != null)
            {
                Vector3 camForw = Vector3.zero;
                if (Camera.main != null)
                {
                    camForw = Camera.main.transform.position + Camera.main.transform.forward * 50 + Vector3.up * 10;
                    camForw.y = 5;
                }
                GameObject clouds = Instantiate(cloudMakerSOURCE, camForw, Quaternion.identity);
            }
        }
        public void setupTreeMaker()
        {
            if (treeMakerSOURCE != null)
            {
                Vector3 camForw = Vector3.zero;
                if (Camera.main != null)
                {
                    camForw = Camera.main.transform.position 
                        +Camera.main.transform.forward * UnityEngine.Random.Range(12,35)    //  16 + 
                        + Camera.main.transform.right * UnityEngine.Random.Range(-22, 22)    //12 
                        - Camera.main.transform.up * 3;
                    camForw.y = 0.8f;
                }
                //if (Camera.current != null)
                //{
                //    camForw = Camera.current.transform.position + Camera.current.transform.forward * 30 + Camera.current.transform.right * 10;
                //    //camForw.y = 5;
                //}
                GameObject clouds = Instantiate(treeMakerSOURCE, camForw, Quaternion.identity);
                TreeGenerator treeGen = clouds.GetComponentInChildren<TreeGenerator>();
                if(treeGen != null)
                {
                    //treeGen.runInEditMode = true;
                    treeGen.Build(); treeGen.Build(); //Debug.Log("Building Tree");
                }
            }
        }
        public void setupBlobTreeMaker()
        {
            if (treeBlobMakerSOURCE != null)
            {
                Vector3 camForw = Vector3.zero;
                if (Camera.main != null)
                {
                    camForw = Camera.main.transform.position + Camera.main.transform.forward * 20 + Camera.main.transform.up * 9;
                    //camForw.y = 5;
                }
                GameObject clouds = Instantiate(treeBlobMakerSOURCE, camForw, Quaternion.identity);
            }
        }


        public void setupInstancedGrass()
        {
            if (grassInstancedSOURCE != null && grassInstanced == null)
            {
                Vector3 camForw = Vector3.zero;
                if (Camera.main != null)
                {
                    camForw = Camera.main.transform.position + Camera.main.transform.forward * 80;
                    camForw.y = 0;
                }
                GameObject grass = Instantiate(grassInstancedSOURCE, Vector3.zero, Quaternion.identity);
                grassInstanced = grass;
            }
            if (grassInstanced != null && grassMaterial != null)
            {
                DepthRendererCT_URP depthRenderer = grassInstanced.GetComponentInChildren<DepthRendererCT_URP>();
                if (depthRenderer != null)
                {
                    bool matFound = false;
                    for (int i = 0; i < depthRenderer.Materials.Count; i++)
                    {
                        if (depthRenderer.Materials[i] == grassMaterial)
                        {
                            matFound = true; break;
                        }
                    }
                    if (!matFound)
                    {
                        depthRenderer.Materials.Add(grassMaterial);
                    }
                }
                InstancedIndirectGrassRenderer grassRenderer = grassInstanced.GetComponentInChildren<InstancedIndirectGrassRenderer>();
                if (grassRenderer != null)
                {
                    grassRenderer.instanceMaterial = grassMaterial;
                }
            }
        }


        public void setupGrassBendingFunc(bool disable)
        {    
            if(1==1)
            {
#if (UNITY_EDITOR)
                //https://forum.unity.com/threads/access-renderer-feature-settings-at-runtime.770918/
                //https://forum.unity.com/threads/urp-adding-a-renderfeature-from-script.1117060/ // v1.1.9f

                UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);
               
                //m_DefaultRendererIndex
                FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
                int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
                Debug.Log("Default renderer ID = " + rendererDefaultIndex);

                FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
                ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

                List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
                bool foundFogFeature = false;
                for (int i = 0; i < features.Count; i++)
                {
                    //if find, all good, if not set it up
                    if (features[i].GetType() == typeof(GrassBendingRTPrePass)) //if (features[i].name == "NewBlitVolumeFogSRP")
                    {
                        if (disable)
                        {
                            features[i].SetActive(false);
                            setupGrassBending = false;
                        }
                        else
                        {
                            features[i].SetActive(true);
                            setupGrassBending = true;
                        }
                        foundFogFeature = true;
                    }
                    //Debug.Log(features[i].name);
                }
                if (foundFogFeature && !disable)
                {
                    Debug.Log("The Grass Bending forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
                }
                else if (!disable)
                {
                    //SET IT UP
                    //if (volumeFogMaterial != null)
                    {
                        GrassBendingRTPrePass volumeFOGFeature = ScriptableObject.CreateInstance<GrassBendingRTPrePass>(); //new BlitVolumeFogSRP();

                        //Define settings
                        //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                        //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;

                        volumeFOGFeature.name = "GrassBendingRTPrePassGIBLI";
                        ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                        BlitVolumeFogSRPfeature.Create();

                        AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                        renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                        renderDATA.SetDirty();
                        EditorUtility.SetDirty(renderDATA);

                        Debug.Log("The Grass Bending forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                    }
                    //else
                    //{
                    //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                    //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                    //}
                }                
#endif
                //setupGrassBending = false;
            }
        }

        ///////////////////////////////////////// GRAB SCREEN SETUP /////////////////////////////////////////
       
        public void setupGrabScreenFeatureFunc(bool disable)
        {            
#if (UNITY_EDITOR)
                UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

                FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
                int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
                //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

                FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
                ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

                List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
                bool foundFogFeature = false;
                for (int i = 0; i < features.Count; i++)
                {
                    //if find, all good, if not set it up
                    if (features[i].GetType() == typeof(GrabScreenFeature))
                    {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupGrabScreenFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupGrabScreenFeature = true;
                    }
                    foundFogFeature = true;
                    }
                    //Debug.Log(features[i].name);
                }
                if (foundFogFeature && !disable)
                {
                    Debug.Log("The Grab Screen forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
                }
            else if (!disable)
            {
                    //SET IT UP
                    //if (volumeFogMaterial != null)
                    {
                        GrabScreenFeature volumeFOGFeature = ScriptableObject.CreateInstance<GrabScreenFeature>(); //new BlitVolumeFogSRP();

                        //Define settings
                        //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                        //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                        //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                        volumeFOGFeature.name = "GrabScreenGIBLI";
                        ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                        BlitVolumeFogSRPfeature.Create();

                        AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                        renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                        renderDATA.SetDirty();
                        EditorUtility.SetDirty(renderDATA);

                        Debug.Log("The Grab Screen forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                    }
                    //else
                    //{
                    //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                    //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                    //}
                }
#endif            
        }

        ///////////////////////////////////////// DEPTH NORMALS SETUP /////////////////////////////////////////
       
        public void setupDepthNormalsFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(DepthNormalsFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupDepthNormalsFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupDepthNormalsFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Depth Normals forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    DepthNormalsFeature volumeFOGFeature = ScriptableObject.CreateInstance<DepthNormalsFeature>();  ///////////// FEATURE

                    //Define settings
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "DepthNormalsGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Depth Normals forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// DEPTH NORMALS SETUP /////////////////////////////////////////
        
        public void setupOutlineFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(OutlineFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupOutlineFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupOutlineFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Outline forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    OutlineFeature volumeFOGFeature = ScriptableObject.CreateInstance<OutlineFeature>();  ///////////// FEATURE

                    //Define settings
                    if (depthLineShader != null)
                    {
                        volumeFOGFeature.settings.outlineMaterial = OutlineShader;
                    }
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "OutlineGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Outline forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// DEPTH LINE SETUP /////////////////////////////////////////
        
        public void setupDepthLineFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(DepthLineRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupDepthLineFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupDepthLineFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Depth Line forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    DepthLineRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<DepthLineRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    if(depthLineShader != null)
                    {
                        volumeFOGFeature.settings.shader = depthLineShader;
                    }
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "DepthLineRenderGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Depth Line forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// DIFFUSION SETUP /////////////////////////////////////////
        
        public void setupDiffusionFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(DiffusionRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupDiffusionFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupDiffusionFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Diffusion forward renderer feature is already added in the Default renderer in the URP pipeline asset.");                
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    DiffusionRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<DiffusionRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    if (DiffusionShader != null)
                    {
                        volumeFOGFeature.settings.shader = DiffusionShader;
                    }                    
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "DiffusionGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Diffusion forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// FLARE SETUP /////////////////////////////////////////
       
        public void setupFlareFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(FlareRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupFlareFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupFlareFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Flare forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    FlareRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<FlareRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    if (FlareShader != null)
                    {
                        volumeFOGFeature.settings.shader = FlareShader;
                    }                    
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "FlareGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Flare forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// GRADIENT FOG SETUP /////////////////////////////////////////
        
        public void setupGradientFogFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(GradientFogRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupGradientFogFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupGradientFogFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Gradient Fog forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    GradientFogRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<GradientFogRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    if (GradientFogShader != null)
                    {
                        volumeFOGFeature.settings.shader = GradientFogShader;
                    }                    
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "GradientFogGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Gradient Fog forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// LIGHT SHAFTS SETUP /////////////////////////////////////////
       
        public void setupLightShaftsFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(LightShaftRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupLightShaftsFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupLightShaftsFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Light Shafts forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    LightShaftRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<LightShaftRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    if (ShaftsShader != null)
                    {
                        volumeFOGFeature.settings.shader = ShaftsShader;
                    }                   
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "LightShaftsGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Light Shafts forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// RAY MARCH SETUP /////////////////////////////////////////
       
        public void setupRayMarchFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(SimpleRaymarchRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupRayMarchFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupRayMarchFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Ray March forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    SimpleRaymarchRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<SimpleRaymarchRenderFeature>();  ///////////// FEATURE

                    //Define settings                   
                    if (RaymarchShader != null)
                    {
                        volumeFOGFeature.settings.shader = RaymarchShader;
                    }                    
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "RayMarchGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Ray March forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// STAR GLOW SETUP /////////////////////////////////////////
       
        public void setupStarGlowFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(StarGlowRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupStarGlowFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupStarGlowFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Star Glow forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    StarGlowRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<StarGlowRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "StarGlowGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Star Glow forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// CUSTOM POST PROCESSING SETUP /////////////////////////////////////////
       
        public void setupCustomPostProcessFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(PostProcessRenderFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupCustomPostProcessFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupCustomPostProcessFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Custom Post Process forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    PostProcessRenderFeature volumeFOGFeature = ScriptableObject.CreateInstance<PostProcessRenderFeature>();  ///////////// FEATURE

                    //Define settings
                    if (PostProcessOrderConfig != null)
                    {
                        volumeFOGFeature.config = PostProcessOrderConfig;
                    }
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "CustomPostProcessGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Custom Post Process forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// Mobile Screen Space Reflections SETUP /////////////////////////////////////////
      
        public void setupScreenSReflectionFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(MobileSSPRRendererFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupScreenSReflectionFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupScreenSReflectionFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Screen Space Reflections forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    MobileSSPRRendererFeature volumeFOGFeature = ScriptableObject.CreateInstance<MobileSSPRRendererFeature>();  ///////////// FEATURE

                    //Define settings
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "ScreenSpaceReflectionGIBLI";                                                    ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Screen Space Reflections forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// METABALLS 2D SETUP /////////////////////////////////////////
       
        public void setupMatballFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(MetaballRender2D)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupMatballFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupMatballFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Metaball Render 2D forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    MetaballRender2D volumeFOGFeature = ScriptableObject.CreateInstance<MetaballRender2D>();  ///////////// FEATURE

                    //Define settings
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "MetaballRender2DGIBLI";                                                   ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Metaball Render 2D forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// PIXELATE SETUP /////////////////////////////////////////
       
        public void setupPixelateFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(PixelateFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupPixelateFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupPixelateFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Pixelate forward renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    PixelateFeature volumeFOGFeature = ScriptableObject.CreateInstance<PixelateFeature>();  ///////////// FEATURE

                    //Define settings                    
                    if (PixelateShader != null)
                    {
                        volumeFOGFeature.settings.pixelateMaterial = PixelateShader;
                    }                    
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "PixelateFeatureGIBLI";                                                   ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Pixelate forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }

        ///////////////////////////////////////// GRASS BENDING SETUP /////////////////////////////////////////
      
        public void setupRippleFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(WorldSpaceRippleFeature)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupRippleFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupRippleFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Ripple renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    WorldSpaceRippleFeature volumeFOGFeature = ScriptableObject.CreateInstance<WorldSpaceRippleFeature>();  ///////////// FEATURE

                    //Define settings
                    if (RippleShader != null)
                    {
                        volumeFOGFeature.settings.rippleMaterial = RippleShader;
                    }
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "RippleGIBLI";                                                   ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Ripple forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }


        ///////////////////////////////////////// GRASS BENDING SETUP /////////////////////////////////////////

        public void setupCavityFeatureFunc(bool disable)
        {
#if (UNITY_EDITOR)
            UniversalRenderPipelineAsset pipeline = ((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline);

            FieldInfo propertyInfoA = pipeline.GetType().GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            int rendererDefaultIndex = ((int)propertyInfoA?.GetValue(pipeline));
            //Debug.Log("Default renderer ID = " + rendererDefaultIndex);

            FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);//REFLECTION
            ScriptableRendererData renderDATA = ((ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[rendererDefaultIndex];

            List<ScriptableRendererFeature> features = renderDATA.rendererFeatures;
            bool foundFogFeature = false;
            for (int i = 0; i < features.Count; i++)
            {
                //if find, all good, if not set it up
                if (features[i].GetType() == typeof(ScreenSpaceCavity)) ///////////////////// FEATURE
                {
                    if (disable)
                    {
                        features[i].SetActive(false);
                        setupCavityFeature = false;
                    }
                    else
                    {
                        features[i].SetActive(true);
                        setupCavityFeature = true;
                    }
                    foundFogFeature = true;
                }
                //Debug.Log(features[i].name);
            }
            if (foundFogFeature && !disable)
            {
                Debug.Log("The Cavity renderer feature is already added in the Default renderer in the URP pipeline asset.");
            }
            else if (!disable)
            {
                //SET IT UP
                //if (volumeFogMaterial != null)
                {
                    ScreenSpaceCavity volumeFOGFeature = ScriptableObject.CreateInstance<ScreenSpaceCavity>();  ///////////// FEATURE

                    //Define settings
                    //if (RippleShader != null)
                    //{
                    //    volumeFOGFeature.settings.rippleMaterial = RippleShader;
                    //}
                    //volumeFOGFeature.settings.blitMaterial = volumeFogMaterial;
                    //volumeFOGFeature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
                    //volumeFOGFeature.settings.TextureName = "_GrabPassTransparent";

                    volumeFOGFeature.name = "CavityGIBLI";                                                   ///////////// FEATURE
                    ScriptableRendererFeature BlitVolumeFogSRPfeature = volumeFOGFeature as ScriptableRendererFeature;
                    BlitVolumeFogSRPfeature.Create();

                    AssetDatabase.AddObjectToAsset(BlitVolumeFogSRPfeature, renderDATA);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(BlitVolumeFogSRPfeature, out var guid, out long localId);
                    renderDATA.rendererFeatures.Add(BlitVolumeFogSRPfeature);
                    renderDATA.SetDirty();
                    EditorUtility.SetDirty(renderDATA);

                    Debug.Log("The Cavity forward renderer feature is now added in the Default renderer in the URP pipeline asset.");
                }
                //else
                //{
                //    Debug.Log("The Ethereal volumetric fog material is not assigned, please assign the 'VolumeFogSRP_FORWARD_URP' material in the 'VolumeFogMaterial' slot in the " +
                //        "connect script and then enable the 'Setup Forward Renderer' checkbox to setup the fog.");
                //}
            }
#endif            
        }



        ///////////////////////////////////////// GRASS BENDING SETUP /////////////////////////////////////////
        public void addGIBLIURPAllEffectsVolume()
        {
            if(GIBLI_Effects_Volume_SOURCE != null)
            {
                GameObject Volume = Instantiate(GIBLI_Effects_Volume_SOURCE);
                GIBLI_Effects_Volume = Volume.GetComponent<Volume>();
            }
        }

        ///////////////////////////////////////// GRASS BENDING SETUP /////////////////////////////////////////

        //// END IMAGE EFFECTS SETUP


    }
}