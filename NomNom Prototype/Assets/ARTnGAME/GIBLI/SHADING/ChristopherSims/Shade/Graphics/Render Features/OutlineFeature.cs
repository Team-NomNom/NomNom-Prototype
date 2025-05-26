using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Artngame.GIBLI
{
    public class OutlineFeature : ScriptableRendererFeature
    {
        class OutlinePass : ScriptableRenderPass
        {


#if UNITY_2023_3_OR_NEWER
            //GRAPH
            //v0.1
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
                string passName = "Outline";
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
                        //var renderContext = ctx.GetRenderContext();//ctx.GetRenderContext();
                        OnCameraSetupA(cmd, data);
                        ExecutePass(cmd, data, ctx);
                    });
                }
            }
            //public static FieldInfo GraphRenderContext = typeof(InternalRenderGraphContext).GetField("renderContext", BindingFlags.NonPublic | BindingFlags.Instance);
            //public static FieldInfo IntRenderGraphContext = typeof(UnsafeGraphContext).GetField("wrappedContext", BindingFlags.NonPublic | BindingFlags.Instance);
            //public static ScriptableRenderContext GetRenderContextA( UnsafeGraphContext unsafeContext)
            //{
            //    return (ScriptableRenderContext)GraphRenderContext.GetValue(GetInternalRenderGraphContext(unsafeContext));
            //}
            //public static InternalRenderGraphContext GetInternalRenderGraphContext(UnsafeGraphContext unsafeContext)
            //{
            //    return (InternalRenderGraphContext)IntRenderGraphContext.GetValue(unsafeContext);
            //}
            //public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
            void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
            {
                //TEST
                CommandBuffer unsafeCmd = command;// CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                //v1.1.0
                if (Camera.main == null)
                {
                    return;
                }              

                if (1==1)
                {
                    CommandBuffer cmd = unsafeCmd;// CommandBufferPool.Get(m_ProfilerTag);
                    RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
                    opaqueDesc.depthBufferBits = 0;

                    //v1.6
                    if (Camera.main != null && data.cameraData.camera == Camera.main)
                    {
                      
                        cmd.Blit(source, source, outlineMaterial, 0);
                       
                    }

                    //RenderFullVolumetricClouds(currentCamera, cmd, opaqueDesc);                        
                }
            }
            public void OnCameraSetupA(CommandBuffer cmd, PassData renderingData)//(CommandBuffer cmd, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
            {                
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                int rtW = opaqueDesc.width;
                int rtH = opaqueDesc.height;
                //int xres = (int)(rtW / ((float)downSample));
                //int yres = (int)(rtH / ((float)downSample));
                //if (_handleA == null || _handleA.rt.width != xres || _handleA.rt.height != yres)
                //{
                //    //Debug.Log("Alloc");
                //    _handleA = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D);
                //}

                var renderer = renderingData.cameraData.renderer;
                //v0.1
                //_handle.Init(settings.textureId);

                //_handle = RTHandles.Alloc(settings.textureId, name: settings.textureId);
                destination = renderingData.colorTargetHandleA;// (settings.destination == BlitFullVolumeNebulaSRP.Target.Color)
                                                               //? renderer.cameraColorTargetHandle //UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget //v0.1
                                                               //: _handle;
                //v0.1
                //source = renderer.cameraColorTarget;
                source = renderingData.colorTargetHandleA;// renderer.cameraColorTargetHandle;
            }
