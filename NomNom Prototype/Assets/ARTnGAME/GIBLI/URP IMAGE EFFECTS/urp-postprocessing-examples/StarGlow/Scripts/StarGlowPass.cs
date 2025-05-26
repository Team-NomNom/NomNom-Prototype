using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Artngame.GIBLI.Toguchi.Rendering
{
    public class StarGlowPass : ScriptableRenderPass
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
        public void OnCameraSetupA(CommandBuffer cmd, PassData data)
        {
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            var renderer = data.cameraData.renderer;

            this.currentRenderTarget = data.colorTargetHandleA;        
        }
        void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
        {
            CommandBuffer unsafeCmd = command;
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            if (data.cameraData.camera == Camera.main)
            {
                //v1.6
                if (Camera.main != null && data.cameraData.camera == Camera.main)
                {
                    //cmd.Blit(source, source, outlineMaterial, 0);
                }

                var commandBuffer = command;
                // RenderRG(commandBuffer, data);

                if (!isCreatedMaterial)
                {
                    return;
                }

                if (!data.cameraData.postProcessEnabled)
                {
                    return;
                }

                var volumeStack = VolumeManager.instance.stack;
                starGlow = volumeStack.GetComponent<StarGlow>();
                if (starGlow == null)
                {
                    return;
                }
                if (!starGlow.IsActive())
                {
                    return;
                }

                //var command = CommandBufferPool.Get(RenderTag);
                RenderRG(command, data);
            }
        }
        private void RenderRG(CommandBuffer command, PassData data)
        {
            ref var cameraData = ref data.cameraData;

            var source = currentRenderTarget;
            var destination = TempTargetId;
            var blurTex1 = BlurTex1Id;
            var blurTex2 = BlurTex2Id;
            var composite = CompositeTargetId;

            var width = cameraData.camera.scaledPixelWidth;
            var height = cameraData.camera.scaledPixelHeight;

            GetTremporaryRT(command, width, height, destination);
            GetTremporaryRT(command, width, height, blurTex1);
            GetTremporaryRT(command, width, height, blurTex2);
            GetTremporaryRT(command, width, height, composite);

            command.SetGlobalVector(ParameterId,
                 new Vector4(starGlow.Threshold.value, starGlow.Intensity.value, starGlow.Attenuation.value, 1f));
            command.SetGlobalTexture(MainTexId, source);
            command.Blit(source, destination, starGlowMaterial, 0);

            var angle = 360f / starGlow.StreakCount.value;

            for (int i = 1; i <= starGlow.StreakCount.value; i++)
            {
                var offset = (Quaternion.AngleAxis(angle * i + starGlow.Angle.value, Vector3.forward) * Vector2.down).normalized;
                command.SetGlobalVector(OffsetId, new Vector2(offset.x, offset.y));
                command.SetGlobalInt(IterationId, 1);
                command.Blit(destination, blurTex1, starGlowMaterial, 1);

                for (int j = 2; j <= starGlow.Iteration.value; j++)
                {
                    command.SetGlobalInt(IterationId, 1);
                    command.Blit(blurTex1, blurTex2, starGlowMaterial, 1);

                    // swap
                    var temp = blurTex1;
                    blurTex1 = blurTex2;
                    blurTex2 = temp;
                }
                command.Blit(blurTex1, composite, starGlowMaterial, 2);
            }
            command.SetGlobalTexture(CompositeTexId, composite);
            command.Blit(source, source, starGlowMaterial, 3);
            command.ReleaseTemporaryRT(destination);
            command.ReleaseTemporaryRT(blurTex1);
            command.ReleaseTemporaryRT(blurTex2);
            command.ReleaseTemporaryRT(composite);
        }
#endif







        private static readonly string RenderTag = "StarGlow";
        private static readonly string ShaderName = "Toguchi/PostProcessing/StarGlow";

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int TempTargetId = Shader.PropertyToID("_TempTargetStarGlow");
        private static readonly int BlurTex1Id = Shader.PropertyToID("_BlurTex1");
        private static readonly int BlurTex2Id = Shader.PropertyToID("_BlurTex2");
        private static readonly int CompositeTargetId = Shader.PropertyToID("_CompositeTarget");

        private static readonly int ParameterId = Shader.PropertyToID("_Parameter");
        private static readonly int CompositeTexId = Shader.PropertyToID("_CompositeTex");
        private static readonly int IterationId = Shader.PropertyToID("_Iteration");
        private static readonly int OffsetId = Shader.PropertyToID("_Offset");


        private StarGlow starGlow;
        private Material starGlowMaterial;
        private RenderTargetIdentifier currentRenderTarget;
        private bool isCreatedMaterial;

        public StarGlowPass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            var shader = Shader.Find(ShaderName);
            if(shader == null)
            {
                Debug.LogError($"Shader = {ShaderName} が存在しません");
                return;
            }

            starGlowMaterial = CoreUtils.CreateEngineMaterial(shader);
            isCreatedMaterial = true;
        }

        public void SetupPass(in RenderTargetIdentifier currentRenderTarget)
        {
            this.currentRenderTarget = currentRenderTarget;
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
#if UNITY_2022_1_OR_NEWER
            this.currentRenderTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;//renderer.cameraColorTarget;  //v0.1
#else
            this.currentRenderTarget = renderer.cameraColorTarget;  //v0.1
#endif
        }
