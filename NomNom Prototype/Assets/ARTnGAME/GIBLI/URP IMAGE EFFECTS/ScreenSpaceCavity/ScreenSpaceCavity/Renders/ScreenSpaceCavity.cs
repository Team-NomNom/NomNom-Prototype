using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Artngame.GIBLI.MalyaWka.ScreenSpaceCavity.Renders
{
    [Serializable]
    public class ScreenSpaceCavitySettings
    {
        internal enum CavityType { Both = 0, Curvature = 1, Cavity = 2, Disable = 3 }

        [SerializeField] internal CavityType cavityType = CavityType.Both;
        [SerializeField] internal bool debug = false;
        
        [SerializeField, Delayed] internal float curvatureScale = 1.0f;
        [SerializeField, Delayed] internal float curvatureRidge = 0.25f;
        [SerializeField, Delayed] internal float curvatureValley = 0.25f;
        
        [SerializeField, Delayed] internal float cavityDistance = 0.25f;
        [SerializeField, Delayed] internal float cavityAttenuation = 0.015625f;
        [SerializeField, Delayed] internal float cavityRidge = 1.25f;
        [SerializeField, Delayed] internal float cavityValley = 1.25f;
        [SerializeField, Delayed] internal int cavitySamples = 4;
    }
    
    public class ScreenSpaceCavity : ScriptableRendererFeature
    {
        public ScreenSpaceCavitySettings Settings => m_Settings;

        [SerializeField, HideInInspector] private Shader m_Shader = null;
        [SerializeField] private ScreenSpaceCavitySettings m_Settings = new ScreenSpaceCavitySettings();
        
        private RTHandle m_DepthTexture; //v0.1
        private RTHandle m_NormalsTexture; //v0.1
        
        private Material m_Material;
        private ScreenSpaceCavityPass m_SSCPass = null;

        private const string k_ShaderName = "Hidden/MalyaWka/ScreenSpaceCavity/Cavity";// private Shader m_Shader = null;
        private const string k_DebugKeyword = "_CAVITY_DEBUG";
        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        private const string k_TypeCurvatureKeyword = "_TYPE_CURVATURE";
        private const string k_TypeCavityKeyword = "_TYPE_CAVITY";

        public override void Create()
        {
            if (m_SSCPass == null)
            {
                m_SSCPass = new ScreenSpaceCavityPass();
            }

            GetMaterial();
            m_SSCPass.profilerTag = name;
            m_SSCPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterial())
            {
                Debug.LogError($"{GetType().Name}.AddRenderPasses(): Missing material. {m_SSCPass.profilerTag} " +
                               $"render pass will not be added. Check for missing reference in the renderer resources.");
                return;
            }

            bool shouldAdd = m_SSCPass.Setup(m_Settings);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_SSCPass);
            }
        }

