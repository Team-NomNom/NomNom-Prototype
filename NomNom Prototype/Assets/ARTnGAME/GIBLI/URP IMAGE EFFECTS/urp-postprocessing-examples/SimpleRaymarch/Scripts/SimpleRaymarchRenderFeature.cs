using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Artngame.GIBLI.Toguchi.Rendering
{
    public class SimpleRaymarchRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            public Shader shader;
        }

        public Settings settings = new Settings();

        private SimpleRaymarchPass _pass;

        public override void Create()
        {
            this.name = "SimpleRaymarch";
            _pass = new SimpleRaymarchPass(settings.renderPassEvent, settings.shader);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //_pass.Setup(renderer.cameraColorTarget);//v1.5
            renderer.EnqueuePass(_pass);
        }
    }
}