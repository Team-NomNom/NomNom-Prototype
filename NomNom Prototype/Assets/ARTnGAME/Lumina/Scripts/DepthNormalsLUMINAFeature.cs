﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Artngame.LUMINA
{
    public class DepthNormalsLUMINAFeature : ScriptableRendererFeature
    {
        class DepthNormalsPass : ScriptableRenderPass
        {
            private RTHandle destination { get; set; }// RenderTargetHandle destination { get; set; }//v0.1

            private Material depthNormalsMaterial = null;
            private FilteringSettings m_FilteringSettings;
            ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");

            public DepthNormalsPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material)
            {
                m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
                this.depthNormalsMaterial = material;
            }

            public void Setup(RTHandle destination)// RenderTargetHandle destination)
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
                descriptor.colorFormat = RenderTextureFormat.ARGB32;

                cmd.GetTemporaryRT(Shader.PropertyToID(destination.name), descriptor, FilterMode.Point);//   destination.id, descriptor, FilterMode.Point); //v0.1 
                
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

                using (new ProfilingScope())//using (new ProfilingSample(cmd, "DepthNormals Prepass"))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                    var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                    drawSettings.perObjectData = PerObjectData.None;

                    ref CameraData cameraData = ref renderingData.cameraData;
                    Camera camera = cameraData.camera;
                    //if (XRGraphics.enabled)// if (cameraData.isStereoEnabled)
                    //    context.StartMultiEye(camera);


                    drawSettings.overrideMaterial = depthNormalsMaterial;

                    //https://www.ronja-tutorials.com/post/018-postprocessing-normal/
                    camera.depthTextureMode = camera.depthTextureMode | DepthTextureMode.DepthNormals;

                    context.DrawRenderers(renderingData.cullResults, ref drawSettings,
                        ref m_FilteringSettings);

                    cmd.SetGlobalTexture("_CameraDepthNormalsTexture",  Shader.PropertyToID(destination.name));// destination.id); //v0.1
                    //cmd.SetGlobalTexture("_CameraDepthNormalsTextureGIBLI", destination.id);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
                //if (destination != renderingData.cameraData.renderer.cameraColorTargetHandle)//RenderTargetHandle.CameraTarget) //v0.1
                //{
                //    cmd.ReleaseTemporaryRT(destination.id);
                //    destination = RenderTargetHandle.CameraTarget;
                //}
            }
        }

        DepthNormalsPass depthNormalsPass;
        RTHandle depthNormalsTexture;// RenderTargetHandle depthNormalsTexture; //v0.1
        Material depthNormalsMaterial;

        public override void Create()
        {
            depthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
            depthNormalsPass = new DepthNormalsPass(RenderQueueRange.opaque, -1, depthNormalsMaterial);
            depthNormalsPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            
            //v0.1
            depthNormalsTexture = RTHandles.Alloc("_CameraDepthNormalsTexture", name: "_CameraDepthNormalsTexture");    
            //depthNormalsTexture.Init("_CameraDepthNormalsTexture");
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            depthNormalsPass.Setup(depthNormalsTexture);
            renderer.EnqueuePass(depthNormalsPass);
        }
    }


}