#endif

            /*
            //GRAPH
            /// ///////// GRAPH
            /// </summary>
            // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
            private class PassData
            {    //v0.1               
                internal TextureHandle src;
                public Material BlitMaterial { get; set; }
            }
            private Material m_BlitMaterial;

            TextureHandle tmpBuffer1A;
            TextureHandle tmpBuffer1Aa;

            RTHandle _handleA;
            TextureHandle tmpBuffer2A;

            RTHandle _handleTAART;
            TextureHandle _handleTAA;

            RTHandle _handleTAART2;
            TextureHandle _handleTAA2;

            RTHandle _handleTAART3;
            TextureHandle _handleTAA3;
            RTHandle _handleTAART4;
            TextureHandle _handleTAA4;

            Camera currentCamera;
            float prevDownscaleFactor;//v0.1
                                      //public Material blitMaterial = null;

            int offset = 6;

            // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();                            

                bool useOnlyShafts = false;
                if (useOnlyShafts)
                {
                    //GraphSunShaftsOnly(renderGraph, frameData);
                }
                else
                {
                    GraphOceanis(renderGraph, frameData);
                }

            }//END MAIN GRAPH

            void GraphOceanis(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (Camera.main != null)
                {
                    m_BlitMaterial = outlineMaterial;
                    Camera.main.depthTextureMode = DepthTextureMode.Depth;

                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                    if (Camera.main != null && cameraData.camera != Camera.main)
                    {
                        return;
                    }

                    //CONFIGURE
                    float downScaler = 1;
                    float downScaledX = (desc.width / (float)(downScaler));
                    float downScaledY = (desc.height / (float)(downScaler));

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

                    if (_handleTAART2 == null || _handleTAART2.rt.width != xres || _handleTAART2.rt.height != yres || _handleTAART2.rt.useMipMap == false)
                    {
                        _handleTAART2 = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D,
                            useMipMap: true, autoGenerateMips: true
                            );
                        _handleTAART2.rt.wrapMode = TextureWrapMode.Clamp;
                        _handleTAART2.rt.filterMode = FilterMode.Trilinear;
                    }
                    _handleTAA2 = renderGraph.ImportTexture(_handleTAART2); //_TempTex


                    if (_handleTAART3 == null || _handleTAART3.rt.width != xres || _handleTAART3.rt.height != yres || _handleTAART3.rt.useMipMap == false)
                    {
                        _handleTAART3 = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D,
                            useMipMap: true, autoGenerateMips: true
                            );
                        _handleTAART3.rt.wrapMode = TextureWrapMode.Clamp;
                        _handleTAART3.rt.filterMode = FilterMode.Trilinear;
                    }
                    _handleTAA3 = renderGraph.ImportTexture(_handleTAART3); //_TempTex

                    if (_handleTAART4 == null || _handleTAART4.rt.width != xres || _handleTAART4.rt.height != yres || _handleTAART4.rt.useMipMap == false)
                    {
                        _handleTAART4 = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D,
                            useMipMap: true, autoGenerateMips: true
                            );
                        _handleTAART4.rt.wrapMode = TextureWrapMode.Clamp;
                        _handleTAART4.rt.filterMode = FilterMode.Trilinear;
                    }
                    _handleTAA4 = renderGraph.ImportTexture(_handleTAART4); //_TempTex

                    tmpBuffer1A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true);
                    tmpBuffer1Aa = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1Aa", false);
                    TextureHandle sourceTexture = resourceData.activeColorTexture;

                    ////////////////  SUN SHAFTS  ////////////////////////////////////////////////////////////////////////////////
                    Material sheetSHAFTS = outlineMaterial;
                  
                    string passNameAAaaa = "DO 1aaaa";
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameAAaaa, out var passData))
                    {
                        passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                        desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        builder.UseTexture(passData.src, AccessFlags.Read);
                        builder.SetRenderAttachment(_handleTAA3, 0, AccessFlags.Write);
                        builder.AllowPassCulling(false);
                        passData.BlitMaterial = outlineMaterial;
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //   ExecuteBlitPassCLEAR(data, context, 14, passData.src));// 
                        ExecuteBlitPass(data, context, 10, passData.src));
                    }

                    //string passNameA = "DO 2";
                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA, out var passData))
                    //{
                    //    passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                    //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                    //    builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.SetRenderAttachment(tmpBuffer1A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //    builder.AllowPassCulling(false);
                    //    passData.BlitMaterial = sheetSHAFTS;
                    //    builder.AllowGlobalStateModification(true);
                    //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    //    ExecuteBlitPassCLEAR(data, context, 0, passData.src));
                    //}

                    //string passNameAA = "DO 1";
                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameAA, out var passData))
                    //{
                    //        passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                    //        desc.msaaSamples = 1; desc.depthBufferBits = 0;
                    //        builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //        builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //        builder.SetRenderAttachment(_handleTAA2, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //        builder.AllowPassCulling(false);
                    //        passData.BlitMaterial = sheetSHAFTS;
                    //        builder.AllowGlobalStateModification(true);
                    //        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    //            ExecuteBlitPassTWO2(data, context, 0, passData.src, tmpBuffer1A));
                    //}

                    //string passNameAAaa = "DO 1aaa";
                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameAAaa, out var passData))
                    //{
                    //    passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                    //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                    //    builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.UseTexture(_handleTAA4, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    //builder.UseTexture(_handleTAA3, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    //builder.UseTexture(_handleTAA3, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.SetRenderAttachment(_handleTAA3, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //    builder.AllowPassCulling(false);
                    //    passData.BlitMaterial = sheetSHAFTS;
                    //    //builder.AllowGlobalStateModification(true);
                    //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    //        ExecuteBlitPassTWO2b(data, context, 0, tmpBuffer2A, _handleTAA4));
                    //}
                    //Debug.Log("IN");
                    //BLIT FINAL
                    //cmd.Blit(temp1, renderingData.cameraData.renderer.cameraColorTargetHandle); //v0.1
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler))
                    {
                        passData.BlitMaterial = sheetSHAFTS;
                        // Similar to the previous pass, however now we set destination texture as input and source as output.
                        builder.UseTexture(_handleTAA3, AccessFlags.Read);
                        passData.src = _handleTAA3;
                        builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);
                        builder.AllowGlobalStateModification(true);
                        // We use the same BlitTexture API to perform the Blit operation.
                        builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                    }

                }//END CAMERA CHECK
            }//END FULL OCEANIS GRAPH            

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
             ){
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
                if (data.BlitMaterial == null)
                {
                    Debug.Log("data.BlitMaterial == null");
                }
                Blitter.BlitTexture(context.cmd,
                    data.src,
                    new Vector4(1, 1, 0, 0),
                    data.BlitMaterial,
                    pass);
            }
            static void ExecuteBlitPassCLEAR(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
            {
                Blitter.BlitTexture(context.cmd,
                    data.src,
                    new Vector4(1, 1, 0, 0),
                    data.BlitMaterial,
                    pass);
            }
            static void ExecuteBlitPassA(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
            {
                data.BlitMaterial.SetTexture("_MainTexA", tmpBuffer1aa);
                if (data.BlitMaterial == null)
                {
                    Debug.Log("data.BlitMaterial == null");
                }
                Blitter.BlitTexture(context.cmd,
                    data.src,
                    new Vector4(1, 1, 0, 0),
                    data.BlitMaterial,
                    pass);
            }
            static void ExecuteBlitPassNOTEX(PassData data, RasterGraphContext context, int pass, UniversalCameraData cameraData)
            {
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO2(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1);// _CloudTexP", tmpBuffer1);
                data.BlitMaterial.SetTexture("_Skybox", tmpBuffer2);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO2a(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1);// _CloudTexP", tmpBuffer1);
                data.BlitMaterial.SetTexture("_ColorBuffer", tmpBuffer2);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO2c(PassData data, RasterGraphContext context, int pass)
            {          
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO2b(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle MaskMapA)//, TextureHandle tmpBuffer3)
            {
                data.BlitMaterial.SetTexture("_SourceTex", data.src);
                data.BlitMaterial.SetTexture("_MainTexB", tmpBuffer1);
                data.BlitMaterial.SetTexture("_WaterInterfaceTex", MaskMapA);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTWO(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1);// _CloudTexP", tmpBuffer1);
                data.BlitMaterial.SetTexture("_TemporalAATexture", tmpBuffer2);
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
                Blitter.BlitTexture(rgContext.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, 8);
            }
            //private Material m_BlitMaterial;
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("After Opaques");
            ////// END GRAPH

            */







            private RenderTargetIdentifier source { get; set; }

