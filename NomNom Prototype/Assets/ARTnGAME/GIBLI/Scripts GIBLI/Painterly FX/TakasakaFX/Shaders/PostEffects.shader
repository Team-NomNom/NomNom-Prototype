Shader "Hidden/PostEffects"
{

	Properties
	{
		//_MainTex("Main Texture", 2D) = "white" {}
	    effectsChoice("effectsChoice", Int) = 0
		//AKF
		_AKFRadius("_AKFRadius", Float) = 1
		_AKFMaskRadius("_AKFMaskRadius", Float) = 1
		_AKFSharpness("_AKFSharpness", Vector) = (1,1,1,1)
		_AKFSampleStep("_AKFSampleStep", Int) = 2
		_AKFOverlapX("_AKFOverlapX", Float) = 1
		_AKFOverlapY("_AKFOverlapY", Float) = 1

		//LIC
		_LICScale("_LICScale", Float) = 1
		_LICMaxLen("_LICMaxLen", Float) = 1
		_LICVariance("_LICVariance", Float) = 1

	}

	CGINCLUDE
			#include "UnityCG.cginc"
			#include "Common.cginc"
			#include "Noise.cginc"
			#include "Color.cginc"
			#include "Canvas.cginc"
			#include "Edge.cginc"
			#include "Smooth.cginc"
			#include "AKF.cginc"
			#include "SBR.cginc"
			#include "BF.cginc"
			#include "WCR.cginc"
			#include "FXDoG.cginc"


			//START GRAPH
			uniform float4 _BlitScaleBias;
			uniform float4 _BlitScaleBiasRt;
			uniform float _BlitMipLevel;
			uniform float2 _BlitTextureSize;
			uniform uint _BlitPaddingSize;
			uniform int _BlitTexArraySlice;
			uniform float4 _BlitDecodeInstructions;			          

			// Generates a triangle in homogeneous clip space, s.t.
			// v0 = (-1, -1, 1), v1 = (3, -1, 1), v2 = (-1, 3, 1).
			float2 GetFullScreenTriangleTexCoord(uint vertexID)
			{
				#if UNITY_UV_STARTS_AT_TOP
					return float2((vertexID << 1) & 2, 1.0 - (vertexID & 2));
				#else
					return float2((vertexID << 1) & 2, vertexID & 2);
				#endif
			}

			float4 GetFullScreenTriangleVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
			{
				// note: the triangle vertex position coordinates are x2 so the returned UV coordinates are in range -1, 1 on the screen.
				float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
				float4 pos = float4(uv * 2.0 - 1.0, z, 1.0);
			#ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
				pos = ApplyPretransformRotation(pos);
			#endif
				return pos;
			}

			#if SHADER_API_GLES
				struct AttributesB
				{
					float4 positionOS       : POSITION;
					float2 uv               : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
				#else
				struct AttributesB
				{
					uint vertexID : SV_VertexID;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
			#endif

			struct VaryingsB
			{
					float4 positionCS : SV_POSITION;
					float2 uv   : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
			};
			VaryingsB VertABC(AttributesB input)
			{
				VaryingsB output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if SHADER_API_GLES
					float4 pos = input.positionOS;
					float2 uv  = input.uv;
				#else
					float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
					float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
				#endif

				output.positionCS = pos;
				output.uv   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
				return output;
			}
			//END GRAPH
	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			Name "Entry"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragEntry
			ENDCG
		}

		Pass //1
		{
			Stencil
			{
				Ref 1
				Comp Equal
			}

			Name "MaskFace"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragMask
			float4 fragMask(v2f_img i) : SV_Target{ return 1.0; }
			ENDCG
		}
		Pass //2
		{
			Stencil
			{
				Ref 2
				Comp Equal
			}

			Name "MaskBody"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragMask
			float4 fragMask(v2f_img i) : SV_Target{ return 0.5; }
			ENDCG
		}
		
		Pass //3
		{
			Name "SBR"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSBR
			ENDCG
		}
		Pass//4
		{
			Name "WCR"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragWCR
			ENDCG
		}
		Pass//5
		{
			Name "HandTremor"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragHandTremor
			ENDCG
		}
		Pass//6
		{
			Name "BF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragBF
			ENDCG
		}
		Pass//7
		{
			Name "FBF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFBF
			ENDCG
		}
		Pass//8
		{
			Name "AKF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragAKF
			ENDCG
		}
		Pass//9
		{
			Name "SNN"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSNN
			ENDCG
		}
		Pass//10
		{
			Name "Posterize"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragPosterize
			ENDCG
		}
		Pass//11
		{
			Name "Outline"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragOutline
			ENDCG
		}
		Pass//12
		{
			Name "FXDoGGradient"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFXDoGGradient
			ENDCG
		}
		Pass//13
		{
			Name "FXDoGTangent"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFXDoGTangent
			ENDCG
		}
		Pass//14
		{
			Name "TFM"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragTFM
			ENDCG
		}
		Pass//15
		{
			Name "LIC"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragLIC
			ENDCG
		}
		Pass//16
		{
			Name "Lerp"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragLerp
			ENDCG
		}
		Pass//17
		{
			Name "Sobel3"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSobel3
			ENDCG
		}
		Pass//18
		{
			Name "GBlur"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragGBlur
			ENDCG
		}
		Pass//19
		{
			Name "GBlur2"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragGBlur2
			ENDCG
		}
		Pass//20
		{
			Name "Sharpen"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSharpen
			ENDCG
		}
		Pass//21
		{
			Name "UnsharpMask"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragUnsharpMask
			ENDCG
		}
		Pass//22
		{
			Name "Complementary"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragComplementary
			ENDCG
		}
		Pass//23
		{
			Name "RGB2HSV"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2HSV
			ENDCG
		}
		Pass//24
		{
			Name "HSV2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragHSV2RGB
			ENDCG
		}
		Pass//25
		{
			Name "RGB2HSL"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2HSL
			ENDCG
		}
		Pass//26
		{
			Name "HSL2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragHSL2RGB
			ENDCG
		}
		Pass//27
		{
			Name "RGB2YUV"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2YUV
			ENDCG
		}
		Pass//28
		{
			Name "YUV2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragYUV2RGB
			ENDCG
		}
		Pass//29
		{
			Name "RGB2LAB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2LAB
			ENDCG
		}
		Pass//30
		{
			Name "LAB2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragLAB2RGB
			ENDCG
		}
		Pass//31
		{
			Name "GNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragGNoise
			ENDCG
		}
		Pass//32
		{
			Name "SNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSNoise
			ENDCG
		}
		Pass//33
		{
			Name "FNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFNoise
			ENDCG
		}
		Pass//34
		{
			Name "VNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragVNoise
			ENDCG
		}

		Pass//35
		{
			Name "Test"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragTest
			ENDCG
		}
		Pass//36
		{
			Name "TestBF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragTestBF
			ENDCG
		}






		//////////////////// GRAPH

		Pass //0 + 37 = 37 !!!!!!!!!!!!!!!!!!!
		{
			Name "Entry"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragEntry
			ENDCG
		}

		Pass //1 + 37 = 38 !!!!!!!!!!!!!!!!!!!
		{
			Stencil
			{
				Ref 1
				Comp Equal
			}

			Name "MaskFace"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragMask
			float4 fragMask(v2f_img i) : SV_Target{ return 1.0; }
			ENDCG
		}
		Pass //2
		{
			Stencil
			{
				Ref 2
				Comp Equal
			}

			Name "MaskBody"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragMask
			float4 fragMask(v2f_img i) : SV_Target{ return 0.5; }
			ENDCG
		}
		
		Pass //3
		{
			Name "SBR"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragSBR
			ENDCG
		}
		Pass//4
		{
			Name "WCR"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragWCR
			ENDCG
		}
		Pass//5
		{
			Name "HandTremor"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragHandTremor
			ENDCG
		}
		Pass//6
		{
			Name "BF"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragBF
			ENDCG
		}
		Pass//7
		{
			Name "FBF"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragFBF
			ENDCG
		}
		Pass//8
		{
			Name "AKF"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragAKF
			ENDCG
		}
		Pass//9
		{
			Name "SNN"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragSNN
			ENDCG
		}
		Pass//10
		{
			Name "Posterize"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragPosterize
			ENDCG
		}
		Pass//11
		{
			Name "Outline"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragOutline
			ENDCG
		}
		Pass//12
		{
			Name "FXDoGGradient"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragFXDoGGradient
			ENDCG
		}
		Pass//13
		{
			Name "FXDoGTangent"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragFXDoGTangent
			ENDCG
		}
		Pass//14
		{
			Name "TFM"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragTFM
			ENDCG
		}
		Pass//15
		{
			Name "LIC"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragLIC
			ENDCG
		}
		Pass//16
		{
			Name "Lerp"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragLerp
			ENDCG
		}
		Pass//17
		{
			Name "Sobel3"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragSobel3
			ENDCG
		}
		Pass//18
		{
			Name "GBlur"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragGBlur
			ENDCG
		}
		Pass//19
		{
			Name "GBlur2"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragGBlur2
			ENDCG
		}
		Pass//20
		{
			Name "Sharpen"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragSharpen
			ENDCG
		}
		Pass//21
		{
			Name "UnsharpMask"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragUnsharpMask
			ENDCG
		}
		Pass//22
		{
			Name "Complementary"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragComplementary
			ENDCG
		}
		Pass//23
		{
			Name "RGB2HSV"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragRGB2HSV
			ENDCG
		}
		Pass//24
		{
			Name "HSV2RGB"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragHSV2RGB
			ENDCG
		}
		Pass//25
		{
			Name "RGB2HSL"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragRGB2HSL
			ENDCG
		}
		Pass//26
		{
			Name "HSL2RGB"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragHSL2RGB
			ENDCG
		}
		Pass//27
		{
			Name "RGB2YUV"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragRGB2YUV
			ENDCG
		}
		Pass//28
		{
			Name "YUV2RGB"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragYUV2RGB
			ENDCG
		}
		Pass//29
		{
			Name "RGB2LAB"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragRGB2LAB
			ENDCG
		}
		Pass//30
		{
			Name "LAB2RGB"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragLAB2RGB
			ENDCG
		}
		Pass//31
		{
			Name "GNoise"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragGNoise
			ENDCG
		}
		Pass//32
		{
			Name "SNoise"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragSNoise
			ENDCG
		}
		Pass//33
		{
			Name "FNoise"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragFNoise
			ENDCG
		}
		Pass//34
		{
			Name "VNoise"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragVNoise
			ENDCG
		}

		Pass//35
		{
			Name "Test"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragTest
			ENDCG
		}
		Pass//36+37 = 73
		{
			Name "TestBF"
			CGPROGRAM
			#pragma vertex VertABC
			#pragma fragment fragTestBF
			ENDCG
		}

		///////////////////// END GRAPH
	}
}
