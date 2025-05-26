using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Artngame.GIBLI.Toguchi.Rendering
{
    public class DepthLinePass : CustomPostProcessingPass<DepthLine>
    {
        protected override string RenderTag => "DepthLine";

        protected override void BeforeRender(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            commandBuffer.SetGlobalFloat("_DepthLineBias", Component.bias.value);
            commandBuffer.SetGlobalFloat("_DepthLineWidth", Component.lineWidth.value);
            commandBuffer.SetGlobalFloat("_DepthLineSamplingRate", Component.samplingScale.value);
            commandBuffer.SetGlobalFloat("_DepthLineMaxWeight", Component.maxWeight.value);
        }
#if UNITY_2023_3_OR_NEWER
        protected override void BeforeRenderRG(CommandBuffer commandBuffer, PassData renderingData)
        {
            commandBuffer.SetGlobalFloat("_DepthLineBias", Component.bias.value);
            commandBuffer.SetGlobalFloat("_DepthLineWidth", Component.lineWidth.value);
            commandBuffer.SetGlobalFloat("_DepthLineSamplingRate", Component.samplingScale.value);
            commandBuffer.SetGlobalFloat("_DepthLineMaxWeight", Component.maxWeight.value);
        }
#endif
        protected override bool IsActive()
        {
            return Component.IsActive();
        }

        public DepthLinePass(RenderPassEvent renderPassEvent, Shader shader) : base(renderPassEvent, shader)
        {
        }
    }
}