using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Artngame.GIBLI
{
    [ExecuteInEditMode]
    public class GrabScreenFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public string TextureName = "_GrabPassTransparent";
            public LayerMask LayerMask;
        }

        class GrabPass : ScriptableRenderPass
        {
            RTHandle tempColorTarget; //v0.1
            public Settings settings;

            RenderTargetIdentifier cameraTarget;

            public GrabPass(Settings s)
            {
                settings = s;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                
                //tempColorTarget.Init(settings.TextureName);//v0.1
                tempColorTarget = RTHandles.Alloc(settings.TextureName, name: settings.TextureName); //v0.1
            }

            public void Setup(RenderTargetIdentifier cameraTarget)
            {
                this.cameraTarget = cameraTarget;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                cmd.GetTemporaryRT(Shader.PropertyToID(tempColorTarget.name), cameraTextureDescriptor);//v0.1
                cmd.SetGlobalTexture(settings.TextureName, tempColorTarget);//v0.1
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.Blit( cameraTarget, tempColorTarget);//v0.1

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(Shader.PropertyToID(tempColorTarget.name));//v0.1
            }
        }

        class RenderPass : ScriptableRenderPass
        {
            public Settings settings;
            List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

            FilteringSettings m_FilteringSettings;
            RenderStateBlock m_RenderStateBlock;

            public RenderPass(Settings settings)
            {
                this.settings = settings;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;

                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));

                m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, settings.LayerMask);
                m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawSettings;
                drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        GrabPass grabPass;
        RenderPass renderPass;
        [SerializeField] public Settings settings;

        public override void Create()
        {
            grabPass = new GrabPass(settings);
            renderPass = new RenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_2022_1_OR_NEWER
            grabPass.Setup(renderingData.cameraData.renderer.cameraColorTargetHandle);//renderer.cameraColorTarget);//v0.1
#else
            grabPass.Setup(renderer.cameraColorTarget);//v0.1
#endif
            renderer.EnqueuePass(grabPass);
            renderer.EnqueuePass(renderPass);
        }
    }
}
