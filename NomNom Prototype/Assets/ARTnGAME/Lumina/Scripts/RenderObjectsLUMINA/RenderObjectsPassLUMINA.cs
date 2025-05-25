using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
//using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Artngame.LUMINA
{
    /// <summary>
    /// The scriptable render pass used with the render objects renderer feature.
    /// </summary>
   // [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal")]
    public class RenderObjectsPassLUMINA : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        RenderObjectsLUMINA.CustomCameraSettings m_CameraSettings;


        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material overrideMaterial { get; set; }

        //v0.1
        public RenderTexture threeDTexture;

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int overrideMaterialPassIndex { get; set; }

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader overrideShader { get; set; }

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int overrideShaderPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private PassData m_PassData;

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        [Obsolete("Use SetDepthState instead", true)]
        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            SetDepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets up the stencil settings for the pass.
        /// </summary>
        /// <param name="reference">The stencil reference value.</param>
        /// <param name="compareFunction">The comparison function to use.</param>
        /// <param name="passOp">The stencil operation to use when the stencil test passes.</param>
        /// <param name="failOp">The stencil operation to use when the stencil test fails.</param>
        /// <param name="zFailOp">The stencil operation to use when the stencil test fails because of depth.</param>
        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }

        RenderStateBlock m_RenderStateBlock;

        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="cameraSettings">The settings for custom cameras values.</param>
        public RenderObjectsPassLUMINA(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjectsLUMINA.CustomCameraSettings cameraSettings)            
        {
            profilingSampler = new ProfilingSampler(profilerTag);
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings);
        }

       // internal RenderObjectsPassLUMINA(URPProfileId profileId, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjectsLUMINA.CustomCameraSettings cameraSettings)
        //{
        //    profilingSampler = ProfilingSampler.Get(profileId);
       //     Init(renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings);
        //}

        internal void Init(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjectsLUMINA.CustomCameraSettings cameraSettings)
        {
            m_PassData = new PassData();

            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            this.overrideShader = null;
            this.overrideShaderPassIndex = 0;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var tag in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(tag));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_CameraSettings = cameraSettings;
        }

        /// <inheritdoc/>
        [Obsolete("DeprecationMessage.CompatibilityScriptingAPIObsolete", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //UniversalRenderingData universalRenderingData = renderingData.frameData.Get<UniversalRenderingData>();
            //UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            //UniversalLightData lightData = renderingData.frameData.Get<UniversalLightData>();

            //var cmd = CommandBufferPool.Get();// CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer);

            //using (new ProfilingScope(cmd, profilingSampler))
            //{
            //    InitPassData(cameraData, ref m_PassData);
            //    InitRendererLists(universalRenderingData, lightData, ref m_PassData, context, default(RenderGraph), false);

            //    ExecutePass(m_PassData, cmd, m_PassData.rendererList, renderingData.cameraData.IsCameraProjectionMatrixFlipped());
            //}
        }

        public static void SetViewAndProjectionMatricesA(RasterCommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool setInverseMatrices) { 
            SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, setInverseMatrices); 
        }
        //public static readonly int viewMatrix = Shader.PropertyToID("unity_MatrixV");
        //public static readonly int projectionMatrix = Shader.PropertyToID("glstate_matrix_projection");
        //public static readonly int viewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");

        //public static readonly int inverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");
        //public static readonly int inverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");
        //public static readonly int inverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");
        public static void SetViewAndProjectionMatrices(RasterCommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool setInverseMatrices)
        {
            Matrix4x4 viewAndProjectionMatrix = projectionMatrix * viewMatrix;
            cmd.SetGlobalMatrix("unity_MatrixV", viewMatrix);
            cmd.SetGlobalMatrix("glstate_matrix_projection", projectionMatrix);
            cmd.SetGlobalMatrix("unity_MatrixVP", viewAndProjectionMatrix);

            if (setInverseMatrices)
            {
                Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
                Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(projectionMatrix);
                Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;
                cmd.SetGlobalMatrix("unity_MatrixInvV", inverseViewMatrix);
                cmd.SetGlobalMatrix("unity_MatrixInvP", inverseProjectionMatrix);
                cmd.SetGlobalMatrix("unity_MatrixInvVP", inverseViewProjection);
            }
        }
        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            Camera camera = passData.cameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            //Rect pixelRect = passData.cameraData.pixelRect;
            //ref frameData.Get<UniversalCameraData>().pixelRect;
            float cameraAspect = (float)1280 / (float)720;

            //float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

            if (passData.cameraSettings.overrideCamera)
            {
                if (passData.cameraData.xr.enabled)
                {
                    Debug.LogWarning("RenderObjects pass is configured to override camera matrices. While rendering in stereo camera matrices cannot be overridden.");
                }
                else
                {
                    Matrix4x4 projectionMatrix = Matrix4x4.Perspective(passData.cameraSettings.cameraFieldOfView, cameraAspect,
                        camera.nearClipPlane, camera.farClipPlane);
                    projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);

                    Matrix4x4 viewMatrix = passData.cameraData.GetViewMatrix();
                    Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                    viewMatrix.SetColumn(3, cameraTranslation + passData.cameraSettings.offset);

                    //RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                    SetViewAndProjectionMatricesA(cmd, viewMatrix, projectionMatrix, false);
                }
            }

            //var activeDebugHandler = GetActiveDebugHandler(passData.cameraData);
            //if (activeDebugHandler != null)
            //{
            //    passData.debugRendererLists.DrawWithRendererList(cmd);
            //}
            //else
            //{
                cmd.DrawRendererList(rendererList);
           // }

            if (passData.cameraSettings.overrideCamera && passData.cameraSettings.restoreCamera && !passData.cameraData.xr.enabled)
            {
                //RenderingUtils.SetViewAndProjectionMatrices(cmd, passData.cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(passData.cameraData.GetProjectionMatrix(0), isYFlipped), false);
                SetViewAndProjectionMatricesA(cmd, passData.cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(passData.cameraData.GetProjectionMatrix(0), isYFlipped), false);
            }            
        }

        private class PassData
        {
            internal RenderObjectsLUMINA.CustomCameraSettings cameraSettings;
            internal RenderPassEvent renderPassEvent;

            internal TextureHandle color;
            internal RendererListHandle rendererListHdl;
            //internal DebugRendererLists debugRendererLists;

            internal UniversalCameraData cameraData;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.cameraSettings = m_CameraSettings;
            passData.renderPassEvent = renderPassEvent;
            passData.cameraData = cameraData;
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
                passData.cameraData, lightData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;

            //v0.1 if shader not defined, grab by material if defined or by LUMINA
            drawingSettings.overrideShader = overrideShader;            
            if (Camera.main != null )
            {
                Artngame.LUMINA.LUMINA Lumina = Camera.main.GetComponent<Artngame.LUMINA.LUMINA>();
                if (Lumina != null)
                {
                    drawingSettings.overrideShader = Lumina.preRenderers.shader;
                }
            }

            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;

           // var activeDebugHandler = GetActiveDebugHandler(passData.cameraData);
            var filterSettings = m_FilteringSettings;
            if (useRenderGraph)
            {
                //if (activeDebugHandler != null)
                //{
                //    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(renderGraph,
                //        ref renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
                //}
                //else
                //{
                //RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings,
                CreateRendererListWithRenderStateBlockA(renderGraph, ref renderingData.cullResults, drawingSettings,
                    m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
               // }
            }
            else
            {
                //if (activeDebugHandler != null)
                //{
                //    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(context, ref renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
                //}
                //else
                //{
   //                 CreateRendererListWithRenderStateBlockA(context, ref renderingData.cullResults, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererList);
                //}
            }
        }
        static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];
        static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];
        public static void CreateRendererListWithRenderStateBlockA(RenderGraph renderGraph, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererListHandle rl)
        {
            s_ShaderTagValues[0] = ShaderTagId.none;
            s_RenderStateBlocks[0] = rsb;
            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(cullResults, ds, fs)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };
            rl = renderGraph.CreateRendererList(param);
        }


        // Create a RendererList using a RenderStateBlock override is quite common so we have this optimized utility function for it
        public static void CreateRendererListWithRenderStateBlockA(ScriptableRenderContext context, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererList rl)
        {
//            RendererListParams param = new RendererListParams();
//            unsafe
//            {
//                // Taking references to stack variables in the current function does not require any pinning (as long as you stay within the scope)
//                // so we can safely alias it as a native array
//                RenderStateBlock* rsbPtr = &rsb;
//                var stateBlocks = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<RenderStateBlock>(rsbPtr, 1, Allocator.None);

//                var shaderTag = ShaderTagId.none;
//                var tagValues = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ShaderTagId>(&shaderTag, 1, Allocator.None);

//                // Inside CreateRendererList (below), we pass the NativeArrays to C++ by calling GetUnsafeReadOnlyPtr
//                // This will check read access but NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray does not set up the SafetyHandle (by design) so create/add it here
//                // NOTE: we explicitly share the handle
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//                var safetyHandle = AtomicSafetyHandle.Create();
//                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);

//                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref stateBlocks, safetyHandle);
//                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tagValues, safetyHandle);
//#endif

//                // Create & schedule the RL
//                param = new RendererListParams(cullResults, ds, fs)
//                {
//                    tagValues = tagValues,
//                    stateBlocks = stateBlocks

//                };

//                rl = context.CreateRendererList(ref param);

//                // we need to explicitly release the SafetyHandle
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//                AtomicSafetyHandle.Release(safetyHandle);
//#endif
//            }
        }


        //v0.1
        class UAVResources : ContextItem
        {
            public TextureHandle uavTextureBuffer { get; set; }
            public BufferHandle uavBuffer { get; set; }

            public override void Reset()
            {
                uavTextureBuffer = TextureHandle.nullHandle;
                uavBuffer = BufferHandle.nullHandle;
            }
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                InitPassData(cameraData, ref passData);

                passData.color = resourceData.activeColorTexture;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);

                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle[] dBufferHandles = resourceData.dBuffer;
                for (int i = 0; i < dBufferHandles.Length; ++i)
                {
                    TextureHandle dBuffer = dBufferHandles[i];
                    if (dBuffer.IsValid())
                        builder.UseTexture(dBuffer, AccessFlags.Read);
                }

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);

                InitRendererLists(renderingData, lightData, ref passData, default(ScriptableRenderContext), renderGraph, true);
               // var activeDebugHandler = GetActiveDebugHandler(passData.cameraData);
                //if (activeDebugHandler != null)
                //{
                //    passData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                //}
                //else
                //{
                    builder.UseRendererList(passData.rendererListHdl);
              //  }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                /////     //if (cameraData.xr.enabled)
                /////////      //    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && cameraData.xrUniversal.canFoveateIntermediatePasses);


                //v0.1
                //https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-import-a-texture.html
                //if (threeDTexture != null) {
                //    RTHandle renderTexture = RTHandles.Alloc(threeDTexture);
                //    TextureHandle textureHandle = renderGraph.ImportTexture(renderTexture);
                //    builder.SetRandomAccessAttachment(textureHandle, 1, AccessFlags.ReadWrite);
                //}
               
                if (Camera.main != null)
                {
                    Artngame.LUMINA.LUMINA Lumina = Camera.main.GetComponent<Artngame.LUMINA.LUMINA>();
                    if (Lumina != null)
                    {
                        RTHandle renderTexture = RTHandles.Alloc(Lumina.integerVolume);
                        TextureHandle textureHandle = renderGraph.ImportTexture(renderTexture);
                        builder.SetRandomAccessAttachment(textureHandle, 1, AccessFlags.ReadWrite);
                    }
                }

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(data, rgContext.cmd, data.rendererListHdl, isYFlipped);
                });
            }
        }
    }
}
