using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Artngame.GIBLI.SleeplessOwl.URPPostProcessing
{
    public class PostProcessRenderPass : ScriptableRenderPass
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
                    //OnCameraSetupA(cmd, data);
                    ExecutePass(cmd, data, ctx);
                });
            }
        }
        public void OnCameraSetupA(CommandBuffer cmd, PassData data)
        {
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            var renderer = data.cameraData.renderer;            

            //_renderTargetIdentifier = data.colorTargetHandleA;
        }
        void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
        {
            CommandBuffer unsafeCmd = command;
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            if (data.cameraData.camera == Camera.main)
            {
                if (Camera.main != null && data.cameraData.camera == Camera.main)
                {
                    //cmd.Blit(source, source, outlineMaterial, 0);
                }

                var commandBuffer = command;
                // RenderRG(commandBuffer, data);


                if (volumeTypeList.Count == 0)
                    return;

                if (data.cameraData.postProcessEnabled == false)
                    return;

                //collect active pp volume
                activeVolumeList.Clear();
                bool isSceneViewCamera = data.cameraData.isSceneViewCamera;
                foreach (var item in volumeTypeList)
                {
                    var volumeComp = VolumeManager.instance.stack.GetComponent(item) as PostProcessVolumeComponent;

                    if (volumeComp.IsActive() == false)
                        continue;
                    if (isSceneViewCamera && volumeComp.visibleInSceneView == false)
                        continue;

                    activeVolumeList.Add(volumeComp);
                    volumeComp.SetupIfNeed();
                }

                if (activeVolumeList.Count <= 0)
                    return;

                CommandBuffer cb = command;// CommandBufferPool.Get();
                cb.name = displayName;

                var cameraData = data.cameraData;
                var pixelRect = cameraData.camera.pixelRect;
                float scale = cameraData.isSceneViewCamera ? 1 : cameraData.renderScale;
                int width = (int)(pixelRect.width * scale);// cameraData.camera.pixelWidth;// (int)(pixelRect.width * scale);
                int height = (int)(pixelRect.height * scale); //cameraData.camera.pixelHeight;//(int)(pixelRect.height * scale);
                cb.GetTemporaryRT(cycleRT_1, width, height, 0, FilterMode.Bilinear, defaultHDRFormat);
                cb.GetTemporaryRT(cycleRT_2, width, height, 0, FilterMode.Bilinear, defaultHDRFormat);
                var target = cycleRT_1;
                var source = cycleRT_2;

                for (int i = 0; i < activeVolumeList.Count; i++)
                {
                    var volumeComp = activeVolumeList[i];

                    if (i == 0)
                    {
                        cb.Blit(BuiltinRenderTextureType.CurrentActive, source);
                    }
                    else
                    {
                        CoreUtils.Swap(ref target, ref source);
                    }

                    RenderTargetIdentifier renderTarget;
                    bool isFinalVolume = i == activeVolumeList.Count - 1;
                    if (isFinalVolume)
                    {
                        bool renderToDefaultColorTexture =
                            renderPassEvent == RenderPassEvent.BeforeRenderingPostProcessing
                            || renderPassEvent == RenderPassEvent.AfterRenderingSkybox;

                        if (renderToDefaultColorTexture)
                        {
                            //可通过相同方式按引用访问值。 在某些情况下，按引用访问值可避免潜在的高开销复制操作，从而提高性能。 例如，以下语句显示如何定义一个用于引用值的 ref 局部变量。
                            //ref ScriptableRenderer renderer = ref cameraData.renderer;
                            renderTarget = data.colorTargetHandleA;// data.cameraData.renderer.cameraColorTargetHandle;//renderer.cameraColorTarget;// cameraColorTargetHandle; //v0.1

                        }
                        else
                        {
                           // renderTarget = data.colorTargetHandleA;
                            renderTarget = BuiltinRenderTextureType.CameraTarget;
                        }
                    }
                    else
                    {
                        renderTarget = target;
                    }
                    cb.SetRenderTarget(renderTarget);

                    cb.BeginSample(volumeComp.displayName);
                    volumeComp.Render(cb, data.cameraData.camera, source, renderTarget);
                    cb.EndSample(volumeComp.displayName);
                }

                cb.ReleaseTemporaryRT(source);
                cb.ReleaseTemporaryRT(target);
                //context.ExecuteCommandBuffer(cb);
                //cb.Clear();
                //CommandBufferPool.Release(cb);





            }
        }
