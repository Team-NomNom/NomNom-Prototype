using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Artngame.GIBLI
{
    [CustomEditor(typeof(effectsControllerGIBLI))]
    public class effectsControllerGIBLIEditor : Editor
    {
        //v1.2
        public float scaleParameterBounds = 1;

        //v1.2
        //SerializedProperty _BaseHeight;

        //void OnEnable()
        //{
        //    // Setup the SerializedProperties
        //    _BaseHeight = this.serializedObject.FindProperty("_BaseHeight");
        //}

        public override void OnInspectorGUI()
        {
            //v1.2
            EditorGUI.BeginChangeCheck();
            //https://answers.unity.com/questions/1330417/editor-scripting-best-way-to-change-variables-of-a.html
            serializedObject.Update();
            // Show the default GUI controls except scaleProperty
            //DrawPropertiesExcluding(serializedObject, scalePropertySerializedName);

            effectsControllerGIBLI tree = (effectsControllerGIBLI)target;

            //v1.2
            Undo.RecordObject(tree,"GIBLI param change");
            //ScriptableObject.cha

            EditorGUILayout.HelpBox("--------- IMAGE EFFECTS - VOLUME -------", MessageType.None);
            EditorGUILayout.HelpBox("Add Volume that enables Volume FX - Drawing - Cloud Shadows - Outline - Glitch - Halftone - World Position Effects", MessageType.Info);
            if (GUILayout.Button("Add GIBLI All Effects Volume"))
            {
                tree.addGIBLIURPAllEffectsVolume();
            }
            EditorGUILayout.HelpBox("Add Renderer Future to enable Volume FX - Drawing - Cloud Shadows - Outline - Glitch - Halftone - World Position Effects", MessageType.Info);
            if (GUILayout.Button("Setup Custom Post Process Feature"))
            {
                tree.setupCustomPostProcessFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }

            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupCustomPostProcessFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Volume FX Feature"))
            {
                tree.setupCustomPostProcessFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            //EditorGUILayout.BeginHorizontal();
            //if (GUILayout.Button("Enable Custom Post Process Feature"))
            //{
            //    tree.setupCustomPostProcessFeatureFunc(true);
            //}
            //if (GUILayout.Button("Disable Custom Post Process Feature"))
            //{
            //    tree.setupCustomPostProcessFeatureFunc(false);
            //}
            //EditorGUILayout.EndHorizontal();                                 

            //Various image effects
            EditorGUILayout.HelpBox("Add Renderer Future to enable Special Image Effects", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Depth Line Feature"))
            {
                tree.setupDepthLineFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupDepthLineFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupDepthLineFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Diffusion Feature"))
            {
                tree.setupDiffusionFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupDiffusionFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupDiffusionFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Flare Feature"))
            {
                tree.setupFlareFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupFlareFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupFlareFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Gradient Fog Feature"))
            {
                tree.setupGradientFogFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupGradientFogFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupGradientFogFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Light Shafts Feature"))
            {
                tree.setupLightShaftsFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupLightShaftsFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupLightShaftsFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();
            //if (GUILayout.Button("Setup Pixelate Feature"))
            //{
            //    tree.setupPixelateFeatureFunc(false);
            //}
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup RayMarch Feature"))
            {
                tree.setupRayMarchFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupRayMarchFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupRayMarchFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Ripple Feature"))
            {
                tree.setupRippleFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupRippleFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupRippleFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Star Glow Feature"))
            {
                tree.setupStarGlowFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupStarGlowFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupStarGlowFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();


            /////////////////// IMAGE EFFECTS - NOT IN VOLUME
            EditorGUILayout.HelpBox("--------- IMAGE EFFECTS - OUTLINES -------", MessageType.None);
            //Pencil dots effect support
            EditorGUILayout.HelpBox("Add Renderer Future to enable variance in Pencil Outline effects", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Depth Normals Feature"))
            {
                tree.setupDepthNormalsFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupDepthNormalsFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupDepthNormalsFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.grey * 2;
            if (GUILayout.Button("Setup Outline Feature"))
            {
                tree.setupOutlineFeatureFunc(false);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupOutlineFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupOutlineFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.grey * 2;

           // Undo.RecordObject(tree, "Image FX Change");
            tree.Outline_Depth_Sensitivity = EditorGUILayout.Slider("Depth Sensitivity", tree.Outline_Depth_Sensitivity, 0, 1);//  GUILayout.Label("Depth Sensitivity"));
            tree.Outline_Normals_Sensitivity = EditorGUILayout.Slider("Normals Sensitivity", tree.Outline_Normals_Sensitivity, 0, 10);
            tree.Outline_Color_Sensitivity = EditorGUILayout.Slider("Color Sensitivity", tree.Outline_Color_Sensitivity, 0, 10);
            tree.OutlineThickness = EditorGUILayout.Slider("Outline Thickness", tree.OutlineThickness, 0, 10);
            tree.Outline_Color = EditorGUILayout.ColorField("Outline Color", tree.Outline_Color);
            if (tree.OutlineShader != null)
            {
                tree.OutlineShader.SetFloat("Outline_Depth_Sensitivity", tree.Outline_Depth_Sensitivity);
                tree.OutlineShader.SetFloat("Outline_Normals_Sensitivity", tree.Outline_Normals_Sensitivity);
                tree.OutlineShader.SetFloat("Outline_Color_Sensitivity", tree.Outline_Color_Sensitivity);
                tree.OutlineShader.SetColor("Outline_Color", tree.Outline_Color);
                tree.OutlineShader.SetFloat("OutlineThickness", tree.OutlineThickness);
            }




            EditorGUILayout.HelpBox("--------- IMAGE EFFECTS - CAVITY -------", MessageType.None);
            //CAVITY
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Cavity Feature"))
            {
                tree.setupCavityFeatureFunc(false);

                if (Camera.main != null)
                {
                    connectVisualFXGIBLI cavity = Camera.main.gameObject.GetComponent<connectVisualFXGIBLI>();
                    if (cavity == null)
                    {
                        cavity = Camera.main.gameObject.AddComponent<connectVisualFXGIBLI>();
                    }
                    tree.cavityController = cavity;
                    GameObject cavitySample = Instantiate(tree.cavitySOURCE);
                }

                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            if (!tree.setupCavityFeature)
            {
                GUI.backgroundColor = Color.grey * 1;
            }
            if (GUILayout.Button("Disable Feature", GUILayout.Width(115)))
            {
                tree.setupCavityFeatureFunc(true);
                SceneView.RepaintAll();
                EditorUtility.SetDirty(tree);
            }
            GUI.backgroundColor = Color.grey * 2;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Cavity Type (Both,Curve,Cavity,Disable)", MessageType.None);
            tree.cavityType = EditorGUILayout.IntSlider(tree.cavityType, 0, 3);
            tree.debugCavity = EditorGUILayout.Toggle("Greyscale Cavity", tree.debugCavity);
            tree.curvatureScale = EditorGUILayout.Slider("Curvature Scale", tree.curvatureScale, 0, 5);
            tree.curvatureRidge = EditorGUILayout.Slider("Curvature Ridge", tree.curvatureRidge, 0, 2);
            tree.curvatureValley = EditorGUILayout.Slider("Curvature Valley", tree.curvatureValley, 0, 2);
            tree.cavityDistance = EditorGUILayout.Slider("Curvature Scale", tree.cavityDistance, 0, 1);
            tree.cavityAttenuation = EditorGUILayout.Slider("Curvature Ridge", tree.cavityAttenuation, 0, 1);
            tree.cavityRidge = EditorGUILayout.Slider("Curvature Valley", tree.cavityRidge, 0, 3);
            tree.cavityValley = EditorGUILayout.Slider("Curvature Valley", tree.cavityValley, 0, 3);
            tree.cavitySamples = EditorGUILayout.IntSlider("Curvature Valley", tree.cavitySamples, 1, 12);

            /////////////////// IMAGE EFFECTS - RIPPLE
            EditorGUILayout.HelpBox("--------- IMAGE EFFECTS - RIPPLE -------", MessageType.None);
            //Pencil dots effect support
            EditorGUILayout.HelpBox("Control Ripple effects", MessageType.Info);
            tree.RippleStrength = EditorGUILayout.Slider("RippleStrength", tree.RippleStrength, 0, 0.1f);//  GUILayout.Label("Depth Sensitivity"));
            tree.RippleScale = EditorGUILayout.Slider("RippleScale", tree.RippleScale, 0, 50);
            tree.RippleFalloff = EditorGUILayout.Slider("RippleFalloff", tree.RippleFalloff, -1000, 1000);
            tree.RippleNoiseTiling = EditorGUILayout.Slider("RippleNoiseTiling", tree.RippleNoiseTiling, -1.5f, 1.5f);
            tree.RippleNoiseStep = EditorGUILayout.Slider("RippleNoiseStep", tree.RippleNoiseStep, 0, 1);
            if (tree.RippleShader != null)
            {
                tree.RippleShader.SetFloat("_RippleStrength", tree.RippleStrength);
                tree.RippleShader.SetFloat("_RippleScale", tree.RippleScale);
                tree.RippleShader.SetFloat("_RippleFalloff", tree.RippleFalloff);
                tree.RippleShader.SetFloat("_NoiseTiling", tree.RippleNoiseTiling);
                tree.RippleShader.SetFloat("_NoiseStep", tree.RippleNoiseStep);
            }


            //Refracted transparency support
            //EditorGUILayout.HelpBox("Add Renderer Future to enable correct Refracted transparency", MessageType.Info);
            //if (GUILayout.Button("Setup Grab Screen Feature"))
            //{
            //    tree.setupGrabScreenFeatureFunc(false);
            //}

            //v0.2
            //SKYBOX
            if (GUILayout.Button("Configure Skybox"))
            {
                //assign material
                RenderSettings.skybox = tree.skyboxMaterial;
                //open properties
                if (tree.skyboxSettings)
                {
                    tree.skyboxSettings = false;
                }
                else
                {
                    tree.skyboxSettings = true;
                }
            }
            if (tree.skyboxSettings)
            {
                tree._ControlVector = EditorGUILayout.Vector3Field("Sky Control Vector", tree._ControlVector, GUILayout.Width(400));
                tree.posterizeFactor = EditorGUILayout.Slider("posterize Factor", tree.posterizeFactor, -10, 10);
                tree.posterizeColorNumber = EditorGUILayout.Slider("posterize Colors Number", tree.posterizeColorNumber, 1, 50);
                tree.darkenSun = EditorGUILayout.Vector3Field("Sky Darken Sun", tree.darkenSun, GUILayout.Width(400));
                tree.SkyExposure = EditorGUILayout.Slider("posterize Colors Number", tree.SkyExposure, 0, 10);

                tree.SunSize = EditorGUILayout.Slider("Sun Size", tree.SunSize, 0, 1);
                tree.SunSizeConvergence = EditorGUILayout.Slider("Sun Size Convergence", tree.SunSizeConvergence, 1, 10);
                tree.AtmosphereThickness = EditorGUILayout.Slider("Atmosphere Thickness", tree.AtmosphereThickness, 0, 10);

                tree.SkyIntensity = EditorGUILayout.Slider("Sky Intensity", tree.SkyIntensity, -10, 10);
                tree.SkyIntensityB = EditorGUILayout.Slider("Sky Intensity B", tree.SkyIntensityB, -10, 10);
                tree.SkyExponent = EditorGUILayout.Slider("Sky Exponent", tree.SkyExponent, -10, 10);
                tree.SkyExponent1 = EditorGUILayout.Slider("Sky Exponent 1", tree.SkyExponent1, -10, 10);
                tree.SkyExponent2 = EditorGUILayout.Slider("Sky Exponent 2", tree.SkyExponent2, -10, 10);

                tree.GroundColor = EditorGUILayout.ColorField("Ground Color", tree.GroundColor);
                tree.SkyTint = EditorGUILayout.ColorField("Sky Tint", tree.SkyTint);
                tree.SkyColor1 = EditorGUILayout.ColorField("Sky Color 1", tree.SkyColor1);
                tree.SkyColor2 = EditorGUILayout.ColorField("Sky Color 2", tree.SkyColor2);
                tree.SkyColor3 = EditorGUILayout.ColorField("Sky Color 3", tree.SkyColor3);
            }

            EditorGUILayout.HelpBox("--------- RIVER -------", MessageType.None);
            //RIVER
            EditorGUILayout.HelpBox("Add Toon River with Spline Control", MessageType.Info);
            if (GUILayout.Button("Setup River System"))
            {
                tree.setupRiver();
            }

            EditorGUILayout.HelpBox("--------- CLOUDS -------", MessageType.None);
            //RIVER
            EditorGUILayout.HelpBox("Add Toon Clouds", MessageType.Info);
            if (GUILayout.Button("Setup Clouds"))
            {
                tree.setupClouds();
            }
            EditorGUILayout.HelpBox("Add Cloud Maker with meta blobs, make sure to disable the blob making script MCBlob for performance after finish cloud shaping", MessageType.Info);
            if (GUILayout.Button("Setup Cloud Maker"))
            {
                tree.setupCloudMaker();
            }



            EditorGUILayout.HelpBox("--------- TREES -------", MessageType.None);
            //RIVER
            EditorGUILayout.HelpBox("Add Toon Trees", MessageType.Info);
            if (GUILayout.Button("Setup Toon Tree Maker"))
            {
                tree.setupTreeMaker();
            }
            EditorGUILayout.HelpBox("Add Tree Maker with meta blobs, make sure to disable the blob making script MCBlob for performance after finish cloud shaping", MessageType.Info);
            if (GUILayout.Button("Setup Toon Blob Tree Maker"))
            {
                tree.setupBlobTreeMaker();
            }



            EditorGUILayout.HelpBox("--------- GRASS -------", MessageType.None);
            //GRASS
            EditorGUILayout.HelpBox("Add Renderer Future to enable Grass Bending by Interactors", MessageType.Info);
            if (GUILayout.Button("Setup Grass Bending Feature"))
            {
                tree.setupGrassBendingFunc(false);
            }
            if (GUILayout.Button("Setup Instanced Grass"))
            {
                tree.setupInstancedGrass();
            }
            //v0.2
            //GRASS CONTROLS
            if (GUILayout.Button("Configure Grass"))
            {                
                //open properties
                if (tree.grassSettings)
                {
                    tree.grassSettings = false;
                }
                else
                {
                    tree.grassSettings = true;
                }
            }
            if (tree.grassSettings)
            {
                //v1.2
                tree.scaleParameterBounds = EditorGUILayout.Slider("scale Parameter Bounds", tree.scaleParameterBounds, 1, 10);

                float minA = -20 * scaleParameterBounds; float maxA = 20 * scaleParameterBounds; //v1.2

                tree._BaseColor = EditorGUILayout.ColorField("Base Grass Color", tree._BaseColor);
                if (tree._BaseColorTexture != null)
                {
                    tree._BaseColorTexture = (Texture2D)EditorGUILayout.ObjectField("Grass Texture", tree._BaseColorTexture, typeof(Texture2D), false);
                }
                tree._GroundColor = EditorGUILayout.ColorField("Ground Grass Color", tree._GroundColor);

                tree._GrassWidth = EditorGUILayout.Slider("_GrassWidth", tree._GrassWidth, minA, maxA);
                tree._GrassHeight = EditorGUILayout.Slider("_GrassHeight", tree._GrassHeight, minA, maxA);
                tree._WindAIntensity = EditorGUILayout.Slider("_WindAIntensity", tree._WindAIntensity, minA, maxA);
                tree._WindAFrequency = EditorGUILayout.Slider("_WindAFrequency", tree._WindAFrequency, minA, maxA);
                tree._WindBIntensity = EditorGUILayout.Slider("_WindBIntensity", tree._WindBIntensity, minA, maxA);
                tree._WindBFrequency = EditorGUILayout.Slider("_WindBFrequency", tree._WindBFrequency, minA, maxA);
                tree._WindCIntensity = EditorGUILayout.Slider("_WindCIntensity", tree._WindCIntensity, minA, maxA);
                tree._WindCFrequency = EditorGUILayout.Slider("_WindCFrequency", tree._WindCFrequency, minA, maxA);                

                tree._WindATiling = EditorGUILayout.Vector3Field("_WindATiling", tree._WindATiling, GUILayout.Width(400));
                tree._WindAWrap = EditorGUILayout.Vector3Field("_WindAWrap", tree._WindAWrap, GUILayout.Width(400));                              

                tree._WindBTiling = EditorGUILayout.Vector3Field("_WindBTiling", tree._WindBTiling, GUILayout.Width(400));
                tree._WindBWrap = EditorGUILayout.Vector3Field("_WindBWrap", tree._WindBWrap, GUILayout.Width(400));                

                tree._WindCTiling = EditorGUILayout.Vector3Field("_WindCTiling", tree._WindCTiling, GUILayout.Width(400));
                tree._WindCWrap = EditorGUILayout.Vector3Field("_WindCWrap", tree._WindCWrap, GUILayout.Width(400));

                tree._RandomNormal = EditorGUILayout.Slider("_RandomNormal",  tree._RandomNormal, minA, maxA);
                tree._grassHeight = EditorGUILayout.Slider("_grassHeight",   tree._grassHeight, minA, maxA);
                tree._respectHeight = EditorGUILayout.Slider("_respectHeight", tree._respectHeight, minA, maxA);

                tree._BaseHeight = EditorGUILayout.Slider("_BaseHeight",    tree._BaseHeight, minA*20, maxA*20); //v1.2

                //float commonScale = this.scaleProperty.vector3Value.x;
                //EditorGUILayout.LabelField("_BaseHeight");
                //EditorGUILayout.PropertyField(_BaseHeight);
                //_BaseHeight.floatValue = EditorGUILayout.Slider(_BaseHeight.floatValue, minA * 100, maxA * 100);
                //tree._BaseHeight = _BaseHeight.floatValue;


                tree._NoiseAmplitude = EditorGUILayout.Slider("_NoiseAmplitude",tree._NoiseAmplitude, minA, maxA);
                tree._NoiseFreqX = EditorGUILayout.Slider("_NoiseFreqX",    tree._NoiseFreqX, minA, maxA);
                tree._NoiseFreqZ = EditorGUILayout.Slider("_NoiseFreqZ",    tree._NoiseFreqZ, minA, maxA);
                tree._NoiseOffsetY = EditorGUILayout.Slider("_NoiseOffsetY",  tree._NoiseOffsetY, minA, maxA);
                tree._textureFeed = EditorGUILayout.Slider("_textureFeed",   tree._textureFeed, minA, maxA);                
            }




            EditorGUILayout.HelpBox("--------- METABALLS -------", MessageType.None);
            //2D METABALLS
            EditorGUILayout.HelpBox("Add Renderer Future to enable 2D Mataballs", MessageType.Info);
            if (GUILayout.Button("Setup Metaball 2D Feature"))
            {
                tree.setupMatballFeatureFunc(false);
            }

            EditorGUILayout.HelpBox("--------- REFLECTIONS -------", MessageType.None);
            //Screen Space Reflection
            EditorGUILayout.HelpBox("Add Renderer Future to enable Screen Space Reflections", MessageType.Info);
            if (GUILayout.Button("Setup Screen Space Reflection Feature"))
            {
                tree.setupScreenSReflectionFeatureFunc(false);
            }

            

            DrawDefaultInspector();

            //v1.2
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                // This code will unsave the current scene if there's any change in the editor GUI.
                // Hence user would forcefully need to save the scene before changing scene
                EditorUtility.SetDirty(tree); //EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            //EditorUtility.SetDirty(tree);
            //v1.2
            //EditorUtility.SetDirty(tree);
        }
    }
}