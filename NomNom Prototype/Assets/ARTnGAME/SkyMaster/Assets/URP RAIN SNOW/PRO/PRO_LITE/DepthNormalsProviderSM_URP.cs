using UnityEngine;
using System.Collections;
namespace Artngame.SKYMASTER
{
    [ExecuteInEditMode]
    public class DepthNormalsProviderSM_URP : MonoBehaviour
    {
        public Texture2D SnowTexture;

        public UnityEngine.RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32;
        public string textureName = "_CameraDepthNormalsTextureA";
        public string globalTextureName = "_CameraDepthNormalsTexture";
        ////v0.1a
        //[HideInInspector]
        //public float shadowPower = 0.1f;
        //[HideInInspector]
        //public float shadowPowerA = 1f;
        //public float screenRainPower = 1;
        //[HideInInspector]
        //public float screenBrightness = 1;
        //[HideInInspector]
        //public float objectsRainPower = 0;
        //public float rainContrast = 1;
        //public float rainPower = 1;
        //[HideInInspector]
        //public float snowBrightness = 1;
        //[HideInInspector]
        //public float snowBumpPower = 1;
        //[HideInInspector]
        //public Color SnowColor = Color.white;
        //[HideInInspector]
        //public float SnowTextureScale = 0.1f;

        ////[Range(0, 1)]
        //public float BottomThreshold = 0f;
        ////[Range(0, 1)]
        //public float TopThreshold = 1f;

        public Material snowMaterial;

        ////v0.1
        //[HideInInspector]
        //public Texture2D SnowBumpTex;
        //[HideInInspector]
        //public float SnowBumpPowerScale = 1; //Vector2 SnowBumpPowerScale = new Vector2(1, 1);
        //[HideInInspector]
        //public float Shineness = 1;
        //[HideInInspector]
        //public float specularPower = 1;

        ////OUTLINE
        //[HideInInspector]
        //public float OutlineThickness = 0;
        //[HideInInspector]
        //public float DepthSensitivity = 0;
        //[HideInInspector]
        //public float NormalsSensitivity = 0;
        //[HideInInspector]
        //public float ColorSensitivity = 0;
        //[HideInInspector]
        //public Vector4 OutlineColor = new Vector4(0, 0, 0, 1);
        //[HideInInspector]
        //public Vector4 OutlineControls = new Vector4(1, 1, 1, 1);

        ////RAIN
        //public Vector4 interactPointRadius = new Vector4(0, 0, 0, 0);
        //public Vector4 radialControls = new Vector4(0, 0, 0, 0);
        //public Vector4 directionControls = new Vector4(0, 0, 0, 0);
        //public Vector4 wipeControls = new Vector4(0, 0, 0, 0);
        ////MASKED
        //public Vector4 mainTexTilingOffset = new Vector4(1, 1, 0, 0);
        //public float maskPower = 0;
        //public float _Size = 1;
        //public float _Distortion = 0;
        //public float _Blur = 0;
        //public Vector4 _TimeOffset = new Vector4(0, 0, 0, 0);
        //public Vector4 _EraseCenterRadius = new Vector4(0, 0, 0, 0);
        //public float erasePower = 0;
        //public float _TileNumCausticRotMin = 0;
        //public Vector4 _RainSmallDirection = new Vector4(0, 0, 0, 0);


        ////RIPPLES
        //public Texture2D RainRipples;
        //public float RainIntensity = 0;
        //public float RippleAnimSpeed = 0.2f;
        //public float RippleTiling = 1;
        //public float WaterBumpDistance = 1000;


        void OnEnable()
        {
            //_material = new Material(Shader.Find("SkyMaster/ScreenSpaceSnowSM"));
            //GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
        }
        public bool enableDepthNormalsTex = true;
        public Camera normalsCam;
        public RenderTexture depthNormals;
        public bool createDepthNormalsTex = true;
        public bool addDepthInNormalsTex = false;
        //RenderTexture middleA;
        void Update()// OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            //if(snowMaterial == null)
            //{
            //    return;
            //}


            //v0.1
            //if (depthMask != null)
            //{
            //    RenderTexture rt = UnityEngine.RenderTexture.active;
            //    UnityEngine.RenderTexture.active = depthMask;
            //    GL.Clear(true, true, Color.clear);
            //    UnityEngine.RenderTexture.active = rt;

            //    GL.ClearWithSkybox(true, normalsCam);
            //    depthMask.DiscardContents();
            //}

            //if (testDEPTHmask != null)
            //{
            //    //snowMaterial.SetTexture("_MainTex", depthMask);
            //    testDEPTHmask.SetTexture("_BaseTex", depthMask);
            //}

            if (enableDepthNormalsTex && normalsCam != null)
            {
                if (depthNormals != null && (depthNormals.width == Screen.width && depthNormals.height == Screen.height && (!addDepthInNormalsTex || (depthNormals.depth == 16 && addDepthInNormalsTex))))
                {
                    if (normalsCam.targetTexture == null || normalsCam.targetTexture != depthNormals)
                    {
                        normalsCam.targetTexture = depthNormals;
                    }
                    //GL.ClearWithSkybox(true, normalsCam);
                    //normalsCam.Render();
                    Shader.SetGlobalTexture(globalTextureName, depthNormals);//
                                                                                         // Debug.Log("SEND");
                    if (snowMaterial != null)
                    {
                        snowMaterial.SetTexture(textureName, depthNormals);
                    }
                }
                else
                {
                    if (createDepthNormalsTex)
                    {
                        if (addDepthInNormalsTex)
                        {
                            depthNormals = new RenderTexture(Screen.width, Screen.height, 16, textureFormat);
                        }
                        else
                        {
                            depthNormals = new RenderTexture(Screen.width, Screen.height, 0, textureFormat);
                        }
                        if (normalsCam.targetTexture == null || normalsCam.targetTexture != depthNormals)
                        {
                            normalsCam.targetTexture = depthNormals;
                        }
                        //GL.ClearWithSkybox(true, normalsCam);
                        //normalsCam.Render();
                        Shader.SetGlobalTexture(globalTextureName, depthNormals);//
                        if (snowMaterial != null)
                        {
                            snowMaterial.SetTexture(textureName, depthNormals);
                        }
                    }
                }
            }



