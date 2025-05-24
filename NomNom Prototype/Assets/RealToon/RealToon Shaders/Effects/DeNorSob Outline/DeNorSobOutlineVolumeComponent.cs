//RealToon - DeNorSob Outline Effect (HDRP - Post Processing)
//MJQStudioWorks
//2024

using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[VolumeComponentMenu("Post-processing/RealToon/DeNorSob Outline")]
[VolumeRequiresRendererFeatures(typeof(DeNorSobOutlineRendererFeature))]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class DeNorSobOutlineVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public DeNorSobOutlineVolumeComponent()
    {
        displayName = "DeNorSob Outline";
    }

    [Space(10)]

    [Header("[RealToon Effects - DeNorSob Outline]")]
    [Header("*Hover your mouse on the option for descriptions/info.")]

    [Space(20)]

    [Tooltip("How thick or thin the outline is.")]
    public MinFloatParameter OutlineWidth = new MinFloatParameter(0f, 0, true);

    [Space(20)]

    [Header("**Depth and Normal Based Outline**")]

    [Tooltip("This will adjust the depth based outline threshold.")]
    public FloatParameter DepthThreshold = new FloatParameter(900.0f, true);

    [Space(10)]

    [Tooltip("This will adjust the normal based outline threshold.")]
    public FloatParameter NormalThreshold = new FloatParameter(1.3f, true);
    [Tooltip("This will adjust the min of the normal to get more normal based outline details.")]
    public FloatParameter NormalMin = new FloatParameter(1.0f, false);
    [Tooltip("This will adjust the max of the normal to get more normal based outline details.")]
    public FloatParameter NormalMax = new FloatParameter(1.0f, false);

    [Space(20)]

    [Header("**Sobel Outline**")]
    [Tooltip("This will render outline all on the screen")]
    public BoolParameter SobelOutline = new BoolParameter(false, false);

    [Tooltip("This will adjust the sobel threshold.\n\n*Sobel Outline is needed to be enabled for this to work.")]

    public MinFloatParameter SobelOutlineThreshold = new MinFloatParameter(300.0f, 0, false);

    [Space(6)]

    [Tooltip("The amount of whites or bright colors to be affected by the outline.\n\n*Sobel Outline is needed to be enabled for this to work.")]
    public FloatParameter WhiteThreshold = new FloatParameter(0.0f, false);

    [Tooltip("The amount of blacks or dark colors to be affected by the outline.\n\n*Sobel Outline is needed to be enabled for this to work.")]
    public FloatParameter BlackThreshold = new FloatParameter(0.0f, false);

    [Space(20)]

    [Header("**Color**")]
    [Tooltip("Outline Color")]
    public ColorParameter OutlineColor = new ColorParameter(Color.black, true);

    [Tooltip("How strong the outline color is.")]
    public FloatParameter ColorIntensity = new FloatParameter(1.0f, true);

    [Tooltip("Mix full screen color image to the outline color.")]
    public BoolParameter MixFullScreenColor = new BoolParameter(false);

    [Space(20)]

    [Header("**Settings**")]
    [Tooltip("Show the outline only.")]
    public BoolParameter ShowOutlineOnly = new BoolParameter(false, true);

    [Space(6)]

    [Tooltip("Mix Depth-Normal Based Outline and Sobel Outline.")]
    public BoolParameter MixDephNormalAndSobelOutline = new BoolParameter(false, true);

    
    public bool IsActive()
    {
        return OutlineWidth.GetValue<float>() > 0.0f;
    }
}
