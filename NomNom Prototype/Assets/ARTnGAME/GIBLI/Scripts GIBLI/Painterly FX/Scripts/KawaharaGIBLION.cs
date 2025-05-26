using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.GIBLI
{
   [ExecuteInEditMode]
    public class KawaharaGIBLION : MonoBehaviour
    {
        public bool updateEffects = true;
        public Material KawaharaMaterial;
        public Material NewEffectsMaterial;
        public int effectCategory = 0; //select between Kawahard and other effects shader

        public bool KawaharaProperties = false;
        public bool newEffectsProperties = false;

        //KAWAHARA
        //AKF
        public float AKFRadius = 8;
        public float AKFMaskRadius = 5;
        public Vector4 AKFSharpness = new Vector4(3, -0.3f, 1, 1);
        public int AKFSampleStep = 2;
        public float AKFOverlapX = 0.27f;
        public float AKFOverlapY = 4.2f;
        //LIC
        [Range(1,5)]
        public float LICScale = 2;
        public float LICMaxLen = 11;
        public float LICVariance = 0.13f;



        /////// NEW EFFECTS

        //2.water color
        public Texture2D waterColorTexture;
        public int KernelSize = 2;

        //////1. OUTLINE
        public float OutlineThickness = 3;
        public float DepthSensitivity = 0.4f;//0.5
        public float NormalsSensitivity = 7;//0.44
        public float ColorSensitivity = 3.84f;//0.1
        public Color OutlineColor = new Color(0.14f, 0.1f, 0.1f, 0);//new Color(0, 0, 0, 0);
        public Vector4 OutlineControls = new Vector4(3, 0, 0, 0);//new Vector4(0, 0, 0, 0);
        public float TexelScale = 0.2f;// 0.1f;





        ////////// 3. "UltraEffects/UnderwaterGIBLION"
        public Texture2D BumpMap;
        public float WaterColourStrength= 0.01f;
		public Color WaterColour = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        public float FogStrength = 0.1f;
        public float UnderwaterFactor =0;

        ////////// 4. "SMO/Complete/PixelSNES"
        public Vector4 SNESFactor = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        ////////5. "SMO/Complete/Neon"
        public Vector4 NeonFactor = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        //////6. shaders-pmd - Hatching
        public float SmudgeStrengthHatching = 0.002f;
        public float DrawingStrengthHatching = 0.35f;

        public Texture2D HatchingTex;
        public Vector4 TilingOffsetHatching = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
        public float HatchingSpeed = 2.5f;

        public Vector4 HatchFactor = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        //////7.shaders-retro - GameBoyRamp
        public Texture2D GameboyRampTex;
		public Vector4 GameboyFactor = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

		///DEPTH
		public Vector4 worldPosScaler = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

		////8.KNITTING
		public Texture2D KnitwearMap;
		public float KnitwearDivision= 1;
		public float KnitwearAspect= 1;
		public float KnitwearShear = 1;
		public float KnitwearDistortionStrength= 1;
		public Vector4 KnitFactor = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
                       

        // Start is called before the first frame update
        void Start()
        { }


        // Update is called once per frame
        void Update()
        {
            if (updateEffects)
            {
                if (KawaharaMaterial != null && NewEffectsMaterial != null)
                {
                    //2.water color
                    if (waterColorTexture != null)
                    {
                        NewEffectsMaterial.SetTexture("_MainTex", waterColorTexture);
                    }
                    NewEffectsMaterial.SetInt("_KernelSize", KernelSize);

                    NewEffectsMaterial.SetFloat("OutlineThickness", OutlineThickness);
                    NewEffectsMaterial.SetFloat("DepthSensitivity", DepthSensitivity);
                    NewEffectsMaterial.SetFloat("NormalsSensitivity", NormalsSensitivity);
                    NewEffectsMaterial.SetFloat("ColorSensitivity", ColorSensitivity);
                    NewEffectsMaterial.SetColor("OutlineColor", OutlineColor);
                    NewEffectsMaterial.SetVector("OutlineControls", OutlineControls);
                    NewEffectsMaterial.SetFloat("TexelScale", TexelScale);

                    ////////// 3. "UltraEffects/UnderwaterGIBLION"
                    if (BumpMap != null)
                    {
                        NewEffectsMaterial.SetTexture("_BumpMap", BumpMap);
                    }
                    NewEffectsMaterial.SetFloat("_WaterColourStrength", WaterColourStrength);
                    NewEffectsMaterial.SetColor("_WaterColour", WaterColour);
                    NewEffectsMaterial.SetFloat("_FogStrength", FogStrength);
                    NewEffectsMaterial.SetFloat("UnderwaterFactor", UnderwaterFactor);

                    ////////// 4. "SMO/Complete/PixelSNES"
                    NewEffectsMaterial.SetVector("SNESFactor", SNESFactor);

                    ////////5. "SMO/Complete/Neon"
                    NewEffectsMaterial.SetVector("NeonFactor", NeonFactor);

                    //////6. shaders-pmd - Hatching
                    NewEffectsMaterial.SetFloat("_SmudgeStrengthHatching", SmudgeStrengthHatching);
                    NewEffectsMaterial.SetFloat("_DrawingStrengthHatching", DrawingStrengthHatching);
                    if (HatchingTex != null)
                    {
                        NewEffectsMaterial.SetTexture("_HatchingTex", HatchingTex);
                    }
                    NewEffectsMaterial.SetVector("_TilingOffsetHatching", TilingOffsetHatching);
                    NewEffectsMaterial.SetFloat("_HatchingSpeed", HatchingSpeed);
                    NewEffectsMaterial.SetVector("HatchFactor", HatchFactor);

                    //////7.shaders-retro - GameBoyRamp
                    if (GameboyRampTex != null)
                    {
                        NewEffectsMaterial.SetTexture("_GameboyRampTex", GameboyRampTex);
                    }
                    NewEffectsMaterial.SetVector("GameboyFactor", GameboyFactor);
                    ///DEPTH
                    NewEffectsMaterial.SetVector("worldPosScaler", worldPosScaler);

                    ////8.KNITTING
                    if (KnitwearMap != null)
                    {
                        NewEffectsMaterial.SetTexture("_KnitwearMap", KnitwearMap);
                    }
                    NewEffectsMaterial.SetFloat("_KnitwearDivision", KnitwearDivision);
                    NewEffectsMaterial.SetFloat("_KnitwearAspect", KnitwearAspect);
                    NewEffectsMaterial.SetFloat("_KnitwearShear", KnitwearShear);
                    NewEffectsMaterial.SetFloat("_KnitwearDistortionStrength", KnitwearDistortionStrength);
                    NewEffectsMaterial.SetVector("KnitFactor", KnitFactor);
    }//END NewEffectsMaterial





                if (KawaharaMaterial != null)
                {
                    if (effectCategory == 0)
                    {
                        KawaharaMaterial.SetInt("effectsChoice", 0);
                    }
                    else if (effectCategory == 1)
                    {
                        KawaharaMaterial.SetInt("effectsChoice", 1);
                    }
                    else
                    {
                        KawaharaMaterial.SetInt("effectsChoice", 0);
                    }

                    //AKF
                    KawaharaMaterial.SetFloat("_AKFRadius", AKFRadius);
                    KawaharaMaterial.SetFloat("_AKFMaskRadius", AKFMaskRadius);
                    KawaharaMaterial.SetVector("_AKFSharpness", AKFSharpness);
                    KawaharaMaterial.SetInt("_AKFSampleStep", AKFSampleStep);
                    KawaharaMaterial.SetFloat("_AKFOverlapX", AKFOverlapX);
                    KawaharaMaterial.SetFloat("_AKFOverlapY", AKFOverlapY);
                    //LIC
                    KawaharaMaterial.SetFloat("_LICScale", LICScale);
                    KawaharaMaterial.SetFloat("_LICMaxLen", LICMaxLen);
                    KawaharaMaterial.SetFloat("_LICVariance", LICVariance);
                }//END KawaharaMaterial

                
            }
        }
    }
}