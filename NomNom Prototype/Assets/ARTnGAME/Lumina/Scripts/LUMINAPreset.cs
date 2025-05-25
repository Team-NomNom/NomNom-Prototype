using UnityEngine;
using System.Collections;
namespace Artngame.LUMINA
{
    public class LUMINAPreset : ScriptableObject
    {
        public Artngame.LUMINA.LUMINA.VoxelResolution voxelResolution = Artngame.LUMINA.LUMINA.VoxelResolution.high;
        public bool voxelAA = false;
        [Range(0, 2)]
        public int innerOcclusionLayers = 1;
        public bool infiniteBounces = true;

        [Range(0.01f, 1.0f)]
        public float temporalBlendWeight = 0.15f;
        public bool useBilateralFiltering = true;
        public bool halfResolution = true;
        public bool stochasticSampling = true;
        public bool doReflections = true;

        [Range(1, 128)]
        public int cones = 13;
        [Range(1, 32)]
        public int coneTraceSteps = 8;
        [Range(0.1f, 2.0f)]
        public float coneLength = 1.0f;
        [Range(0.5f, 6.0f)]
        public float coneWidth = 6.0f;
        [Range(0.0f, 4.0f)]
        public float coneTraceBias = 0.63f;
        [Range(0.0f, 4.0f)]
        public float occlusionStrength = 1.0f;
        [Range(0.0f, 4.0f)]
        public float nearOcclusionStrength = 0.0f;
        [Range(0.001f, 4.0f)]
        public float occlusionPower = 1.0f;
        [Range(0.0f, 14.0f)]
        public float nearLightGain = 1.0f;
        [Range(0.0f, 14.0f)]
        public float giGain = 1.0f;
        [Range(0.0f, 14.0f)]
        public float secondaryBounceGain = 1.0f;
        [Range(12, 128)]
        public int reflectionSteps = 64;
        [Range(0.001f, 4.0f)]
        public float reflectionOcclusionPower = 1.0f;
        [Range(0.0f, 1.0f)]
        public float skyReflectionIntensity = 1.0f;
        public bool gaussianMipFilter = false;

        [Range(0.1f, 4.0f)]
        public float farOcclusionStrength = 1.0f;
        [Range(0.1f, 4.0f)]
        public float farthestOcclusionStrength = 1.0f;

        [Range(3, 16)]
        public int secondaryCones = 6;
        [Range(0.1f, 4.0f)]
        public float secondaryOcclusionStrength = 1.0f;

        //v0.6
        public float contrastA = 0;
        public Vector4 ReflectControl = new Vector4(1, 1, 0, 0);

        //v0.7
        public Vector4 DitherControl = new Vector4(0, 1, 1, 1);

        //v1.2
        public float smoothNormals = 0;

        //v1.3
        public bool proxyNoGeom = false;

        //v1.5
        public bool enableLocalLightGI = false;
        public float shadowedLocalPower = 0;
        public float shadowlessLocalPower = 0;
        public float shadowlessLocalOcclusion = 0;

        //v1.6
        public Vector3 cutoff = new Vector3(1, 1, 0);
    }
}