using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using System.Reflection;

//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Artngame.GIBLI.Toguchi.Rendering
{
    public abstract class CustomPostProcessingPass<T> : ScriptableRenderPass where T : VolumeComponent
    {


#if UNITY_2023_3_OR_NEWER
        //GRAPH
        public class PassData
        {
            public RenderingData renderingData;
            public UniversalCameraData cameraData;
            public CullingResults cullResults;
            public TextureHandle colorTargetHandleA;
            public void Init(ContextContainer frameData, IUnsafeRenderGraphBuilder builder = null)
            {
                cameraData = frameData.Get<UniversalCameraData>();
                cullResults = frameData.Get<UniversalRenderingData>().cullResults;
            }
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Giblion shafts pass";
            using (var builder = renderGraph.AddUnsafePass<PassData>(passName,
                out var data))
            {
                builder.AllowPassCulling(false);
                data.Init(frameData, builder);
                builder.AllowGlobalStateModification(true);
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                data.colorTargetHandleA = resourceData.activeColorTexture;
                builder.UseTexture(data.colorTargetHandleA, AccessFlags.ReadWrite);

                builder.SetRenderFunc<PassData>((data, ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    OnCameraSetupA(cmd, data);
                    ExecutePass(cmd, data, ctx);
                });
            }
        }
        void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
        {

            //RenderingData renderingDataA = (RenderingData)R_RenderingData_frameData.GetValue(renderingDataA);// GetRenderingData(ref renderingDataA);
            //Debug.Log(renderingDataA.cameraData.camera.scaledPixelWidth);
            // command.Clear();
            CommandBuffer unsafeCmd = command;
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            //RenderingData renderingData = new RenderingData();
           // GetFrameData(ref renderingData);// = new CameraData();
           // Debug.Log(renderingData.cameraData.camera.name);
            //renderingData.ca

            if (data.cameraData.camera == Camera.main)
            {
                //renderingData.cameraData.camera = data.cameraData.camera;

                //         command.Blit(data.colorTargetHandleA, data.colorTargetHandleA);
                //command.Clear();
                //v1.6
                if (Camera.main != null && data.cameraData.camera == Camera.main)
                {
                    //cmd.Blit(source, source, outlineMaterial, 0);
                }

                if (Material == null || !data.cameraData.postProcessEnabled)
                {
                    return;
                }

                var volumeStack = VolumeManager.instance.stack;
                Component = volumeStack.GetComponent<T>();
                if (Component == null || !Component.active || !IsActive())
                {
                    return;
                }

                var commandBuffer = command;// CommandBufferPool.Get(RenderTag);
                RenderRG(commandBuffer, data);
                // context.ExecuteCommandBuffer(commandBuffer);
                // CommandBufferPool.Release(commandBuffer);

                //cmd.Blit(finalDepthPyramid, data.colorTargetHandleA, Vector2.one * tempSlices[debug].scale, Vector2.zero, debug, 0);
            }
        }
        private void RenderRG(CommandBuffer commandBuffer, PassData data)
        {
            var source = _renderTargetIdentifier;
            var dest = _tempRenderTargetIdentifier;

            SetupRenderTextureRG(commandBuffer, data);

            BeforeRenderRG(commandBuffer, data);

            CopyToTempBufferRG(commandBuffer, data, source, dest);

            RenderRG(commandBuffer, data, dest, source);

            CleanupRenderTextureRG(commandBuffer, data);
        }
        protected virtual void SetupRenderTextureRG(CommandBuffer commandBuffer, PassData data)
        {
            ref var cameraData = ref data.cameraData;

            // var desc = new RenderTextureDescriptor(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight);

            RenderTextureFormat forma = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            var desc = new RenderTextureDescriptor(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight, forma,16);
            
            // desc.colorFormat = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            // RT確保
            commandBuffer.GetTemporaryRT(TempColorBufferId, desc);
        }
        protected abstract void BeforeRenderRG(CommandBuffer commandBuffer, PassData data);
        protected virtual void CopyToTempBufferRG(CommandBuffer commandBuffer, PassData data,
          RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            commandBuffer.Blit(source, dest);
        }
        protected virtual void RenderRG(CommandBuffer commandBuffer, PassData data,
           RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            commandBuffer.Blit(source, dest, Material);
        }
        protected virtual void CleanupRenderTextureRG(CommandBuffer commandBuffer, PassData data)
        {
            // RT開放
            commandBuffer.ReleaseTemporaryRT(TempColorBufferId);
        }
        public void OnCameraSetupA(CommandBuffer cmd, PassData data)
        {
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            var renderer = data.cameraData.renderer;
            //destination = renderingData.colorTargetHandleA;
            //source = renderingData.colorTargetHandleA;

            _renderTargetIdentifier = data.colorTargetHandleA;

            _tempRenderTargetIdentifier = new RenderTargetIdentifier(TempColorBufferId);
        }
      //  public static ContextContainer GetFrameData( ref RenderingData renderingData)
        //{
       //     return (ContextContainer)R_RenderingData_frameData.GetValue(renderingData);
       // }
        // public RenderingData GetRenderingData(this ref RenderingData renderingData)
        // {
        //    return (RenderingData)R_RenderingData_frameData.GetValue(renderingData);
        // }
        //static FieldInfo R_RenderingData_frameData = typeof(RenderingData).GetField("frameData", BindingFlags.NonPublic | BindingFlags.Instance);
        //static FieldInfo R_RenderingData_frameData = typeof(RenderingData).GetField("frameData", BindingFlags.NonPublic | BindingFlags.Instance);

#endif



        protected static readonly int TempColorBufferId = UnityEngine.Shader.PropertyToID("_TempColorBuffer");
        
        protected Shader Shader;
        protected Material Material;
        protected T Component;

        private RenderTargetIdentifier _renderTargetIdentifier;
        private RenderTargetIdentifier _tempRenderTargetIdentifier;
        
        protected abstract string RenderTag { get; }
        
        public CustomPostProcessingPass(RenderPassEvent renderPassEvent, Shader shader)
        {
            this.renderPassEvent = renderPassEvent;
            Shader = shader;

            if (shader == null)
            {
                return;
            }
            
            Material = CoreUtils.CreateEngineMaterial(shader);
        }
        
        public virtual void Setup(in RenderTargetIdentifier renderTargetIdentifier)
        {
            _renderTargetIdentifier = renderTargetIdentifier;
            _tempRenderTargetIdentifier = new RenderTargetIdentifier(TempColorBufferId);
        }



        //v1.5
#if UNITY_2020_2_OR_NEWER
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // get a copy of the current camera’s RenderTextureDescriptor
            // this descriptor contains all the information you need to create a new texture
            //RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            //v0.1
            var renderer = renderingData.cameraData.renderer;
            //source = renderer.cameraColorTarget;

            //RenderTargetHandle.CameraTarget
            //destination = RenderTargetHandle.CameraTarget;
#if UNITY_2022_1_OR_NEWER
            _renderTargetIdentifier = renderingData.cameraData.renderer.cameraColorTargetHandle;//renderer.cameraColorTarget;//renderTargetIdentifier; //v0.1
#else
            _renderTargetIdentifier = renderer.cameraColorTarget;//renderTargetIdentifier; //v0.1
#endif
            _tempRenderTargetIdentifier = new RenderTargetIdentifier(TempColorBufferId);
        }
#endif


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Material == null || !renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            var volumeStack = VolumeManager.instance.stack;
            Component = volumeStack.GetComponent<T>();
            if (Component == null || !Component.active || !IsActive())
            {
                return;
            }
            
            var commandBuffer = CommandBufferPool.Get(RenderTag);
            Render(commandBuffer, ref renderingData);
            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        private void Render(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            var source = _renderTargetIdentifier;
            var dest = _tempRenderTargetIdentifier;
            
            SetupRenderTexture(commandBuffer, ref renderingData);
            
            BeforeRender(commandBuffer, ref renderingData);
            
            CopyToTempBuffer(commandBuffer, ref renderingData, source, dest);
            
            Render(commandBuffer, ref renderingData, dest, source);
            
            CleanupRenderTexture(commandBuffer, ref renderingData);
        }

     



        protected virtual void SetupRenderTexture(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

           // var desc = new RenderTextureDescriptor(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight);
           // desc.colorFormat = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            RenderTextureFormat forma = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            var desc = new RenderTextureDescriptor(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight, forma, 16);

            // RT確保
            commandBuffer.GetTemporaryRT(TempColorBufferId, desc);
        }
       

        protected virtual void CleanupRenderTexture(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            // RT開放
            commandBuffer.ReleaseTemporaryRT(TempColorBufferId);
        }

        protected virtual void CopyToTempBuffer(CommandBuffer commandBuffer, ref RenderingData renderingData,
            RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            commandBuffer.Blit(source, dest);
        }

        protected virtual void Render(CommandBuffer commandBuffer, ref RenderingData renderingData,
            RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            commandBuffer.Blit(source, dest, Material);
        }

        protected abstract void BeforeRender(CommandBuffer commandBuffer, ref RenderingData renderingData);

        protected abstract bool IsActive();
    }
}