#if UNITY_2020_2_OR_NEWER
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }
#endif

        private bool GetMaterial()
        {
            if (m_Material != null)
            {
                return true;
            }

            if (m_Shader == null)
            {
                m_Shader = Shader.Find(k_ShaderName);
                if (m_Shader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            m_SSCPass.material = m_Material;
            return m_Material != null;
        }

        private class ScreenSpaceCavityPass : ScriptableRenderPass
        {
            private bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
            
            internal string profilerTag;
            internal Material material;

            private ScreenSpaceCavitySettings m_CurrentSettings;
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSC");
            private RenderTextureDescriptor m_Descriptor;
            
            private static readonly int CurvatureParamsID = Shader.PropertyToID("_CurvatureParams");
            private static readonly int CavityParamsID = Shader.PropertyToID("_CavityParams");
            private static int CavityTexture = Shader.PropertyToID("_CavityTexture");//v0.1
            private static readonly int SourceSize = Shader.PropertyToID("_SourceSize");
            private static readonly string ScreenSpaceCavity = "_SCREEN_SPACE_CAVITY";

            private RenderTargetIdentifier m_SSCTextureTarget =
                new RenderTargetIdentifier(CavityTexture, 0, CubemapFace.Unknown, -1);
            private const string k_SSCTextureName = "_ScreenSpaceCavityTexture";
            
            internal ScreenSpaceCavityPass()
            {
                m_CurrentSettings = new ScreenSpaceCavitySettings();
            }
            
            internal bool Setup(ScreenSpaceCavitySettings featureSettings)
            {
                m_CurrentSettings = featureSettings;

                //v0.2
                if(Camera.main != null)
                {
                    connectVisualFXGIBLI visualFX = Camera.main.gameObject.GetComponent<connectVisualFXGIBLI>();
                    if(visualFX != null && visualFX.cavitySettings != null)
                    {
                        m_CurrentSettings = visualFX.cavitySettings;
                    }
                }

#if UNITY_2020_2_OR_NEWER
                //ConfigureInput(ScriptableRenderPassInput.Normal); //v0.2
#else
                GetMaterial();
#endif
                return material != null;
            }
            
            internal static void SetSourceSize(CommandBuffer cmd, RenderTextureDescriptor desc)
            {
                float width = desc.width;
                float height = desc.height;
                if (desc.useDynamicScale)
                {
                    width *= ScalableBufferManager.widthScaleFactor;
                    height *= ScalableBufferManager.heightScaleFactor;
                }
                cmd.SetGlobalVector(SourceSize, new Vector4(width, height, 1.0f / width, 1.0f / height));
            }

#if UNITY_2020_2_OR_NEWER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                Vector4 curvatureParams = new Vector4(
                    m_CurrentSettings.curvatureScale,
                    m_CurrentSettings.cavitySamples,  
                    0.5f / Mathf.Max(Mathf.Sqrt(m_CurrentSettings.curvatureRidge), 1e-4f),
                    0.7f / Mathf.Max(Mathf.Sqrt(m_CurrentSettings.curvatureValley), 1e-4f));
                material.SetVector(CurvatureParamsID, curvatureParams);
                
                Vector4 cavityParams = new Vector4(
                    m_CurrentSettings.cavityDistance, 
                    m_CurrentSettings.cavityAttenuation,
                    m_CurrentSettings.cavityRidge,
                    m_CurrentSettings.cavityValley);
                material.SetVector(CavityParamsID, cavityParams);

                CoreUtils.SetKeyword(material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

                switch (m_CurrentSettings.cavityType)
                {
                    case ScreenSpaceCavitySettings.CavityType.Both:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, true);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Curvature:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, true);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, false);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Cavity:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, false);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Disable:
                        //CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, false);
                        //CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                m_Descriptor = cameraTargetDescriptor;
                m_Descriptor.msaaSamples = 1;
                m_Descriptor.depthBufferBits = 0;
                m_Descriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.RHalf;
                cmd.GetTemporaryRT(CavityTexture, m_Descriptor, FilterMode.Bilinear);

		//v0.1
		 //CavityTexture = RTHandles.Alloc("_CavityTexture", name: "_CavityTexture"); //v0.1
                //ConfigureTarget(CavityTexture);
                //ConfigureClear(ClearFlag.None, Color.white);
            }
#else
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
            	//v0.1
            	ConfigureTarget(CavityTexture);
            	ConfigureClear(ClearFlag.None, Color.white);
            
                Vector4 curvatureParams = new Vector4(
                   m_CurrentSettings.curvatureScale,
                   m_CurrentSettings.cavitySamples,
                   0.5f / Mathf.Max(Mathf.Sqrt(m_CurrentSettings.curvatureRidge), 1e-4f),
                   0.7f / Mathf.Max(Mathf.Sqrt(m_CurrentSettings.curvatureValley), 1e-4f));
                material.SetVector(CurvatureParamsID, curvatureParams);

                Vector4 cavityParams = new Vector4(
                    m_CurrentSettings.cavityDistance,
                    m_CurrentSettings.cavityAttenuation,
                    m_CurrentSettings.cavityRidge,
                    m_CurrentSettings.cavityValley);
                material.SetVector(CavityParamsID, cavityParams);

                //CoreUtils.SetKeyword(material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

                switch (m_CurrentSettings.cavityType)
                {
                    case ScreenSpaceCavitySettings.CavityType.Both:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, true);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Curvature:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, true);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, false);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Cavity:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, false);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Disable:
                        //CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, false);
                        //CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RenderTextureDescriptor cameraTargetDescriptor = cameraTextureDescriptor;// renderingData.cameraData.cameraTargetDescriptor;
                m_Descriptor = cameraTargetDescriptor;
                m_Descriptor.msaaSamples = 1;
                m_Descriptor.depthBufferBits = 0;
                m_Descriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.RHalf;
                cmd.GetTemporaryRT(CavityTexture, m_Descriptor, FilterMode.Bilinear);

                ConfigureTarget(CavityTexture);
                ConfigureClear(ClearFlag.None, Color.white);
            }
            public void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                Vector4 curvatureParams = new Vector4(
                    m_CurrentSettings.curvatureScale,
                    m_CurrentSettings.cavitySamples,
                    0.5f / Mathf.Max(Mathf.Sqrt(m_CurrentSettings.curvatureRidge), 1e-4f),
                    0.7f / Mathf.Max(Mathf.Sqrt(m_CurrentSettings.curvatureValley), 1e-4f));
                material.SetVector(CurvatureParamsID, curvatureParams);

                Vector4 cavityParams = new Vector4(
                    m_CurrentSettings.cavityDistance,
                    m_CurrentSettings.cavityAttenuation,
                    m_CurrentSettings.cavityRidge,
                    m_CurrentSettings.cavityValley);
                material.SetVector(CavityParamsID, cavityParams);

                CoreUtils.SetKeyword(material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

                switch (m_CurrentSettings.cavityType)
                {
                    case ScreenSpaceCavitySettings.CavityType.Both:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, true);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Curvature:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, true);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, false);
                        break;
                    case ScreenSpaceCavitySettings.CavityType.Cavity:
                        CoreUtils.SetKeyword(material, k_TypeCurvatureKeyword, false);
                        CoreUtils.SetKeyword(material, k_TypeCavityKeyword, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                m_Descriptor = cameraTargetDescriptor;
                m_Descriptor.msaaSamples = 1;
                m_Descriptor.depthBufferBits = 0;
                m_Descriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.RHalf;
                cmd.GetTemporaryRT(CavityTexture, m_Descriptor, FilterMode.Bilinear);

                ConfigureTarget(CavityTexture);
                ConfigureClear(ClearFlag.None, Color.white);
            }
