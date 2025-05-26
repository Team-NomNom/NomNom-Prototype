using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Artngame.GIBLI
{
    public class WorldSpaceRippleFeature : UnityEngine.Rendering.Universal.ScriptableRendererFeature
    {
        class WorldSpaceRipplePass : UnityEngine.Rendering.Universal.ScriptableRenderPass
        {
            private RenderTargetIdentifier source { get; set; }
            
            public Material rippleMaterial = null;
            RTHandle temporaryColorTexture; //v0.1

#if UNITY_2022_1_OR_NEWER
            private RTHandle destination { get; set; } //v0.1
            public void Setup(RenderTargetIdentifier source, RTHandle destination) //v0.1
            {
                this.source = source;
                this.destination = destination;
            }
#else
            private RenderTargetHandle destination { get; set; } //v0.1
            public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination) //v0.1
            {
                this.source = source;
                this.destination = destination;
            }
#endif
            public WorldSpaceRipplePass(Material rippleMaterial)
            {
                this.rippleMaterial = rippleMaterial;
            }



            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {

            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("WorldSpaceRipple Pass");

                RenderTextureDescriptor opaqueDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDescriptor.depthBufferBits = 0;

#if UNITY_2022_1_OR_NEWER
                if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                {
#else
                if (destination == UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                {
#endif
                    cmd.GetTemporaryRT( Shader.PropertyToID(temporaryColorTexture.name), opaqueDescriptor, FilterMode.Point); //v0.1
                    cmd.Blit( source, temporaryColorTexture, rippleMaterial, 0); //v0.1
                    cmd.Blit( temporaryColorTexture, source); //v0.1

                }
#if UNITY_2022_1_OR_NEWER
                else cmd.Blit( source, destination, rippleMaterial, 0); //v0.1
#else
                else cmd.Blit(source, destination.id, rippleMaterial, 0); //v0.1
#endif



                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {

                //if (destination == UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                  //  cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
            }
        }

        [System.Serializable]
        public class WorldSpaceRippleSettings
        {
            public Material rippleMaterial = null;
        }

        public WorldSpaceRippleSettings settings = new WorldSpaceRippleSettings();
        WorldSpaceRipplePass ripplePass;
        RTHandle rippleTexture; //v0.1

        public override void Create()
        {
            ripplePass = new WorldSpaceRipplePass(settings.rippleMaterial);
            ripplePass.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingPostProcessing;
            
            //rippleTexture.Init("_WorldSpaceRippleTexture"); //v0.1
            rippleTexture = RTHandles.Alloc("_WorldSpaceRippleTexture", name: "_WorldSpaceRippleTexture"); //v0.1
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            if (settings.rippleMaterial == null)
            {
                Debug.LogWarningFormat("Missing Outline Material");
                return;
            }
            //ripplePass.Setup(renderer.cameraColorTarget, UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget); //v0.1
#if UNITY_2022_1_OR_NEWER
            ripplePass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraColorTargetHandle); //v0.1
#else
            ripplePass.Setup(renderer.cameraColorTarget, UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget); //v0.1
#endif
            renderer.EnqueuePass(ripplePass);
        }
    }
}

