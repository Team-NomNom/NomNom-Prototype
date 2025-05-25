using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthNormalsByLayerLUMINAFeature : ScriptableRendererFeature
{
    class DepthNormalsByLayerLUMINAPass : ScriptableRenderPass
    {
        private RTHandle destination { get; set; }// RenderTargetHandle destination { get; set; }//v0.1

        private Material depthNormalsMaterial = null;
        private FilteringSettings m_FilteringSettings;
        ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");

        //v0.1
        string outTextureName;
        RenderTexture renderTexture;
        bool exportToName;

        public DepthNormalsByLayerLUMINAPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material, string outTextureNameA, RenderTexture renderTextureA, bool exportToNameA)//v0.1
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            this.depthNormalsMaterial = material;

            //v0.1
            this.outTextureName = outTextureNameA;
            this.renderTexture = renderTextureA;
            this.exportToName = exportToNameA;
        }

        public void Setup(RTHandle destination)// RenderTargetHandle destination) //v0.1
        {
            this.destination = destination;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            descriptor.depthBufferBits = 32;
            descriptor.colorFormat = RenderTextureFormat.ARGBFloat;// ARGB32;

            //cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Point);
            cmd.GetTemporaryRT(Shader.PropertyToID(destination.name), descriptor, FilterMode.Trilinear);//v0.1
            ConfigureTarget(destination);//.Identifier()); //v0.1
            ConfigureClear(ClearFlag.All, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("DepthNormals Prepass");

           // using (new ProfilingSample(cmd, "DepthNormals Prepass"))
           // {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;

            //v0.1

            //if (XRGraphics.enabled)
            //{//  if (cameraData.isStereoEnabled)
            //    context.StartMultiEye(camera);
            //}


                drawSettings.overrideMaterial = depthNormalsMaterial;


                context.DrawRenderers(renderingData.cullResults, ref drawSettings,
                    ref m_FilteringSettings);

                if (exportToName)
                {
                    cmd.SetGlobalTexture(outTextureName, Shader.PropertyToID(destination.name));// destination.id); //v0.1// "_CameraDepthNormalsTextureTOP", destination.id); //v0.1
            }
                //v0.1
                //cmd.Blit(destination.id, sett);
                if (renderTexture != null)
                {
                    cmd.Blit(Shader.PropertyToID(destination.name), renderTexture);// destination.id); //v0.1  //destination.id, renderTexture);//v0.1
            }
           // }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            //v0.1
            //if (destination != RenderTargetHandle.CameraTarget)
            //{
            //    cmd.ReleaseTemporaryRT(destination.id);
            //    destination = RenderTargetHandle.CameraTarget;
            //}
        }
    }
       
 [System.Serializable]
    public class DepthNormalsSettings
    {
        // public Material outlineMaterial = null;
        public LayerMask layerMask;
        // public RenderTexture output;
        public string outTextureName = "_CameraDepthNormalsTextureTOP";//v0.1
        public bool exportToName = true;
        public RenderTexture renderTexture;
    }

    public DepthNormalsSettings settings = new DepthNormalsSettings();
    DepthNormalsByLayerLUMINAPass depthNormalsPass;
    RTHandle depthNormalsTexture;//RenderTargetHandle depthNormalsTexture;//v0.1
    Material depthNormalsMaterial;

    public override void Create()
    {
        depthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
       // depthNormalsPass = new DepthNormalsByLayerLUMINAPass(RenderQueueRange.opaque, settings.layerMask, depthNormalsMaterial, settings.outTextureName, settings.renderTexture, settings.exportToName);
        depthNormalsPass = new DepthNormalsByLayerLUMINAPass(RenderQueueRange.opaque, settings.layerMask, depthNormalsMaterial, settings.outTextureName, settings.renderTexture, settings.exportToName);
        depthNormalsPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;

        //v0.1
        //depthNormalsTexture = RTHandles.Alloc("_CameraDepthNormalsTexture", name: "_CameraDepthNormalsTexture");
        depthNormalsTexture = RTHandles.Alloc(settings.outTextureName, name: settings.outTextureName);
        // depthNormalsTexture.Init(settings.outTextureName);// "_CameraDepthNormalsTextureTOP");//v0.1
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        depthNormalsPass.Setup(depthNormalsTexture);
        renderer.EnqueuePass(depthNormalsPass);
    }
}


