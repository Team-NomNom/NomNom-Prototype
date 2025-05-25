using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
//using Unity.Rendering;

using UnityEngine.Experimental.Rendering;

//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Artngame.LUMINA.LimWorks.Rendering.URP.ScreenSpaceReflections
{
    public struct ScreenSpaceReflectionsSettings
    {
        public float scaleHorizon;
        public float StepStrideLength;
        public float MaxSteps;
        public uint Downsample;
        public float MinSmoothness;
    }
    [ExecuteAlways]
    public class LimSSR : ScriptableRendererFeature
    {
        public enum RaytraceModes
        {
            LinearTracing = 0,
            HiZTracing = 1,
        }
        public static ScreenSpaceReflectionsSettings GetSettings()
        {
            return new ScreenSpaceReflectionsSettings()
            {
                scaleHorizon = ssrFeatureInstance.Settings.scaleHorizon,
                Downsample = ssrFeatureInstance.Settings.downSample,
                MaxSteps = ssrFeatureInstance.Settings.maxSteps,
                MinSmoothness = ssrFeatureInstance.Settings.minSmoothness,
                StepStrideLength = ssrFeatureInstance.Settings.stepStrideLength,
            };
        }
        public static bool Enabled { get; set; } = true;
        public static void SetSettings(ScreenSpaceReflectionsSettings screenSpaceReflectionsSettings)
        {
            ssrFeatureInstance.Settings = new SSRSettings()
            {
                scaleHorizon = Mathf.Clamp(screenSpaceReflectionsSettings.scaleHorizon, 0, 1),
                stepStrideLength = Mathf.Clamp(screenSpaceReflectionsSettings.StepStrideLength, 0.001f, float.MaxValue),
                maxSteps = Mathf.Max(screenSpaceReflectionsSettings.MaxSteps, 8),
                downSample = (uint)Mathf.Clamp(screenSpaceReflectionsSettings.Downsample, 0, 2),
                minSmoothness = Mathf.Clamp01(screenSpaceReflectionsSettings.MinSmoothness),
                SSRShader = ssrFeatureInstance.Settings.SSRShader,
                SSR_Instance = ssrFeatureInstance.Settings.SSR_Instance,
                tracingMode = TracingMode
            };
        }
        public static RaytraceModes TracingMode
        {
            get { return ssrFeatureInstance.Settings.tracingMode; }
            set { ssrFeatureInstance.Settings.tracingMode = value; }
        }

        /// <summary>
        /// //////////////////////////////////////////////// SSR PASS
        /// </summary>

        [ExecuteAlways]
        internal class SsrPass : ScriptableRenderPass
        {



#if UNITY_2023_3_OR_NEWER
            /// <summary>
            /// ///////// GRAPH
            /// </summary>
            // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
            private class PassData
            {    //v0.1               
                internal TextureHandle src;
                public Material BlitMaterial { get; set; }

                //UNSAFE
                public TextureHandle colorTargetHandleA;
                public UniversalCameraData cameraData;
                public void Init(ContextContainer frameData, IUnsafeRenderGraphBuilder builder = null)
                {
                    cameraData = frameData.Get<UniversalCameraData>();
                }
            }
            private Material m_BlitMaterial;

            TextureHandle tmpBuffer1A;

            RTHandle _handleA;
            TextureHandle tmpBuffer2A;

            RTHandle _handleTAART;
            TextureHandle _handleTAA;

            Camera currentCamera;
            float prevDownscaleFactor;//v0.1
            public Material blitMaterial = null;

            // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (Camera.main != null)
                {
                    m_BlitMaterial = Settings.SSR_Instance;

                    Camera.main.depthTextureMode = DepthTextureMode.Depth;

                    UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                    if (cameraData != null)
                    {
                        Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
                        //Matrix4x4 projectionMatrix = cameraData.GetGPUProjectionMatrix(0);
                        Matrix4x4 projectionMatrix = cameraData.camera.projectionMatrix;
                        projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);

#if UNITY_EDITOR
                        if (cameraData.isSceneViewCamera)
                        {
                            Settings.SSR_Instance.SetFloat("_RenderScale", 1);
                        }
                        else
                        {
                            Settings.SSR_Instance.SetFloat("_RenderScale", cameraData.renderScale);
                        }
#else
            //Settings.SSR_Instance.SetFloat("_RenderScale", renderingData.cameraData.renderScale);
            Settings.SSR_Instance.SetFloat("_RenderScale", cameraData.renderScale);
#endif
                        Settings.SSR_Instance.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                        Settings.SSR_Instance.SetMatrix("_ProjectionMatrix", projectionMatrix);
                        Settings.SSR_Instance.SetMatrix("_InverseViewMatrix", viewMatrix.inverse);
                        Settings.SSR_Instance.SetMatrix("_ViewMatrix", viewMatrix);

                        Settings.SSR_Instance.SetFloat("scaleHorizon", Settings.scaleHorizon);
                    }

                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                    if (Camera.main != null && cameraData.camera != Camera.main)
                    {
                        return;
                    }

                    //CONFIGURE
                    //cameraTextureDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                    //cameraTextureDescriptor.mipCount = 8;
                    //cameraTextureDescriptor.autoGenerateMips = true;
                    //cameraTextureDescriptor.useMipMap = true;

                    //reflectionMapID = Shader.PropertyToID("_ReflectedColorMap");
                    float downScaler = Scale;
                    downScaledX = (ScreenWidth / (float)(downScaler));
                    downScaledY = (ScreenHeight / (float)(downScaler));
                    Settings.SSR_Instance.SetVector("_TargetResolution", new Vector4(ScreenWidth, ScreenHeight, 0, 0));
                    //cmd.GetTemporaryRT(reflectionMapID, Mathf.CeilToInt(downScaledX), Mathf.CeilToInt(downScaledY), 0, FilterMode.Point, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default, 1, false);

                    //tempRenderID = Shader.PropertyToID("_TempTex");
                    //cmd.GetTemporaryRT(tempRenderID, cameraTextureDescriptor, FilterMode.Trilinear);

                    desc.msaaSamples = 1;
                    desc.depthBufferBits = 0;
                    int rtW = desc.width;
                    int rtH = desc.height;
                    int xres = (int)(rtW / ((float)1));
                    int yres = (int)(rtH / ((float)1));
                    if (_handleA == null || _handleA.rt.width != xres || _handleA.rt.height != yres)
                    {
                        //_handleA = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D);
                        _handleA = RTHandles.Alloc(Mathf.CeilToInt(downScaledX), Mathf.CeilToInt(downScaledY), colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                            dimension: TextureDimension.Tex2D);
                    }
                    tmpBuffer2A = renderGraph.ImportTexture(_handleA);//reflectionMapID                            

                    if (_handleTAART == null || _handleTAART.rt.width != xres || _handleTAART.rt.height != yres || _handleTAART.rt.useMipMap == false)
                    {
                        //_handleTAART.rt.DiscardContents();
                        //_handleTAART.rt.useMipMap = true;// = 8;
                        //_handleTAART.rt.autoGenerateMips = true;                       
                        _handleTAART = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D,
                            useMipMap: true, autoGenerateMips: true
                            );
                        _handleTAART.rt.wrapMode = TextureWrapMode.Clamp;
                        _handleTAART.rt.filterMode = FilterMode.Trilinear;
                        //Debug.Log(_handleTAART.rt.mipmapCount);
                    }
                    _handleTAA = renderGraph.ImportTexture(_handleTAART); //_TempTex


                    tmpBuffer1A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true);

                    TextureHandle sourceTexture = resourceData.activeColorTexture;

                    if (!Settings.useUnsafePass)
                    {
                        string passName = "BLIT1 Keep Source";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(passData.src, AccessFlags.Read);
                            builder.SetRenderAttachment(_handleTAA, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = m_BlitMaterial;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                ExecuteBlitPass(data, context, 4, passData.src));
                        }

                        //calculate reflection
                        //if (Settings.tracingMode == RaytraceModes.HiZTracing)
                        //{
                        //    commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 2);
                        //}
                        //else
                        //{
                        //    commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 0);
                        //}      


                        //calculate reflection
                        if (Settings.tracingMode == RaytraceModes.HiZTracing)
                        {
                            //commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 2);
                            passName = "DO SSR";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {

                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                //builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                                builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                                builder.AllowPassCulling(false);
                                passData.BlitMaterial = m_BlitMaterial;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                   // ExecuteBlitPass(data, context, LUMINA.Pass.GetCameraDepthTexture, tmpBuffer1A));
                                   ExecuteBlitPassNOTEX(data, context, 7, cameraData));
                            }
                        }
                        else
                        {
                            //commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 0);
                            passName = "DO SSR";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {

                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                //builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                                builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                                builder.AllowPassCulling(false);
                                passData.BlitMaterial = m_BlitMaterial;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                   // ExecuteBlitPass(data, context, LUMINA.Pass.GetCameraDepthTexture, tmpBuffer1A));
                                   ExecuteBlitPassNOTEX(data, context, 5, cameraData));
                            }
                        }



                        ////compose reflection with main texture
                        //commandBuffer.Blit(Source, tempRenderID);
                        //commandBuffer.Blit(tempRenderID, Source, Settings.SSR_Instance, 1);

                        passName = "compose reflection with main texture";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(_handleTAA, AccessFlags.Read);
                            builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                            builder.SetRenderAttachment(tmpBuffer1A, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = m_BlitMaterial;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                               // ExecuteBlitPass(data, context, LUMINA.Pass.GetCameraDepthTexture, tmpBuffer1A));
                               ExecuteBlitPassTWO(data, context, 6, _handleTAA, tmpBuffer2A));
                        }

                        //BLIT FINAL
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler))
                        {
                            passData.BlitMaterial = m_BlitMaterial;
                            // Similar to the previous pass, however now we set destination texture as input and source as output.
                            builder.UseTexture(tmpBuffer1A, AccessFlags.Read);
                            passData.src = tmpBuffer1A;
                            builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);
                            // We use the same BlitTexture API to perform the Blit operation.
                            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        }
                    }
                    else
                    {
                        //UNSAFE PASS
                        string passName = "SSR LIM Unsafe pass";
                        using (var builder = renderGraph.AddUnsafePass<PassData>(passName,
                            out var data))
                        {
                            builder.AllowPassCulling(false);
                            data.Init(frameData, builder);
                            builder.AllowGlobalStateModification(true);
                            //UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
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

                }
            }

            //UNSAFE
            void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
            {
                Source = data.colorTargetHandleA;

                CommandBuffer unsafeCmd = command;
                RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;
                //v1.6
                if (Camera.main != null && data.cameraData.camera == Camera.main)
                {
                    //cmd.Blit(source, source, outlineMaterial, 0);

                    CommandBuffer commandBuffer = command;// CommandBufferPool.Get("Screen space reflections");

                    //calculate reflection
                    if (Settings.tracingMode == RaytraceModes.HiZTracing)
                    {
                        commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 2);
                    }
                    else
                    {
                        commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 0);
                    }

                    //compose reflection with main texture
                     commandBuffer.Blit(Source, tempRenderID);
                     commandBuffer.Blit(tempRenderID, Source, Settings.SSR_Instance, 1);
                   // commandBuffer.Blit(Source, data.colorTargetHandleA, Settings.SSR_Instance, 1);
                }
            }
            void OnCameraSetupA(CommandBuffer cmd, PassData data)
            {

                RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
                int rtW = opaqueDesc.width;
                int rtH = opaqueDesc.height;
                var renderer = data.cameraData.renderer;
                //
                //              base.Configure(cmd, cameraTextureDescriptor);
                //ConfigureInput(ScriptableRenderPassInput.Motion | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Normal);

                opaqueDesc.colorFormat = RenderTextureFormat.DefaultHDR;
                opaqueDesc.mipCount = 8;
                opaqueDesc.autoGenerateMips = true;
                opaqueDesc.useMipMap = true;

                reflectionMapID = Shader.PropertyToID("_ReflectedColorMap");

                float downScaler = Scale;
                downScaledX = (ScreenWidth / (float)(downScaler));
                downScaledY = (ScreenHeight / (float)(downScaler));
                Settings.SSR_Instance.SetVector("_TargetResolution", new Vector4(ScreenWidth, ScreenHeight, 0, 0));
                //Debug.Log(ScreenWidth);

                cmd.GetTemporaryRT(reflectionMapID, Mathf.CeilToInt(downScaledX), Mathf.CeilToInt(downScaledY), 0, FilterMode.Point, 
                    RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default, 1, false);

                tempRenderID = Shader.PropertyToID("_TempTex");
                cmd.GetTemporaryRT(tempRenderID, opaqueDesc, FilterMode.Trilinear);

            }

            static void ExecuteBlitPassTEX9NAME(PassData data, RasterGraphContext context, int pass,
         string texname1, TextureHandle tmpBuffer1,
         string texname2, TextureHandle tmpBuffer2,
         string texname3, TextureHandle tmpBuffer3,
         string texname4, TextureHandle tmpBuffer4,
         string texname5, TextureHandle tmpBuffer5,
         string texname6, TextureHandle tmpBuffer6,
         string texname7, TextureHandle tmpBuffer7,
         string texname8, TextureHandle tmpBuffer8,
         string texname9, TextureHandle tmpBuffer9,
         string texname10, TextureHandle tmpBuffer10
         )
            {
                data.BlitMaterial.SetTexture(texname1, tmpBuffer1);
                data.BlitMaterial.SetTexture(texname2, tmpBuffer2);
                data.BlitMaterial.SetTexture(texname3, tmpBuffer3);
                data.BlitMaterial.SetTexture(texname4, tmpBuffer4);
                data.BlitMaterial.SetTexture(texname5, tmpBuffer5);
                data.BlitMaterial.SetTexture(texname6, tmpBuffer6);
                data.BlitMaterial.SetTexture(texname7, tmpBuffer7);
                data.BlitMaterial.SetTexture(texname8, tmpBuffer8);
                data.BlitMaterial.SetTexture(texname9, tmpBuffer9);
                data.BlitMaterial.SetTexture(texname10, tmpBuffer10);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            //temporal
            static void ExecuteBlitPassTEN(PassData data, RasterGraphContext context, int pass,
                TextureHandle tmpBuffer1, TextureHandle tmpBuffer2, TextureHandle tmpBuffer3,
                string varname1, float var1,
                string varname2, float var2,
                string varname3, Matrix4x4 var3,
                string varname4, Matrix4x4 var4,
                string varname5, Matrix4x4 var5,
                string varname6, Matrix4x4 var6,
                string varname7, Matrix4x4 var7
                )
            {
                data.BlitMaterial.SetTexture("_CloudTex", tmpBuffer1);
                data.BlitMaterial.SetTexture("_PreviousColor", tmpBuffer2);
                data.BlitMaterial.SetTexture("_PreviousDepth", tmpBuffer3);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
                //lastFrameViewProjectionMatrix = viewProjectionMatrix;
                //lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
            }

            static void ExecuteBlitPassTHREE(PassData data, RasterGraphContext context, int pass,
                TextureHandle tmpBuffer1, TextureHandle tmpBuffer2, TextureHandle tmpBuffer3)
            {
                data.BlitMaterial.SetTexture("_ColorBuffer", tmpBuffer1);
                data.BlitMaterial.SetTexture("_PreviousColor", tmpBuffer2);
                data.BlitMaterial.SetTexture("_PreviousDepth", tmpBuffer3);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPass(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1aa);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassNOTEX(PassData data, RasterGraphContext context, int pass, UniversalCameraData cameraData)
            {
                 //Matrix4x4 projectionMatrix = cameraData.GetGPUProjectionMatrix(0);

                

                // data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1aa);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1);// _CloudTexP", tmpBuffer1);
                data.BlitMaterial.SetTexture("_ReflectedColorMap", tmpBuffer2);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO_MATRIX(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2, Matrix4x4 matrix)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1);// _CloudTexP", tmpBuffer1);
                data.BlitMaterial.SetTexture("_CameraDepthCustom", tmpBuffer2);
                data.BlitMaterial.SetMatrix("frustumCorners", matrix);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTEXNAME(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa, string texname)
            {
                data.BlitMaterial.SetTexture(texname, tmpBuffer1aa);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTEX5NAME(PassData data, RasterGraphContext context, int pass,
                string texname1, TextureHandle tmpBuffer1,
                string texname2, TextureHandle tmpBuffer2,
                string texname3, TextureHandle tmpBuffer3,
                string texname4, TextureHandle tmpBuffer4,
                string texname5, TextureHandle tmpBuffer5
                )
            {
                data.BlitMaterial.SetTexture(texname1, tmpBuffer1);
                data.BlitMaterial.SetTexture(texname2, tmpBuffer2);
                data.BlitMaterial.SetTexture(texname3, tmpBuffer3);
                data.BlitMaterial.SetTexture(texname4, tmpBuffer4);
                data.BlitMaterial.SetTexture(texname5, tmpBuffer5);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            // It is static to avoid using member variables which could cause unintended behaviour.
            static void ExecutePass(PassData data, RasterGraphContext rgContext)
            {
                Blitter.BlitTexture(rgContext.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, 3);
            }
            //private Material m_BlitMaterial;
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("After Opaques");
            ////// END GRAPH

#endif





            public RenderTargetIdentifier Source { get; internal set; }
            int reflectionMapID;
            int tempRenderID;

            internal SSRSettings Settings { get; set; }
            float downScaledX;
            float downScaledY;

            internal float RenderScale { get; set; }
            internal float ScreenHeight { get; set; }
            internal float ScreenWidth { get; set; }
            internal float Scale => Settings.tracingMode == RaytraceModes.HiZTracing ? 1 : Settings.downSample + 1;

            //static RenderTexture tempSource;

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.Configure(cmd, cameraTextureDescriptor);
                //ConfigureInput(ScriptableRenderPassInput.Motion | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Normal);

                cameraTextureDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                cameraTextureDescriptor.mipCount = 8;
                cameraTextureDescriptor.autoGenerateMips = true;
                cameraTextureDescriptor.useMipMap = true;

                reflectionMapID = Shader.PropertyToID("_ReflectedColorMap");

                float downScaler = Scale;
                downScaledX = (ScreenWidth / (float)(downScaler));
                downScaledY = (ScreenHeight / (float)(downScaler));
                Settings.SSR_Instance.SetVector("_TargetResolution", new Vector4(ScreenWidth, ScreenHeight, 0, 0));


                cmd.GetTemporaryRT(reflectionMapID, Mathf.CeilToInt(downScaledX), Mathf.CeilToInt(downScaledY), 0, FilterMode.Point, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default, 1, false);

                tempRenderID = Shader.PropertyToID("_TempTex");
                cmd.GetTemporaryRT(tempRenderID, cameraTextureDescriptor, FilterMode.Trilinear);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer commandBuffer = CommandBufferPool.Get("Screen space reflections");

                //calculate reflection
                if (Settings.tracingMode == RaytraceModes.HiZTracing)
                {
                    commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 2);
                }
                else
                {
                    commandBuffer.Blit(Source, reflectionMapID, Settings.SSR_Instance, 0);
                }

                //compose reflection with main texture
                commandBuffer.Blit(Source, tempRenderID);
                commandBuffer.Blit(tempRenderID, Source, Settings.SSR_Instance, 1);
                //commandBuffer.Blit(tempRenderID, Source, Settings.SSR_Instance, 2);
                context.ExecuteCommandBuffer(commandBuffer);

                CommandBufferPool.Release(commandBuffer);
            }
            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(tempRenderID);
                cmd.ReleaseTemporaryRT(reflectionMapID);
            }

        }

        [System.Serializable]
        internal class SSRSettings
        {
            public RaytraceModes tracingMode = RaytraceModes.LinearTracing;
            public float stepStrideLength = .03f;
            public float scaleHorizon = 1;
            public float maxSteps = 128;
            public uint downSample = 0;
            public float minSmoothness = 0.5f;
            public bool reflectSky = true;
            //public float scaleHorizon = 1;
            [HideInInspector] public Material SSR_Instance;
            [HideInInspector] public Shader SSRShader;
            public bool useUnsafePass = false;
        }

        internal SsrPass renderPass = null;
        internal static LimSSR ssrFeatureInstance;
        [SerializeField] SSRSettings Settings = new SSRSettings();

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled || !Enabled)
            {
                return;
            }

            if(!GetMaterial())
            {
                Debug.LogError("Cannot find ssr shader!");
                return;
            }