#endif

            //v0.1
            private const string k_ShaderName = "Hidden/MalyaWka/ScreenSpaceCavity/Cavity";
            private Shader m_Shader = null;
            private bool GetMaterial()
            {
                if (material != null)
                {
                    return true;
                }

                if (m_Shader == null)
                {
                    m_Shader = Shader.Find(k_ShaderName);
                    if (m_Shader == null)
                    {
                        return false;
                    }
                }

                material = CoreUtils.CreateEngineMaterial(m_Shader);
                //m_SSCPass.material = material;
                return material != null;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if(m_CurrentSettings.cavityType == ScreenSpaceCavitySettings.CavityType.Disable)
                {
                    return;
                }

                //Debug.Log("IN1 _ " + CavityTexture);
                if (material == null)// || m_Descriptor != null)
                {
                    GetMaterial();
                    if (material == null)
                    {
                        Debug.LogError($"{GetType().Name}.Execute(): Missing material. {profilerTag} render pass " +
                                   $"will not execute. Check for missing reference in the renderer resources.");
                        return;
                    }
                }
                //Debug.Log("IN2");
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
                
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    //Debug.Log("IN3");
                    //SETUP
                    //OnCameraSetup(cmd, ref renderingData);


                    CoreUtils.SetKeyword(cmd, ScreenSpaceCavity, true);
                    CoreUtils.SetKeyword(cmd, k_DebugKeyword, m_CurrentSettings.debug);
                    
                    SetSourceSize(cmd, m_Descriptor);
                    
                    cmd.SetRenderTarget(
                        m_SSCTextureTarget,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store,
                        m_SSCTextureTarget,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.DontCare
                    );

                    //Debug.Log("IN4");

                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material);
                    
                    cmd.SetGlobalTexture(k_SSCTextureName, m_SSCTextureTarget);
                }
                
                context.ExecuteCommandBuffer(cmd);
                
                CommandBufferPool.Release(cmd);
                //https://forum.unity.com/threads/scriptablerenderpass-not-changing-render-target.1064636/
                //cmd.Clear();
                //cmd.ClearRenderTarget(CavityTexture);
                //Debug.Log("IN5" + CavityTexture);
            }

#if UNITY_2020_2_OR_NEWER
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }

                CoreUtils.SetKeyword(cmd, ScreenSpaceCavity, false);
                CoreUtils.SetKeyword(cmd, k_DebugKeyword, false);
                cmd.ReleaseTemporaryRT(CavityTexture);
            }
#else
            /// Cleanup any allocated resources that were created during the execution of this render pass.
            private RenderTargetHandle destination { get; set; }
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (destination != RenderTargetHandle.CameraTarget)
                {
                    cmd.ReleaseTemporaryRT(CavityTexture);
                    cmd.ReleaseTemporaryRT(destination.id);
                    destination = RenderTargetHandle.CameraTarget;
                }
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }

                CoreUtils.SetKeyword(cmd, ScreenSpaceCavity, false);
                CoreUtils.SetKeyword(cmd, k_DebugKeyword, false);
                //cmd.ReleaseTemporaryRT(CavityTexture);
                //cmd.Release();
                //CavityTexture = -1;
               // Debug.Log("IN6" + CavityTexture);
            }
#endif

        }
    }
}
