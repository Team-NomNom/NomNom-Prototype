using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
namespace Artngame.GIBLI.Utilities//UnityEngine.Rendering.Universal //v0.1
{
    public class Blit : ScriptableRendererFeature
    {
        [System.Serializable]
        public class BlitSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
            
            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
            public Target destination = Target.Color;
            public string textureId = "_BlitPassTexture";
        }
        
        public enum Target
        {
            Color,
            Texture
        }

        public BlitSettings settings = new BlitSettings();
#if UNITY_2022_1_OR_NEWER
        RTHandle m_RenderTextureHandle;//RenderTargetHandle m_RenderTextureHandle; //v0.1
#else
        RenderTargetHandle m_RenderTextureHandle; //v0.1
#endif

        BlitPass blitPass;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            blitPass = new BlitPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);

            ///v0.1
            //m_RenderTextureHandle.Init(settings.textureId);
#if UNITY_2022_1_OR_NEWER
            m_RenderTextureHandle = RTHandles.Alloc(settings.textureId, name: settings.textureId); //v0.1
#else
            m_RenderTextureHandle.Init(settings.textureId);
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_2022_1_OR_NEWER
            var src = renderer.cameraColorTargetHandle;// renderer.cameraColorTarget;//v0.1
            var dest = (settings.destination == Target.Color) ? renderingData.cameraData.renderer.cameraColorTargetHandle : m_RenderTextureHandle;// RenderTargetHandle.CameraTarget : m_RenderTextureHandle;//v0.1
#else
            var src = renderer.cameraColorTarget;// renderer.cameraColorTarget;//v0.1
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : m_RenderTextureHandle;//v0.1

#endif
            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            blitPass.Setup(src, dest);
            renderer.EnqueuePass(blitPass);
        }
    }
}