#if UNITY_2022_1_OR_NEWER
#else
            SetMaterialProperties(in renderingData);
            renderPass.Source = renderer.cameraColorTarget;
#endif
            Settings.SSR_Instance.SetVector("_WorldSpaceViewDir", renderingData.cameraData.camera.transform.forward);

            renderingData.cameraData.camera.depthTextureMode |= (DepthTextureMode.MotionVectors | DepthTextureMode.Depth | DepthTextureMode.DepthNormals);
            float renderscale = renderingData.cameraData.isSceneViewCamera ? 1 : renderingData.cameraData.renderScale;

            renderPass.RenderScale = renderscale;
            renderPass.ScreenHeight = renderingData.cameraData.cameraTargetDescriptor.height;
            renderPass.ScreenWidth = renderingData.cameraData.cameraTargetDescriptor.width;

            Settings.SSR_Instance.SetFloat("stride", Settings.stepStrideLength);
            Settings.SSR_Instance.SetFloat("numSteps", Settings.maxSteps);
            Settings.SSR_Instance.SetFloat("minSmoothness", Settings.minSmoothness);
            Settings.SSR_Instance.SetInt("reflectSky", Settings.reflectSky ? 1 : 0);
#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
            var d = UnityEngine.Rendering.Universal.UniversalRenderPipelineDebugDisplaySettings.Instance.AreAnySettingsActive;
            if (!d)
            {
                renderer.EnqueuePass(renderPass);
            }
