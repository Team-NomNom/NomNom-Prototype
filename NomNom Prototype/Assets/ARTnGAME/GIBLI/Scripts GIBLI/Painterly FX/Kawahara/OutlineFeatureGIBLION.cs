using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEngine.Experimental.Rendering;

#if UNITY_2023_3_OR_NEWER
//GRAPH
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Artngame.GIBLI
{
    /// <summary>
    /// /////////////////////////////////  MAIN FX
    /// </summary>
    public class OutlineFeatureGIBLION : ScriptableRendererFeature
    {
        class OutlinePass : ScriptableRenderPass
        {

#if UNITY_2023_3_OR_NEWER
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
                        builder.SetRenderAttachment(_handleTAA2, 0, AccessFlags.Write);
                        builder.AllowPassCulling(false);
                        passData.BlitMaterial = outlineMaterial;
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        ExecuteBlitPass(data, context, 2, passData.src));
                    }


                  


                    int effectsChoice = painterlyMaterial.GetInt("effectsChoice");
                    if (effectsChoice == 0)
                    {
                        //                 cmd.Blit(source, destination, outlineMaterial, 0); //v0.1
                        //EFFECTS CHOICE 0
                        string passNameA = "DO 1";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(passData.src, AccessFlags.Read);
                            builder.SetRenderAttachment(_handleTAA3, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = outlineMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassCLEAR(data, context, 3, passData.src));
                        }

                    }//END FX TYPE 0
                    else if (effectsChoice == 1)
                    {
                      

          //            //InitWorkRT(1280, 720);
                        int width = 1280;
                        int height = 720;
                        for (int i = 0; i < workRT.Length; ++i)
                        { 
                            if (workRTGH[i] == null || workRTGH[i].rt.width != xres || workRTGH[i].rt.height != yres || workRTGH[i].rt.useMipMap == false)
                            {
                                workRTGH[i] = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D,
                                    useMipMap: true, autoGenerateMips: true
                                    );
                                workRTGH[i].rt.wrapMode = TextureWrapMode.Clamp;
                                workRTGH[i].rt.filterMode = FilterMode.Trilinear;
                               
                            }
                            workRTG[i] = renderGraph.ImportTexture(workRTGH[i]); //_TempTex
                            //     if (_handleTAART4 == null || _handleTAART4.rt.width != xres || _handleTAART4.rt.height != yres || _handleTAART4.rt.useMipMap == false)
                            //{
                            //    _handleTAART4 = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D,
                            //        useMipMap: true, autoGenerateMips: true
                            //        );
                            //    _handleTAART4.rt.wrapMode = TextureWrapMode.Clamp;
                            //    _handleTAART4.rt.filterMode = FilterMode.Trilinear;
                            //}
                            //_handleTAA4 = renderGraph.ImportTexture(_handleTAART4); //_TempTex

                            //if (workRT[i] == null || workRT[i].width != width || workRT[i].height != height)
                            //{
                            /*
                            if (workRT[i] != null) {
                                workRT[i].Release();
                            }
                            // GetTemporaryMipMap
                            workRT[i] = new RenderTexture(width, height, 24, RENDER_TEXTURE_FORMAT);
                            workRT[i].hideFlags = HideFlags.DontSave;
                            workRT[i].filterMode = FilterMode.Bilinear;
                            workRT[i].useMipMap = true;
                            */
                            // }
                        }

                        ////// 1 BLIT 1 ///////                      cmd.Blit(source, workRT[RT_WORK0]); //v0.1
                        //
                        string passNameA1 = "DO 1";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA1, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(passData.src, AccessFlags.Read);
                            builder.SetRenderAttachment(workRTG[RT_WORK0], 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = outlineMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassCLEAR(data, context, 2, passData.src));
                        }


                        // ------------------- SST() ----------------------
                        ////////// 1 SET TEX 1 ///////////                      painterlyMaterial.SetTexture("_MainTex", workRT[RT_WORK0]);

                        int offset = 37;

                        //RenderSobel(workRT[RT_WORK0], workRT[RT_SOBEL], painterlyMaterial, cmd, 1.0f);
                        //public void RenderSobel(RenderTexture src, RenderTexture dst, Material mat, CommandBuffer cmd, float carryDigit = 1.0f)//RenderTargetIdentifier dst, Material mat, CommandBuffer cmd, float carryDigit = 1.0f)
                        //{
                        float carryDigit = 1.0f;
                        painterlyMaterial.SetFloat("_SobelCarryDigit", carryDigit);
                        //Blit(src, dst, "Sobel3");
                        //cmd.Blit(src, dst, mat, "Sobel3");
                        ////// 1 BLIT 1 ///////                      cmd.Blit(workRT[RT_WORK0], workRT[RT_SOBEL], painterlyMaterial, 17);
                        //
                        string passNameA11 = "DO 11";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA11, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                            builder.UseTexture(workRTG[RT_WORK0], AccessFlags.Read);
                            builder.SetRenderAttachment(workRTG[RT_SOBEL], 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = painterlyMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassMAIN(data, context, 17 + offset, workRTG[RT_WORK0]));
                        }

                        //Blit(cmd, src, dst, mat, 17);// "Sobel3");
                        //Graphics.Blit(cmd, src, dst, mat, 17);                        
                        ////////// 1 SET TEX 1 ///////////                      painterlyMaterial.SetTexture("_RT_SOBEL", workRT[RT_SOBEL]);


                        // 後段のために桁下げを登録しておく
                        painterlyMaterial.SetFloat("_SobelInvCarryDigit", 1.0f / carryDigit);
                        //}                        

                        //painterlyMaterial.SetFloat("_SobelInvCarryDigit", 1.0f / 1.0f); NO
                        //Blit(cmd, workRT[RT_WORK0], workRT[RT_SOBEL], painterlyMaterial, 17); NO

                        GBlur gblur = new GBlur();
                        gblur.BlurSize = 2;  gblur.DomainBias = 1; gblur.DomainVariance = 1; //gblur.DomainWeight = 2;
                        gblur.InvDomainSigma = -1;  gblur.LOD = 0; gblur.Mean = 1;  //gblur.OffsetX = 0; //gblur.OffsetY = 0.1f;
                        gblur.SampleLen = 1;  gblur.TileSize = 2; gblur.UsePreCalc = false;

         //             //UpdateGBlur(gblur, painterlyMaterial);
                        //public void UpdateGBlur(GBlur gb, Material mat)
                        //{
                            //if (!needsUpdate) { return; }
                            //InsGBlur blurparams = new InsGBlur();
                            painterlyMaterial.SetInt("_GBlurLOD", gblur.LOD);
                            painterlyMaterial.SetInt("_GBlurTileSize", gblur.TileSize);
                            painterlyMaterial.SetInt("_GBlurSampleLen", gblur.SampleLen);
                            painterlyMaterial.SetInt("_GBlurSize", gblur.BlurSize);
                            painterlyMaterial.SetFloat("_GBlurInvDomainSigma", gblur.InvDomainSigma);
                            painterlyMaterial.SetFloat("_GBlurDomainVariance", gblur.DomainVariance);
                            painterlyMaterial.SetFloat("_GBlurDomainBias", gblur.DomainBias);
                            painterlyMaterial.SetFloat("_GBlurMean", gblur.Mean);

                        // if (!gblur.UsePreCalc) { return; }
                        if (gblur.UsePreCalc)
                        {
                            painterlyMaterial.SetFloatArray("_GBlurOffsetX", gblur.OffsetX);
                            painterlyMaterial.SetFloatArray("_GBlurOffsetY", gblur.OffsetY);
                            painterlyMaterial.SetFloatArray("_GBlurDomainWeight", gblur.DomainWeight);
                        }
                       // }

        //              //RenderGBlur(workRT[RT_SOBEL], workRT[RT_WORK7], gblur, painterlyMaterial, cmd);
                        //public void RenderGBlur(RenderTexture src, RenderTexture dst, GBlur gb, Material mat, CommandBuffer cmd)
                        //{
                            // Blit(src, dst, gb.UsePreCalc ? "GBlur2" : "GBlur");
                            bool UsePreCalc = false;
                        ////// 1 BLIT 1 ///////                          cmd.Blit(workRT[RT_SOBEL], workRT[RT_WORK7], painterlyMaterial, UsePreCalc ? 19 : 18);
                        //}
                        //
                        string passNameA111 = "DO 111";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA111, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                            builder.UseTexture(workRTG[RT_SOBEL], AccessFlags.Read);
                            builder.SetRenderAttachment(workRTG[RT_WORK7], 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = painterlyMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassSOBEL(data, context, UsePreCalc ? 19 + offset : 18 + offset, workRTG[RT_SOBEL]));
                        }

                        //UpdateTFM();
                        //                //RenderTFM(workRT[RT_WORK7], painterlyMaterial, cmd);//writes to workRT[RT_TFM] --- PINK
                        //public void RenderTFM(RenderTexture src, Material mat, CommandBuffer cmd) { RenderTFM(src, workRT[RT_TFM], mat, cmd); }
                        //public void RenderTFM(RenderTexture src, RenderTexture dst, Material mat, CommandBuffer cmd)
                        //{
                        //Blit(src, dst, "TFM");
                        ////// 1 BLIT 1 ///////                      cmd.Blit(workRT[RT_WORK7], workRT[RT_TFM], painterlyMaterial, 14);
                        //
                        string passNameA1111 = "DO 1111";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA1111, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                            builder.UseTexture(workRTG[RT_WORK7], AccessFlags.Read);
                            builder.SetRenderAttachment(workRTG[RT_TFM], 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = painterlyMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassMAIN(data, context, 14 + offset, workRTG[RT_WORK7]));
                        }

                        // 後段のためにRTを登録しておく
                        ////////// 1 SET TEX 1 ///////////                      painterlyMaterial.SetTexture("_RT_TFM", workRT[RT_TFM]);
                        //}

                        //LIC
                        LIC lic = new LIC();
                        lic.Scale = 1;// debug.LICScale;
                        lic.MaxLen = 1;// debug.LICSigma;           
                        lic.Variance = 1.0f / (lic.MaxLen * lic.MaxLen * 2.0f);

                        // RenderLIC(workRT[RT_OUTLINE], painterlyMaterial, cmd);
                        //public void RenderLIC(RenderTexture dst, Material mat, CommandBuffer cmd)
                        //{
                        //Blit(RT_TFM, dst, "LIC");
                        ////// 1 BLIT 1 ///////                           cmd.Blit(workRT[RT_TFM], workRT[RT_OUTLINE], painterlyMaterial, 15);
                        string passNameA11111 = "DO 11111";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA11111, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                            builder.UseTexture(workRTG[RT_TFM], AccessFlags.Read);
                            builder.SetRenderAttachment(workRTG[RT_OUTLINE], 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = painterlyMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassNAMED(data, context, 15 + offset, workRTG[RT_TFM], "_RT_TFM"));
                        }

                        //}
                        //public void RenderLIC(int dst, Material mat, CommandBuffer cmd)
                        //{
                        //    RenderLIC(workRT[dst], mat, cmd);
                        //}

                        AKF akfP = new AKF();
                        InsAKF akfParams = new InsAKF();
                        akfParams.CenterOverlap = 1;
                        akfParams.DefaultParameters = false;
                        akfParams.MaskRadiusRatio = 0.5f;
                        akfParams.Radius = 1;
                        akfParams.Sharpness = 1;
                        akfParams.SideOverlap = 0.1f;
                        akfP.Set(akfParams);
                        ////////// 1 SET TEX 1 ///////////                      painterlyMaterial.SetTexture("_RT_ORIG", workRT[RT_WORK0]);
                        ////// 1 BLIT 1 ///////                      cmd.Blit(workRT[RT_OUTLINE], workRT[RT_WORK3], painterlyMaterial, 8);//v0.1
                        string passNameA11a = "DO 11a";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA11a, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                            builder.UseTexture(workRTG[RT_WORK0], AccessFlags.Read);
                            //builder.SetRenderAttachment(workRTG[RT_WORK3], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                            builder.SetRenderAttachment(_handleTAA3, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = painterlyMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassTWOGIBLI(data, context, 8 + offset, workRTG[RT_OUTLINE], workRTG[RT_WORK0], "_RT_ORIG"));
                        }

                        //Debug.Log("IN1");
                        ////// 1 BLIT 1 ///////                      cmd.Blit(workRT[RT_WORK3], source);//AKF //v0.1
                        //string passNameA11b = "DO 11b";
                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA11b, out var passData))
                        //{
                        //    passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                        //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        //    //builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.UseTexture(workRTG[RT_WORK3], IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(_handleTAA3, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    builder.AllowPassCulling(false);
                        //    passData.BlitMaterial = painterlyMaterial;
                        //    builder.AllowGlobalStateModification(true);
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //    ExecuteBlitPassMAIN(data, context, 17 + offset, workRTG[RT_WORK3]));
                        //}

                        //string passNameAaa = "DO 1aa";
                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameAaa, out var passData))
                        //{
                        //    passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                        //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        //    builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(_handleTAA3, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    builder.AllowPassCulling(false);
                        //    passData.BlitMaterial = outlineMaterial;
                        //    builder.AllowGlobalStateModification(true);
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //    ExecuteBlitPassCLEAR(data, context, 2, passData.src));
                        //}

                    }
                    else
                    {
                        string passNameA = "DO 1";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passNameA, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture; //SOURCE TEXTURE
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(passData.src, AccessFlags.Read);
                            builder.SetRenderAttachment(_handleTAA3, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = outlineMaterial;
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassCLEAR(data, context, 3, passData.src));
                        }
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
            static void ExecuteBlitPassMAIN(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
            {
                Blitter.BlitTexture(context.cmd,
                    tmpBuffer1aa,
                    new Vector4(1, 1, 0, 0),
                    data.BlitMaterial,
                    pass);
            }
            static void ExecuteBlitPassSOBEL(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
            {
                data.BlitMaterial.SetTexture("_RT_SOBEL", tmpBuffer1aa);
                Blitter.BlitTexture(context.cmd,
                    data.src,
                    new Vector4(1, 1, 0, 0),
                    data.BlitMaterial,
                    pass);
            }
            static void ExecuteBlitPassNAMED(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa, string name)
            {
                data.BlitMaterial.SetTexture(name, tmpBuffer1aa);
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

            static void ExecuteBlitPassTWOGIBLI(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2, string name)
            {
                data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1);// _CloudTexP", tmpBuffer1);
                data.BlitMaterial.SetTexture(name, tmpBuffer2);
                Blitter.BlitTexture(context.cmd, tmpBuffer1, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
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
                Blitter.BlitTexture(rgContext.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, 1);
            }
            //private Material m_BlitMaterial;
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("After Opaques");
            ////// END GRAPH
           
            //GRAPH
            private TextureHandle[] workRTG = new TextureHandle[RENDER_TEXTURE_COUNT];
            private RTHandle[] workRTGH = new RTHandle[RENDER_TEXTURE_COUNT];
#endif





            private RenderTargetIdentifier source { get; set; }

#if UNITY_2022_1_OR_NEWER
            private RTHandle destination { get; set; } //v0.1
#else
            private RenderTargetHandle destination { get; set; } //v0.1
#endif
            public Material outlineMaterial = null;
            public Material painterlyMaterial = null;

            //RTHandle temporaryColorTexture; //v0.1

#if UNITY_2022_1_OR_NEWER
            public void Setup(RenderTargetIdentifier source, RTHandle destination) //v0.1
            {
#else
            RenderTargetHandle temporaryColorTexture; //v0.1
            public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination) //v0.1
            {
#endif

#if UNITY_2020_2_OR_NEWER
                ConfigureInput(ScriptableRenderPassInput.Color); //v0.2
#else
               // GetMaterial();
#endif


                this.source = source;
                this.destination = destination;


               // InitWorkRT(source);
               
               
            }

            

            public OutlinePass(Material outlineMaterial, Material painterlyMaterial)
            {
                this.outlineMaterial = outlineMaterial;
                this.painterlyMaterial = painterlyMaterial;


            }
#if UNITY_2020_2_OR_NEWER
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }
#if UNITY_2022_1_OR_NEWER

#else
                if (destination == RenderTargetHandle.CameraTarget) //v0.1
                    cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
#endif

                //cmd.ReleaseTemporaryRT(CavityTexture);
            }
#endif



            //v1.5
#if UNITY_2020_2_OR_NEWER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // get a copy of the current camera’s RenderTextureDescriptor
                // this descriptor contains all the information you need to create a new texture
                //RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                
                //v0.1
                var renderer = renderingData.cameraData.renderer;
#if UNITY_2022_1_OR_NEWER
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;//renderer.cameraColorTarget;//v0.1
                //RenderTargetHandle.CameraTarget
                destination = renderingData.cameraData.renderer.cameraColorTargetHandle;//RenderTargetHandle.CameraTarget;//v0.1                
#else
                source = renderingData.cameraData.renderer.cameraColorTarget;//renderer.cameraColorTarget;//v0.1
                //RenderTargetHandle.CameraTarget
                destination = RenderTargetHandle.CameraTarget;//v0.1
#endif

            }