#endif

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(!isCreatedMaterial)
            {
                return;
            }

            if(!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            var volumeStack = VolumeManager.instance.stack;
            starGlow = volumeStack.GetComponent<StarGlow>();
            if(starGlow == null)
            {
                return;
            }
            if(!starGlow.IsActive())
            {
                return;
            }

            var command = CommandBufferPool.Get(RenderTag);
            Render(command, ref renderingData);
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }

        private void Render(CommandBuffer command, ref RenderingData renderingData)
        {                                                        
            ref var cameraData = ref renderingData.cameraData;

            var source = currentRenderTarget;
            var destination = TempTargetId;
            var blurTex1 = BlurTex1Id;
            var blurTex2 = BlurTex2Id;
            var composite = CompositeTargetId;

            var width = cameraData.camera.scaledPixelWidth;
            var height = cameraData.camera.scaledPixelHeight;

            GetTremporaryRT(command, width, height, destination);
            GetTremporaryRT(command, width, height, blurTex1);
            GetTremporaryRT(command, width, height, blurTex2);
            GetTremporaryRT(command, width, height, composite);

            command.SetGlobalVector(ParameterId,
                 new Vector4(starGlow.Threshold.value, starGlow.Intensity.value, starGlow.Attenuation.value, 1f));

            command.SetGlobalTexture(MainTexId, source);

            command.Blit(source, destination, starGlowMaterial, 0);

            var angle = 360f / starGlow.StreakCount.value;

            for (int i = 1; i <= starGlow.StreakCount.value; i++)
            {
                var offset = (Quaternion.AngleAxis(angle * i + starGlow.Angle.value, Vector3.forward) * Vector2.down).normalized;
                command.SetGlobalVector(OffsetId, new Vector2(offset.x, offset.y));
                command.SetGlobalInt(IterationId, 1);

                command.Blit(destination, blurTex1, starGlowMaterial, 1);

                for(int j = 2; j <= starGlow.Iteration.value; j++)
                {
                    command.SetGlobalInt(IterationId, 1);
                    command.Blit(blurTex1, blurTex2, starGlowMaterial, 1);

                    // swap
                    var temp = blurTex1;
                    blurTex1 = blurTex2;
                    blurTex2 = temp;
                }

                command.Blit(blurTex1, composite, starGlowMaterial, 2);
            }

            command.SetGlobalTexture(CompositeTexId, composite);
            command.Blit(source, source, starGlowMaterial, 3);

            command.ReleaseTemporaryRT(destination);
            command.ReleaseTemporaryRT(blurTex1);
            command.ReleaseTemporaryRT(blurTex2);
            command.ReleaseTemporaryRT(composite);
        }

        private void GetTremporaryRT(CommandBuffer command, int width, int height, int destination)
        {
            command.GetTemporaryRT(destination,
                width / starGlow.Divide.value, height / starGlow.Divide.value, 0,
                FilterMode.Point,
                RenderTextureFormat.Default);
        }
    }
}