#if UNITY_2022_1_OR_NEWER
            private RTHandle destination { get; set; } //v0.1
#else
            private RenderTargetHandle destination { get; set; } //v0.1
#endif

            public Material outlineMaterial = null;

#if UNITY_2022_1_OR_NEWER
            public void Setup(RenderTargetIdentifier source, RTHandle destination)//v0.1
            {
                this.source = source;
                this.destination = destination;
                //temporaryColorTexture = RTHandles.Alloc("temporaryColorTexture", name: "temporaryColorTexture"); //v0.1
            }
#else
            RenderTargetHandle temporaryColorTexture; //v0.1
            public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)//v0.1
            {
                this.source = source;
                this.destination = destination;
            }
#endif

            public OutlinePass(Material outlineMaterial)
            {
                this.outlineMaterial = outlineMaterial;
            }

            //v1.5
#if UNITY_2020_2_OR_NEWER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // get a copy of the current camera’s RenderTextureDescriptor
                // this descriptor contains all the information you need to create a new texture
                //RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

                // _handle = RTHandles.Alloc(settings.textureId, name: settings.textureId); //v0.1

                var renderer = renderingData.cameraData.renderer;
#if UNITY_2022_1_OR_NEWER
                destination = renderingData.cameraData.renderer.cameraColorTargetHandle; //UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget //v0.1                          
                source = renderingData.cameraData.renderer.cameraColorTargetHandle; 