#endif








        private string displayName;
        private int cycleRT_1 = Shader.PropertyToID("cycleRT_1");
        private int cycleRT_2 = Shader.PropertyToID("cycleRT_2");
        private List<Type> volumeTypeList;
        private List<PostProcessVolumeComponent> activeVolumeList;
        private GraphicsFormat defaultHDRFormat;

        public PostProcessRenderPass(RenderPassEvent passEvent, PostProcessOrderConfig config)
        {
            displayName = $"CustomPostProcessPass {passEvent}";
            renderPassEvent = passEvent;

            activeVolumeList = new List<PostProcessVolumeComponent>();
            volumeTypeList = new List<Type>();

            var piplineAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render)
                && piplineAsset.supportsHDR)
            {
                defaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            }
            else
            {
                defaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
            }

            //collect all custom postprocess volume belong this InjectionPoint
            var allVolumeTypes = VolumeManager.instance.baseComponentTypeArray;
            if (allVolumeTypes != null) {
                //Debug.Log(allVolumeTypes.Length);
                foreach (var volumeName in config.GetVolumeList((InjectionPoint)renderPassEvent))
                {
                    var volumeType = allVolumeTypes.ToList().Find((t) => { return t.ToString() == volumeName; });

                    //check volume type is valid
                    Assert.IsNotNull(volumeType, $"Can't find Volume : [{volumeName}] , Remove it from config");
                    volumeTypeList.Add(volumeType);
                }
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (volumeTypeList.Count == 0)
                return;

            if (renderingData.cameraData.postProcessEnabled == false)
                return;

            //collect active pp volume
            activeVolumeList.Clear();
            bool isSceneViewCamera = renderingData.cameraData.isSceneViewCamera;
            foreach (var item in volumeTypeList)
            {
                var volumeComp = VolumeManager.instance.stack.GetComponent(item) as PostProcessVolumeComponent;

                if (volumeComp.IsActive() == false)
                    continue;
                if (isSceneViewCamera && volumeComp.visibleInSceneView == false)
                    continue;

                activeVolumeList.Add(volumeComp);
                volumeComp.SetupIfNeed();
            }

            if (activeVolumeList.Count <= 0)
                return;

            CommandBuffer cb = CommandBufferPool.Get();
            cb.name = displayName;

            var cameraData = renderingData.cameraData;
            var pixelRect = cameraData.camera.pixelRect;
            float scale = cameraData.isSceneViewCamera ? 1 : cameraData.renderScale;
            int width = (int)(pixelRect.width * scale);
            int height = (int)(pixelRect.height * scale);
            cb.GetTemporaryRT(cycleRT_1, width, height, 0, FilterMode.Bilinear, defaultHDRFormat);
            cb.GetTemporaryRT(cycleRT_2, width, height, 0, FilterMode.Bilinear, defaultHDRFormat);
            var target = cycleRT_1;
            var source = cycleRT_2;

            for (int i = 0; i < activeVolumeList.Count; i++)
            {
                var volumeComp = activeVolumeList[i];

                if (i == 0)
                {
                    cb.Blit(BuiltinRenderTextureType.CurrentActive, source);
                }
                else
                {
                    CoreUtils.Swap(ref target, ref source);
                }

                RenderTargetIdentifier renderTarget;
                bool isFinalVolume = i == activeVolumeList.Count - 1;
                if (isFinalVolume)
                {
                    bool renderToDefaultColorTexture =
                        renderPassEvent == RenderPassEvent.BeforeRenderingPostProcessing
                        || renderPassEvent == RenderPassEvent.AfterRenderingSkybox;

                    if (renderToDefaultColorTexture)
                    {
                        //可通过相同方式按引用访问值。 在某些情况下，按引用访问值可避免潜在的高开销复制操作，从而提高性能。 例如，以下语句显示如何定义一个用于引用值的 ref 局部变量。
                        ref ScriptableRenderer renderer = ref cameraData.renderer;
#if UNITY_2022_1_OR_NEWER
                        renderTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;//renderer.cameraColorTarget;// cameraColorTargetHandle; //v0.1
#else
                        renderTarget = renderer.cameraColorTarget;// cameraColorTargetHandle; //v0.1
#endif
                    }
                    else
                    {
                        renderTarget = BuiltinRenderTextureType.CameraTarget;
                    }
                }
                else
                {
                    renderTarget = target;
                }
                cb.SetRenderTarget(renderTarget);

                cb.BeginSample(volumeComp.displayName);
                volumeComp.Render(cb, renderingData.cameraData.camera, source, renderTarget);
                cb.EndSample(volumeComp.displayName);
            }

            cb.ReleaseTemporaryRT(source);
            cb.ReleaseTemporaryRT(target);
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
            CommandBufferPool.Release(cb);
        }
    }
}