            //snowMaterial.SetFloat("_CameraRainPower", screenRainPower);
            //snowMaterial.SetFloat("_ObjectRainPower", objectsRainPower);
            //snowMaterial.SetFloat("_rainContrast", rainContrast);
            //snowMaterial.SetFloat("_rainPower", rainPower);
            //snowMaterial.SetFloat("_snowBrightness", snowBrightness);

            ////v0.1a
            //snowMaterial.SetFloat("shadowPower", shadowPower);
            //snowMaterial.SetFloat("shadowPowerA", shadowPowerA);
            //snowMaterial.SetFloat("screenRainPower", screenRainPower);
            //snowMaterial.SetFloat("screenBrightness", screenBrightness);

            //// set 
            //snowMaterial.SetMatrix("_CamToWorld", GetComponent<Camera>().cameraToWorldMatrix);
            //snowMaterial.SetColor("_SnowColor", SnowColor);
            //snowMaterial.SetFloat("_BottomThreshold", BottomThreshold);
            //snowMaterial.SetFloat("_TopThreshold", TopThreshold);
            //snowMaterial.SetTexture("_snowTexture", SnowTexture);////
            //snowMaterial.SetFloat("_SnowTexScale", SnowTextureScale);// * Camera.main.Far);

            ////v0.1
            //snowMaterial.SetTexture("_snowBumpMap", SnowBumpTex);//  snowMaterial.SetTexture("_SnowBumpTex", SnowBumpTex);
            //snowMaterial.SetFloat("Shineness", Shineness);
            //snowMaterial.SetFloat("_snowBumpScale", SnowBumpPowerScale); //snowMaterial.SetVector("SnowBumpPowerScale", SnowBumpPowerScale);
            //snowMaterial.SetFloat("_ShininessSnow", specularPower);//snowMaterial.SetFloat("specularPower", specularPower);
            //snowMaterial.SetFloat("_snowBumpPower", snowBumpPower);

            ////OUTLINE
            //snowMaterial.SetFloat("OutlineThickness", OutlineThickness);
            //snowMaterial.SetFloat("Outline_Depth_Sensitivity", DepthSensitivity);
            //snowMaterial.SetFloat("Outline_Normals_Sensitivity", NormalsSensitivity);
            //snowMaterial.SetFloat("Outline_Color_Sensitivity", ColorSensitivity);
            //snowMaterial.SetVector("Outline_Color", OutlineColor);
            ////snowMaterial.SetVector("OutlineControls", OutlineControls);
            //snowMaterial.SetFloat("EdgesControlA", OutlineControls.x);
            //snowMaterial.SetFloat("EdgesControlB", OutlineControls.y);
            //snowMaterial.SetFloat("EdgesControlC", OutlineControls.z);
            //snowMaterial.SetFloat("EdgesControlD", OutlineControls.w);
            //// snowMaterial.SetVector("_MainTex_TexelSize", new Vector4(1.0f / src.width, 1.0f / src.height, src.width, src.height));


            ////RAIN
            //snowMaterial.SetVector("_interactPointRadius", interactPointRadius);////
            //snowMaterial.SetVector("_radialControls", radialControls);////
            //snowMaterial.SetVector("_directionControls", directionControls);////
            //snowMaterial.SetVector("_wipeControls", wipeControls);////
            ////MASKED
            //snowMaterial.SetVector("_mainTexTilingOffset", mainTexTilingOffset);////
            //snowMaterial.SetFloat("_maskPower", maskPower);////
            //snowMaterial.SetFloat("_Size", _Size);
            //snowMaterial.SetFloat("_Distortion", _Distortion);
            //snowMaterial.SetFloat("_Blur", _Blur);
            ////v0.6
            //snowMaterial.SetVector("_TimeOffset", _TimeOffset);
            //snowMaterial.SetVector("_EraseCenterRadius", _EraseCenterRadius);
            //snowMaterial.SetFloat("_erasePower", erasePower);////
            ////v0.3
            //snowMaterial.SetFloat("_TileNumCausticRotMin", _TileNumCausticRotMin);
            //snowMaterial.SetVector("_RainSmallDirection", _RainSmallDirection);


            ////RIPPLES
            //snowMaterial.SetTexture("_Lux_RainRipples", RainRipples);
            //snowMaterial.SetFloat("_Lux_RainIntensity", RainIntensity);
            //snowMaterial.SetFloat("_Lux_RippleAnimSpeed", RippleAnimSpeed);
            //snowMaterial.SetFloat("_Lux_RippleTiling", RippleTiling);
            //snowMaterial.SetFloat("_Lux_WaterBumpDistance", WaterBumpDistance);


            //// execute the shader on input texture (src) and write to output (dest)
            ////if (middleA == null)
            ////{
            //   // middleA = new RenderTexture(src.width, src.height, 24, RenderTextureFormat.ARGB32);// ARGBFloat);
            ////}
            ////Graphics.Blit(src, dest, _material);
            //snowMaterial.SetFloat("doOutline", 0);
            ////Graphics.Blit(src, middleA, snowMaterial);////////////////////////////// CHECK
            //snowMaterial.SetFloat("doOutline", 1);
            ////Graphics.Blit(middleA, dest, snowMaterial);
        }
    }
}