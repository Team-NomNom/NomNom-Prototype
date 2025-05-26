using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using System.Collections.Generic;

namespace Artngame.GIBLI
{
    public class GrassBendingRTPrePass : ScriptableRendererFeature
    {
        class CustomRenderPass : ScriptableRenderPass
        {

#if UNITY_2023_3_OR_NEWER
            //GRAPH

            /*
            public class PassData
            {
                public RenderingData renderingData;
                public UniversalCameraData cameraData;
                public CullingResults cullResults;
                public TextureHandle colorTargetHandleA;
                public ContextContainer frameDataA;
                public void Init(ContextContainer frameData, IUnsafeRenderGraphBuilder builder = null)
                {
                    cameraData = frameData.Get<UniversalCameraData>();
                    cullResults = frameData.Get<UniversalRenderingData>().cullResults;
                    frameDataA = frameData;
                }
            }
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                string passName = "Screen space Cavity pass";
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
                        ExecutePass(cmd, data, ctx, renderGraph);
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

                //512*512 is big enough for this demo's max grass count, can use a much smaller RT in regular use case
                //TODO: make RT render pos follow main camera view frustrum, allow using a much smaller size RT
                cmd.GetTemporaryRT(_GrassBendingRT_pid, new RenderTextureDescriptor(512, 512, RenderTextureFormat.R8, 0));

                ///v0.1
                if (_GrassBendingRT_rti == null)
                {
                    _GrassBendingRT_rti = RTHandles.Alloc("_GrassBendingRT", name: "_GrassBendingRT"); //v0.1
                   
                }
                //ConfigureTarget(_GrassBendingRT_rti);//_GrassBendingRT_rti);
                cmd.SetRenderTarget(_GrassBendingRT_rti);

            }
            void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx, RenderGraph renderGraph)//, RasterGraphContext context)
            {
                CommandBuffer unsafeCmd = command;
                RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                if (!InstancedIndirectGrassRenderer.instance)
                {
                    //Debug.LogWarning("InstancedIndirectGrassRenderer not found, abort GrassBendingRTPrePass's Execute");
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get("GrassBendingRT");

                //make a new view matrix that is the same as an imaginary camera above grass center 1 units and looking at grass(bird view)
                //scale.z is -1 because view space will look into -Z while world space will look into +Z
                //camera transform's local to world's inverse means camera's world to view = world to local
                Matrix4x4 viewMatrix = Matrix4x4.TRS(InstancedIndirectGrassRenderer.instance.transform.position + new Vector3(0, 1, 0), Quaternion.LookRotation(-Vector3.up), new Vector3(1, 1, -1)).inverse;

                //ortho camera with 1:1 aspect, size = 50
                float sizeX = InstancedIndirectGrassRenderer.instance.transform.localScale.x;
                float sizeZ = InstancedIndirectGrassRenderer.instance.transform.localScale.z;
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-sizeX, sizeX, -sizeZ, sizeZ, 0.5f, 1.5f);

                //override view & Projection matrix
                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                //context.ExecuteCommandBuffer(cmd);

                //draw all trail renderer using SRP batching
                var drawSetting = CreateDrawingSettingsA(GrassBending_stid, data, SortingCriteria.CommonTransparent);
                var filterSetting = new FilteringSettings(RenderQueueRange.all);





                // Access the relevant frame data from the Universal Render Pipeline
                UniversalRenderingData universalRenderingData = data.frameDataA.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = data.frameDataA.Get<UniversalCameraData>();
                UniversalLightData lightData = data.frameDataA.Get<UniversalLightData>();

                //var sortFlags = cameraData.defaultOpaqueSortFlags;
                //RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
                //FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, -1);

                //ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[]
                //{
                //new ShaderTagId("UniversalForwardOnly"),
                //new ShaderTagId("UniversalForward"),
                //new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                //new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                //};

                //m_ShaderTagIdList.Clear();

                //foreach (ShaderTagId sid in forwardOnlyShaderTagIds)
                //    m_ShaderTagIdList.Add(sid);

                //DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortFlags);

                var param = new RendererListParams(universalRenderingData.cullResults, drawSetting, filterSetting);
                //passData.rendererListHandle = renderGraph.CreateRendererList(param);
                //ctx.cmd.rende
                RendererListHandle rlh =  renderGraph.CreateRendererList(param);
                //context.DrawRenderers(renderingData.cullResults, ref drawSetting, ref filterSetting);
                cmd.DrawRendererList(rlh);//, ref drawSetting, ref filterSetting);





                //data.frameDataA.GetCameraData().render

                //restore camera matrix
                cmd.Clear();
                cmd.SetViewProjectionMatrices(data.cameraData.camera.worldToCameraMatrix, data.cameraData.camera.projectionMatrix);

                //set global RT
                cmd.SetGlobalTexture(_GrassBendingRT_pid, new RenderTargetIdentifier(_GrassBendingRT_pid));

                //Debug.Log(_GrassBendingRT_pid);
                //if (data.cameraData.camera == Camera.main)
                //{
                //}
            }
            DrawingSettings CreateDrawingSettingsA(ShaderTagId shaderTagId, PassData data, SortingCriteria sortingCriteria)
            {
                ContextContainer frameData = data.frameDataA;
                UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                return RenderingUtils.CreateDrawingSettings(shaderTagId, universalRenderingData, cameraData, lightData, sortingCriteria);
            }
            */
            // Layer mask used to filter objects to put in the renderer list
            