#endif




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
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Outline Pass");

                RenderTextureDescriptor opaqueDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDescriptor.depthBufferBits = 0;

                //v1.6
                if (Camera.main != null && renderingData.cameraData.camera == Camera.main)
                {

#if UNITY_2022_1_OR_NEWER
                    if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//RenderTargetHandle.CameraTarget) //v0.1
                    {
#else
                    if (destination == RenderTargetHandle.CameraTarget) //v0.1
                    {
#endif
                        int effectsChoice = painterlyMaterial.GetInt("effectsChoice");
                        if (effectsChoice == 0)
                        {

#if UNITY_2022_1_OR_NEWER
                            cmd.Blit( source, destination, outlineMaterial, 0); //v0.1
#else
                            //cmd.GetTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name), opaqueDescriptor, FilterMode.Point);//v0.1
                            //cmd.Blit( source, temporaryColorTexture, outlineMaterial, 0); //v0.1
                            //cmd.Blit( temporaryColorTexture, source); //v0.1
                            cmd.GetTemporaryRT(temporaryColorTexture.id, opaqueDescriptor, FilterMode.Point);
                            Blit(cmd, source, temporaryColorTexture.Identifier(), outlineMaterial, 0);
                            Blit(cmd, temporaryColorTexture.Identifier(), source);
#endif

                        }//END FX TYPE 0
                        else
                        {

                            InitWorkRT(1280, 720);
                            cmd.Blit(source, workRT[RT_WORK0]); //v0.1
                            // ------------------- SST() ----------------------
                            painterlyMaterial.SetTexture("_MainTex", workRT[RT_WORK0]);
                            RenderSobel(workRT[RT_WORK0], workRT[RT_SOBEL], painterlyMaterial, cmd, 1.0f);

                            // 後段のために桁下げを登録しておく
                            //painterlyMaterial.SetFloat("_SobelInvCarryDigit", 1.0f / 1.0f);
                            //Blit(cmd, workRT[RT_WORK0], workRT[RT_SOBEL], painterlyMaterial, 17);

                            GBlur gblur = new GBlur();
                            gblur.BlurSize = 2;
                            gblur.DomainBias = 1;
                            gblur.DomainVariance = 1;
                            //gblur.DomainWeight = 2;
                            gblur.InvDomainSigma = -1;
                            gblur.LOD = 0;
                            gblur.Mean = 1;
                            //gblur.OffsetX = 0;
                            //gblur.OffsetY = 0.1f;
                            gblur.SampleLen = 1;
                            gblur.TileSize = 2;
                            gblur.UsePreCalc = false;

                            UpdateGBlur(gblur, painterlyMaterial);
                            RenderGBlur(workRT[RT_SOBEL], workRT[RT_WORK7], gblur, painterlyMaterial, cmd);

                            //UpdateTFM();
                            RenderTFM(workRT[RT_WORK7], painterlyMaterial, cmd);//writes to workRT[RT_TFM] --- PINK

                            //LIC
                            LIC lic = new LIC();
                            lic.Scale = 1;// debug.LICScale;
                            lic.MaxLen = 1;// debug.LICSigma;           
                            lic.Variance = 1.0f / (lic.MaxLen * lic.MaxLen * 2.0f);
                            // UpdateLIC(lic, painterlyMaterial);
                            //Blit(cmd, source, workRT[RT_WORK5]);
                            //painterlyMaterial.SetTexture("_RT_TFM", workRT[RT_WORK0]);//painterlyMaterial.SetTexture("_RT_MASK", workRT[RT_WORK5]);
                            RenderLIC(workRT[RT_OUTLINE], painterlyMaterial, cmd);

                            //AKF //pass 8
                            //akf.Set(pe.AKFParameters);
                            //shader.UpdateAKF(akf);
                            //shader.RenderAKF(shader.RT_WORK0, dst);
                            AKF akfP = new AKF();
                            InsAKF akfParams = new InsAKF();
                            akfParams.CenterOverlap = 1;
                            akfParams.DefaultParameters = false;
                            akfParams.MaskRadiusRatio = 0.5f;
                            akfParams.Radius = 1;
                            akfParams.Sharpness = 1;
                            akfParams.SideOverlap = 0.1f;
                            akfP.Set(akfParams);
                            //UpdateAKF(akfP, painterlyMaterial);
                            //    painterlyMaterial.SetTexture("_RT_TFM", workRT[RT_TFM]);
                            //    painterlyMaterial.SetTexture("_RT_ORIG", workRT[RT_OUTLINE]);
                            //Blit(cmd, source, workRT[RT_WORK4]);
                            painterlyMaterial.SetTexture("_RT_ORIG", workRT[RT_WORK0]);
                            //painterlyMaterial.SetTexture("_RT_TFM", workRT[RT_WORK4]);
                            cmd.Blit( workRT[RT_OUTLINE], workRT[RT_WORK3], painterlyMaterial, 8);//v0.1



                 //           cmd.GetTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name), opaqueDescriptor, FilterMode.Point);//v0.1
                            //Blit(cmd, source, temporaryColorTexture.Identifier(), painterlyMaterial, 2);

                            //       Blit(cmd, source, temporaryColorTexture.Identifier(), outlineMaterial, 0);
                            //       Blit(cmd, temporaryColorTexture.Identifier(), source);

                            //  Blit(cmd, source, workRT[RT_WORK4], outlineMaterial, 0);
                            //Blit(cmd, workRT[RT_WORK4], source);

                            // Blit(cmd, source, workRT[RT_WORK4]);
                            //  painterlyMaterial.SetTexture("_RT_TFM", workRT[RT_WORK4]);
                            // Blit(cmd, source, workRT[RT_WORK4]);
                            // painterlyMaterial.SetTexture("_RT_TFM", workRT[RT_OUTLINE]);
                            // painterlyMaterial.SetTexture("_RT_ORIG", workRT[RT_WORK4]);
                            // Blit(cmd, workRT[RT_WORK4], source, painterlyMaterial, 8);
                            // Blit(cmd, workRT[RT_OUTLINE], source);

                            //TEST
                            //Blit(cmd, workRT[RT_SOBEL], source);//SOBEL 
                            //Blit(cmd, workRT[RT_WORK0], source);//GBlur
                            //Blit(cmd, workRT[RT_TFM], source);//TFM - PINK
                            //Blit(cmd, workRT[RT_OUTLINE], source);//LIC
                            //Blit(cmd, workRT[RT_WORK2], source);//AKF
                            cmd.Blit( workRT[RT_WORK3], source);//AKF //v0.1

                            //Blit(cmd, workRT[RT_WORK7], source);//AKF
                        }//END FX TYPE 1
                    }
                    //else Blit(cmd, source, destination.Identifier(), outlineMaterial, 0);
                }//v1.6

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
#if UNITY_2022_1_OR_NEWER
           
#else
                if (destination == RenderTargetHandle.CameraTarget) //v0.1
                 cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
#endif
            }






            //////////////////////// PAINTERLY /////////////////////////
            ////// <summary>
            /// /////// PAINTERLY
            private void InitWorkRT(int width, int height)
            {
                for (int i = 0; i < workRT.Length; ++i)
                {
                    if (workRT[i] == null || workRT[i].width != width || workRT[i].height != height)
                    {
                        if (workRT[i] != null) { workRT[i].Release(); }
                        // GetTemporaryはMipMapが使えないのでnewで確保する
                        // （MipMapが必要な処理だけ別途RTを確保した方が負荷は減る）
                        workRT[i] = new RenderTexture(width, height, 24, RENDER_TEXTURE_FORMAT);
                        workRT[i].hideFlags = HideFlags.DontSave;
                        workRT[i].filterMode = FilterMode.Bilinear;
                        workRT[i].useMipMap = true;
                        //workRB[i] = workRT[i].colorBuffer;
                        //SetTexture(warkRTName[i], workRT[i]);
                    }
                }
                //Graphics.SetRenderTarget(workRB, workRT[0].depthBuffer);
                //SetTexture("_MainTex", src);
                //SetTexture("_RT_ORIG", src);
                //Blit(src, "Entry");
            }
            /// </summary>
            /// <param name="outlineMaterial"></param>
            /// <param name="painterlyMaterial"></param>
            private const int RENDER_TEXTURE_COUNT = 8;


            private RenderTexture[] workRT = new RenderTexture[RENDER_TEXTURE_COUNT];


           

            public readonly int RT_WORK0 = 0, RT_ORIG = 1, RT_WORK2 = 2, RT_WORK3 = 3;
            public readonly int RT_WORK4 = 4, RT_WORK5 = 5, RT_WORK6 = 6, RT_WORK7 = 7;
            public readonly int RT_TFM = 2;
            public readonly int RT_SOBEL = 3;
            public readonly int RT_OUTLINE = 4;
            public readonly int RT_SNOISE = 6;
            public readonly int RT_FNOISE = 7;
            public readonly int RT_SBR_HSV = 0;
            public readonly int RT_LERP0 = 5;
            public readonly int RT_LERP1 = 6;
            public readonly int RT_LERP2 = 7;
            public RenderTexture GetRT(int index) { return workRT[index]; }
            // ARGBFloatはモバイルでunsupportedのエラーが出るためhalfを使う
            private readonly RenderTextureFormat RENDER_TEXTURE_FORMAT = RenderTextureFormat.ARGBHalf;
            // halfの精度は–60000～60000で小数点以下約3桁。前段のバッファは桁をずらして精度を上げる
            private readonly float CARRY_DIGIT = 10000.0f;
            public void RenderSobel(RenderTexture src, RenderTexture dst, Material mat, CommandBuffer cmd, float carryDigit = 1.0f)//RenderTargetIdentifier dst, Material mat, CommandBuffer cmd, float carryDigit = 1.0f)
            {
                // 桁上げして精度を高める
                mat.SetFloat("_SobelCarryDigit", carryDigit);

                // Blit(src, dst, "Sobel3");
                //cmd.Blit(src, dst, mat, "Sobel3");
                cmd.Blit(src, dst, mat, 17);
                //Blit(cmd, src, dst, mat, 17);// "Sobel3");
                //Graphics.Blit(cmd, src, dst, mat, 17);

                // 後段のためにRTを登録しておく
                mat.SetTexture("_RT_SOBEL", workRT[RT_SOBEL]);
                // 後段のために桁下げを登録しておく
                mat.SetFloat("_SobelInvCarryDigit", 1.0f / carryDigit);
            }
            public void RenderSobel(RenderTexture src, int dst, Material mat, CommandBuffer cmd) { RenderSobel(src, workRT[dst], mat, cmd, CARRY_DIGIT); }

            public void UpdateGBlur(GBlur gb, Material mat)
            {
                //if (!needsUpdate) { return; }
                //InsGBlur blurparams = new InsGBlur();
                mat.SetInt("_GBlurLOD", gb.LOD);
                mat.SetInt("_GBlurTileSize", gb.TileSize);
                mat.SetInt("_GBlurSampleLen", gb.SampleLen);
                mat.SetInt("_GBlurSize", gb.BlurSize);
                mat.SetFloat("_GBlurInvDomainSigma", gb.InvDomainSigma);
                mat.SetFloat("_GBlurDomainVariance", gb.DomainVariance);
                mat.SetFloat("_GBlurDomainBias", gb.DomainBias);
                mat.SetFloat("_GBlurMean", gb.Mean);

                if (!gb.UsePreCalc) { return; }
                mat.SetFloatArray("_GBlurOffsetX", gb.OffsetX);
                mat.SetFloatArray("_GBlurOffsetY", gb.OffsetY);
                mat.SetFloatArray("_GBlurDomainWeight", gb.DomainWeight);
            }
            public void RenderGBlur(RenderTexture src, RenderTexture dst, GBlur gb, Material mat, CommandBuffer cmd)
            {
                // Blit(src, dst, gb.UsePreCalc ? "GBlur2" : "GBlur");
                bool UsePreCalc = false;
                cmd.Blit(src, dst, mat, UsePreCalc ? 19 : 18);
            }
            public void RenderGBlur(RenderTexture src, int dst, GBlur gb, Material mat, CommandBuffer cmd) { RenderGBlur(src, workRT[dst], gb, mat, cmd); }

            public void RenderTFM(RenderTexture src, RenderTexture dst, Material mat, CommandBuffer cmd)
            {
                //Blit(src, dst, "TFM");
                cmd.Blit(src, dst, mat, 14);
                // 後段のためにRTを登録しておく
                mat.SetTexture("_RT_TFM", workRT[RT_TFM]);
            }
            public void RenderTFM(RenderTexture src, Material mat, CommandBuffer cmd) { RenderTFM(src, workRT[RT_TFM], mat, cmd); }

            private void SST()
            {
                //RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
                //UpdateGBlur(gblur);
                //RenderGBlur(shader.RT_SOBEL, shader.RT_WORK0, gblur);

                //UpdateTFM();
                //RenderTFM(shader.RT_WORK0);
            }

            public void LIC(RenderTexture dst, Material mat, CommandBuffer cmd)
            {
                LIC lic = new LIC();

                lic.Scale = 1;// debug.LICScale;
                lic.MaxLen = 1;// debug.LICSigma;           
                lic.Variance = 1.0f / (lic.MaxLen * lic.MaxLen * 2.0f);

                SST();
                UpdateLIC(lic, mat);
                RenderLIC(dst, mat, cmd);
            }

            public void UpdateLIC(LIC lic, Material mat)
            {
                //if (!needsUpdate) { return; }
                mat.SetFloat("_LICScale", lic.Scale);
                mat.SetFloat("_LICMaxLen", lic.MaxLen);
                mat.SetFloat("_LICVariance", lic.Variance);
            }

            public void RenderLIC(RenderTexture dst, Material mat, CommandBuffer cmd)
            {
                //Blit(RT_TFM, dst, "LIC");
                cmd.Blit(workRT[RT_TFM], dst, mat, 15);
            }
            public void RenderLIC(int dst, Material mat, CommandBuffer cmd)
            {
                RenderLIC(workRT[dst], mat, cmd);
            }
            public void UpdateAKF(AKF akf, Material mat)
            {
                //if (!needsUpdate) { return; }

                mat.SetFloat("_AKFRadius", akf.Radius);
                mat.SetFloat("_AKFMaskRadius", akf.MaskRadius);
                mat.SetFloat("_AKFSharpness", akf.Sharpness);
                mat.SetInt("_AKFSampleStep", akf.SampleStep);
                mat.SetFloat("_AKFOverlapX", akf.OverlapX);
                mat.SetFloat("_AKFOverlapY", akf.OverlapY);
            }
            //public void RenderAKF(int src, RT dst) { Blit(src, dst, "AKF"); }
            //public void RenderAKF(int src, int dst) { RenderAKF(src, workRT[dst]); }
            //////////////////////// END PAINTERLY /////////////////////////

        }///////// END PASS

        [System.Serializable]
        public class OutlineSettings
        {
            public Material outlineMaterial = null;
            public Material painterlyMaterial = null;
        }

       

        public OutlineSettings settings = new OutlineSettings();
        OutlinePass outlinePass;
        RTHandle outlineTexture; //v0.1
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;//v1.6a
        public override void Create()
        {
            outlinePass = new OutlinePass(settings.outlineMaterial, settings.painterlyMaterial);
            outlinePass.renderPassEvent = renderPassEvent;//  RenderPassEvent.AfterRenderingTransparents;//v1.6a
            
            //outlineTexture.Init("_OutlineTexture"); //v0.1
            outlineTexture = RTHandles.Alloc("_OutlineTexture", name: "_OutlineTexture"); //v0.1
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
            if (settings.painterlyMaterial == null)
            {
                Debug.LogWarningFormat("Missing Painterly Material");
                return;
            }
            //outlinePass.Setup(renderer.cameraColorTarget, RenderTargetHandle.CameraTarget);//v1.5
            renderer.EnqueuePass(outlinePass);
        }


        


    }


    /// <summary>
    /// PAINTERLY
    ///  //////////////////////////////////////////////////////////////////////////////////////////////////
    // Gaussian Blur
    //////////////////////////////////////////////////////////////////////////////////////////////////
    ///[Serializable]
    public class InsGBlur
    {
        [SerializeField, Range(1, 64)] internal int SampleLen = 16;
        [SerializeField, Range(0, 3)] internal int LOD = 2;
        [HideInInspector] [SerializeField, Range(0.1f, 10.0f)] internal float DomainSigma = 1.0f;
        [HideInInspector] [SerializeField, Range(0.1f, 10.0f)] internal float DomainBias = 1.0f;
    }
    public class GBlur
    {
        public int LOD, TileSize, SampleLen, BlurSize;
        public float InvDomainSigma;
        public float DomainVariance;
        public float DomainBias;
        public float Mean;
        public bool UsePreCalc = true;
        public float[] OffsetX = new float[256];
        public float[] OffsetY = new float[256];
        public float[] DomainWeight = new float[256];

        public void Set(InsGBlur gb)
        {
            LOD = gb.LOD;
            TileSize = 1 << LOD;
            SampleLen = Mathf.Max(TileSize, gb.SampleLen);
            BlurSize = SampleLen / TileSize;

            float domainSigma = SampleLen * (1.0f / TileSize) * gb.DomainSigma;
            InvDomainSigma = 1.0f / domainSigma;
            DomainVariance = 1.0f / (domainSigma * domainSigma * 2.0f);
            DomainBias = gb.DomainBias;

            Mean = SampleLen * 0.5f;

            UsePreCalc = true;
            if (BlurSize * BlurSize > 256)
            {
                UsePreCalc = false;
                return;
            }

            // 指数計算等を事前に済ませておく
            Vector2 offset = new Vector2();
            Vector2 mean = new Vector2(Mean, Mean);
            for (int y = 0; y < BlurSize; ++y)
            {
                for (int x = 0; x < BlurSize; ++x)
                {
                    int index = y * BlurSize + x;
                    offset.Set(x, y);
                    offset = offset * TileSize - mean;
                    OffsetX[index] = offset.x;
                    OffsetY[index] = offset.y;

                    offset *= InvDomainSigma * DomainBias;
                    float dot = offset.x * offset.x + offset.y * offset.y;
                    float weight = Mathf.Exp(-0.5f * dot) * DomainVariance;
                    DomainWeight[index] = weight;
                }
            }
        }
    }
    public class LIC
    {
        public float Scale, MaxLen, Variance;
        //public void Set()//DebugOptions debug)
        //{
        //    Scale = debug.LICScale;
        //    MaxLen = debug.LICSigma;
        //    // Gσ(x) = exp(−(x^2) / (2 * σ^2)) の (2 * σ^2)
        //    // 分母として使うので逆数にしておく
        //    Variance = 1.0f / (debug.LICSigma * debug.LICSigma * 2.0f);
        //}
    }
    /// </summary>
     //////////////////////////////////////////////////////////////////////////////////////////////////
    // Anisotropic Kuwahara Filter
    //////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Anisotropic Kuwahara Filter
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsAKF
    {
        //[Button("SetDefaultParamsAKF", "Set Default Parameters")]
        [SerializeField] internal bool DefaultParameters;
        internal const float RadiusMin = 4.0f, RadiusMax = 32.0f;
        [SerializeField, Range(RadiusMin, RadiusMax)] internal float Radius = 16.0f;
        [SerializeField, Range(0.2f, 1.0f)] internal float MaskRadiusRatio = 0.5f;
        [SerializeField, Range(0.1f, 8.0f)] internal float Sharpness = 8.0f;
        // 分割領域の両脇での他領域との重複量
        [SerializeField, Range(0.1f, 3.0f)] internal float SideOverlap = 1.5f;
        // 分割領域の中心での他領域との重複量
        [SerializeField, Range(0.1f, 1.0f)] internal float CenterOverlap = 0.3f;
    }
    public class AKF
    {
        public float Radius, MaskRadius, Sharpness, OverlapX, OverlapY;
        public readonly int SampleStep = 2; // 固定

        public void Set(InsAKF akf)
        {
            Radius = akf.Radius;
            // RadiusMinを下回ると塗り漏れが発生する
            MaskRadius = Mathf.Max(InsAKF.RadiusMin, akf.Radius * akf.MaskRadiusRatio);
            Sharpness = akf.Sharpness;
            // 楕円の分割数。固定
            float DIV_NUM = 8.0f;
            // (x + zeta) - eta * y^2 = 0。 zeta はそのまま引数 CenterOverlap で指定           
            // eta = (cos(PI/(SideOverlap*DIV_NUM)) + CenterOverlap) - sin(PI/(sideOverlap*DIV_NUM))^2
            // 正規分布らしい重み付けにするには CenterOverlap≒1/3、SideOverlap≒3/2くらいが目安
            float theta = akf.SideOverlap * (Mathf.PI * (1.0f / DIV_NUM));
            float cosTheta = Mathf.Cos(theta), sinTheta = Mathf.Sin(theta);
            float invSinThetaSq = 1.0f / sinTheta * sinTheta;
            OverlapY = (akf.CenterOverlap + cosTheta) * invSinThetaSq;
            OverlapX = akf.CenterOverlap;
        }
    }



}
