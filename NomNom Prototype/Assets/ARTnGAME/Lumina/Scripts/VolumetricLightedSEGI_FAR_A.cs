using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//using Unity.Mathematics;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
#endif

namespace Artngame.LUMINA
{
    public class VolumetricLightedSEGI_FAR_A : ScriptableRendererFeature
    {
        [System.Serializable]
        public class VolumetricLightScatteringSettings
        {
            [Header("Volumetric Properties")]
            [Range(0.1f, 1f)]
            public float resolutionScale = 0.5f;
            [Range(0.0f, 1.0f)]
            public float intensity = 1.0f;
            [Range(0.0f, 1.0f)]
            public float blurWidth = 0.85f;
            [Range(0.0f, 0.2f)]
            public float fadeRange = 0.85f;
            [Range(50, 200)]
            public uint numSamples = 100;

            [Header("Noise Properties")]
            //public float2 noiseSpeed = 0.5f;
            public Vector2 noiseSpeed = 0.5f * Vector2.one;
            public float noiseScale = 1.0f;
            [Range(0.0f, 1.0f)]
            public float noiseStrength = 0.6f;

            //v0.1
            public RenderPassEvent eventA = RenderPassEvent.AfterRenderingSkybox;
        }

        class LightScatteringPass : ScriptableRenderPass
        {

#if UNITY_2023_3_OR_NEWER
            //v0.6
            Matrix4x4 lastFrameViewProjectionMatrix;
            Matrix4x4 viewProjectionMatrix;
            Matrix4x4 lastFrameInverseViewProjectionMatrix;
            public float downSample = 1;
            public float depthDilation = 1;
            public bool enabledTemporalAA = false;
            public float TemporalResponse = 1;
            public float TemporalGain = 1;

            RTHandle _handleA;
            RTHandle _handleB;
            RTHandle _handleC;
            RTHandle previousGIResultRT;
            RTHandle previousCameraDepthRT;
            TextureHandle previousGIResult;
            TextureHandle previousCameraDepth;

            RTHandle _handleTAART;
            TextureHandle _handleTAA;

            string m_ProfilerTag;
            public Material blitMaterial = null;
            bool allowHDR = true;// false; //v0.7
            /// <summary>
            /// ///////// GRAPH
            /// </summary>
            // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
            private class PassData
            {    //v0.1               
                internal TextureHandle src;
                internal TextureHandle tmpBuffer1;
                // internal TextureHandle copySourceTexture;
                public Material BlitMaterial { get; set; }
                // public TextureHandle SourceTexture { get; set; }
            }
            private Material m_BlitMaterial;
            TextureHandle tmpBuffer1A;
            TextureHandle tmpBuffer2A;
            TextureHandle tmpBuffer3A;
            TextureHandle previousFrameTextureA;
            TextureHandle previousDepthTextureA;
            TextureHandle currentDepth;
            TextureHandle currentNormal;

            TextureHandle reflectionsRG;
            RTHandle _handlereflectionsRG;
            // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                //LUMINA
                //GRAPH
                string passName = "VisualizeVoxels";
                //v0.1
                bool enableGI = false;
                if (Camera.main != null)
                {
                    LUMINA_FAR_A segi = Camera.main.GetComponent<LUMINA_FAR_A>();
                    if (segi != null)
                    {
                        enableGI = !segi.disableGI;
                    }
                    else
                    {
                        return;
                    }

                    m_BlitMaterial = blitMaterial;
                    m_BlitMaterial = segi.material;
                    blitMaterial = segi.material;

                    //if (!_occludersMaterial || !_radialBlurMaterial) InitializeMaterials();
                    if (RenderSettings.sun == null || !RenderSettings.sun.enabled || !enableGI
                        || (Camera.main != null && Camera.current != null && Camera.current != Camera.main) || Camera.main == null
                        ) { return; }

                    //Debug.Log(Camera.main.orthographic);
                    Camera.main.depthTextureMode = DepthTextureMode.Depth;

                    UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                    if (Camera.main != null && cameraData.camera != Camera.main)
                    {
                        //Debug.Log("No cam0");
                        return;
                    }

                    //passData.tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1", false);
                    desc.msaaSamples = 1;
                    desc.depthBufferBits = 0;
                    int rtW = desc.width;
                    int rtH = desc.height;
                    int xres = (int)(rtW / ((float)downSample));
                    int yres = (int)(rtH / ((float)downSample));
                    if (_handleA == null || _handleA.rt.width != xres || _handleA.rt.height != yres)
                    {
                        _handleA = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                    }
                    if (_handleB == null || _handleB.rt.width != xres || _handleB.rt.height != yres)
                    {
                        _handleB = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                    }
                    if (_handleC == null || _handleC.rt.width != xres || _handleC.rt.height != yres)
                    {
                        _handleC = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                    }
                    tmpBuffer2A = renderGraph.ImportTexture(_handleA);
                    previousFrameTextureA = renderGraph.ImportTexture(_handleB);
                    previousDepthTextureA = renderGraph.ImportTexture(_handleC);

                    if (_handlereflectionsRG == null || _handlereflectionsRG.rt.width != xres || _handlereflectionsRG.rt.height != yres)
                    {
                        _handlereflectionsRG = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                    }
                    reflectionsRG = renderGraph.ImportTexture(_handlereflectionsRG);


                    if (_handleTAART == null || _handleTAART.rt.width != xres || _handleTAART.rt.height != yres)
                    {
                        _handleTAART = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                        _handleTAART.rt.wrapMode = TextureWrapMode.Clamp;
                        _handleTAART.rt.filterMode = FilterMode.Bilinear;
                    }
                    _handleTAA = renderGraph.ImportTexture(_handleTAART);
                  

                    //LUMINA         
                    //previousGIResult = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
                    //previousGIResult.wrapMode = TextureWrapMode.Clamp;
                    //previousGIResult.filterMode = FilterMode.Bilinear;
                    //previousGIResult.useMipMap = true;    
                    //previousGIResult.autoGenerateMips = false;
                    //previousCameraDepth = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                    //previousCameraDepth.wrapMode = TextureWrapMode.Clamp;
                    //previousCameraDepth.filterMode = FilterMode.Bilinear;
                    //previousCameraDepth.Create();
                    //previousCameraDepth.hideFlags = HideFlags.HideAndDontSave;
                    if (previousGIResultRT == null || previousGIResultRT.rt.width != xres || previousGIResultRT.rt.height != yres)
                    {
                        previousGIResultRT = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                    }
                    if (previousCameraDepthRT == null || previousCameraDepthRT.rt.width != xres || previousCameraDepthRT.rt.height != yres)
                    {
                        previousCameraDepthRT = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex2D);
                    }
                    previousGIResult = renderGraph.ImportTexture(previousGIResultRT);
                    previousCameraDepth = renderGraph.ImportTexture(previousCameraDepthRT);