            TextureHandle _GrassBendingRT_rtiTH;

            // List of shader tags used to build the renderer list
            private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

           

            // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
            private class PassData
            {
                public RendererListHandle rendererListHandle;
                public TextureHandle destinationO;
                public UniversalCameraData cameraDataA;
            }

            // Sample utility method that showcases how to create a renderer list via the RenderGraph API
            private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
            {
                // Access the relevant frame data from the Universal Render Pipeline
                UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                var sortFlags = cameraData.defaultOpaqueSortFlags;
                RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
                FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, -1);// m_LayerMask);

                ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[]
                {
                    GrassBending_stid
                //    ,
                //new ShaderTagId("UniversalForwardOnly"),
                //new ShaderTagId("UniversalForward"),
                //new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                //new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                };

                m_ShaderTagIdList.Clear();

                foreach (ShaderTagId sid in forwardOnlyShaderTagIds)
                    m_ShaderTagIdList.Add(sid);

                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortFlags);

                var param = new RendererListParams(universalRenderingData.cullResults, drawSettings, filterSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(param);
            }


            static void ExecutePassPASS(PassData data, RasterGraphContext context)
            {
                Debug.Log("1111sss ");// + tex.GetDescriptor());
                context.cmd.SetGlobalTexture("_GrassBendingRT",data.destinationO);
            }
            // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
            static void ExecutePass(PassData data, RasterGraphContext context)
            {
               // Debug.Log("II111NN");
                Matrix4x4 viewMatrix = Matrix4x4.TRS(InstancedIndirectGrassRenderer.instance.transform.position + new Vector3(0, 1, 0), Quaternion.LookRotation(-Vector3.up), new Vector3(1, 1, -1)).inverse;

                //ortho camera with 1:1 aspect, size = 50
                float sizeX = InstancedIndirectGrassRenderer.instance.transform.localScale.x;
                float sizeZ = InstancedIndirectGrassRenderer.instance.transform.localScale.z;
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-sizeX, sizeX, -sizeZ, sizeZ, 0.5f, 1.5f);

                //override view & Projection matrix
                context.cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                // context.ExecuteCommandBuffer(cmd);

