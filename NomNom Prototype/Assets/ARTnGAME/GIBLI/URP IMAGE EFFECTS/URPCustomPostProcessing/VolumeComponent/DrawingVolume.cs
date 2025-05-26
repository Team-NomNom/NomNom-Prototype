using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Artngame.GIBLI.SleeplessOwl.URPPostProcessing
{
    [Serializable, VolumeComponentMenu("SleeplessOwl PostProcessing/Drawing")]
    public sealed class DrawingVolume : PostProcessVolumeComponent
    {
        //v0.1
        public IntParameter debugPass = new IntParameter(-1);
        public TextureParameter drawingTex = new TextureParameter(null);
        public FloatParameter shiftCycleTime = new FloatParameter(1.0f);
        public FloatParameter strength = new FloatParameter(0.5f);
        public FloatParameter tiling = new FloatParameter(10.0f);
        public FloatParameter smudge = new FloatParameter(1.0f);
        public FloatParameter depthThreshold = new FloatParameter(0.99f);

        //v0.2
        public const int PASS_WOBB = 1;
        public const int PASS_EDGE = 2;
        public const int PASS_PAPER = 3;

        public const string PROP_WOBB_TEX = "_WobbTex";
        public const string PROP_WOBB_TEX_SCALE = "_WobbScale";
        public const string PROP_WOBB_POWER = "_WobbPower";
        public const string PROP_EDGE_SIZE = "_EdgeSize";
        public const string PROP_EDGE_POWER = "_EdgePower";
        public const string PROP_PAPER_TEX = "_PaperTex";
        public const string PROP_PAPER_SCALE = "_PaperScale";
        public const string PROP_PAPER_POWER = "_PaperPower";

        //public Shader filter;
        public TextureParameter wobbTex = new TextureParameter(null); //public Texture wobbTex;
        public FloatParameter wobbScale = new FloatParameter(1.0f); //public float wobbScale = 1f;
        public FloatParameter wobbPower = new FloatParameter(0.01f); //public float wobbPower = 0.01f;
        public FloatParameter edgeSize = new FloatParameter(1.0f); //public float edgeSize = 1f;
        public FloatParameter edgePower = new FloatParameter(3.0f); //public float edgePower = 3f;
        PaperData[] paperDataset;
        public TextureParameter paperTexA = new TextureParameter(null); //public Texture paperTexA;
        public FloatParameter paperScaleA = new FloatParameter(1.0f); //public float paperScaleA = 1f;
        public FloatParameter paperPowerA = new FloatParameter(1.0f); //public float paperPowerA = 1f;
        public TextureParameter paperTexB = new TextureParameter(null); //public Texture paperTexB;
        public FloatParameter paperScaleB = new FloatParameter(1.0f); //public float paperScaleA = 1f;
        public FloatParameter paperPowerB = new FloatParameter(1.0f); //public float paperPowerA = 1f;
        public TextureParameter paperTexC = new TextureParameter(null); //public Texture paperTexC;
        public FloatParameter paperScaleC = new FloatParameter(1.0f); //public float paperScaleA = 1f;
        public FloatParameter paperPowerC = new FloatParameter(1.0f); //public float paperPowerA = 1f;
                                                                      //END v0.2

        //v0.3
        [Range(0.05f, 5.0f)]
        public FloatParameter edgeWidth = new FloatParameter ( 0.3f );
        //[ColorUsageAttribute(true, true, 0f, 20f, 0.125f, 3f)]
        public ColorParameter edgeColor = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1) );
        [Range(0.0f, 1.0f)]
        public FloatParameter backgroundFade = new FloatParameter(1f );
        //[ColorUsageAttribute(true, true, 0f, 20f, 0.125f, 3f)]
        public ColorParameter backgroundColor = new ColorParameter(new Color(1.0f, 1.0f, 1.0f, 1) );
        //END v0.3



        public FloatParameter SampleDistance = new FloatParameter(0);
        public FloatParameter StrengthPow = new FloatParameter(30);

        Material material;
        public override bool visibleInSceneView => true;
        public override InjectionPoint InjectionPoint => InjectionPoint.AfterOpaqueAndSky;
        public override bool IsActive()
        {
            return SampleDistance.value != 0;
        }

        static class IDs
        {
            internal readonly static int _sampleDistance = Shader.PropertyToID("_sampleDistance");
            internal readonly static int _strengthPow = Shader.PropertyToID("_strengthPow");

            internal readonly static int _drawingTex = Shader.PropertyToID("_DrawingTex");
            internal readonly static int _OverlayOffset = Shader.PropertyToID("_OverlayOffset");
            internal readonly static int _strength = Shader.PropertyToID("_Dtrength");
            internal readonly static int _tiling = Shader.PropertyToID("_Tiling");
            internal readonly static int _smudge = Shader.PropertyToID("_Smudge");
            internal readonly static int _depthThreshold = Shader.PropertyToID("_DepthThreshold");

            //v0.3
            internal static readonly int Params = Shader.PropertyToID("_Params");
            internal static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
            internal static readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
        }

        public void UpdateParameter()
        {
            material.SetFloat(IDs._sampleDistance, SampleDistance.value);
            material.SetFloat(IDs._strengthPow, StrengthPow.value);

            //v0.1
            bool isOffset = (Time.time % shiftCycleTime.value) < (shiftCycleTime.value / 2.0f);
            if (drawingTex != null)
            {
                material.SetTexture(IDs._drawingTex, drawingTex.value);
                //baseMaterial.SetTexture("_DrawingTex", drawingTex);
            }
            //baseMaterial.SetFloat("_OverlayOffset", isOffset ? 0.5f : 0.0f);            
            //material.SetTexture(IDs._drawingTex, drawingTex.value);
            //material.SetFloat(IDs._shiftCycleTime, shiftCycleTime.value);
            material.SetFloat(IDs._OverlayOffset, isOffset ? 0.5f : 0.0f);
            material.SetFloat(IDs._strength, strength.value);
            material.SetFloat(IDs._tiling, tiling.value);
            material.SetFloat(IDs._smudge, smudge.value);
            material.SetFloat(IDs._depthThreshold, depthThreshold.value);

            //v0.2
        }

        public override void Initialize()
        {
            material = CoreUtils.CreateEngineMaterial("SleeplessOwl/Post-Processing/Drawing");
        }

        public override void Render(CommandBuffer cb, Camera camera, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            UpdateParameter();

            //v0.1

            cb.SetPostProcessSourceTexture(source);
            //cb.DrawFullScreenTriangle(material, destination,0);

            //v0.2
            RenderFXImage(cb, source, destination, material);
        }

        // public static void SetPostProcessSourceTexture(this CommandBuffer cb, RenderTargetIdentifier identifier)
        //{
        //    cb.SetGlobalTexture(PostBufferID, identifier);
        //}

        //v0.2
        void RenderFXImage(CommandBuffer cb, RenderTargetIdentifier src, RenderTargetIdentifier dst, Material _filterMat)//RenderTexture src, RenderTexture dst)
        {
            _filterMat.SetTexture(PROP_WOBB_TEX, wobbTex.value);
            _filterMat.SetFloat(PROP_WOBB_TEX_SCALE, wobbScale.value);
            _filterMat.SetFloat(PROP_WOBB_POWER, wobbPower.value);
            _filterMat.SetFloat(PROP_EDGE_SIZE, edgeSize.value);
            _filterMat.SetFloat(PROP_EDGE_POWER, edgePower.value);

            //cb.get
            var rt0 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var rt1 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            //var rt0 = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            //var rt1 = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            PaperData paperData = new PaperData();
            paperData.paperTex = paperTexA.value;
            paperData.paperScale = paperScaleA.value;
            paperData.paperPower = paperPowerA.value;
            paperDataset = new PaperData[3];
            paperDataset[0] = paperData;
            paperData.paperTex = paperTexB.value;
            paperData.paperScale = paperScaleB.value;
            paperData.paperPower = paperPowerB.value;
            paperDataset[1] = paperData;
            paperData.paperTex = paperTexC.value;
            paperData.paperScale = paperScaleC.value;
            paperData.paperPower = paperPowerC.value;
            paperDataset[2] = paperData;

            var nPapers = paperDataset.Length;

            cb.DrawFullScreenTriangle(_filterMat, rt1, PASS_WOBB); //Graphics.Blit(src, rt1, _filterMat, PASS_WOBB);

            Swap(ref rt0, ref rt1);

            if (nPapers > 0)
            {
                cb.SetPostProcessSourceTexture(rt0);//SET MAIN TEXTURE named _PostSource
                cb.DrawFullScreenTriangle(_filterMat, rt1, PASS_EDGE);//Graphics.Blit(rt0, rt1, _filterMat, PASS_EDGE);
                Swap(ref rt0, ref rt1);

                for (var i = 0; i < nPapers; i++)
                {
                    paperDataset[i].SetProps(_filterMat);

                    //Graphics.Blit(rt0, (i == (nPapers - 1) ? dst : rt1), _filterMat, PASS_PAPER);
                    cb.DrawFullScreenTriangle(_filterMat, (i == (nPapers - 1) ? dst : rt1), PASS_WOBB);

                    Swap(ref rt0, ref rt1);
                }
            }
            else
            {
                cb.SetPostProcessSourceTexture(rt0);//SET MAIN TEXTURE named _PostSource
                cb.DrawFullScreenTriangle(_filterMat, dst, PASS_WOBB); //Graphics.Blit(rt0, dst, _filterMat, PASS_EDGE);
            }

            RenderTexture.ReleaseTemporary(rt0);
            RenderTexture.ReleaseTemporary(rt1);

            ////TEST PASSES
            if (debugPass.value != -1)
            {
                //pass 0 - LICENSE 0 Daniel Ilett MIT - Image Ultra
                //passes 1-3 - LICENSE 0 fuqunaga WaterColorFilter

                //pass 4
                if (debugPass.value == 4) //EDGE DETECT - X-EFFECTS
                {
                    _filterMat.SetVector(IDs.Params, new Vector2(edgeWidth.value, backgroundFade.value));
                    _filterMat.SetColor(IDs.EdgeColor, edgeColor.value);
                    _filterMat.SetColor(IDs.BackgroundColor, backgroundColor.value);
                }

                cb.SetPostProcessSourceTexture(src);
                cb.DrawFullScreenTriangle(material, dst, debugPass.value);
            }


            //cb.SetPostProcessSourceTexture(src);
            // cb.DrawFullScreenTriangle(material, dst, 0);
        }
        void Swap(ref RenderTexture src, ref RenderTexture dst)
        {
            var tmp = src;
            src = dst;
            dst = tmp;
        }

        [System.Serializable]
        public class PaperData
        {
            public Texture paperTex;
            public float paperScale = 1f;
            public float paperPower = 1f;

            public void SetProps(Material mat)
            {
                mat.SetTexture(PROP_PAPER_TEX, paperTex);
                mat.SetFloat(PROP_PAPER_SCALE, paperScale);
                mat.SetFloat(PROP_PAPER_POWER, paperPower);
            }
        }
        //END v0.2
    }
}