#else
                destination = UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget; //v0.1                          
                source = renderingData.cameraData.renderer.cameraColorTarget;
#endif

            }
#endif

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            //public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            //{

            // }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Outline Pass");

                RenderTextureDescriptor opaqueDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDescriptor.depthBufferBits = 0;

                //v1.6
                if (Camera.main != null && renderingData.cameraData.camera == Camera.main)
                {
                    //if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//RenderTargetHandle.CameraTarget) //v0.1
                    //{

                    //temporaryColorTexture = RTHandles.Alloc("temporaryColorTexture", name: "temporaryColorTexture"); //v0.1

                    //cmd.GetTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name), opaqueDescriptor, FilterMode.Point); //v0.1
                    //cmd.Blit( source, temporaryColorTexture, outlineMaterial, 0); //v0.1
                    //cmd.Blit( temporaryColorTexture, destination); //v0.1

#if UNITY_2022_1_OR_NEWER
                    cmd.Blit(source, destination, outlineMaterial, 0); //v0.1
#else
                    cmd.GetTemporaryRT(temporaryColorTexture.id, opaqueDescriptor, FilterMode.Point);
                    Blit(cmd, source, temporaryColorTexture.Identifier(), outlineMaterial, 0);
                    Blit(cmd, temporaryColorTexture.Identifier(), source);
#endif

                    //}
                    //else cmd.Blit( source, destination, outlineMaterial, 0); //v0.1

                    //cmd.ReleaseTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name));//v0.1
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }


            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
#if UNITY_2022_1_OR_NEWER
                
#else
                if (destination == RenderTargetHandle.CameraTarget)
                { //v0.1
                    cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
                }
#endif
            }
        }

        [System.Serializable]
        public class OutlineSettings
        {
            public Material outlineMaterial = null;
        }

        public OutlineSettings settings = new OutlineSettings();
        OutlinePass outlinePass;

#if UNITY_2022_1_OR_NEWER
        RTHandle outlineTexture; //v0.1
#else
        RenderTargetHandle outlineTexture; //v0.1
#endif

        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;//v1.6a
        public override void Create()
        {
            outlinePass = new OutlinePass(settings.outlineMaterial);
            outlinePass.renderPassEvent = renderPassEvent; //RenderPassEvent.AfterRenderingTransparents;//v1.6a

            //
#if UNITY_2022_1_OR_NEWER
            outlineTexture = RTHandles.Alloc("_OutlineTexture", name: "_OutlineTexture"); //v0.1
#else
            outlineTexture.Init("_OutlineTexture"); //v0.1
#endif

        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.outlineMaterial == null)
            {
                Debug.LogWarningFormat("Missing Outline Material");
                return;
            }
            //outlinePass.Setup(renderer.cameraColorTarget, RenderTargetHandle.CameraTarget);//v1.5
            renderer.EnqueuePass(outlinePass);
        }
    }


}