                //draw all trail renderer using SRP batching
                // var drawSetting = CreateDrawingSettings(GrassBending_stid, ref renderingData, SortingCriteria.CommonTransparent);
                //var filterSetting = new FilteringSettings(RenderQueueRange.all);
                context.cmd.ClearRenderTarget(RTClearFlags.All, Color.white, 1, 0);
                // context.DrawRenderers(renderingData.cullResults, ref drawSetting, ref filterSetting);
                context.cmd.DrawRendererList(data.rendererListHandle);

                //restore camera matrix//
              //  context.cmd.cle
                
                context.cmd.SetViewProjectionMatrices(data.cameraDataA.camera.worldToCameraMatrix, data.cameraDataA.camera.projectionMatrix);

                // context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.green, 1, 0);

                //Debug.Log("IINN");

                //context.cmd.SetGlobalTexture("_GrassBendingRT",  data.destinationO);
            }


            TextureHandle destinationAA;


            // This is where the renderGraph handle can be accessed.
            // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                string passName = "RenderList Render Pass";

                if (!InstancedIndirectGrassRenderer.instance)
                {
                    //Debug.LogWarning("InstancedIndirectGrassRenderer not found, abort GrassBendingRTPrePass's Execute");
                    return;
                }

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;// 16;

                //if (_GrassBendingRT_rti == null)
                //{
                //    //_GrassBendingRT_rti = RTHandles.Alloc("_GrassBendingRT", name: "_GrassBendingRT"); //v0.1
                //    _GrassBendingRT_rti = RTHandles.Alloc(desc.width, desc.height, name: "_GrassBendingRT");//,depthBufferBits: DepthBits.Depth16 );

                //    //_GrassBendingRT_rti.depth
                //}
                //destinationAA = renderGraph.ImportTexture(_GrassBendingRT_rti);//
                destinationAA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_GrassBendingRT", true);

                // This simple pass clears the current active color texture, then renders the scene geometry associated to the m_LayerMask layer.
                // Add scene geometry to your own custom layers and experiment switching the layer mask in the render feature UI.
                // You can use the frame debugger to inspect the pass output

                // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                {
                    // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                    // The active color and depth textures are the main color and depth buffers that the camera renders into
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                    // Fill up the passData with the data needed by the pass
                    InitRendererLists(frameData, ref passData, renderGraph);

                    // Make sure the renderer list is valid
                    if (!passData.rendererListHandle.IsValid())
                        return;

                    // We declare the RendererList we just created as an input dependency to this pass, via UseRendererList()
                    builder.UseRendererList(passData.rendererListHandle);

                   // builder.AllowPassCulling(false);

                    //builder.AllowGlobalStateModification(true);

                    // Setup as a render target via UseTextureFragment and UseTextureFragmentDepth, which are the equivalent of using the old cmd.SetRenderTarget(color,depth)
                    // builder.UseTextureFragment(resourceData.activeColorTexture, 0);
                    // //        builder.UseTexture(resourceData.activeColorTexture, 0);

                    //builder.UseTextureFragmentDepth(resourceData.activeDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //if (_GrassBendingRT_rti == null)
                    //{
                    //    _GrassBendingRT_rti = RTHandles.Alloc("_GrassBendingRT", name: "_GrassBendingRT"); //v0.1
                    //}//
                    ///_GrassBendingRT_rti.





                    passData.cameraDataA = cameraData;

                   // destinationAA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_GrassBendingRT", false);

                    passData.destinationO = destinationAA;

                    ///builder.UseTexture(destinationAA);
                    //passData.destinationO = resourceData.activeColorTexture;
                    //builder.UseTexture(passData.destinationO, AccessFlags.ReadWrite);//

                    // builder.UseTexture(resourceData.activeColorTexture, 0);

                    // builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                    // builder.SetRenderAttachment(passData.destinationO, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(destinationAA,0);//, 0);//, AccessFlags.Write);

                    builder.SetGlobalTextureAfterPass(destinationAA, _GrassBendingRTTex);

                    //Debug.Log("A1");
                    // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                    //Debug.Log("A2");
                }

                //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Pass global grass texture", out var passData))
                //{
                //    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                //    builder.AllowPassCulling(false);
                //    builder.AllowGlobalStateModification(true);