                    // TextureDesc descA = new TextureDesc(1280, 720);
                    // tmpBuffer1A = renderGraph.CreateTexture(descA);
                    //tmpBuffer2A = renderGraph.CreateTexture(descA);
                    tmpBuffer1A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true);
                    //tmpBuffer2A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer2A", true);
                    tmpBuffer3A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer3A", true);
                    //previousFrameTextureA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "previousFrameTextureA", true);
                    //previousDepthTextureA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "previousDepthTextureA", true);
                    //desc.depthBufferBits = 16;
                    //desc.depthStencilFormat = GraphicsFormat.
                    currentDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "currentDepth", true);
                    currentNormal = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "currentNormal", true);
                    // }
                    TextureHandle sourceTexture = resourceData.activeColorTexture;

                    if (cameraData.camera == Camera.main)
                    {


                        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                        RenderTextureDescriptor opaqueDesc = cameraData.cameraTargetDescriptor;
                        opaqueDesc.depthBufferBits = 0;

                        //v0.1
                        ///////////////////////////////////////////////////////////////// RENDER FOG
                        Material _material = blitMaterial;



                        //////////////////////////////////////////////////////////// START LUMINA
                        if (segi != null && segi.enabled)
                        {
                            if (segi.notReadyToRender || Camera.main == null)
                            {
                                //Blit(cmd, source, source);
                                //Graphics.Blit(source, destination);
                                //v0.1
                                //cmd.Blit(source, destination); /// BLITTER
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT EMPTY 1", out var passData, m_ProfilingSampler))
                                {
                                    passData.BlitMaterial = m_BlitMaterial;
                                    // Similar to the previous pass, however now we set destination texture as input and source as output.
                                    builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                                    passData.src = tmpBuffer2A;
                                    builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);
                                    // We use the same BlitTexture API to perform the Blit operation.
                                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                                }
                                return;
                            }

                            //Set parameters
                            Shader.SetGlobalFloat("SEGIVoxelScaleFactor_FAR_A", segi.voxelScaleFactor);

                            if (!segi.material)//v0.2a
                            {
                                segi.material = new Material(Shader.Find("Hidden/SEGI"));
                                //material.hideFlags = HideFlags.HideAndDontSave;//v0.2
                            }

                            segi.material.SetMatrix("CameraToWorld", segi.attachedCamera.cameraToWorldMatrix);
                            segi.material.SetMatrix("WorldToCamera", segi.attachedCamera.worldToCameraMatrix);
                            segi.material.SetMatrix("ProjectionMatrixInverse", segi.attachedCamera.projectionMatrix.inverse);
                            segi.material.SetMatrix("ProjectionMatrix", segi.attachedCamera.projectionMatrix);
                            segi.material.SetInt("FrameSwitch", segi.frameCounter);
                            Shader.SetGlobalInt("SEGIFrameSwitch", segi.frameCounter);
                            segi.material.SetVector("CameraPosition", segi.transform.position);
                            segi.material.SetFloat("DeltaTime", Time.deltaTime);

                            segi.material.SetInt("StochasticSampling", segi.stochasticSampling ? 1 : 0);
                            segi.material.SetInt("TraceDirections", segi.cones);
                            segi.material.SetInt("TraceSteps", segi.coneTraceSteps);
                            segi.material.SetFloat("TraceLength", segi.coneLength);
                            segi.material.SetFloat("ConeSize", segi.coneWidth);
                            segi.material.SetFloat("OcclusionStrength", segi.occlusionStrength);
                            segi.material.SetFloat("OcclusionPower", segi.occlusionPower);
                            segi.material.SetFloat("ConeTraceBias", segi.coneTraceBias);
                            segi.material.SetFloat("GIGain", segi.giGain);
                            segi.material.SetFloat("NearLightGain", segi.nearLightGain);
                            segi.material.SetFloat("NearOcclusionStrength", segi.nearOcclusionStrength);
                            segi.material.SetInt("DoReflections", segi.doReflections ? 1 : 0);
                            segi.material.SetInt("HalfResolution", segi.halfResolution ? 1 : 0);
                            segi.material.SetInt("ReflectionSteps", segi.reflectionSteps);
                            segi.material.SetFloat("ReflectionOcclusionPower", segi.reflectionOcclusionPower);
                            segi.material.SetFloat("SkyReflectionIntensity", segi.skyReflectionIntensity);
                            segi.material.SetFloat("FarOcclusionStrength", segi.farOcclusionStrength);
                            segi.material.SetFloat("FarthestOcclusionStrength", segi.farthestOcclusionStrength);
                            segi.material.SetTexture("NoiseTexture", segi.blueNoise[segi.frameCounter % 64]);
                            segi.material.SetFloat("BlendWeight", segi.temporalBlendWeight);

                            //v0.4
                            segi.material.SetFloat("contrastA", segi.contrastA);
                            segi.material.SetVector("ReflectControl", segi.ReflectControl);

                            //v0.7
                            segi.material.SetVector("ditherControl",
                            new Vector4(segi.DitherControl.x,
                            Mathf.Clamp(segi.DitherControl.y, 0.1f, 10),
                            Mathf.Clamp(segi.DitherControl.z, 0.1f, 10),
                            Mathf.Clamp(segi.DitherControl.w, 0.1f, 10))); //v1.2b

                            //v1.2
                            segi.material.SetFloat("smoothNormals", segi.smoothNormals);

                            //If Visualize Voxels is enabled, just render the voxel visualization shader pass and return
                            if (segi.visualizeVoxels)
                            {
                                //Blit(cmd, segi.blueNoise[segi.frameCounter % 64], destination);
                                //v0.1
                                //cmd.Blit(source, destination, segi.material, LUMINA.Pass.VisualizeVoxels); //BLITTER
                                passName = "VisualizeVoxels";
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                                {
                                    passData.src = resourceData.activeColorTexture;
                                    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                    //builder.UseTexture(tmpBuffer1A, AccessFlags.Read);
                                    builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                                    builder.AllowPassCulling(false);
                                    passData.BlitMaterial = m_BlitMaterial;
                                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                        ExecuteBlitPass(data, context, LUMINA.Pass.VisualizeVoxels, passData.src));
                                }
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT FINAL 1", out var passData, m_ProfilingSampler))
                                {
                                    passData.BlitMaterial = m_BlitMaterial;
                                    // Similar to the previous pass, however now we set destination texture as input and source as output.
                                    builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                                    passData.src = tmpBuffer2A;
                                    builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);
                                    // We use the same BlitTexture API to perform the Blit operation.
                                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                                }
                                return;
                            }

                            //Setup temporary textures
                            //                   RenderTexture gi1 = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.ARGBHalf);
                            //                   RenderTexture gi2 = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.ARGBHalf);
                            //                   RenderTexture reflections = null;

                            //If reflections are enabled, create a temporary render buffer to hold them
                            //                   if (segi.doReflections)
                            //                    {
                            //                        reflections = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                            //                    }

                            //Setup textures to hold the current camera depth and normal
                            //                   RenderTexture currentDepth = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                            //                    currentDepth.filterMode = FilterMode.Point;

                            //                    RenderTexture currentNormal = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                            //                    currentNormal.filterMode = FilterMode.Point;

                            //Get the camera depth and normals
                            //v0.1
     /////                    cmd.Blit(source, currentDepth, segi.material, LUMINA.Pass.GetCameraDepthTexture);//v0.1
                            passName = "GetCameraDepthTexture";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                //builder.UseTexture(tmpBuffer1A, AccessFlags.Read);
                                builder.SetRenderAttachment(currentDepth, 0, AccessFlags.Write);
                                builder.AllowPassCulling(false);
                                passData.BlitMaterial = m_BlitMaterial;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                   // ExecuteBlitPass(data, context, LUMINA.Pass.GetCameraDepthTexture, tmpBuffer1A));
                                   ExecuteBlitPass(data, context, 15, passData.src));
                            }


                           
                            ////                     segi.material.SetTexture("CurrentDepth", currentDepth);
                            

                            //v0.1
                            /////               cmd.Blit(source, currentNormal, segi.material, LUMINA.Pass.GetWorldNormals);
                            passName = "GetCameraNormalsTexture";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                //builder.UseTexture(tmpBuffer1A, AccessFlags.Read);
                                builder.SetRenderAttachment(currentNormal, 0, AccessFlags.Write);
                                builder.AllowPassCulling(false);
                                passData.BlitMaterial = m_BlitMaterial;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                    // ExecuteBlitPass(data, context, LUMINA.Pass.GetWorldNormals, tmpBuffer1A));
                                    ExecuteBlitPass(data, context, 16, passData.src));
                            }

                            /////                      segi.material.SetTexture("CurrentNormal", currentNormal);


                        

                            //v0.1 - check depths
                            //if (segi.visualizeNORMALS)
                            //{
                            //    //v0.1
                            //    cmd.Blit(currentNormal, destination);
                            //    return;
                            //}

                            //Set the previous GI result and camera depth textures to access them in the shader
                            ////                       segi.material.SetTexture("PreviousGITexture", segi.previousGIResult);
                            ////                       Shader.SetGlobalTexture("PreviousGITexture", segi.previousGIResult);
                            ///                       Shader.SetGlobalTexture("PreviousGITexture", previousGIResult);
                            ////                       segi.material.SetTexture("PreviousDepth", segi.previousCameraDepth);

                            //Render diffuse GI tracing result
                            //v0.1
                            /////                      cmd.Blit(source, gi2, segi.material, LUMINA.Pass.DiffuseTrace);
                            passName = "DiffuseTrace";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                builder.UseTexture(currentDepth, AccessFlags.Read);
                                builder.UseTexture(currentNormal, AccessFlags.Read);
                                builder.UseTexture(previousGIResult, AccessFlags.Read);
                                builder.UseTexture(previousGIResult, AccessFlags.Read);
                                builder.UseTexture(previousCameraDepth, AccessFlags.Read);
                                builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                                builder.AllowPassCulling(false);
                                passData.BlitMaterial = m_BlitMaterial;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                    //ExecuteBlitPassTEX5NAME(data, context, LUMINA.Pass.DiffuseTrace,
                                    ExecuteBlitPassTEX5NAME(data, context, 17,
                                    "CurrentDepth", currentDepth,
                                    "CurrentNormal", currentNormal,
                                    "PreviousGITexture_FAR_A", previousGIResult,
                                    "PreviousGITexture_FAR_A", previousGIResult,
                                    "PreviousDepth", previousCameraDepth
                                    ));
                            }





                            //if (segi.visualizeDEPTH)
                            //{
                            //    //v0.1
                            //    cmd.Blit(gi2, destination);
                            //    return;
                            //}

                            //if (segi.doReflections)
                            //{
                            //    //Render GI reflections result
                            //    //v0.1
                            //    cmd.Blit(source, reflections, segi.material, LUMINA.Pass.SpecularTrace);
                            //    segi.material.SetTexture("Reflections", reflections);
                            //}
                            if (segi.doReflections)
                            {
                                passName = "doReflections";
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                                {
                                    passData.src = resourceData.activeColorTexture;
                                    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                    builder.UseTexture(currentDepth, AccessFlags.Read);
                                    builder.UseTexture(currentNormal, AccessFlags.Read);
                                    builder.UseTexture(previousGIResult, AccessFlags.Read);
                                    builder.UseTexture(previousGIResult, AccessFlags.Read);
                                    builder.UseTexture(previousCameraDepth, AccessFlags.Read);
                                    builder.SetRenderAttachment(reflectionsRG, 0, AccessFlags.Write);
                                    builder.AllowPassCulling(false);
                                    passData.BlitMaterial = m_BlitMaterial;
                                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                        //ExecuteBlitPassTEX5NAME(data, context, LUMINA.Pass.DiffuseTrace,
                                        ExecuteBlitPassTEX5NAME(data, context, 21,
                                        "CurrentDepth", currentDepth,
                                        "CurrentNormal", currentNormal,
                                        "PreviousGITexture_FAR_A", previousGIResult,
                                        "PreviousGITexture_FAR_A", previousGIResult,
                                        "PreviousDepth", previousCameraDepth
                                        ));
                                }
                            }
                            //TEST REFL TEXTURE
                            //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler))
                            //{
                            //    passData.BlitMaterial = m_BlitMaterial;
                            //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                            //    builder.UseTexture(reflectionsRG, AccessFlags.Read);
                            //    passData.src = reflectionsRG;
                            //    builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);
                            //    // We use the same BlitTexture API to perform the Blit operation.
                            //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                            //}

                            //Perform bilateral filtering
                            /*
                            if (segi.useBilateralFiltering)
                            {
                                segi.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));
                                //v0.1
                                cmd.Blit(gi2, gi1, segi.material, LUMINA.Pass.BilateralBlur);

                                segi.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                                //v0.1
                                cmd.Blit(gi1, gi2, segi.material, LUMINA.Pass.BilateralBlur);

                                segi.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));
                                //v0.1
                                cmd.Blit(gi2, gi1, segi.material, LUMINA.Pass.BilateralBlur);

                                segi.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                                //v0.1
                                cmd.Blit(gi1, gi2, segi.material, LUMINA.Pass.BilateralBlur);
                            }
                            */

                            //If Half Resolution tracing is enabled
                            if (segi.giRenderRes == 2)
                            {
                                /*
                                RenderTexture.ReleaseTemporary(gi1);

                                //Setup temporary textures
                                RenderTexture gi3 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                                RenderTexture gi4 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);


                                //Prepare the half-resolution diffuse GI result to be bilaterally upsampled
                                gi2.filterMode = FilterMode.Point;
                                //v0.1
                                cmd.Blit(gi2, gi4);

                                RenderTexture.ReleaseTemporary(gi2);

                                gi4.filterMode = FilterMode.Point;
                                gi3.filterMode = FilterMode.Point;


                                //Perform bilateral upsampling on half-resolution diffuse GI result
                                segi.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                                //v0.1
                                cmd.Blit(gi4, gi3, segi.material, LUMINA.Pass.BilateralUpsample);
                                segi.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));

                                //Perform temporal reprojection and blending
                                if (segi.temporalBlendWeight < 1.0f)
                                {
                                    //v0.1
                                    cmd.Blit(gi3, gi4);
                                    //v0.1
                                    cmd.Blit(gi4, gi3, segi.material, LUMINA.Pass.TemporalBlend);
                                    //v0.1
                                    cmd.Blit(gi3, segi.previousGIResult);
                                    //v0.1
                                    cmd.Blit(source, segi.previousCameraDepth, segi.material, LUMINA.Pass.GetCameraDepthTexture);
                                }

                                //Set the result to be accessed in the shader
                                segi.material.SetTexture("GITexture", gi3);

                                //Actually apply the GI to the scene using gbuffer data
                                //v0.1
                                cmd.Blit(source, destination, segi.material, segi.visualizeGI ? LUMINA.Pass.VisualizeGI : LUMINA.Pass.BlendWithScene);

                                //Release temporary textures
                                RenderTexture.ReleaseTemporary(gi3);
                                RenderTexture.ReleaseTemporary(gi4);
                                */
                        }
                        else    //If Half Resolution tracing is disabled
                            {
                                //Perform temporal reprojection and blending
                                //if (segi.temporalBlendWeight < 1.0f)
                                //{
                                //    //v0.1
                                //    cmd.Blit(gi2, gi1, segi.material, LUMINA.Pass.TemporalBlend);
                                //    //v0.1
                                //    cmd.Blit(gi1, segi.previousGIResult);
                                //    //v0.1
                                //    cmd.Blit(source, segi.previousCameraDepth, segi.material, LUMINA.Pass.GetCameraDepthTexture);
                                //}

                                //Actually apply the GI to the scene using gbuffer data
                                ////                     segi.material.SetTexture("GITexture", segi.temporalBlendWeight < 1.0f ? gi1 : gi2);
                                //v0.1
                                ////                     cmd.Blit(source, destination, segi.material, segi.visualizeGI ? LUMINA.Pass.VisualizeGI : LUMINA.Pass.BlendWithScene);


                                ////                    segi.material.SetTexture("GITexture", tmpBuffer2A);


                                if (segi.doReflections)
                                {
                                    passName = "BlendWithSceneREFLECTIONS";
                                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                                    {
                                        passData.src = resourceData.activeColorTexture;
                                        desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                        builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                                        builder.UseTexture(reflectionsRG, AccessFlags.Read);
                                        builder.SetRenderAttachment(tmpBuffer3A, 0, AccessFlags.Write);
                                        builder.AllowPassCulling(false);
                                        passData.BlitMaterial = m_BlitMaterial;
                                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                            //ExecuteBlitPassTEXNAME(data, context, LUMINA.Pass.BlendWithScene, tmpBuffer2A, "GITexture"));
                                            ExecuteBlitPassTEXNAME_TWO(data, context, 18, tmpBuffer2A, "GITexture", reflectionsRG, "Reflections"));
                                    }
                                }
                                else
                                {
                                    passName = "BlendWithScene";
                                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                                    {
                                        passData.src = resourceData.activeColorTexture;
                                        desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                        builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                                        builder.SetRenderAttachment(tmpBuffer3A, 0, AccessFlags.Write);
                                        builder.AllowPassCulling(false);
                                        passData.BlitMaterial = m_BlitMaterial;
                                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                            //ExecuteBlitPassTEXNAME(data, context, LUMINA.Pass.BlendWithScene, tmpBuffer2A, "GITexture"));
                                            ExecuteBlitPassTEXNAME(data, context, 18, tmpBuffer2A, "GITexture"));
                                    }
                                }

                                //TESTER CODE
                                //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler))
                                //{
                                //    passData.BlitMaterial = m_BlitMaterial;
                                //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                                //    passData.src = builder.UseTexture(tmpBuffer3A, IBaseRenderGraphBuilder.AccessFlags.Read);
                                //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                                //    // We use the same BlitTexture API to perform the Blit operation.
                                //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                                //}
                                //return;
                                //END TESTER CODE

                                //Release temporary textures
                                //RenderTexture.ReleaseTemporary(gi1);
                                //RenderTexture.ReleaseTemporary(gi2);
                            }

                            //Release temporary textures
                            //                  RenderTexture.ReleaseTemporary(currentDepth);
                            //                  RenderTexture.ReleaseTemporary(currentNormal);

                            //Visualize the sun depth texture
                            //if (segi.visualizeSunDepthTexture)
                            //{
                            //    //v0.1
                            //    cmd.Blit(segi.sunDepthTexture, destination);
                            //}


                            //Release the temporary reflections result texture
                            //if (segi.doReflections)
                            //{
                            //    RenderTexture.ReleaseTemporary(reflections);
                            //}

                            //Set matrices/vectors for use during temporal reprojection
                            segi.material.SetMatrix("ProjectionPrev", segi.attachedCamera.projectionMatrix);
                            segi.material.SetMatrix("ProjectionPrevInverse", segi.attachedCamera.projectionMatrix.inverse);
                            segi.material.SetMatrix("WorldToCameraPrev", segi.attachedCamera.worldToCameraMatrix);
                            segi.material.SetMatrix("CameraToWorldPrev", segi.attachedCamera.cameraToWorldMatrix);
                            segi.material.SetVector("CameraPositionPrev", segi.transform.position);

                            //Advance the frame counter
                            segi.frameCounter = (segi.frameCounter + 1) % (64);

                            //////////////////////////////////////////////////////////// END LUMINA




                            //v1.9.9.5 - Ethereal v1.1.8
                            //_material.SetInt("_visibleLightsCount", renderingData.cullResults.visibleLights.Length); 
                            //v2.0
                            //updateMaterialKeyword(useOnlyFog, "ONLY_FOG", _material);                    
                            //Debug.Log(_material.HasProperty("controlByColor"));                    
                            //var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
                            var format = allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9 //v LWRP //v0.7

                            //CONVERT A1                   
                            //        RecordRenderGraphBLIT1(renderGraph, frameData, desc, cameraData, renderingData, resourceData, _material, ref tmpBuffer1A, 24);// 21);
                            //passName = "BLIT1 Keep Source";
                            //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            //{
                            //    passData.src = resourceData.activeColorTexture;
                            //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //    builder.UseTexture(passData.src, IBaseRenderGraphBuilder.AccessFlags.Read);
                            //    builder.SetRenderAttachment(tmpBuffer1A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                            //    builder.AllowPassCulling(false);
                            //    passData.BlitMaterial = m_BlitMaterial;
                            //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            //        ExecuteBlitPass(data, context, 14, passData.src));
                            //}

                            ///// MORE 1                  

                            //WORLD RECONSTRUCT        
                            Matrix4x4 camToWorld = Camera.main.cameraToWorldMatrix;
                            _material.SetMatrix("_InverseView", camToWorld);

                            //v0.6                    
                            //RecordRenderGraphBLIT1(renderGraph, frameData, desc, cameraData, renderingData, resourceData, _material, ref tmpBuffer2A, 6);

                            //passName = "BLIT2";
                            //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            //{
                            //    passData.src = resourceData.activeColorTexture;
                            //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //    builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                            //    builder.SetRenderAttachment(tmpBuffer2A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                            //    builder.AllowPassCulling(false);
                            //    passData.BlitMaterial = m_BlitMaterial;
                            //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            //        ExecuteBlitPass(data, context, (int)segi.temporalBlendWeight * 14, tmpBuffer1A));
                            //}

                            //passName = "BLIT2";
                            //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            //{
                            //    passData.src = resourceData.activeColorTexture;
                            //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            //    builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                            //    builder.SetRenderAttachment(tmpBuffer2A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                            //    builder.AllowPassCulling(false);
                            //    passData.BlitMaterial = m_BlitMaterial;
                            //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            //        ExecuteBlitPass(data, context, (int)segi.temporalBlendWeight*14, tmpBuffer1A));
                            //}

                            ///// TEMPORAL
                            if (segi.temporalBlendWeight < 1 && Time.fixedTime > 0.05f)
                            //if (enabledTemporalAA && Time.fixedTime > 0.05f)
                            {
                                //NEW
                                _material.SetFloat("_TemporalResponse",2f);
                                _material.SetFloat("_TemporalGain", segi.temporalBlendWeight * 5f);

                                var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                                var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                                _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                                viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                                _material.SetMatrix("_InverseViewProjectionMatrix", viewProjectionMatrix.inverse);
                                _material.SetMatrix("_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix);
                                _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix);

                                //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                                Matrix4x4 mt = lastFrameViewProjectionMatrix * cameraData.camera.cameraToWorldMatrix;
                                _material.SetMatrix("_FrameMatrix", mt);

                                passName = "BLIT_TAA";
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                                {
                                    passData.src = resourceData.activeColorTexture;
                                    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                    builder.UseTexture(tmpBuffer3A, AccessFlags.Read);
                                    builder.UseTexture(previousFrameTextureA, AccessFlags.Read);
                                    builder.UseTexture(previousDepthTextureA, AccessFlags.Read);
                                    builder.SetRenderAttachment(_handleTAA, 0, AccessFlags.Write);
                                    builder.AllowPassCulling(false);
                                    passData.BlitMaterial = m_BlitMaterial;
                                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                        //ExecuteBlitPassTHREE(data, context, 19, tmpBuffer3A, previousFrameTextureA, previousDepthTextureA));
                                        ExecuteBlitPassTEN(data, context, 19, tmpBuffer3A, previousFrameTextureA, previousDepthTextureA,
                                        "_TemporalResponse", 2f,
                                        "_TemporalGain", segi.temporalBlendWeight * 5f,
                                        "_InverseProjectionMatrix", projectionMatrix.inverse,
                                        "_InverseViewProjectionMatrix", viewProjectionMatrix.inverse,
                                        "_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix,
                                        "_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix,
                                        "_FrameMatrix", mt
                                        ));
                                }
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT_TAA 1", out var passData, m_ProfilingSampler))
                                {
                                    builder.AllowGlobalStateModification(true);
                                    passData.BlitMaterial = m_BlitMaterial;
                                    builder.UseTexture(_handleTAA, AccessFlags.Read);
                                    passData.src = _handleTAA;
                                    builder.SetRenderAttachment(previousFrameTextureA, 0, AccessFlags.Write);
                                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                                }
                                using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT_TAA 2", out var passData, m_ProfilingSampler))
                                {
                                    builder.AllowGlobalStateModification(true);
                                    passData.BlitMaterial = m_BlitMaterial;
                                    builder.UseTexture(_handleTAA, AccessFlags.Read);
                                    passData.src = _handleTAA;
                                    builder.SetRenderAttachment(tmpBuffer3A, 0, AccessFlags.Write);
                                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                                }
                            }

                            //v0.6
                            lastFrameViewProjectionMatrix = viewProjectionMatrix;
                            lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
                            /////// END MORE 1 

                            ///////////////////////////////////////////////////////////////// END RENDER FOG
                        }
                    }//END A Connector check

                    // Now we will add another pass to “resolve” the modified color buffer we have to the pipelinebuffer by doing the reverse blit, from destination to source. Later in this tutorial we will
                    // explore some alternatives that we can do to optimize this second blit away and avoid the round trip.
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler))
                    {
                        passData.BlitMaterial = m_BlitMaterial;
                        // Similar to the previous pass, however now we set destination texture as input and source as output.
                        builder.UseTexture(tmpBuffer3A, AccessFlags.Read);
                        passData.src = tmpBuffer3A;
                        builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);
                        // We use the same BlitTexture API to perform the Blit operation.
                        builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                    }                
                }
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
                data.BlitMaterial.SetTexture("_ColorBuffer", tmpBuffer1);
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
            static void ExecuteBlitPassTEXNAME(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa, string texname)
            {
                data.BlitMaterial.SetTexture(texname, tmpBuffer1aa);
                Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            }
            static void ExecuteBlitPassTEXNAME_TWO(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa, string texname
                , TextureHandle tmpBuffer1aaa, string texnamea)
            {
                data.BlitMaterial.SetTexture(texname, tmpBuffer1aa);
                data.BlitMaterial.SetTexture(texnamea, tmpBuffer1aaa);
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
                Blitter.BlitTexture(rgContext.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, 13);
            }
            //private Material m_BlitMaterial;
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("After Opaques");
            ////// END GRAPH
