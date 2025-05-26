using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/*
 * Blit Renderer Feature                                                https://github.com/Cyanilux/URP_BlitRenderFeature
 * ------------------------------------------------------------------------------------------------------------------------
 * Based on the Blit from the UniversalRenderingExamples
 * https://github.com/Unity-Technologies/UniversalRenderingExamples/tree/master/Assets/Scripts/Runtime/RenderPasses
 * 
 * Extended to allow for :
 * - Specific access to selecting a source and destination (via current camera's color / texture id / render texture object
 * - Automatic switching to using _AfterPostProcessTexture for After Rendering event, in order to correctly handle the blit after post processing is applied
 * - Setting a _InverseView matrix (cameraToWorldMatrix), for shaders that might need it to handle calculations from screen space to world.
 *     e.g. reconstruct world pos from depth : https://twitter.com/Cyanilux/status/1269353975058501636 
 * ------------------------------------------------------------------------------------------------------------------------
 * @Cyanilux
*/
namespace Artngame.GIBLI
{
    public class Blit : ScriptableRendererFeature
    {

        public class BlitPass : ScriptableRenderPass
        {

            public Material blitMaterial = null;
            public FilterMode filterMode { get; set; }

            private BlitSettings settings;

            private RenderTargetIdentifier source { get; set; }
            private RenderTargetIdentifier destination { get; set; }

            RTHandle m_TemporaryColorTexture; //v0.1
            RTHandle m_DestinationTexture; //v0.1
            string m_ProfilerTag;

            public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag)
            {
                this.renderPassEvent = renderPassEvent;
                this.settings = settings;
                blitMaterial = settings.blitMaterial;
                m_ProfilerTag = tag;
                //m_TemporaryColorTexture.Init("_TemporaryColorTexture"); //v0.1
                m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture"); //v0.1
                if (settings.dstType == Target.TextureID)
                {
                    //m_DestinationTexture.Init(settings.dstTextureId); //v0.1
                    m_DestinationTexture = RTHandles.Alloc(settings.dstTextureId, name: settings.dstTextureId); //v0.1
                }
            }

            public void Setup(RenderTargetIdentifier source, RenderTargetIdentifier destination)
            {
                this.source = source;
                this.destination = destination;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                if (settings.setInverseViewMatrix)
                {
                    Shader.SetGlobalMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);
                }

                if (settings.dstType == Target.TextureID)
                {
                    cmd.GetTemporaryRT(Shader.PropertyToID(m_DestinationTexture.name), opaqueDesc, filterMode); //v0.1
                }

                //Debug.Log($"src = {source},     dst = {destination} ");
                // Can't read and write to same color target, use a TemporaryRT
                if (source == destination || (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor))
                {
                    cmd.GetTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name), opaqueDesc, filterMode); //v0.1
                    cmd.Blit( source, m_TemporaryColorTexture, blitMaterial, settings.blitMaterialPassIndex); //v0.1
                    cmd.Blit( m_TemporaryColorTexture, destination); //v0.1
                }
                else
                {
                    cmd.Blit( source, destination, blitMaterial, settings.blitMaterialPassIndex); //v0.1
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (settings.dstType == Target.TextureID)
                {
                    cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_DestinationTexture.name)); //v0.1
                }
                if (source == destination || (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor))
                {
                    cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name)); //v0.1
                }
            }
        }

        [System.Serializable]
        public class BlitSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Material blitMaterial = null;
            public int blitMaterialPassIndex = 0;
            public bool setInverseViewMatrix = false;

            public Target srcType = Target.CameraColor;
            public string srcTextureId = "_CameraColorTexture";
            public RenderTexture srcTextureObject;

            public Target dstType = Target.CameraColor;
            public string dstTextureId = "_BlitPassTexture";
            public RenderTexture dstTextureObject;
        }

        public enum Target
        {
            CameraColor,
            TextureID,
            RenderTextureObject
        }

        public BlitSettings settings = new BlitSettings();

        BlitPass blitPass;

        private RenderTargetIdentifier srcIdentifier, dstIdentifier;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            blitPass = new BlitPass(settings.Event, settings, name);

            if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing)
            {
                Debug.LogWarning("Note that the \"After Rendering Post Processing\"'s Color target doesn't seem to work? (or might work, but doesn't contain the post processing) :( -- Use \"After Rendering\" instead!");
            }

            UpdateSrcIdentifier();
            UpdateDstIdentifier();
        }

        private void UpdateSrcIdentifier()
        {
            srcIdentifier = UpdateIdentifier(settings.srcType, settings.srcTextureId, settings.srcTextureObject);
        }

        private void UpdateDstIdentifier()
        {
            dstIdentifier = UpdateIdentifier(settings.dstType, settings.dstTextureId, settings.dstTextureObject);
        }

        private RenderTargetIdentifier UpdateIdentifier(Target type, string s, RenderTexture obj)
        {
            if (type == Target.RenderTextureObject)
            {
                return obj;
            }
            else if (type == Target.TextureID)
            {
                //RenderTargetHandle m_RTHandle = new RenderTargetHandle();
                //m_RTHandle.Init(s);
                //return m_RTHandle.Identifier();
                return s;
            }
            return new RenderTargetIdentifier();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {

            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing)
            {
            }
            else if (settings.Event == RenderPassEvent.AfterRendering && renderingData.postProcessingEnabled)
            {
                // If event is AfterRendering, and src/dst is using CameraColor, switch to _AfterPostProcessTexture instead.
                if (settings.srcType == Target.CameraColor)
                {
                    settings.srcType = Target.TextureID;
                    settings.srcTextureId = "_AfterPostProcessTexture";
                    UpdateSrcIdentifier();
                }
                if (settings.dstType == Target.CameraColor)
                {
                    settings.dstType = Target.TextureID;
                    settings.dstTextureId = "_AfterPostProcessTexture";
                    UpdateDstIdentifier();
                }
            }
            else
            {
                // If src/dst is using _AfterPostProcessTexture, switch back to CameraColor
                if (settings.srcType == Target.TextureID && settings.srcTextureId == "_AfterPostProcessTexture")
                {
                    settings.srcType = Target.CameraColor;
                    settings.srcTextureId = "";
                    UpdateSrcIdentifier();
                }
                if (settings.dstType == Target.TextureID && settings.dstTextureId == "_AfterPostProcessTexture")
                {
                    settings.dstType = Target.CameraColor;
                    settings.dstTextureId = "";
                    UpdateDstIdentifier();
                }
            }
#if UNITY_2022_1_OR_NEWER
            var src = (settings.srcType == Target.CameraColor) ? renderingData.cameraData.renderer.cameraColorTargetHandle : srcIdentifier; //v0.1
            var dest = (settings.dstType == Target.CameraColor) ? renderingData.cameraData.renderer.cameraColorTargetHandle : dstIdentifier; //v0.1
#else
            var src = (settings.srcType == Target.CameraColor) ? renderingData.cameraData.renderer.cameraColorTarget : srcIdentifier; //v0.1
            var dest = (settings.dstType == Target.CameraColor) ? renderingData.cameraData.renderer.cameraColorTarget : dstIdentifier; //v0.1
#endif
            blitPass.Setup(src, dest);
            renderer.EnqueuePass(blitPass);
        }
    }
}
