using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
namespace Artngame.GIBLI.Utilities//UnityEngine.Rendering.Universal//.Experiemntal.Rendering.Universal //v0.1
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    internal class BlitPass : ScriptableRenderPass
    {
        public enum RenderTarget
        {
            Color,
            RenderTexture,
        }

        public Material blitMaterial = null;
        public int blitShaderPassIndex = 0;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }

#if UNITY_2022_1_OR_NEWER
        private RTHandle destination { get; set; }// RenderTargetHandle destination { get; set; } //v0.1        
#else
        private RenderTargetHandle destination { get; set; }// RenderTargetHandle destination { get; set; } //v0.1
#endif

        RTHandle m_TemporaryColorTexture;// RenderTargetHandle m_TemporaryColorTexture; //v0.1
        string m_ProfilerTag;

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public BlitPass(RenderPassEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            this.blitShaderPassIndex = blitShaderPassIndex;
            m_ProfilerTag = tag;
            //m_TemporaryColorTexture.Init("_TemporaryColorTexture");//v0.1
            m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture"); //v0.1
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
#if UNITY_2022_1_OR_NEWER
        public void Setup(RenderTargetIdentifier source, RTHandle destination)// RenderTargetHandle destination) //v0.1
        {
            this.source = source;
            this.destination = destination;
        }
#else
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)// RenderTargetHandle destination) //v0.1
        {
            this.source = source;
            this.destination = destination;
        }
#endif

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // Can't read and write to same color target, create a temp render target to blit. 
#if UNITY_2022_1_OR_NEWER
            if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//RenderTargetHandle.CameraTarget) //v0.1
            {
#else
            if (destination == RenderTargetHandle.CameraTarget) //v0.1
            {
#endif
                cmd.GetTemporaryRT( Shader.PropertyToID(m_TemporaryColorTexture.name), opaqueDesc, filterMode);
                //v0.1
                cmd.Blit( source, m_TemporaryColorTexture, blitMaterial, blitShaderPassIndex);
                //v0.1
                cmd.Blit( m_TemporaryColorTexture, source);
            }
            else
            {
                //v0.1
#if UNITY_2022_1_OR_NEWER
                cmd.Blit(source, destination, blitMaterial, blitShaderPassIndex);
#else
                Blit(cmd, source, destination.id, blitMaterial, blitShaderPassIndex);
#endif
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            //if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//RenderTargetHandle.CameraTarget) //v0.1
             //   cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
        }
    }
}