#endif





            //v0.1
            //private readonly RenderTargetHandle _occluders = RenderTargetHandle.CameraTarget;
            //private readonly RenderTargetHandle _occluders = RenderTargetHandle.CameraTarget;
            //if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//  UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1


            private readonly VolumetricLightScatteringSettings _settings;
            private readonly List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>();
            //private Material _occludersMaterial;
            //private Material _radialBlurMaterial;
            private FilteringSettings _filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            private RenderTargetIdentifier _cameraColorTargetIdent;

            private RenderTargetIdentifier source;

            public LightScatteringPass(VolumetricLightScatteringSettings settings)
            {
                ///_occluders.Init("_OccludersMap");//v0.1
                _settings = settings;

                _shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                _shaderTagIdList.Add(new ShaderTagId("LightweightForward"));
                _shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            }

            public void SetCameraColorTarget(RenderTargetIdentifier _cameraColorTargetIdent)
              => this._cameraColorTargetIdent = _cameraColorTargetIdent;

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
#if UNITY_2020_2_OR_NEWER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // get a copy of the current camera’s RenderTextureDescriptor
                // this descriptor contains all the information you need to create a new texture
                RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

                // disable the depth buffer because we are not going to use it
                cameraTextureDescriptor.depthBufferBits = 0;

                // scale the texture dimensions
                cameraTextureDescriptor.width = Mathf.RoundToInt(cameraTextureDescriptor.width * _settings.resolutionScale);
                cameraTextureDescriptor.height = Mathf.RoundToInt(cameraTextureDescriptor.height * _settings.resolutionScale);

                // create temporary render texture
           //     cmd.GetTemporaryRT(_occluders.id, cameraTextureDescriptor, FilterMode.Bilinear);//v0.1

                // finish configuration
           //     ConfigureTarget(_occluders.Identifier());//v0.1


                //v0.1
                var renderer = renderingData.cameraData.renderer;
                //v0.1
                //source = renderer.cameraColorTarget;
