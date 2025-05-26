using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Artngame.GIBLI
{
    [CustomEditor(typeof(KawaharaGIBLION))]
    public class KawaharaGIBLIONEditor : Editor
    {

        private SerializedProperty effectCategory;

        private SerializedProperty KawaharaMaterial;
        private SerializedProperty NewEffectsMaterial;

        //NEW FX
        private SerializedProperty waterColorTexture;
        private SerializedProperty BumpMap;
        private SerializedProperty HatchingTex; 
        private SerializedProperty GameboyRampTex;
        private SerializedProperty KnitwearMap;

        //KAWAHARA
        //private SerializedProperty AKFRadius;
        //private SerializedProperty AKFMaskRadius;
        //private SerializedProperty AKFSharpness;
        //private SerializedProperty AKFSampleStep;
        //private SerializedProperty AKFOverlapX;
        //private SerializedProperty AKFOverlapY;
        //private SerializedProperty LICScale;
        //private SerializedProperty LICMaxLen;
        //private SerializedProperty LICVariance;

        void OnEnable()
        {
            //v1.2
            effectCategory = serializedObject.FindProperty("effectCategory");
            KawaharaMaterial = serializedObject.FindProperty("KawaharaMaterial");
            NewEffectsMaterial = serializedObject.FindProperty("NewEffectsMaterial");

            //NEW FX
            waterColorTexture = serializedObject.FindProperty("waterColorTexture");
            BumpMap = serializedObject.FindProperty("BumpMap");

            HatchingTex = serializedObject.FindProperty("HatchingTex");
            GameboyRampTex = serializedObject.FindProperty("GameboyRampTex");
            KnitwearMap = serializedObject.FindProperty("KnitwearMap");

        //KAWAHARA
        //AKFRadius = serializedObject.FindProperty("AKFRadius");
        //AKFMaskRadius = serializedObject.FindProperty("AKFMaskRadius");
        //AKFSharpness = serializedObject.FindProperty("AKFSharpness");
        //AKFSampleStep = serializedObject.FindProperty("AKFSampleStep");
        //AKFOverlapX = serializedObject.FindProperty("AKFOverlapX");
        //AKFOverlapY = serializedObject.FindProperty("AKFOverlapY");
        //LICScale = serializedObject.FindProperty("LICScale");
        //LICMaxLen = serializedObject.FindProperty("LICMaxLen");
        //LICVariance = serializedObject.FindProperty("LICVariance");
    }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            KawaharaGIBLION kawahara = (KawaharaGIBLION)target;
            Undo.RecordObject(kawahara, "kawahara param change");
            EditorGUILayout.Space();

            kawahara.updateEffects = EditorGUILayout.Toggle("Update Materials", kawahara.updateEffects);
            kawahara.effectCategory = EditorGUILayout.IntSlider("Effect Category", kawahara.effectCategory, 0, 1);

            kawahara.KawaharaProperties = EditorGUILayout.Foldout(kawahara.KawaharaProperties, "Kawahara Effect Controls", true);
            if (kawahara.KawaharaProperties)
            {

                //clothSpawner.positionOffset = EditorGUILayout.Vector3Field("Position Offset", clothSpawner.positionOffset);
                EditorGUILayout.ObjectField(KawaharaMaterial);
                EditorGUILayout.ObjectField(NewEffectsMaterial);//EditorGUILayout.LabelField("New Effect sMaterial");

                kawahara.AKFRadius = EditorGUILayout.FloatField("AKFRadius", kawahara.AKFRadius);
                kawahara.AKFMaskRadius = EditorGUILayout.FloatField("AKFMaskRadius", kawahara.AKFMaskRadius);
                kawahara.AKFSharpness = EditorGUILayout.Vector4Field("AKFSharpness", kawahara.AKFSharpness);
                kawahara.AKFSampleStep = EditorGUILayout.IntField("AKFSampleStep", kawahara.AKFSampleStep);
                kawahara.AKFOverlapX = EditorGUILayout.FloatField("AKFOverlapX", kawahara.AKFOverlapX);
                kawahara.AKFOverlapY = EditorGUILayout.FloatField("AKFOverlapY", kawahara.AKFOverlapY);


                //kawahara.LICScale = EditorGUILayout.FloatField("LICScale", kawahara.LICScale);
                kawahara.LICScale = EditorGUILayout.Slider("LICScale", kawahara.LICScale, 1, 5);

                kawahara.LICMaxLen = EditorGUILayout.FloatField("LICMaxLen", kawahara.LICMaxLen);
                kawahara.LICVariance = EditorGUILayout.FloatField("LICVariance", kawahara.LICVariance);
            }

            kawahara.newEffectsProperties = EditorGUILayout.Foldout(kawahara.newEffectsProperties, "Retro Glow Outline Effect Controls", true);
            if (kawahara.newEffectsProperties)
            {
                EditorGUILayout.ObjectField(waterColorTexture);
                kawahara.KernelSize = EditorGUILayout.IntField("Kernel Size", kawahara.KernelSize);

                kawahara.OutlineThickness = EditorGUILayout.FloatField("Outline Thickness", kawahara.OutlineThickness);
                kawahara.DepthSensitivity = EditorGUILayout.FloatField("Depth Sensitivity", kawahara.DepthSensitivity);
                kawahara.NormalsSensitivity = EditorGUILayout.FloatField("Normals Sensitivity", kawahara.NormalsSensitivity);
                kawahara.ColorSensitivity = EditorGUILayout.FloatField("Color Sensitivity", kawahara.ColorSensitivity);
                kawahara.OutlineColor = EditorGUILayout.ColorField("Outline Color", kawahara.OutlineColor);
                kawahara.OutlineControls = EditorGUILayout.Vector4Field("Outline Controls", kawahara.OutlineControls);
                kawahara.TexelScale = EditorGUILayout.FloatField("Texel Scale", kawahara.TexelScale);

                ////////// 3. "UltraEffects/UnderwaterGIBLION"
                EditorGUILayout.ObjectField(BumpMap);
                kawahara.WaterColourStrength = EditorGUILayout.FloatField("Water Colour Strength", kawahara.WaterColourStrength);
                kawahara.WaterColour = EditorGUILayout.ColorField("Water Colour", kawahara.WaterColour);
                kawahara.FogStrength = EditorGUILayout.FloatField("Fog Strength", kawahara.FogStrength);
                kawahara.UnderwaterFactor = EditorGUILayout.FloatField("Under water Factor", kawahara.UnderwaterFactor);

                ////////// 4. "SMO/Complete/PixelSNES"
                kawahara.SNESFactor = EditorGUILayout.Vector4Field("SNES Factor", kawahara.SNESFactor);

                ////////5. "SMO/Complete/Neon"
                kawahara.NeonFactor = EditorGUILayout.Vector4Field("Neon Factor", kawahara.NeonFactor);

                //////6. shaders-pmd - Hatching
                kawahara.SmudgeStrengthHatching = EditorGUILayout.FloatField("Smudge Strength Hatching", kawahara.SmudgeStrengthHatching);
                kawahara.DrawingStrengthHatching = EditorGUILayout.FloatField("Drawing Strength Hatching", kawahara.DrawingStrengthHatching);

                EditorGUILayout.ObjectField(HatchingTex);//public Texture2D HatchingTex; GameboyRampTex KnitwearMap
                kawahara.TilingOffsetHatching = EditorGUILayout.Vector4Field("Tiling Offset Hatching", kawahara.TilingOffsetHatching);
                kawahara.HatchingSpeed = EditorGUILayout.FloatField("Hatching Speed", kawahara.HatchingSpeed);
                kawahara.HatchFactor = EditorGUILayout.Vector4Field("Hatch Factor", kawahara.HatchFactor);

                //////7.shaders-retro - GameBoyRamp
                EditorGUILayout.ObjectField(GameboyRampTex);// GameboyRampTex;
                kawahara.GameboyFactor = EditorGUILayout.Vector4Field("Gameboy Factor", kawahara.GameboyFactor);

                ///DEPTH
                kawahara.worldPosScaler = EditorGUILayout.Vector4Field("world Position Scaler", kawahara.worldPosScaler);

                ////8.KNITTING
                EditorGUILayout.ObjectField(KnitwearMap);//public Texture2D KnitwearMap;
                kawahara.KnitwearDivision = EditorGUILayout.FloatField("Knitwear Division", kawahara.KnitwearDivision);
                kawahara.KnitwearAspect = EditorGUILayout.FloatField("Knitwear Aspect", kawahara.KnitwearAspect);
                kawahara.KnitwearShear = EditorGUILayout.FloatField("Knitwear Shear", kawahara.KnitwearShear);
                kawahara.KnitwearDistortionStrength = EditorGUILayout.FloatField("Knitwear Distortio nStrength", kawahara.KnitwearDistortionStrength);
                kawahara.KnitFactor = EditorGUILayout.Vector4Field("Knit Factor", kawahara.KnitFactor);


            }

            // DrawDefaultInspector();

            //v1.2
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                // This code will unsave the current scene if there's any change in the editor GUI.
                // Hence user would forcefully need to save the scene before changing scene
                EditorUtility.SetDirty(kawahara); //EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}