#else
            renderer.EnqueuePass(renderPass);
#endif
        }

#if UNITY_2022_1_OR_NEWER
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled || !Enabled)
            {
                return;
            }

            if (!GetMaterial())
            {
                Debug.LogError("Cannot find ssr shader!");
                return;
            }

            SetMaterialProperties(in renderingData);
            renderPass.Source = renderer.cameraColorTargetHandle;
        }
#endif
        //Called from SetupRenderPasses in urp 13+ (2022.1+). called from AddRenderPasses in URP 12 (2021.3)
        void SetMaterialProperties(in RenderingData renderingData)
        {
            var projectionMatrix = renderingData.cameraData.GetGPUProjectionMatrix();
            var viewMatrix = renderingData.cameraData.GetViewMatrix();

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                Settings.SSR_Instance.SetFloat("_RenderScale", 1);
            }
            else
            {
                Settings.SSR_Instance.SetFloat("_RenderScale", renderingData.cameraData.renderScale);
            }
#else
            Settings.SSR_Instance.SetFloat("_RenderScale", renderingData.cameraData.renderScale);
#endif
            Settings.SSR_Instance.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
            Settings.SSR_Instance.SetMatrix("_ProjectionMatrix", projectionMatrix);
            Settings.SSR_Instance.SetMatrix("_InverseViewMatrix", viewMatrix.inverse);
            Settings.SSR_Instance.SetMatrix("_ViewMatrix", viewMatrix);
        }


        private bool GetMaterial()
        {
            if (Settings.SSR_Instance != null)
            {
                return true;
            }

            if (Settings.SSRShader == null)
            {
                Settings.SSRShader = Shader.Find("Hidden/ssr_shader");
                if (Settings.SSRShader == null)
                {
                    return false;
                }
            }

            Settings.SSR_Instance = CoreUtils.CreateEngineMaterial(Settings.SSRShader);

            return Settings.SSR_Instance != null;
        }
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(Settings.SSR_Instance);
        }
        public override void Create()
        {
            ssrFeatureInstance = this;
            renderPass = new SsrPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
                Settings = this.Settings
            };
            GetMaterial();
        }
    }
}