#if UNITY_2022_1_OR_NEWER
                source = renderer.cameraColorTargetHandle;
#else
                source = renderer.cameraColorTarget;
#endif

            }
#else
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                // get a copy of the current camera’s RenderTextureDescriptor
                // this descriptor contains all the information you need to create a new texture
                //RenderTextureDescriptor cameraTextureDescriptor = cameraTextureDescriptor;// renderingData.cameraData.cameraTargetDescriptor;

                // disable the depth buffer because we are not going to use it
                cameraTextureDescriptor.depthBufferBits = 0;

                // scale the texture dimensions
                cameraTextureDescriptor.width = Mathf.RoundToInt(cameraTextureDescriptor.width * _settings.resolutionScale);
                cameraTextureDescriptor.height = Mathf.RoundToInt(cameraTextureDescriptor.height * _settings.resolutionScale);

                // create temporary render texture
           //     cmd.GetTemporaryRT(_occluders.id, cameraTextureDescriptor, FilterMode.Bilinear);//v0.1

                // finish configuration
           //     ConfigureTarget(_occluders.Identifier());//v0.1

                //v0.1
                //var renderer = renderingData.cameraData.renderer;
                source = _cameraColorTargetIdent; //source = renderer.cameraColorTarget;
            }
#endif

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {



                //v0.1
                bool enableGI = false;
                if (Camera.main != null)
                {
                    LUMINA_FAR_A segi = Camera.main.GetComponent<LUMINA_FAR_A>();
                    if (segi != null)
                    {
                        enableGI = !segi.disableGI;
                    }
                }

                //if (!_occludersMaterial || !_radialBlurMaterial) InitializeMaterials();
                if (RenderSettings.sun == null || !RenderSettings.sun.enabled || !enableGI
                    || (Camera.main != null && Camera.current != null && Camera.current != Camera.main) || Camera.main == null
                    ) { return; }

                // get command buffer pool
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, new ProfilingSampler("VolumetricLightScattering")))
                {
                    //if (1 == 0)
                    //{
                    //    // prepares command buffer
                    //    context.ExecuteCommandBuffer(cmd);
                    //    cmd.Clear();

                    //    Camera camera = renderingData.cameraData.camera;
                    //    context.DrawSkybox(camera);

                    //    DrawingSettings drawSettings = CreateDrawingSettings(
                    //      _shaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque
                    //    );
                    //    drawSettings.overrideMaterial = _occludersMaterial;
                    //    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);

                    //    // schedule it for execution and release it after the execution
                    //    context.ExecuteCommandBuffer(cmd);
                    //    CommandBufferPool.Release(cmd);

                    //    //float3 sunDirectionWorldSpace = RenderSettings.sun.transform.forward;
                    //    //float3 cameraDirectionWorldSpace = camera.transform.forward;
                    //    //float3 cameraPositionWorldSpace = camera.transform.position;
                    //    //float3 sunPositionWorldSpace = cameraPositionWorldSpace + sunDirectionWorldSpace;
                    //    //float3 sunPositionViewportSpace = camera.WorldToViewportPoint(sunPositionWorldSpace);
                    //    Vector3 sunDirectionWorldSpace = RenderSettings.sun.transform.forward;
                    //    Vector3 cameraDirectionWorldSpace = camera.transform.forward;
                    //    Vector3 cameraPositionWorldSpace = camera.transform.position;
                    //    Vector3 sunPositionWorldSpace = cameraPositionWorldSpace + sunDirectionWorldSpace;
                    //    Vector3 sunPositionViewportSpace = camera.WorldToViewportPoint(sunPositionWorldSpace);

                    //    //float dotProd = math.dot(-cameraDirectionWorldSpace, sunDirectionWorldSpace);
                    //    //dotProd -= math.dot(cameraDirectionWorldSpace, Vector3.down);

                    //    float dotProd = Vector3.Dot(-cameraDirectionWorldSpace, sunDirectionWorldSpace);
                    //    dotProd -= Vector3.Dot(cameraDirectionWorldSpace, Vector3.down);

                    //    float intensityFader = dotProd / _settings.fadeRange;
                    //    intensityFader = Mathf.Clamp01(intensityFader); //intensityFader = math.saturate(intensityFader);

                    //    _radialBlurMaterial.SetColor("_Color", RenderSettings.sun.color);
                    //    _radialBlurMaterial.SetVector("_Center", new Vector4(
                    //      sunPositionViewportSpace.x, sunPositionViewportSpace.y, 0.0f, 0.0f
                    //    ));
                    //    _radialBlurMaterial.SetFloat("_BlurWidth", _settings.blurWidth);
                    //    _radialBlurMaterial.SetFloat("_NumSamples", _settings.numSamples);
                    //    _radialBlurMaterial.SetFloat("_Intensity", _settings.intensity * intensityFader);

                    //    //_radialBlurMaterial.SetVector("_NoiseSpeed", new float4(_settings.noiseSpeed, 0.0f, 0.0f));
                    //    _radialBlurMaterial.SetVector("_NoiseSpeed", new Vector4(_settings.noiseSpeed.x, _settings.noiseSpeed.y, 0.0f, 0.0f));

                    //    _radialBlurMaterial.SetFloat("_NoiseScale", _settings.noiseScale);
                    //    _radialBlurMaterial.SetFloat("_NoiseStrength", _settings.noiseStrength);

                    //    Blit(cmd, _occluders.Identifier(), _cameraColorTargetIdent, _radialBlurMaterial);
                    //}


                    //v0.1
                    //context.ExecuteCommandBuffer(cmd);
                    //cmd.Clear();
                    Camera cameraA = renderingData.cameraData.camera;
                    RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

                    RenderTexture skyA = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, RenderTextureFormat.ARGBHalf);

		     //v0.1
		     cmd.Blit(source, skyA);
                    //context.DrawSkybox(cameraA);
                    //https://www.febucci.com/2022/05/custom-post-processing-in-urp/
                    //var renderer = renderingData.cameraData.renderer;
                    //source = renderer.cameraColorTarget;
                    //context.sky
                    //Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, skyA);// Blit(cmd, _occluders.Identifier(), skyA);

                    //DEBUG 
                    // Blit(cmd, skyA, _cameraColorTargetIdent);
                    // context.ExecuteCommandBuffer(cmd);
                    //CommandBufferPool.Release(cmd);
                    //RenderTexture.ReleaseTemporary(skyA);                   
                    //return;


                    RenderTexture gi1 = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, RenderTextureFormat.ARGBHalf);

                    //renderingData.cameraData.camera = Camera.main;

                    OnRenderImage(cameraA, cmd, skyA, gi1);

                    //v0.1
		     cmd.Blit(gi1, source);// _cameraColorTargetIdent); //1.0g

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                    RenderTexture.ReleaseTemporary(skyA);
                    RenderTexture.ReleaseTemporary(gi1);
                }
            }



            void OnRenderImage(Camera camera, CommandBuffer cmd, RenderTexture source, RenderTexture destination)
            {



                // Graphics.Blit(source, destination);
                //Blit(cmd, source, destination);
                //return;

                if (Camera.main != null && camera != Camera.main)
                {
                    //Debug.Log("No cam0");
                    return;
                }

                //Debug.Log(Camera.main.orthographic);
                Camera.main.depthTextureMode = DepthTextureMode.Depth;

                //v0.1
                if (Camera.main != null)
                {
                    LUMINA_FAR_A segi = Camera.main.GetComponent<LUMINA_FAR_A>();
                    if (segi != null && segi.enabled)
                    {
                        if (segi.notReadyToRender || Camera.main == null)
                        {
                            //Blit(cmd, source, source);
                            //Graphics.Blit(source, destination);
                            //v0.1
		     		cmd.Blit( source, destination);
                            return;
                        }

                        //Set parameters
                        Shader.SetGlobalFloat("SEGIVoxelScaleFactor_FAR_A", segi.voxelScaleFactor);//Shader.SetGlobalFloat("SEGIVoxelScaleFactor", segi.voxelScaleFactor);

                        if (!segi.material)//v0.2a
                        {
                            segi.material = new Material(Shader.Find("Hidden/SEGI"));
                            //material.hideFlags = HideFlags.HideAndDontSave;//v0.2
                        }

                        segi.material.SetMatrix("CameraToWorld", segi.attachedCamera.cameraToWorldMatrix);
                        segi.material.SetMatrix("WorldToCamera", segi.attachedCamera.worldToCameraMatrix);
                        segi.material.SetMatrix("ProjectionMatrixInverse", segi.attachedCamera.projectionMatrix.inverse);
                        segi.material.SetMatrix("ProjectionMatrix", segi.attachedCamera.projectionMatrix);
                        segi.material.SetInt("FrameSwitch", segi.frameCounter);
                        //Shader.SetGlobalInt("SEGIFrameSwitch", segi.frameCounter);
                        segi.material.SetVector("CameraPosition", segi.transform.position);
                        segi.material.SetFloat("DeltaTime", Time.deltaTime);

                        segi.material.SetInt("StochasticSampling", segi.stochasticSampling ? 1 : 0);
                        segi.material.SetInt("TraceDirections", segi.cones);
                        segi.material.SetInt("TraceSteps", segi.coneTraceSteps);
                        segi.material.SetFloat("TraceLength", segi.coneLength);
                        segi.material.SetFloat("ConeSize", segi.coneWidth);
                        segi.material.SetFloat("OcclusionStrength", segi.occlusionStrength);
                        segi.material.SetFloat("OcclusionPower", segi.occlusionPower);
                        segi.material.SetFloat("ConeTraceBias", segi.coneTraceBias);
                        segi.material.SetFloat("GIGain", segi.giGain);
                        segi.material.SetFloat("NearLightGain", segi.nearLightGain);
                        segi.material.SetFloat("NearOcclusionStrength", segi.nearOcclusionStrength);
                        segi.material.SetInt("DoReflections", segi.doReflections ? 1 : 0);
                        segi.material.SetInt("HalfResolution", segi.halfResolution ? 1 : 0);
                        segi.material.SetInt("ReflectionSteps", segi.reflectionSteps);
                        segi.material.SetFloat("ReflectionOcclusionPower", segi.reflectionOcclusionPower);
                        segi.material.SetFloat("SkyReflectionIntensity", segi.skyReflectionIntensity);
                        segi.material.SetFloat("FarOcclusionStrength", segi.farOcclusionStrength);
                        segi.material.SetFloat("FarthestOcclusionStrength", segi.farthestOcclusionStrength);
                        segi.material.SetTexture("NoiseTexture", segi.blueNoise[segi.frameCounter % 64]);
                        segi.material.SetFloat("BlendWeight", segi.temporalBlendWeight);

                        //v0.4
                        segi.material.SetFloat("contrastA", segi.contrastA);
                        segi.material.SetVector("ReflectControl", segi.ReflectControl);

                        //v0.7
                        segi.material.SetVector("ditherControl",
                        new Vector4(segi.DitherControl.x,
                        Mathf.Clamp(segi.DitherControl.y, 0.1f, 10),
                        Mathf.Clamp(segi.DitherControl.z, 0.1f, 10),
                        Mathf.Clamp(segi.DitherControl.w, 0.1f, 10))); //v1.2b

                        //v1.2
                        segi.material.SetFloat("smoothNormals", segi.smoothNormals);

                        //If Visualize Voxels is enabled, just render the voxel visualization shader pass and return
                        if (segi.visualizeVoxels)
                        {
                            //Blit(cmd, segi.blueNoise[segi.frameCounter % 64], destination);
                            //v0.1
		     cmd.Blit( source, destination, segi.material, LUMINA_FAR.Pass.VisualizeVoxels);
                            return;
                        }

                        //Setup temporary textures
                        RenderTexture gi1 = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.ARGBHalf);
                        RenderTexture gi2 = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.ARGBHalf);
                        RenderTexture reflections = null;

                        //If reflections are enabled, create a temporary render buffer to hold them
                        if (segi.doReflections)
                        {
                            reflections = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                        }

                        //Setup textures to hold the current camera depth and normal
                        RenderTexture currentDepth = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                        currentDepth.filterMode = FilterMode.Point;

                        RenderTexture currentNormal = RenderTexture.GetTemporary(source.width / segi.giRenderRes, source.height / segi.giRenderRes, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                        currentNormal.filterMode = FilterMode.Point;

                        //Get the camera depth and normals
                        //v0.1
		     cmd.Blit( source, currentDepth, segi.material, LUMINA_FAR.Pass.GetCameraDepthTexture);//v0.1
                                                                                                          //Blit(cmd, source, currentDepth, segi.material, VolumeLitSEGI.Pass.GetCameraDepthTexture);


                        segi.material.SetTexture("CurrentDepth", currentDepth);
                        //v0.1
		     cmd.Blit( source, currentNormal, segi.material, LUMINA_FAR.Pass.GetWorldNormals);
                        segi.material.SetTexture("CurrentNormal", currentNormal);


                        //v0.1 - check depths
                        if (segi.visualizeNORMALS)
                        {
                            //v0.1
		     cmd.Blit( currentNormal, destination);
                            return;
                        }
                        //if (segi.visualizeDEPTH)
                        //{
                        //    Blit(cmd, currentDepth, destination);
                        //    return;
                        //}



                        //Set the previous GI result and camera depth textures to access them in the shader
                        segi.material.SetTexture("PreviousGITexture_FAR_A", segi.previousGIResult);

                        Shader.SetGlobalTexture("PreviousGITexture_FAR_A", segi.previousGIResult);// Shader.SetGlobalTexture("PreviousGITexture", segi.previousGIResult);

                        segi.material.SetTexture("PreviousDepth", segi.previousCameraDepth);

                        //Render diffuse GI tracing result
                        //v0.1
		     cmd.Blit( source, gi2, segi.material, LUMINA_FAR.Pass.DiffuseTrace);

                        if (segi.visualizeDEPTH)
                        {
                            //v0.1
		     cmd.Blit( gi2, destination);
                            return;
                        }

                        if (segi.doReflections)
                        {
                            //Render GI reflections result
                            //v0.1
		     cmd.Blit( source, reflections, segi.material, LUMINA_FAR.Pass.SpecularTrace);
                            segi.material.SetTexture("Reflections", reflections);
                        }


                        //Perform bilateral filtering
                        if (segi.useBilateralFiltering)
                        {
                            segi.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));
                            //v0.1
		     cmd.Blit( gi2, gi1, segi.material, LUMINA_FAR.Pass.BilateralBlur);

                            segi.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                            //v0.1
		     cmd.Blit( gi1, gi2, segi.material, LUMINA_FAR.Pass.BilateralBlur);

                            segi.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));
                            //v0.1
		     cmd.Blit( gi2, gi1, segi.material, LUMINA_FAR.Pass.BilateralBlur);

                            segi.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                            //v0.1
		     cmd.Blit( gi1, gi2, segi.material, LUMINA_FAR.Pass.BilateralBlur);
                        }

                        //If Half Resolution tracing is enabled
                        if (segi.giRenderRes == 2)
                        {
                            RenderTexture.ReleaseTemporary(gi1);

                            //Setup temporary textures
                            RenderTexture gi3 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                            RenderTexture gi4 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);


                            //Prepare the half-resolution diffuse GI result to be bilaterally upsampled
                            gi2.filterMode = FilterMode.Point;
                            //v0.1
		     cmd.Blit( gi2, gi4);

                            RenderTexture.ReleaseTemporary(gi2);

                            gi4.filterMode = FilterMode.Point;
                            gi3.filterMode = FilterMode.Point;


                            //Perform bilateral upsampling on half-resolution diffuse GI result
                            segi.material.SetVector("Kernel", new Vector2(1.0f, 0.0f));
                            //v0.1
		     cmd.Blit( gi4, gi3, segi.material, LUMINA_FAR.Pass.BilateralUpsample);
                            segi.material.SetVector("Kernel", new Vector2(0.0f, 1.0f));

                            //Perform temporal reprojection and blending
                            if (segi.temporalBlendWeight < 1.0f)
                            {
                                //v0.1
		     cmd.Blit( gi3, gi4);
                                //v0.1
		     cmd.Blit( gi4, gi3, segi.material, LUMINA_FAR.Pass.TemporalBlend);
                                //v0.1
		     cmd.Blit( gi3, segi.previousGIResult);
                                //v0.1
		     cmd.Blit( source, segi.previousCameraDepth, segi.material, LUMINA_FAR.Pass.GetCameraDepthTexture);
                            }

                            //Set the result to be accessed in the shader
                            segi.material.SetTexture("GITexture", gi3);

                            //Actually apply the GI to the scene using gbuffer data
                            //v0.1
		     cmd.Blit( source, destination, segi.material, segi.visualizeGI ? LUMINA_FAR.Pass.VisualizeGI : LUMINA_FAR.Pass.BlendWithScene);

                            //Release temporary textures
                            RenderTexture.ReleaseTemporary(gi3);
                            RenderTexture.ReleaseTemporary(gi4);
                        }
                        else    //If Half Resolution tracing is disabled
                        {
                            //Perform temporal reprojection and blending
                            if (segi.temporalBlendWeight < 1.0f)
                            {
                                //v0.1
		     cmd.Blit( gi2, gi1, segi.material, LUMINA_FAR.Pass.TemporalBlend);
                                //v0.1
		     cmd.Blit( gi1, segi.previousGIResult);
                                //v0.1
		     cmd.Blit( source, segi.previousCameraDepth, segi.material, LUMINA_FAR.Pass.GetCameraDepthTexture);
                            }

                            //Actually apply the GI to the scene using gbuffer data
                            segi.material.SetTexture("GITexture", segi.temporalBlendWeight < 1.0f ? gi1 : gi2);
                            //v0.1
		     cmd.Blit( source, destination, segi.material, segi.visualizeGI ? LUMINA_FAR.Pass.VisualizeGI : LUMINA_FAR.Pass.BlendWithScene);

                            //Release temporary textures
                            RenderTexture.ReleaseTemporary(gi1);
                            RenderTexture.ReleaseTemporary(gi2);
                        }

                        //Release temporary textures
                        RenderTexture.ReleaseTemporary(currentDepth);
                        RenderTexture.ReleaseTemporary(currentNormal);

                        //Visualize the sun depth texture
                        if (segi.visualizeSunDepthTexture){
                            //v0.1
		     cmd.Blit( segi.sunDepthTexture, destination);
		     }


                        //Release the temporary reflections result texture
                        if (segi.doReflections)
                        {
                            RenderTexture.ReleaseTemporary(reflections);
                        }

                        //Set matrices/vectors for use during temporal reprojection
                        segi.material.SetMatrix("ProjectionPrev", segi.attachedCamera.projectionMatrix);
                        segi.material.SetMatrix("ProjectionPrevInverse", segi.attachedCamera.projectionMatrix.inverse);
                        segi.material.SetMatrix("WorldToCameraPrev", segi.attachedCamera.worldToCameraMatrix);
                        segi.material.SetMatrix("CameraToWorldPrev", segi.attachedCamera.cameraToWorldMatrix);
                        segi.material.SetVector("CameraPositionPrev", segi.transform.position);

                        //Advance the frame counter
                        segi.frameCounter = (segi.frameCounter + 1) % (64);
                    }
                }
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
#if UNITY_2020_2_OR_NEWER
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                //cmd.ReleaseTemporaryRT(_occluders.id); //v0.1
            }
#else
            /// Cleanup any allocated resources that were created during the execution of this render pass.
            private RenderTargetHandle destination { get; set; }
            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(_occluders.id);
                if (destination != RenderTargetHandle.CameraTarget)
                {
                    cmd.ReleaseTemporaryRT(destination.id);
                    destination = RenderTargetHandle.CameraTarget;
                }                
            }
#endif

            //private void InitializeMaterials()
            //{
            //    _occludersMaterial = new Material(Shader.Find("Hidden/UnlitColor"));
            //    _radialBlurMaterial = new Material(Shader.Find("Hidden/RadialBlur"));
            //}
        }

        private LightScatteringPass _scriptablePass;
        public VolumetricLightScatteringSettings _settings = new VolumetricLightScatteringSettings();

        /// <inheritdoc/>
        public override void Create()
        {
            _scriptablePass = new LightScatteringPass(_settings);

            // Configures where the render pass should be injected.
            _scriptablePass.renderPassEvent = _settings.eventA;// RenderPassEvent.BeforeRenderingPostProcessing;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_scriptablePass);
            //_scriptablePass.SetCameraColorTarget(renderer.cameraColorTarget);//1.0g
        }

    }
}