                //    //passData.destinationO = resourceData.activeColorTexture;
                //    // builder.UseTexture(destinationAA, 0);
                //    // builder.UseTexture(resourceData.activeColorTexture, 0);

                //    //passData.destinationO = destinationAA;

                //    builder.UseTexture(passData.destinationO,  AccessFlags.Read);//

                //    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePassPASS(data, context));

                //}
            }
            private static readonly int _GrassBendingRTTex = Shader.PropertyToID("_GrassBendingRT");


#endif



            //public LayerMask m_LayerMask;
            private LayerMask m_LayerMask;
            public CustomRenderPass(LayerMask layerMask)
            {
                m_LayerMask = layerMask;
            }









            static readonly int _GrassBendingRT_pid = Shader.PropertyToID("_GrassBendingRT");
            
            //static readonly RenderTargetIdentifier _GrassBendingRT_rti = new RenderTargetIdentifier(_GrassBendingRT_pid);
            
            //v0.1
            private RTHandle _GrassBendingRT_rti;
            
            ShaderTagId GrassBending_stid = new ShaderTagId("GrassBending");

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            //public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                //512*512 is big enough for this demo's max grass count, can use a much smaller RT in regular use case
                //TODO: make RT render pos follow main camera view frustrum, allow using a much smaller size RT
                cmd.GetTemporaryRT(_GrassBendingRT_pid, new RenderTextureDescriptor(512, 512, RenderTextureFormat.R8, 0));
                
                ///v0.1
                _GrassBendingRT_rti = RTHandles.Alloc("_GrassBendingRT", name: "_GrassBendingRT"); //v0.1
                ConfigureTarget(_GrassBendingRT_rti);//_GrassBendingRT_rti);
                ConfigureClear(ClearFlag.All, Color.white);
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!InstancedIndirectGrassRenderer.instance)
                {
                    //Debug.LogWarning("InstancedIndirectGrassRenderer not found, abort GrassBendingRTPrePass's Execute");
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get("GrassBendingRT");

                //make a new view matrix that is the same as an imaginary camera above grass center 1 units and looking at grass(bird view)
                //scale.z is -1 because view space will look into -Z while world space will look into +Z
                //camera transform's local to world's inverse means camera's world to view = world to local
                Matrix4x4 viewMatrix = Matrix4x4.TRS(InstancedIndirectGrassRenderer.instance.transform.position + new Vector3(0, 1, 0), Quaternion.LookRotation(-Vector3.up), new Vector3(1, 1, -1)).inverse;

                //ortho camera with 1:1 aspect, size = 50
                float sizeX = InstancedIndirectGrassRenderer.instance.transform.localScale.x;
                float sizeZ = InstancedIndirectGrassRenderer.instance.transform.localScale.z;
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-sizeX, sizeX, -sizeZ, sizeZ, 0.5f, 1.5f);

                //override view & Projection matrix
                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                context.ExecuteCommandBuffer(cmd);

                //draw all trail renderer using SRP batching
                var drawSetting = CreateDrawingSettings(GrassBending_stid, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSetting = new FilteringSettings(RenderQueueRange.all);
                context.DrawRenderers(renderingData.cullResults, ref drawSetting, ref filterSetting);

                //restore camera matrix
                cmd.Clear();
                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);

                //set global RT
                cmd.SetGlobalTexture(_GrassBendingRT_pid, new RenderTargetIdentifier(_GrassBendingRT_pid));

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(_GrassBendingRT_pid);
            }
        }

        CustomRenderPass m_ScriptablePass;
        public LayerMask m_LayerMask;
        public RenderPassEvent eventA =  RenderPassEvent.AfterRenderingPrePasses;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass(m_LayerMask);

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = eventA;// RenderPassEvent.AfterRenderingPrePasses; //don't do RT switch when rendering _CameraColorTexture, so use AfterRenderingPrePasses
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

}
