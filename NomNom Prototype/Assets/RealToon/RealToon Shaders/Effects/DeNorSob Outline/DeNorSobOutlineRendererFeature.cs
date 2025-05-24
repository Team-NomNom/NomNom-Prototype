using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class DeNorSobOutlineRendererFeature : ScriptableRendererFeature
{
    #region FEATURE_FIELDS

    [SerializeField]
    [HideInInspector]
    private Material m_Material;

    private DeNorSobOutlineRenderPass m_FullScreenPass;

    public RenderPassEvent InjectionPoint = RenderPassEvent.BeforeRenderingTransparents;

    #endregion

    #region FEATURE_METHODS

    public override void Create()
    {

        if (m_Material == null)
            m_Material = new Material(Shader.Find("Hidden/URP/RealToon/Effects/DeNorSobOutline"));

        if (m_Material)
            m_FullScreenPass = new DeNorSobOutlineRenderPass(name, m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (m_Material == null || m_FullScreenPass == null)
            return;

        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        DeNorSobOutlineVolumeComponent myVolume = VolumeManager.instance.stack?.GetComponent<DeNorSobOutlineVolumeComponent>();
        if (myVolume == null || !myVolume.IsActive())
            return;

        //
        m_FullScreenPass.renderPassEvent = InjectionPoint;

        //
        m_FullScreenPass.ConfigureInput(ScriptableRenderPassInput.Normal);

        renderer.EnqueuePass(m_FullScreenPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_FullScreenPass.Dispose();
    }

    #endregion

    private class DeNorSobOutlineRenderPass : ScriptableRenderPass
    {
        #region PASS_FIELDS

        // The material used to render the post-processing effect
        private Material m_Material;

        // The handle to the temporary color copy texture (only used in the non-render graph path)
        private RTHandle m_CopiedColor;

        // The property block used to set additional properties for the material
        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();

        // This constant is meant to showcase how to create a copy color pass that is needed for most post-processing effects
        private static readonly bool kCopyActiveColor = true;

        // This constant is meant to showcase how you can add dept-stencil support to your main pass
        private static readonly bool kBindDepthStencilAttachment = false;

        // Creating some shader properties in advance as this is slightly more efficient than referencing them by string
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");

        #endregion

        public DeNorSobOutlineRenderPass(string passName, Material material)
        {
            profilingSampler = new ProfilingSampler(passName);
            m_Material = material;

            requiresIntermediateTexture = kCopyActiveColor;
        }

        #region PASS_SHARED_RENDERING_CODE

        private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
        {
            Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
        }

        private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material)
        {
            s_SharedPropertyBlock.Clear();
            if(sourceTexture != null)
                s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId, sourceTexture);

            s_SharedPropertyBlock.SetVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));

            //
            DeNorSobOutlineVolumeComponent myVolume = VolumeManager.instance.stack?.GetComponent<DeNorSobOutlineVolumeComponent>();
            if (myVolume != null)

                s_SharedPropertyBlock.SetFloat("_OutlineWidth", myVolume.OutlineWidth.value);

                s_SharedPropertyBlock.SetFloat("_DepthThreshold", myVolume.DepthThreshold.value);

                s_SharedPropertyBlock.SetFloat("_NormalThreshold", myVolume.NormalThreshold.value);
                s_SharedPropertyBlock.SetFloat("_NormalMin", myVolume.NormalMin.value);
                s_SharedPropertyBlock.SetFloat("_NormalMax", myVolume.NormalMax.value);

                s_SharedPropertyBlock.SetFloat("_SobOutSel", myVolume.SobelOutline.value ? 1 : 0);
                s_SharedPropertyBlock.SetFloat("_SobelOutlineThreshold", myVolume.SobelOutlineThreshold.value);
                s_SharedPropertyBlock.SetFloat("_WhiThres", 1.0f - myVolume.WhiteThreshold.value);
                s_SharedPropertyBlock.SetFloat("_BlaThres", myVolume.BlackThreshold.value);

                s_SharedPropertyBlock.SetColor("_OutlineColor", myVolume.OutlineColor.value);
                s_SharedPropertyBlock.SetFloat("_OutlineColorIntensity", myVolume.ColorIntensity.value);
                s_SharedPropertyBlock.SetFloat("_ColOutMiSel", myVolume.MixFullScreenColor.value ? 1 : 0);

                s_SharedPropertyBlock.SetFloat("_OutOnSel", myVolume.ShowOutlineOnly.value ? 1 : 0);

                s_SharedPropertyBlock.SetFloat("_MixDeNorSob", myVolume.MixDephNormalAndSobelOutline.value ? 1 : 0);

                switch (myVolume.SobelOutline.value)
                {
                    case true:
                        material.EnableKeyword("RENDER_OUTLINE_ALL");
                        break;
                    default:
                        material.DisableKeyword("RENDER_OUTLINE_ALL");
                        break;
                }

                switch (myVolume.MixDephNormalAndSobelOutline.value)
                {
                    case true:
                        material.EnableKeyword("MIX_DENOR_SOB");
                        break;
                    default:
                        material.DisableKeyword("MIX_DENOR_SOB");
                        break;
                }

            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        private static RenderTextureDescriptor GetCopyPassTextureDescriptor(RenderTextureDescriptor desc)
        {
            desc.msaaSamples = 1;

            desc.depthBufferBits = (int)DepthBits.None;

            return desc;
        }

        #endregion

        #region PASS_NON_RENDER_GRAPH_PATH

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

            ResetTarget();

            if (kCopyActiveColor)
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopiedColor, GetCopyPassTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor), name: "_DeNorSobOutlineCopyColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                RasterCommandBuffer rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);
                if (kCopyActiveColor)
                {
                    CoreUtils.SetRenderTarget(cmd, m_CopiedColor);
                    ExecuteCopyColorPass(rasterCmd, cameraData.renderer.cameraColorTargetHandle);
                }

                if(kBindDepthStencilAttachment)
                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle, cameraData.renderer.cameraDepthTargetHandle);
                else
                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle);

                ExecuteMainPass(rasterCmd, kCopyActiveColor ? m_CopiedColor : null, m_Material);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            m_CopiedColor?.Release();
        }

        #endregion

        #region PASS_RENDER_GRAPH_PATH

        private class CopyPassData
        {
            public TextureHandle inputTexture;
        }

        private class MainPassData
        {
            public Material material;
            public TextureHandle inputTexture;
        }

        private static void ExecuteCopyColorPass(CopyPassData data, RasterGraphContext context)
        {
            ExecuteCopyColorPass(context.cmd, data.inputTexture);
        }

        private static void ExecuteMainPass(MainPassData data, RasterGraphContext context)
        {
            ExecuteMainPass(context.cmd, data.inputTexture.IsValid() ? data.inputTexture : null, data.material);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            UniversalRenderer renderer = (UniversalRenderer) cameraData.renderer;
            var colorCopyDescriptor = GetCopyPassTextureDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            if (kCopyActiveColor)
            {
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_DeNorSobOutlineColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("DeNorSob Outline", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourcesData.activeColorTexture;
                    builder.UseTexture(resourcesData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyColorPass(data, context));
                }
            }

            using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("DeNorSob Outline", out var passData, profilingSampler))
            {
                passData.material = m_Material;

                if (kCopyActiveColor)
                {
                    passData.inputTexture = copiedColor;
                    builder.UseTexture(copiedColor, AccessFlags.Read);
                }

                builder.SetRenderAttachment(resourcesData.activeColorTexture, 0, AccessFlags.Write);

                if(kBindDepthStencilAttachment)
                    builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture, AccessFlags.Write);

                builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(data, context));
            }
        }

        #endregion
    }
}
