// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
// VFX MIKE TUTORIAL - http://vfxmike.blogspot.com/2018/08/lucky-bioms-clouds.html
Shader "_Clouds/Clouds Unlit Vertex Color3 TEX DEPTH"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[ASEBegin]_TopTexture0("Top Texture 0", 2D) = "white" {}
		_NoiseScaleA("NoiseScale A", Vector) = (1,1,1,0)
		_3dNoiseSizeA("3dNoise Size A", Float) = 0
		_SpeedA("Speed A", Float) = 0
		_DirectionA("Direction A", Vector) = (1,0,0,0)
		_NoiseStrengthA("Noise Strength A", Range(0 , 1)) = 0
		_3dNoiseSizeB("3dNoise Size B", Float) = 0
		_NoiseScaleB("NoiseScale B", Vector) = (1,1,1,0)
		_SpeedB("Speed B", Float) = 0
		_DirectionB("Direction B", Vector) = (1,0,0,0)
		_NoiseStrengthB("Noise Strength B", Range(0 , 1)) = 0
		_NoiseScaleC("NoiseScale C", Vector) = (1,1,1,0)
		_3dNoiseSizeC("3dNoise Size C", Float) = 0
		_SpeedC("SpeedC", Float) = 0
		_DirectionC("DirectionC", Vector) = (1,0,0,0)
		_NoiseStrengthC("Noise Strength C", Range(0 , 1)) = 0
		_textureDetail("textureDetail", Range(0 , 1)) = 0
		_TextureColor("Texture Color", Color) = (0,0,0,0)
		_Tiling("Tiling", Vector) = (0,0,0,0)
		_Fallof("Fallof", Float) = 0
		_VertexColorMult("Vertex Color Mult", Float) = 1
		_RimColor("Rim Color", Color) = (0,0,0,0)
		[ASEEnd]_FresnelBSP("FresnelBSP", Vector) = (0,0,0,0)

			//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
			//_TransStrength( "Trans Strength", Range( 0, 50 ) ) = 1
			//_TransNormal( "Trans Normal Distortion", Range( 0, 1 ) ) = 0.5
			//_TransScattering( "Trans Scattering", Range( 1, 50 ) ) = 2
			//_TransDirect( "Trans Direct", Range( 0, 1 ) ) = 0.9
			//_TransAmbient( "Trans Ambient", Range( 0, 1 ) ) = 0.1
			//_TransShadow( "Trans Shadow", Range( 0, 1 ) ) = 0.5
			//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
			_TessValue("Max Tessellation", Range(1, 32)) = 16
			//_TessMin( "Tess Min Distance", Float ) = 10
			//_TessMax( "Tess Max Distance", Float ) = 25
			//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
			//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

				_BumpIntensity("_BumpIntensity", Float) = 165
				_Displacement("_Displacement", Float) = 165
				_Tiling1("Tiling1", Vector) = (2,2,0,0)
				_Tiling2("Tiling2", Vector) = (1,1,0,0)
				tilesCount("tilesCount", Float) = 21
				cloudSpeed("cloudSpeed", Float) = 1
				pushPullCloud("pushPullCloud", Vector) = (1,1,1,1)

			_NoiseTextureA("_Noise Texture 2D", 2D) = "white" {}


			//v0.1
			//TERRAIN DEPTH
			_DepthCameraPos("Depth Camera Pos", Vector) = (240, 100, 250, 3.0)
			_ShoreContourTex("_ShoreContourTex", 2D) = "white" {}
			_ShoreFadeFactor("_ShoreFadeFactor", Float) = 1.0
			_TerrainScale("Terrain Scale", Float) = 1000.0
			//v2.0.7
			_grassHeight("grassHeight", Float) = 3
			//_grassNormal("grassNormal", Float) = 3
			_respectHeight("_respectHeight", Float) = 0
			//v2.0.8
			_InteractTexture("_Interact Texture", 2D) = "white" {}
			_InteractTexturePos("Interact Texture Pos", Vector) = (0, 5000, 0, 0)
			_InteractTexturePosY("Interact Texture Pos Y Shadows", Float) = 5000
			//v2.1.1
			_shapeOnlyHeight("Shape Only Height", Float) = 0
			//v2.1.11
			_BaseHeight("Base Height", Float) = 0
			//v2.1.20 - //INFINIGRASS - DENSITY EMULATING GRASS
			_NoiseAmplitude("_NoiseAmplitude", Float) = 0
			_NoiseFreqX("_NoiseFreqX ", Float) = 0
			_NoiseFreqZ("_NoiseFreqZ ", Float) = 0
			_NoiseOffsetY("_NoiseOffsetY ", Float) = 0
			//v2.1.20b
			_textureFeed("Use texture feed height", Float) = 1 //default is on
			//END INFINIGRASS 3
			//INFINIGRASS 5 - GRASS BURN
			//BURN 2.1.14
			_NoiseTexture("Noise Texture", 2D) = "white" {}
			_Burnfactor("Burn factor", Range(-1, 1)) = -1//6
			_Burnramp("Burn ramp", 2D) = "white" {}
			_BurnCenter("Ocean Center", Vector) = (0, 0, 0, 0)
			//END INFINIGRASS 5 - GRASS BURN
	}

		SubShader
		{
			LOD 0



			Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
			Cull Back
			AlphaToMask Off

			HLSLINCLUDE
			#pragma target 2.0

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x 



			float _BumpIntensity = 152; float _Displacement = 162;
		float4 _Tiling1;// = float4(2, 2, 1, 1);
		float4 _Tiling2;// = float4(1, 1, 1, 1);
		float tilesCount;
		float cloudSpeed;
		float4 pushPullCloud;


		//INFINIGRASS 3
			//TERRAIN DEPTH
		sampler2D _ShoreContourTex;
		float3 _DepthCameraPos;
		float _ShoreFadeFactor;
		float _TerrainScale;
		//v2.0.7
		float _grassHeight;
		//float _grassNormal;
		float _respectHeight;
		//v2.0.8
		sampler2D _InteractTexture;
		float3 _InteractTexturePos;
		float _InteractTexturePosY;
		//v2.1.1
		float _shapeOnlyHeight;
		//v2.1.11
		float _BaseHeight;
		//v2.1.20
		float _NoiseAmplitude;
		float _NoiseFreqX;
		float _NoiseFreqZ;
		float _NoiseOffsetY;
		//v2.1.20b
		float _textureFeed;
		//v2.1.20			
		//END INFINIGRASS 3
		//INFINIGRASS 5
		//BURN 2.1.14
		uniform sampler2D _NoiseTexture; uniform float4 _NoiseTexture_ST;//
		uniform float _Burnfactor;
		uniform sampler2D _Burnramp; uniform float4 _Burnramp_ST;
		uniform float3 _BurnCenter;
		//END INFINIGRASS 5





		

			#ifndef ASE_TESS_FUNCS
			#define ASE_TESS_FUNCS
			float4 FixedTess(float tessValue)
			{
				return tessValue;
			}

			float CalcDistanceTessFactor(float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos)
			{
				float3 wpos = mul(o2w,vertex).xyz;
				float dist = distance(wpos, cameraPos);
				float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
				return f;
			}

			float4 CalcTriEdgeTessFactors(float3 triVertexFactors)
			{
				float4 tess;
				tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
				tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
				tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
				tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
				return tess;
			}

			float CalcEdgeTessFactor(float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams)
			{
				float dist = distance(0.5 * (wpos0 + wpos1), cameraPos);
				float len = distance(wpos0, wpos1);
				float f = max(len * scParams.y / (edgeLen * dist), 1.0);
				return f;
			}

			float DistanceFromPlane(float3 pos, float4 plane)
			{
				float d = dot(float4(pos,1.0f), plane);
				return d;
			}

			bool WorldViewFrustumCull(float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6])
			{
				float4 planeTest;
				planeTest.x = ((DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f);
				planeTest.y = ((DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f);
				planeTest.z = ((DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f);
				planeTest.w = ((DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f) +
							  ((DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f);
				return !all(planeTest);
			}

			float4 DistanceBasedTess(float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos)
			{
				float3 f;
				f.x = CalcDistanceTessFactor(v0,minDist,maxDist,tess,o2w,cameraPos);
				f.y = CalcDistanceTessFactor(v1,minDist,maxDist,tess,o2w,cameraPos);
				f.z = CalcDistanceTessFactor(v2,minDist,maxDist,tess,o2w,cameraPos);

				return CalcTriEdgeTessFactors(f);
			}

			float4 EdgeLengthBasedTess(float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams)
			{
				float3 pos0 = mul(o2w,v0).xyz;
				float3 pos1 = mul(o2w,v1).xyz;
				float3 pos2 = mul(o2w,v2).xyz;
				float4 tess;
				tess.x = CalcEdgeTessFactor(pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor(pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor(pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
				return tess;
			}

			float4 EdgeLengthBasedTessCull(float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6])
			{
				float3 pos0 = mul(o2w,v0).xyz;
				float3 pos1 = mul(o2w,v1).xyz;
				float3 pos2 = mul(o2w,v2).xyz;
				float4 tess;

				if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
				{
					tess = 0.0f;
				}
				else
				{
					tess.x = CalcEdgeTessFactor(pos1, pos2, edgeLength, cameraPos, scParams);
					tess.y = CalcEdgeTessFactor(pos2, pos0, edgeLength, cameraPos, scParams);
					tess.z = CalcEdgeTessFactor(pos0, pos1, edgeLength, cameraPos, scParams);
					tess.w = (tess.x + tess.y + tess.z) / 3.0f;
				}
				return tess;
			}
			#endif //ASE_TESS_FUNCS
			ENDHLSL


			Pass
			{

				Name "Forward"
				Tags { "LightMode" = "UniversalForwardOnly" }

				Blend One Zero, One Zero
				ZWrite On
				ZTest LEqual
				Offset 0 , 0
				ColorMask RGBA


				HLSLPROGRAM

				#define _NORMAL_DROPOFF_TS 1
				#pragma multi_compile_instancing
				#pragma multi_compile _ LOD_FADE_CROSSFADE
				#pragma multi_compile_fog
				#define ASE_FOG 1
				#define ASE_FIXED_TESSELLATION
				#define TESSELLATION_ON 1
				#pragma require tessellation tessHW
				#pragma hull HullFunction
				#pragma domain DomainFunction
				#define _EMISSION
				#define ASE_SRP_VERSION 70503


				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				#pragma multi_compile _ _SHADOWS_SOFT
				#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

				#pragma multi_compile _ DIRLIGHTMAP_COMBINED
				#pragma multi_compile _ LIGHTMAP_ON

				#pragma vertex vert
				#pragma fragment frag

				#define SHADERPASS_FORWARD

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

				#if ASE_SRP_VERSION <= 70108
				#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				#endif

				#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
					#define ENABLE_TERRAIN_PERPIXEL_NORMAL
				#endif

				#define ASE_NEEDS_FRAG_WORLD_POSITION
				#define ASE_NEEDS_FRAG_WORLD_NORMAL
				#define ASE_NEEDS_FRAG_WORLD_VIEW_DIR


				//v3 - TUTORIAL			

			//noise texture version
			//https://www.shadertoy.com/view/ldB3zc - // The MIT License
			sampler2D _NoiseTextureA;
			float worley3dATEX(float3 p) {
					 float d = 100.0;
					 for (int xo = -1; xo <= 1; ++xo) { //http://ibreakdownshaders.blogspot.com/2015/04/worleyn-on-river.html
					  for (int yo = -1; yo <= 1; ++yo) {
					   for (int zo = -1; zo <= 1; ++zo) {
						   float3 tp = floor(p) + float3(xo, yo, zo);
							float4 noiseMe = tex2Dlod(_NoiseTextureA, float4(tp.xyz* pushPullCloud.w,1)) * pushPullCloud.z *0.1;
							d = min(d, length(p - tp - noiseMe.xyz));
					   }
					  }
					 }
					 return cos(d); // use cosine to get round gradient
					 //return 1.0 - d;
				}

			void displaceClouds(inout float3 worldPos, inout float3 worldNormal, float3 worldTangent, float3 worldBinormal, inout float3 localNormal, inout float occlusion, float tile) {
				//float4 _Tiling1 = float4(2, 2, 1, 1);
				//float4 _Tiling2 = float4(1, 1, 1, 1);
				//float _BumpIntensity = 152; float _Displacement = 162;

				float _Time = _TimeParameters * cloudSpeed;
				// Main tex coords for cloud displacement
				float3 texCoords = worldPos.xyz  * _Tiling1.xyz * tile * 0.01 + _Time.x * _Tiling2.xyz;
				float4 disp = worley3dATEX(texCoords); //tex3Dlod(_WorleyNoiseTex, float4(texCoords, 0));

				// get the coords for the tangent and binormal displacement
				float3 Tan = texCoords + 0.05 * normalize(worldTangent.xyz);
				float3 BiNorm = texCoords + 0.05 * normalize(worldBinormal.xyz);

				// sample the displacement for the tangent normal
				float4 dispT = worley3dATEX(Tan);//tex3Dlod(_WorleyNoiseTex, float4(Tan, 0));
				float4 dispB = worley3dATEX(BiNorm);//tex3Dlod(_WorleyNoiseTex, float4(BiNorm, 0));

				// get tangent normal offset from displacements
				float2 localNormO = float2(disp.x - dispT.x, disp.x - dispB.x);

				// scale the normal by the one over the tiling value
				float divider = (1.0 / tile);
				float3 localNormalA = localNormal;
				localNormalA.xy += localNormO * _BumpIntensity * divider;
				occlusion *= pow(disp.x, divider);

				// set up the tSpace thingy for converting tangent directions to world directions
				float3 tSpace0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
				float3 tSpace1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
				float3 tSpace2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);

				// update the world normal
				worldNormal = float3(dot(tSpace0, localNormalA), dot(tSpace1, localNormalA), dot(tSpace2, localNormalA));				
				worldNormal = normalize(worldNormal);
				//localNormal = localNormalA;

				// push the world position in by the average value of the displacement map
				worldPos -= 0.7937 * worldNormal * _Displacement * divider + pushPullCloud.x * cos(worldPos.x*0.001 + worldPos.z*0.001);
				// push the world position out based on the new world normal
				worldPos += disp.x * worldNormal * _Displacement * divider - pushPullCloud.y * cos(worldPos.x*0.001 + worldPos.z*0.001);
		   }

				struct VertexInput
				{
					float4 vertex : POSITION;
					float3 ase_normal : NORMAL;
					float4 ase_tangent : TANGENT;
					float4 texcoord1 : TEXCOORD1;
					float4 texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct VertexOutput
				{
					float4 clipPos : SV_POSITION;
					float4 lightmapUVOrVertexSH : TEXCOORD0;
					half4 fogFactorAndVertexLight : TEXCOORD1;
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					float4 shadowCoord : TEXCOORD2;
					#endif
					float4 tSpace0 : TEXCOORD3;
					float4 tSpace1 : TEXCOORD4;
					float4 tSpace2 : TEXCOORD5;
					#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
					float4 screenPos : TEXCOORD6;
					#endif
					float4 ase_color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				CBUFFER_START(UnityPerMaterial)
				float4 _RimColor;
				float4 _TextureColor;
				float3 _NoiseScaleC;
				float3 _FresnelBSP;
				float3 _NoiseScaleB;
				float3 _DirectionC;
				float3 _NoiseScaleA;
				float3 _DirectionA;
				float3 _DirectionB;
				float2 _Tiling;
				float _SpeedB;
				float _3dNoiseSizeB;
				float _NoiseStrengthB;
				float _3dNoiseSizeA;
				float _SpeedC;
				float _3dNoiseSizeC;
				float _SpeedA;
				float _NoiseStrengthC;
				float _VertexColorMult;
				float _Fallof;
				float _textureDetail;
				float _NoiseStrengthA;
				#ifdef _TRANSMISSION_ASE
					float _TransmissionShadow;
				#endif
				#ifdef _TRANSLUCENCY_ASE
					float _TransStrength;
					float _TransNormal;
					float _TransScattering;
					float _TransDirect;
					float _TransAmbient;
					float _TransShadow;
				#endif
				#ifdef TESSELLATION_ON
					float _TessPhongStrength;
					float _TessValue;
					float _TessMin;
					float _TessMax;
					float _TessEdgeLength;
					float _TessMaxDisp;
				#endif
				CBUFFER_END
				sampler2D _TopTexture0;


				float3 mod3D289(float3 x) { return x - floor(x / 289.0) * 289.0; }
				float4 mod3D289(float4 x) { return x - floor(x / 289.0) * 289.0; }
				float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
				float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }
				float snoise(float3 v)
				{
					const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
					float3 i = floor(v + dot(v, C.yyy));
					float3 x0 = v - i + dot(i, C.xxx);
					float3 g = step(x0.yzx, x0.xyz);
					float3 l = 1.0 - g;
					float3 i1 = min(g.xyz, l.zxy);
					float3 i2 = max(g.xyz, l.zxy);
					float3 x1 = x0 - i1 + C.xxx;
					float3 x2 = x0 - i2 + C.yyy;
					float3 x3 = x0 - 0.5;
					i = mod3D289(i);
					float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
					float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)
					float4 x_ = floor(j / 7.0);
					float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)
					float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
					float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
					float4 h = 1.0 - abs(x) - abs(y);
					float4 b0 = float4(x.xy, y.xy);
					float4 b1 = float4(x.zw, y.zw);
					float4 s0 = floor(b0) * 2.0 + 1.0;
					float4 s1 = floor(b1) * 2.0 + 1.0;
					float4 sh = -step(h, 0.0);
					float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
					float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
					float3 g0 = float3(a0.xy, h.x);
					float3 g1 = float3(a0.zw, h.y);
					float3 g2 = float3(a1.xy, h.z);
					float3 g3 = float3(a1.zw, h.w);
					float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
					g0 *= norm.x;
					g1 *= norm.y;
					g2 *= norm.z;
					g3 *= norm.w;
					float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
					m = m * m;
					m = m * m;
					float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
					return 42.0 * dot(m, px);
				}

				inline float4 TriplanarSampling194(sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index)
				{
					float3 projNormal = (pow(abs(worldNormal), falloff));
					projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
					float3 nsign = sign(worldNormal);
					half4 xNorm; half4 yNorm; half4 zNorm;
					xNorm = tex2D(topTexMap, tiling * worldPos.zy * float2(nsign.x, 1.0));
					yNorm = tex2D(topTexMap, tiling * worldPos.xz * float2(nsign.y, 1.0));
					zNorm = tex2D(topTexMap, tiling * worldPos.xy * float2(-nsign.z, 1.0));
					return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
				}

				float3 mod2D289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
				float2 mod2D289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
				float3 permute(float3 x) { return mod2D289(((x * 34.0) + 1.0) * x); }
				float snoise(float2 v)
				{
					const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
					float2 i = floor(v + dot(v, C.yy));
					float2 x0 = v - i + dot(i, C.xx);
					float2 i1;
					i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
					float4 x12 = x0.xyxy + C.xxzz;
					x12.xy -= i1;
					i = mod2D289(i);
					float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
					float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
					m = m * m;
					m = m * m;
					float3 x = 2.0 * frac(p * C.www) - 1.0;
					float3 h = abs(x) - 0.5;
					float3 ox = floor(x + 0.5);
					float3 a0 = x - ox;
					m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
					float3 g;
					g.x = a0.x * x0.x + h.x * x0.y;
					g.yz = a0.yz * x12.xz + h.yz * x12.yw;
					return 130.0 * dot(m, g);
				}


				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					float3 objToWorldDir239 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
					float mulTime15 = _TimeParameters.x * _SpeedA;
					float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
					float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (ase_worldPos * _NoiseScaleA))));
					float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
					float3 objToWorldDir213 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
					float mulTime75 = _TimeParameters.x * _SpeedB;
					float simplePerlin3D78 = snoise(((_DirectionB * mulTime75) + (_3dNoiseSizeB * (ase_worldPos * _NoiseScaleB))));
					float3 objToWorldDir257 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
					float mulTime248 = _TimeParameters.x * _SpeedC;
					float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (ase_worldPos * _NoiseScaleC)));
					float simplePerlin3D255 = snoise(temp_output_252_0);

					o.ase_color = v.ase_color;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = ((objToWorldDir239 * _NoiseStrengthA * temp_output_8_0) + (objToWorldDir213 * (0.0 + (simplePerlin3D78 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthB) + (objToWorldDir257 * (0.0 + (simplePerlin3D255 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthC));
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.ase_normal = v.ase_normal;

					float3 positionWS = TransformObjectToWorld(v.vertex.xyz);




					//v0.1
					//////// InfiniGRASS Terrain ADAPT
					float heightDepthCam = _DepthCameraPos.y;
					_TerrainScale = heightDepthCam * 2;//URP
					half2 tileableUv = float2(positionWS.x, positionWS.z);// positionWS.xz;// mul(unity_ObjectToWorld, (v.vertex)).xz;
					float WorldScale = 500;
					WorldScale = _TerrainScale;
					float3 CamPos = float3(250, 0, 250);//_WorldSpaceCameraPos;
					CamPos = _DepthCameraPos;//_WorldSpaceCameraPos;
					float2 Origin = float2(CamPos.x - WorldScale / 2.0, CamPos.z - WorldScale / 2.0);
					float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
					float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
					float4 tex = tex2Dlod(_ShoreContourTex, float4(float2(ScaledTexPoint.x, ScaledTexPoint.y), 0.0, 0.0));
					//URP			
					tex.r = 1 - tex.r;
					//v2.0.8
					WorldScale = _InteractTexturePos.y;
					CamPos = float3(0, 0, 0);//_DepthCameraPos;//_WorldSpaceCameraPos;
					Origin = float2(0, 0);//float2(CamPos.x - WorldScale/2.0 , CamPos.z - WorldScale/2.0);
					UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
					ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
					float4 texInteract = tex2Dlod(_InteractTexture, float4(ScaledTexPoint, 0.0, 0.0));
					if (_textureFeed == 1) { //v2.1.20b
						positionWS.y = positionWS.y*_respectHeight + (pow(abs(heightDepthCam - tex.r*heightDepthCam), _ShoreFadeFactor) + v.texcoord.y*_grassHeight);
						positionWS.y = positionWS.y + _BaseHeight + _DepthCameraPos.y - heightDepthCam; //v2.1.16
					}







					VertexNormalInputs normalInput = GetVertexNormalInputs(v.ase_normal, v.ase_tangent);
					//v3 - VFX MIKE
					float occlus = 1;
					displaceClouds(positionWS, normalInput.normalWS, normalInput.tangentWS, normalInput.bitangentWS, v.ase_normal, occlus, tilesCount);

					/*tilesCount = 21;
					_Displacement = 152;
					_Tiling1.x = 2;_Tiling1.y = 2;
					displaceClouds(positionWS, normalInput.normalWS, normalInput.tangentWS, normalInput.bitangentWS, v.ase_normal, occlus, tilesCount);*/

					float3 positionVS = TransformWorldToView(positionWS);
					float4 positionCS = TransformWorldToHClip(positionWS);

					//	VertexNormalInputs normalInput = GetVertexNormalInputs( v.ase_normal, v.ase_tangent );

						o.tSpace0 = float4(normalInput.normalWS, positionWS.x);
						o.tSpace1 = float4(normalInput.tangentWS, positionWS.y);
						o.tSpace2 = float4(normalInput.bitangentWS, positionWS.z);

						OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy);
						OUTPUT_SH(normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz);

						#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
							o.lightmapUVOrVertexSH.zw = v.texcoord;
							o.lightmapUVOrVertexSH.xy = v.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;
						#endif

						half3 vertexLight = VertexLighting(positionWS, normalInput.normalWS);
						#ifdef ASE_FOG
							half fogFactor = ComputeFogFactor(positionCS.z);
						#else
							half fogFactor = 0;
						#endif
						o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						VertexPositionInputs vertexInput = (VertexPositionInputs)0;
						vertexInput.positionWS = positionWS;
						vertexInput.positionCS = positionCS;
						o.shadowCoord = GetShadowCoord(vertexInput);
						#endif


						o.clipPos = positionCS;
						#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
						o.screenPos = ComputeScreenPos(positionCS);
						#endif
						return o;
					}

					#if defined(TESSELLATION_ON)
					struct VertexControl
					{
						float4 vertex : INTERNALTESSPOS;
						float3 ase_normal : NORMAL;
						float4 ase_tangent : TANGENT;
						float4 texcoord : TEXCOORD0;
						float4 texcoord1 : TEXCOORD1;
						float4 ase_color : COLOR;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct TessellationFactors
					{
						float edge[3] : SV_TessFactor;
						float inside : SV_InsideTessFactor;
					};

					VertexControl vert(VertexInput v)
					{
						VertexControl o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						o.vertex = v.vertex;
						o.ase_normal = v.ase_normal;
						o.ase_tangent = v.ase_tangent;
						o.texcoord = v.texcoord;
						o.texcoord1 = v.texcoord1;
						o.ase_color = v.ase_color;
						return o;
					}

					TessellationFactors TessellationFunction(InputPatch<VertexControl,3> v)
					{
						TessellationFactors o;
						float4 tf = 1;
						float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
						float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
						#if defined(ASE_FIXED_TESSELLATION)
						tf = FixedTess(tessValue);
						#elif defined(ASE_DISTANCE_TESSELLATION)
						tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos);
						#elif defined(ASE_LENGTH_TESSELLATION)
						tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams);
						#elif defined(ASE_LENGTH_CULL_TESSELLATION)
						tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes);
						#endif
						o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
						return o;
					}

					[domain("tri")]
					[partitioning("fractional_odd")]
					[outputtopology("triangle_cw")]
					[patchconstantfunc("TessellationFunction")]
					[outputcontrolpoints(3)]
					VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
					{
					   return patch[id];
					}

					[domain("tri")]
					VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
					{
						VertexInput o = (VertexInput)0;
						o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
						o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
						o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
						o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
						o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
						o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
						#if defined(ASE_PHONG_TESSELLATION)
						float3 pp[3];
						for (int i = 0; i < 3; ++i)
							pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
						float phongStrength = _TessPhongStrength;
						o.vertex.xyz = phongStrength * (pp[0] * bary.x + pp[1] * bary.y + pp[2] * bary.z) + (1.0f - phongStrength) * o.vertex.xyz;
						#endif
						UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
						return VertexFunction(o);
					}
					#else
					VertexOutput vert(VertexInput v)
					{
						return VertexFunction(v);
					}
					#endif

					#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
						#define ASE_SV_DEPTH SV_DepthLessEqual  
					#else
						#define ASE_SV_DEPTH SV_Depth
					#endif

					half4 frag(VertexOutput IN
								#ifdef ASE_DEPTH_WRITE_ON
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 ) : SV_Target
					{
						UNITY_SETUP_INSTANCE_ID(IN);
						UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

						#ifdef LOD_FADE_CROSSFADE
							LODDitheringTransition(IN.clipPos.xyz, unity_LODFade.x);
						#endif

						#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
							float2 sampleCoords = (IN.lightmapUVOrVertexSH.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
							float3 WorldNormal = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
							float3 WorldTangent = -cross(GetObjectToWorldMatrix()._13_23_33, WorldNormal);
							float3 WorldBiTangent = cross(WorldNormal, -WorldTangent);
						#else
							float3 WorldNormal = normalize(IN.tSpace0.xyz);
							float3 WorldTangent = IN.tSpace1.xyz;
							float3 WorldBiTangent = IN.tSpace2.xyz;
						#endif
						float3 WorldPosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
						float3 WorldViewDirection = _WorldSpaceCameraPos.xyz - WorldPosition;
						float4 ShadowCoords = float4(0, 0, 0, 0);
						#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
						float4 ScreenPos = IN.screenPos;
						#endif

						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
							ShadowCoords = IN.shadowCoord;
						#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
							ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
						#endif

						WorldViewDirection = SafeNormalize(WorldViewDirection);

						float mulTime248 = _TimeParameters.x * _SpeedC;
						float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (WorldPosition * _NoiseScaleC)));
						float3 NoiseWorldPos364 = temp_output_252_0;
						float4 triplanar194 = TriplanarSampling194(_TopTexture0, NoiseWorldPos364, WorldNormal, _Fallof, _Tiling, 1.0, 0);
						float mulTime15 = _TimeParameters.x * _SpeedA;
						float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (WorldPosition * _NoiseScaleA))));
						float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
						float NoiseA366 = temp_output_8_0;
						float2 appendResult376 = (float2(WorldPosition.x , WorldPosition.z));
						float simplePerlin2D374 = snoise((appendResult376 * 0.1));
						float4 lerpResult210 = lerp(saturate((pow(IN.ase_color , 0.454545) * _VertexColorMult)) , _TextureColor , (((1.0 - triplanar194.x) * _textureDetail) * saturate(NoiseA366) * (0.25 + (simplePerlin2D374 - -1.0) * (1.0 - 0.25) / (1.0 - -1.0))));
						float fresnelNdotV346 = dot(WorldNormal, WorldViewDirection);
						float fresnelNode346 = (_FresnelBSP.x + _FresnelBSP.y * pow(1.0 - fresnelNdotV346, _FresnelBSP.z));

						float3 Albedo = float3(0.5, 0.5, 0.5);
						float3 Normal = float3(0, 0, 1);
						float3 Emission = saturate((lerpResult210 + (fresnelNode346 * _RimColor))).rgb;
						float3 Specular = 0.5;
						float Metallic = 0;
						float Smoothness = 0.5;
						float Occlusion = 1;
						float Alpha = 1;
						float AlphaClipThreshold = 0.5;
						float AlphaClipThresholdShadow = 0.5;
						float3 BakedGI = 0;
						float3 RefractionColor = 1;
						float RefractionIndex = 1;
						float3 Transmission = 1;
						float3 Translucency = 1;
						#ifdef ASE_DEPTH_WRITE_ON
						float DepthValue = 0;
						#endif

						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif

						InputData inputData;
						inputData.positionWS = WorldPosition;
						inputData.viewDirectionWS = WorldViewDirection;
						inputData.shadowCoord = ShadowCoords;

						#ifdef _NORMALMAP
							#if _NORMAL_DROPOFF_TS
							inputData.normalWS = TransformTangentToWorld(Normal, half3x3(WorldTangent, WorldBiTangent, WorldNormal));
							#elif _NORMAL_DROPOFF_OS
							inputData.normalWS = TransformObjectToWorldNormal(Normal);
							#elif _NORMAL_DROPOFF_WS
							inputData.normalWS = Normal;
							#endif
							inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
						#else
							inputData.normalWS = WorldNormal;
						#endif

						#ifdef ASE_FOG
							inputData.fogCoord = IN.fogFactorAndVertexLight.x;
						#endif

						inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
						#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
							float3 SH = SampleSH(inputData.normalWS.xyz);
						#else
							float3 SH = IN.lightmapUVOrVertexSH.xyz;
						#endif

						inputData.bakedGI = SAMPLE_GI(IN.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
						#ifdef _ASE_BAKEDGI
							inputData.bakedGI = BakedGI;
						#endif
						half4 color = UniversalFragmentPBR(
							inputData,
							Albedo,
							Metallic,
							Specular,
							Smoothness,
							Occlusion,
							Emission,
							Alpha);

						#ifdef _TRANSMISSION_ASE
						{
							float shadow = _TransmissionShadow;

							Light mainLight = GetMainLight(inputData.shadowCoord);
							float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
							mainAtten = lerp(mainAtten, mainAtten * mainLight.shadowAttenuation, shadow);
							half3 mainTransmission = max(0 , -dot(inputData.normalWS, mainLight.direction)) * mainAtten * Transmission;
							color.rgb += Albedo * mainTransmission;

							#ifdef _ADDITIONAL_LIGHTS
								int transPixelLightCount = GetAdditionalLightsCount();
								for (int i = 0; i < transPixelLightCount; ++i)
								{
									Light light = GetAdditionalLight(i, inputData.positionWS);
									float3 atten = light.color * light.distanceAttenuation;
									atten = lerp(atten, atten * light.shadowAttenuation, shadow);

									half3 transmission = max(0 , -dot(inputData.normalWS, light.direction)) * atten * Transmission;
									color.rgb += Albedo * transmission;
								}
							#endif
						}
						#endif

						#ifdef _TRANSLUCENCY_ASE
						{
							float shadow = _TransShadow;
							float normal = _TransNormal;
							float scattering = _TransScattering;
							float direct = _TransDirect;
							float ambient = _TransAmbient;
							float strength = _TransStrength;

							Light mainLight = GetMainLight(inputData.shadowCoord);
							float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
							mainAtten = lerp(mainAtten, mainAtten * mainLight.shadowAttenuation, shadow);

							half3 mainLightDir = mainLight.direction + inputData.normalWS * normal;
							half mainVdotL = pow(saturate(dot(inputData.viewDirectionWS, -mainLightDir)), scattering);
							half3 mainTranslucency = mainAtten * (mainVdotL * direct + inputData.bakedGI * ambient) * Translucency;
							color.rgb += Albedo * mainTranslucency * strength;

							#ifdef _ADDITIONAL_LIGHTS
								int transPixelLightCount = GetAdditionalLightsCount();
								for (int i = 0; i < transPixelLightCount; ++i)
								{
									Light light = GetAdditionalLight(i, inputData.positionWS);
									float3 atten = light.color * light.distanceAttenuation;
									atten = lerp(atten, atten * light.shadowAttenuation, shadow);

									half3 lightDir = light.direction + inputData.normalWS * normal;
									half VdotL = pow(saturate(dot(inputData.viewDirectionWS, -lightDir)), scattering);
									half3 translucency = atten * (VdotL * direct + inputData.bakedGI * ambient) * Translucency;
									color.rgb += Albedo * translucency * strength;
								}
							#endif
						}
						#endif

						#ifdef _REFRACTION_ASE
							float4 projScreenPos = ScreenPos / ScreenPos.w;
							float3 refractionOffset = (RefractionIndex - 1.0) * mul(UNITY_MATRIX_V, float4(WorldNormal, 0)).xyz * (1.0 - dot(WorldNormal, WorldViewDirection));
							projScreenPos.xy += refractionOffset.xy;
							float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR(projScreenPos.xy) * RefractionColor;
							color.rgb = lerp(refraction, color.rgb, color.a);
							color.a = 1;
						#endif

						#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
							color.rgb *= color.a;
						#endif

						#ifdef ASE_FOG
							#ifdef TERRAIN_SPLAT_ADDPASS
								color.rgb = MixFogColor(color.rgb, half3(0, 0, 0), IN.fogFactorAndVertexLight.x);
							#else
								color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
							#endif
						#endif

						#ifdef ASE_DEPTH_WRITE_ON
							outputDepth = DepthValue;
						#endif

						return color;
					}

					ENDHLSL
				}


				Pass
				{

					Name "ShadowCaster"
					Tags { "LightMode" = "ShadowCaster" }

					ZWrite On
					ZTest LEqual
					AlphaToMask Off

					HLSLPROGRAM

					#define _NORMAL_DROPOFF_TS 1
					#pragma multi_compile_instancing
					#pragma multi_compile _ LOD_FADE_CROSSFADE
					#pragma multi_compile_fog
					#define ASE_FOG 1
					#define ASE_FIXED_TESSELLATION
					#define TESSELLATION_ON 1
					#pragma require tessellation tessHW
					#pragma hull HullFunction
					#pragma domain DomainFunction
					#define _EMISSION
					#define ASE_SRP_VERSION 70503


					#pragma vertex vert
					#pragma fragment frag

					#define SHADERPASS_SHADOWCASTER

					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
					#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"



					struct VertexInput
					{
						float4 vertex : POSITION;
						float3 ase_normal : NORMAL;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct VertexOutput
					{
						float4 clipPos : SV_POSITION;
						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 worldPos : TEXCOORD0;
						#endif
						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
						float4 shadowCoord : TEXCOORD1;
						#endif

						UNITY_VERTEX_INPUT_INSTANCE_ID
						UNITY_VERTEX_OUTPUT_STEREO
					};

					CBUFFER_START(UnityPerMaterial)
					float4 _RimColor;
					float4 _TextureColor;
					float3 _NoiseScaleC;
					float3 _FresnelBSP;
					float3 _NoiseScaleB;
					float3 _DirectionC;
					float3 _NoiseScaleA;
					float3 _DirectionA;
					float3 _DirectionB;
					float2 _Tiling;
					float _SpeedB;
					float _3dNoiseSizeB;
					float _NoiseStrengthB;
					float _3dNoiseSizeA;
					float _SpeedC;
					float _3dNoiseSizeC;
					float _SpeedA;
					float _NoiseStrengthC;
					float _VertexColorMult;
					float _Fallof;
					float _textureDetail;
					float _NoiseStrengthA;
					#ifdef _TRANSMISSION_ASE
						float _TransmissionShadow;
					#endif
					#ifdef _TRANSLUCENCY_ASE
						float _TransStrength;
						float _TransNormal;
						float _TransScattering;
						float _TransDirect;
						float _TransAmbient;
						float _TransShadow;
					#endif
					#ifdef TESSELLATION_ON
						float _TessPhongStrength;
						float _TessValue;
						float _TessMin;
						float _TessMax;
						float _TessEdgeLength;
						float _TessMaxDisp;
					#endif
					CBUFFER_END


					float3 mod3D289(float3 x) { return x - floor(x / 289.0) * 289.0; }
					float4 mod3D289(float4 x) { return x - floor(x / 289.0) * 289.0; }
					float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
					float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }
					float snoise(float3 v)
					{
						const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
						float3 i = floor(v + dot(v, C.yyy));
						float3 x0 = v - i + dot(i, C.xxx);
						float3 g = step(x0.yzx, x0.xyz);
						float3 l = 1.0 - g;
						float3 i1 = min(g.xyz, l.zxy);
						float3 i2 = max(g.xyz, l.zxy);
						float3 x1 = x0 - i1 + C.xxx;
						float3 x2 = x0 - i2 + C.yyy;
						float3 x3 = x0 - 0.5;
						i = mod3D289(i);
						float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
						float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)
						float4 x_ = floor(j / 7.0);
						float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)
						float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 h = 1.0 - abs(x) - abs(y);
						float4 b0 = float4(x.xy, y.xy);
						float4 b1 = float4(x.zw, y.zw);
						float4 s0 = floor(b0) * 2.0 + 1.0;
						float4 s1 = floor(b1) * 2.0 + 1.0;
						float4 sh = -step(h, 0.0);
						float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
						float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
						float3 g0 = float3(a0.xy, h.x);
						float3 g1 = float3(a0.zw, h.y);
						float3 g2 = float3(a1.xy, h.z);
						float3 g3 = float3(a1.zw, h.w);
						float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
						g0 *= norm.x;
						g1 *= norm.y;
						g2 *= norm.z;
						g3 *= norm.w;
						float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
						m = m * m;
						m = m * m;
						float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
						return 42.0 * dot(m, px);
					}


					float3 _LightDirection;

					VertexOutput VertexFunction(VertexInput v)
					{
						VertexOutput o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

						float3 objToWorldDir239 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime15 = _TimeParameters.x * _SpeedA;
						float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
						float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (ase_worldPos * _NoiseScaleA))));
						float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
						float3 objToWorldDir213 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime75 = _TimeParameters.x * _SpeedB;
						float simplePerlin3D78 = snoise(((_DirectionB * mulTime75) + (_3dNoiseSizeB * (ase_worldPos * _NoiseScaleB))));
						float3 objToWorldDir257 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime248 = _TimeParameters.x * _SpeedC;
						float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (ase_worldPos * _NoiseScaleC)));
						float simplePerlin3D255 = snoise(temp_output_252_0);

						#ifdef ASE_ABSOLUTE_VERTEX_POS
							float3 defaultVertexValue = v.vertex.xyz;
						#else
							float3 defaultVertexValue = float3(0, 0, 0);
						#endif
						float3 vertexValue = ((objToWorldDir239 * _NoiseStrengthA * temp_output_8_0) + (objToWorldDir213 * (0.0 + (simplePerlin3D78 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthB) + (objToWorldDir257 * (0.0 + (simplePerlin3D255 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthC));
						#ifdef ASE_ABSOLUTE_VERTEX_POS
							v.vertex.xyz = vertexValue;
						#else
							v.vertex.xyz += vertexValue;
						#endif

						v.ase_normal = v.ase_normal;

						float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						o.worldPos = positionWS;
						#endif
						float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

						float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

						#if UNITY_REVERSED_Z
							clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
						#else
							clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
						#endif
						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							VertexPositionInputs vertexInput = (VertexPositionInputs)0;
							vertexInput.positionWS = positionWS;
							vertexInput.positionCS = clipPos;
							o.shadowCoord = GetShadowCoord(vertexInput);
						#endif
						o.clipPos = clipPos;
						return o;
					}

					#if defined(TESSELLATION_ON)
					struct VertexControl
					{
						float4 vertex : INTERNALTESSPOS;
						float3 ase_normal : NORMAL;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct TessellationFactors
					{
						float edge[3] : SV_TessFactor;
						float inside : SV_InsideTessFactor;
					};

					VertexControl vert(VertexInput v)
					{
						VertexControl o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						o.vertex = v.vertex;
						o.ase_normal = v.ase_normal;

						return o;
					}

					TessellationFactors TessellationFunction(InputPatch<VertexControl,3> v)
					{
						TessellationFactors o;
						float4 tf = 1;
						float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
						float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
						#if defined(ASE_FIXED_TESSELLATION)
						tf = FixedTess(tessValue);
						#elif defined(ASE_DISTANCE_TESSELLATION)
						tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos);
						#elif defined(ASE_LENGTH_TESSELLATION)
						tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams);
						#elif defined(ASE_LENGTH_CULL_TESSELLATION)
						tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes);
						#endif
						o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
						return o;
					}

					[domain("tri")]
					[partitioning("fractional_odd")]
					[outputtopology("triangle_cw")]
					[patchconstantfunc("TessellationFunction")]
					[outputcontrolpoints(3)]
					VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
					{
					   return patch[id];
					}

					[domain("tri")]
					VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
					{
						VertexInput o = (VertexInput)0;
						o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
						o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;

						#if defined(ASE_PHONG_TESSELLATION)
						float3 pp[3];
						for (int i = 0; i < 3; ++i)
							pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
						float phongStrength = _TessPhongStrength;
						o.vertex.xyz = phongStrength * (pp[0] * bary.x + pp[1] * bary.y + pp[2] * bary.z) + (1.0f - phongStrength) * o.vertex.xyz;
						#endif
						UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
						return VertexFunction(o);
					}
					#else
					VertexOutput vert(VertexInput v)
					{
						return VertexFunction(v);
					}
					#endif

					#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
						#define ASE_SV_DEPTH SV_DepthLessEqual  
					#else
						#define ASE_SV_DEPTH SV_Depth
					#endif

					half4 frag(VertexOutput IN
								#ifdef ASE_DEPTH_WRITE_ON
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 ) : SV_TARGET
					{
						UNITY_SETUP_INSTANCE_ID(IN);
						UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 WorldPosition = IN.worldPos;
						#endif
						float4 ShadowCoords = float4(0, 0, 0, 0);

						#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
								ShadowCoords = IN.shadowCoord;
							#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
								ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
							#endif
						#endif


						float Alpha = 1;
						float AlphaClipThreshold = 0.5;
						float AlphaClipThresholdShadow = 0.5;
						#ifdef ASE_DEPTH_WRITE_ON
						float DepthValue = 0;
						#endif

						#ifdef _ALPHATEST_ON
							#ifdef _ALPHATEST_SHADOW_ON
								clip(Alpha - AlphaClipThresholdShadow);
							#else
								clip(Alpha - AlphaClipThreshold);
							#endif
						#endif

						#ifdef LOD_FADE_CROSSFADE
							LODDitheringTransition(IN.clipPos.xyz, unity_LODFade.x);
						#endif
						#ifdef ASE_DEPTH_WRITE_ON
							outputDepth = DepthValue;
						#endif
						return 0;
					}

					ENDHLSL
				}


				Pass
				{

					Name "DepthOnly"
					Tags { "LightMode" = "DepthOnly" }

					ZWrite On
					ColorMask 0
					AlphaToMask Off

					HLSLPROGRAM

					#define _NORMAL_DROPOFF_TS 1
					#pragma multi_compile_instancing
					#pragma multi_compile _ LOD_FADE_CROSSFADE
					#pragma multi_compile_fog
					#define ASE_FOG 1
					#define ASE_FIXED_TESSELLATION
					#define TESSELLATION_ON 1
					#pragma require tessellation tessHW
					#pragma hull HullFunction
					#pragma domain DomainFunction
					#define _EMISSION
					#define ASE_SRP_VERSION 70503


					#pragma vertex vert
					#pragma fragment frag

					#define SHADERPASS_DEPTHONLY

					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
					#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"



					struct VertexInput
					{
						float4 vertex : POSITION;
						float3 ase_normal : NORMAL;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct VertexOutput
					{
						float4 clipPos : SV_POSITION;
						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 worldPos : TEXCOORD0;
						#endif
						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
						float4 shadowCoord : TEXCOORD1;
						#endif

						UNITY_VERTEX_INPUT_INSTANCE_ID
						UNITY_VERTEX_OUTPUT_STEREO
					};

					CBUFFER_START(UnityPerMaterial)
					float4 _RimColor;
					float4 _TextureColor;
					float3 _NoiseScaleC;
					float3 _FresnelBSP;
					float3 _NoiseScaleB;
					float3 _DirectionC;
					float3 _NoiseScaleA;
					float3 _DirectionA;
					float3 _DirectionB;
					float2 _Tiling;
					float _SpeedB;
					float _3dNoiseSizeB;
					float _NoiseStrengthB;
					float _3dNoiseSizeA;
					float _SpeedC;
					float _3dNoiseSizeC;
					float _SpeedA;
					float _NoiseStrengthC;
					float _VertexColorMult;
					float _Fallof;
					float _textureDetail;
					float _NoiseStrengthA;
					#ifdef _TRANSMISSION_ASE
						float _TransmissionShadow;
					#endif
					#ifdef _TRANSLUCENCY_ASE
						float _TransStrength;
						float _TransNormal;
						float _TransScattering;
						float _TransDirect;
						float _TransAmbient;
						float _TransShadow;
					#endif
					#ifdef TESSELLATION_ON
						float _TessPhongStrength;
						float _TessValue;
						float _TessMin;
						float _TessMax;
						float _TessEdgeLength;
						float _TessMaxDisp;
					#endif
					CBUFFER_END


					float3 mod3D289(float3 x) { return x - floor(x / 289.0) * 289.0; }
					float4 mod3D289(float4 x) { return x - floor(x / 289.0) * 289.0; }
					float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
					float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }
					float snoise(float3 v)
					{
						const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
						float3 i = floor(v + dot(v, C.yyy));
						float3 x0 = v - i + dot(i, C.xxx);
						float3 g = step(x0.yzx, x0.xyz);
						float3 l = 1.0 - g;
						float3 i1 = min(g.xyz, l.zxy);
						float3 i2 = max(g.xyz, l.zxy);
						float3 x1 = x0 - i1 + C.xxx;
						float3 x2 = x0 - i2 + C.yyy;
						float3 x3 = x0 - 0.5;
						i = mod3D289(i);
						float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
						float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)
						float4 x_ = floor(j / 7.0);
						float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)
						float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 h = 1.0 - abs(x) - abs(y);
						float4 b0 = float4(x.xy, y.xy);
						float4 b1 = float4(x.zw, y.zw);
						float4 s0 = floor(b0) * 2.0 + 1.0;
						float4 s1 = floor(b1) * 2.0 + 1.0;
						float4 sh = -step(h, 0.0);
						float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
						float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
						float3 g0 = float3(a0.xy, h.x);
						float3 g1 = float3(a0.zw, h.y);
						float3 g2 = float3(a1.xy, h.z);
						float3 g3 = float3(a1.zw, h.w);
						float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
						g0 *= norm.x;
						g1 *= norm.y;
						g2 *= norm.z;
						g3 *= norm.w;
						float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
						m = m * m;
						m = m * m;
						float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
						return 42.0 * dot(m, px);
					}


					VertexOutput VertexFunction(VertexInput v)
					{
						VertexOutput o = (VertexOutput)0;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

						float3 objToWorldDir239 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime15 = _TimeParameters.x * _SpeedA;
						float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
						float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (ase_worldPos * _NoiseScaleA))));
						float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
						float3 objToWorldDir213 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime75 = _TimeParameters.x * _SpeedB;
						float simplePerlin3D78 = snoise(((_DirectionB * mulTime75) + (_3dNoiseSizeB * (ase_worldPos * _NoiseScaleB))));
						float3 objToWorldDir257 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime248 = _TimeParameters.x * _SpeedC;
						float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (ase_worldPos * _NoiseScaleC)));
						float simplePerlin3D255 = snoise(temp_output_252_0);

						#ifdef ASE_ABSOLUTE_VERTEX_POS
							float3 defaultVertexValue = v.vertex.xyz;
						#else
							float3 defaultVertexValue = float3(0, 0, 0);
						#endif
						float3 vertexValue = ((objToWorldDir239 * _NoiseStrengthA * temp_output_8_0) + (objToWorldDir213 * (0.0 + (simplePerlin3D78 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthB) + (objToWorldDir257 * (0.0 + (simplePerlin3D255 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthC));
						#ifdef ASE_ABSOLUTE_VERTEX_POS
							v.vertex.xyz = vertexValue;
						#else
							v.vertex.xyz += vertexValue;
						#endif

						v.ase_normal = v.ase_normal;
						float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
						float4 positionCS = TransformWorldToHClip(positionWS);

						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						o.worldPos = positionWS;
						#endif

						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							VertexPositionInputs vertexInput = (VertexPositionInputs)0;
							vertexInput.positionWS = positionWS;
							vertexInput.positionCS = positionCS;
							o.shadowCoord = GetShadowCoord(vertexInput);
						#endif
						o.clipPos = positionCS;
						return o;
					}

					#if defined(TESSELLATION_ON)
					struct VertexControl
					{
						float4 vertex : INTERNALTESSPOS;
						float3 ase_normal : NORMAL;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct TessellationFactors
					{
						float edge[3] : SV_TessFactor;
						float inside : SV_InsideTessFactor;
					};

					VertexControl vert(VertexInput v)
					{
						VertexControl o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						o.vertex = v.vertex;
						o.ase_normal = v.ase_normal;

						return o;
					}

					TessellationFactors TessellationFunction(InputPatch<VertexControl,3> v)
					{
						TessellationFactors o;
						float4 tf = 1;
						float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
						float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
						#if defined(ASE_FIXED_TESSELLATION)
						tf = FixedTess(tessValue);
						#elif defined(ASE_DISTANCE_TESSELLATION)
						tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos);
						#elif defined(ASE_LENGTH_TESSELLATION)
						tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams);
						#elif defined(ASE_LENGTH_CULL_TESSELLATION)
						tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes);
						#endif
						o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
						return o;
					}

					[domain("tri")]
					[partitioning("fractional_odd")]
					[outputtopology("triangle_cw")]
					[patchconstantfunc("TessellationFunction")]
					[outputcontrolpoints(3)]
					VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
					{
					   return patch[id];
					}

					[domain("tri")]
					VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
					{
						VertexInput o = (VertexInput)0;
						o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
						o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;

						#if defined(ASE_PHONG_TESSELLATION)
						float3 pp[3];
						for (int i = 0; i < 3; ++i)
							pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
						float phongStrength = _TessPhongStrength;
						o.vertex.xyz = phongStrength * (pp[0] * bary.x + pp[1] * bary.y + pp[2] * bary.z) + (1.0f - phongStrength) * o.vertex.xyz;
						#endif
						UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
						return VertexFunction(o);
					}
					#else
					VertexOutput vert(VertexInput v)
					{
						return VertexFunction(v);
					}
					#endif

					#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
						#define ASE_SV_DEPTH SV_DepthLessEqual  
					#else
						#define ASE_SV_DEPTH SV_Depth
					#endif
					half4 frag(VertexOutput IN
								#ifdef ASE_DEPTH_WRITE_ON
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 ) : SV_TARGET
					{
						UNITY_SETUP_INSTANCE_ID(IN);
						UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 WorldPosition = IN.worldPos;
						#endif
						float4 ShadowCoords = float4(0, 0, 0, 0);

						#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
								ShadowCoords = IN.shadowCoord;
							#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
								ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
							#endif
						#endif


						float Alpha = 1;
						float AlphaClipThreshold = 0.5;
						#ifdef ASE_DEPTH_WRITE_ON
						float DepthValue = 0;
						#endif

						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif

						#ifdef LOD_FADE_CROSSFADE
							LODDitheringTransition(IN.clipPos.xyz, unity_LODFade.x);
						#endif
						#ifdef ASE_DEPTH_WRITE_ON
						outputDepth = DepthValue;
						#endif
						return 0;
					}
					ENDHLSL
				}


				Pass
				{

					Name "Meta"
					Tags { "LightMode" = "Meta" }

					Cull Off

					HLSLPROGRAM

					#define _NORMAL_DROPOFF_TS 1
					#pragma multi_compile_instancing
					#pragma multi_compile _ LOD_FADE_CROSSFADE
					#pragma multi_compile_fog
					#define ASE_FOG 1
					#define ASE_FIXED_TESSELLATION
					#define TESSELLATION_ON 1
					#pragma require tessellation tessHW
					#pragma hull HullFunction
					#pragma domain DomainFunction
					#define _EMISSION
					#define ASE_SRP_VERSION 70503


					#pragma vertex vert
					#pragma fragment frag

					#define SHADERPASS_META

					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
					#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

					#define ASE_NEEDS_FRAG_WORLD_POSITION
					#define ASE_NEEDS_VERT_NORMAL


					#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

					struct VertexInput
					{
						float4 vertex : POSITION;
						float3 ase_normal : NORMAL;
						float4 texcoord1 : TEXCOORD1;
						float4 texcoord2 : TEXCOORD2;
						float4 ase_color : COLOR;
						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct VertexOutput
					{
						float4 clipPos : SV_POSITION;
						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 worldPos : TEXCOORD0;
						#endif
						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
						float4 shadowCoord : TEXCOORD1;
						#endif
						float4 ase_color : COLOR;
						float4 ase_texcoord2 : TEXCOORD2;
						UNITY_VERTEX_INPUT_INSTANCE_ID
						UNITY_VERTEX_OUTPUT_STEREO
					};

					CBUFFER_START(UnityPerMaterial)
					float4 _RimColor;
					float4 _TextureColor;
					float3 _NoiseScaleC;
					float3 _FresnelBSP;
					float3 _NoiseScaleB;
					float3 _DirectionC;
					float3 _NoiseScaleA;
					float3 _DirectionA;
					float3 _DirectionB;
					float2 _Tiling;
					float _SpeedB;
					float _3dNoiseSizeB;
					float _NoiseStrengthB;
					float _3dNoiseSizeA;
					float _SpeedC;
					float _3dNoiseSizeC;
					float _SpeedA;
					float _NoiseStrengthC;
					float _VertexColorMult;
					float _Fallof;
					float _textureDetail;
					float _NoiseStrengthA;
					#ifdef _TRANSMISSION_ASE
						float _TransmissionShadow;
					#endif
					#ifdef _TRANSLUCENCY_ASE
						float _TransStrength;
						float _TransNormal;
						float _TransScattering;
						float _TransDirect;
						float _TransAmbient;
						float _TransShadow;
					#endif
					#ifdef TESSELLATION_ON
						float _TessPhongStrength;
						float _TessValue;
						float _TessMin;
						float _TessMax;
						float _TessEdgeLength;
						float _TessMaxDisp;
					#endif
					CBUFFER_END
					sampler2D _TopTexture0;


					float3 mod3D289(float3 x) { return x - floor(x / 289.0) * 289.0; }
					float4 mod3D289(float4 x) { return x - floor(x / 289.0) * 289.0; }
					float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
					float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }
					float snoise(float3 v)
					{
						const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
						float3 i = floor(v + dot(v, C.yyy));
						float3 x0 = v - i + dot(i, C.xxx);
						float3 g = step(x0.yzx, x0.xyz);
						float3 l = 1.0 - g;
						float3 i1 = min(g.xyz, l.zxy);
						float3 i2 = max(g.xyz, l.zxy);
						float3 x1 = x0 - i1 + C.xxx;
						float3 x2 = x0 - i2 + C.yyy;
						float3 x3 = x0 - 0.5;
						i = mod3D289(i);
						float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
						float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)
						float4 x_ = floor(j / 7.0);
						float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)
						float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 h = 1.0 - abs(x) - abs(y);
						float4 b0 = float4(x.xy, y.xy);
						float4 b1 = float4(x.zw, y.zw);
						float4 s0 = floor(b0) * 2.0 + 1.0;
						float4 s1 = floor(b1) * 2.0 + 1.0;
						float4 sh = -step(h, 0.0);
						float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
						float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
						float3 g0 = float3(a0.xy, h.x);
						float3 g1 = float3(a0.zw, h.y);
						float3 g2 = float3(a1.xy, h.z);
						float3 g3 = float3(a1.zw, h.w);
						float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
						g0 *= norm.x;
						g1 *= norm.y;
						g2 *= norm.z;
						g3 *= norm.w;
						float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
						m = m * m;
						m = m * m;
						float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
						return 42.0 * dot(m, px);
					}

					inline float4 TriplanarSampling194(sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index)
					{
						float3 projNormal = (pow(abs(worldNormal), falloff));
						projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
						float3 nsign = sign(worldNormal);
						half4 xNorm; half4 yNorm; half4 zNorm;
						xNorm = tex2D(topTexMap, tiling * worldPos.zy * float2(nsign.x, 1.0));
						yNorm = tex2D(topTexMap, tiling * worldPos.xz * float2(nsign.y, 1.0));
						zNorm = tex2D(topTexMap, tiling * worldPos.xy * float2(-nsign.z, 1.0));
						return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
					}

					float3 mod2D289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
					float2 mod2D289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
					float3 permute(float3 x) { return mod2D289(((x * 34.0) + 1.0) * x); }
					float snoise(float2 v)
					{
						const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
						float2 i = floor(v + dot(v, C.yy));
						float2 x0 = v - i + dot(i, C.xx);
						float2 i1;
						i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
						float4 x12 = x0.xyxy + C.xxzz;
						x12.xy -= i1;
						i = mod2D289(i);
						float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
						float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
						m = m * m;
						m = m * m;
						float3 x = 2.0 * frac(p * C.www) - 1.0;
						float3 h = abs(x) - 0.5;
						float3 ox = floor(x + 0.5);
						float3 a0 = x - ox;
						m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
						float3 g;
						g.x = a0.x * x0.x + h.x * x0.y;
						g.yz = a0.yz * x12.xz + h.yz * x12.yw;
						return 130.0 * dot(m, g);
					}


					VertexOutput VertexFunction(VertexInput v)
					{
						VertexOutput o = (VertexOutput)0;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

						float3 objToWorldDir239 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime15 = _TimeParameters.x * _SpeedA;
						float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
						float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (ase_worldPos * _NoiseScaleA))));
						float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
						float3 objToWorldDir213 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime75 = _TimeParameters.x * _SpeedB;
						float simplePerlin3D78 = snoise(((_DirectionB * mulTime75) + (_3dNoiseSizeB * (ase_worldPos * _NoiseScaleB))));
						float3 objToWorldDir257 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime248 = _TimeParameters.x * _SpeedC;
						float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (ase_worldPos * _NoiseScaleC)));
						float simplePerlin3D255 = snoise(temp_output_252_0);

						float3 ase_worldNormal = TransformObjectToWorldNormal(v.ase_normal);
						o.ase_texcoord2.xyz = ase_worldNormal;

						o.ase_color = v.ase_color;

						//setting value to unused interpolator channels and avoid initialization warnings
						o.ase_texcoord2.w = 0;

						#ifdef ASE_ABSOLUTE_VERTEX_POS
							float3 defaultVertexValue = v.vertex.xyz;
						#else
							float3 defaultVertexValue = float3(0, 0, 0);
						#endif
						float3 vertexValue = ((objToWorldDir239 * _NoiseStrengthA * temp_output_8_0) + (objToWorldDir213 * (0.0 + (simplePerlin3D78 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthB) + (objToWorldDir257 * (0.0 + (simplePerlin3D255 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthC));
						#ifdef ASE_ABSOLUTE_VERTEX_POS
							v.vertex.xyz = vertexValue;
						#else
							v.vertex.xyz += vertexValue;
						#endif

						v.ase_normal = v.ase_normal;

						float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						o.worldPos = positionWS;
						#endif

						o.clipPos = MetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST);
						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							VertexPositionInputs vertexInput = (VertexPositionInputs)0;
							vertexInput.positionWS = positionWS;
							vertexInput.positionCS = o.clipPos;
							o.shadowCoord = GetShadowCoord(vertexInput);
						#endif
						return o;
					}

					#if defined(TESSELLATION_ON)
					struct VertexControl
					{
						float4 vertex : INTERNALTESSPOS;
						float3 ase_normal : NORMAL;
						float4 texcoord1 : TEXCOORD1;
						float4 texcoord2 : TEXCOORD2;
						float4 ase_color : COLOR;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct TessellationFactors
					{
						float edge[3] : SV_TessFactor;
						float inside : SV_InsideTessFactor;
					};

					VertexControl vert(VertexInput v)
					{
						VertexControl o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						o.vertex = v.vertex;
						o.ase_normal = v.ase_normal;
						o.texcoord1 = v.texcoord1;
						o.texcoord2 = v.texcoord2;
						o.ase_color = v.ase_color;
						return o;
					}

					TessellationFactors TessellationFunction(InputPatch<VertexControl,3> v)
					{
						TessellationFactors o;
						float4 tf = 1;
						float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
						float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
						#if defined(ASE_FIXED_TESSELLATION)
						tf = FixedTess(tessValue);
						#elif defined(ASE_DISTANCE_TESSELLATION)
						tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos);
						#elif defined(ASE_LENGTH_TESSELLATION)
						tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams);
						#elif defined(ASE_LENGTH_CULL_TESSELLATION)
						tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes);
						#endif
						o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
						return o;
					}

					[domain("tri")]
					[partitioning("fractional_odd")]
					[outputtopology("triangle_cw")]
					[patchconstantfunc("TessellationFunction")]
					[outputcontrolpoints(3)]
					VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
					{
					   return patch[id];
					}

					[domain("tri")]
					VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
					{
						VertexInput o = (VertexInput)0;
						o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
						o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
						o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
						o.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
						o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
						#if defined(ASE_PHONG_TESSELLATION)
						float3 pp[3];
						for (int i = 0; i < 3; ++i)
							pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
						float phongStrength = _TessPhongStrength;
						o.vertex.xyz = phongStrength * (pp[0] * bary.x + pp[1] * bary.y + pp[2] * bary.z) + (1.0f - phongStrength) * o.vertex.xyz;
						#endif
						UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
						return VertexFunction(o);
					}
					#else
					VertexOutput vert(VertexInput v)
					{
						return VertexFunction(v);
					}
					#endif

					half4 frag(VertexOutput IN) : SV_TARGET
					{
						UNITY_SETUP_INSTANCE_ID(IN);
						UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 WorldPosition = IN.worldPos;
						#endif
						float4 ShadowCoords = float4(0, 0, 0, 0);

						#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
								ShadowCoords = IN.shadowCoord;
							#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
								ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
							#endif
						#endif

						float3 ase_worldNormal = IN.ase_texcoord2.xyz;
						float mulTime248 = _TimeParameters.x * _SpeedC;
						float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (WorldPosition * _NoiseScaleC)));
						float3 NoiseWorldPos364 = temp_output_252_0;
						float4 triplanar194 = TriplanarSampling194(_TopTexture0, NoiseWorldPos364, ase_worldNormal, _Fallof, _Tiling, 1.0, 0);
						float mulTime15 = _TimeParameters.x * _SpeedA;
						float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (WorldPosition * _NoiseScaleA))));
						float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
						float NoiseA366 = temp_output_8_0;
						float2 appendResult376 = (float2(WorldPosition.x , WorldPosition.z));
						float simplePerlin2D374 = snoise((appendResult376 * 0.1));
						float4 lerpResult210 = lerp(saturate((pow(IN.ase_color , 0.454545) * _VertexColorMult)) , _TextureColor , (((1.0 - triplanar194.x) * _textureDetail) * saturate(NoiseA366) * (0.25 + (simplePerlin2D374 - -1.0) * (1.0 - 0.25) / (1.0 - -1.0))));
						float3 ase_worldViewDir = (_WorldSpaceCameraPos.xyz - WorldPosition);
						ase_worldViewDir = normalize(ase_worldViewDir);
						float fresnelNdotV346 = dot(ase_worldNormal, ase_worldViewDir);
						float fresnelNode346 = (_FresnelBSP.x + _FresnelBSP.y * pow(1.0 - fresnelNdotV346, _FresnelBSP.z));


						float3 Albedo = float3(0.5, 0.5, 0.5);
						float3 Emission = saturate((lerpResult210 + (fresnelNode346 * _RimColor))).rgb;
						float Alpha = 1;
						float AlphaClipThreshold = 0.5;

						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif

						MetaInput metaInput = (MetaInput)0;
						metaInput.Albedo = Albedo;
						metaInput.Emission = Emission;

						return MetaFragment(metaInput);
					}
					ENDHLSL
				}


				Pass
				{

					Name "Universal2D"
					Tags { "LightMode" = "Universal2D" }

					Blend One Zero, One Zero
					ZWrite On
					ZTest LEqual
					Offset 0 , 0
					ColorMask RGBA

					HLSLPROGRAM

					#define _NORMAL_DROPOFF_TS 1
					#pragma multi_compile_instancing
					#pragma multi_compile _ LOD_FADE_CROSSFADE
					#pragma multi_compile_fog
					#define ASE_FOG 1
					#define ASE_FIXED_TESSELLATION
					#define TESSELLATION_ON 1
					#pragma require tessellation tessHW
					#pragma hull HullFunction
					#pragma domain DomainFunction
					#define _EMISSION
					#define ASE_SRP_VERSION 70503


					#pragma vertex vert
					#pragma fragment frag

					#define SHADERPASS_2D

					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
					#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
					#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"



					#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

					struct VertexInput
					{
						float4 vertex : POSITION;
						float3 ase_normal : NORMAL;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct VertexOutput
					{
						float4 clipPos : SV_POSITION;
						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 worldPos : TEXCOORD0;
						#endif
						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
						float4 shadowCoord : TEXCOORD1;
						#endif

						UNITY_VERTEX_INPUT_INSTANCE_ID
						UNITY_VERTEX_OUTPUT_STEREO
					};

					CBUFFER_START(UnityPerMaterial)
					float4 _RimColor;
					float4 _TextureColor;
					float3 _NoiseScaleC;
					float3 _FresnelBSP;
					float3 _NoiseScaleB;
					float3 _DirectionC;
					float3 _NoiseScaleA;
					float3 _DirectionA;
					float3 _DirectionB;
					float2 _Tiling;
					float _SpeedB;
					float _3dNoiseSizeB;
					float _NoiseStrengthB;
					float _3dNoiseSizeA;
					float _SpeedC;
					float _3dNoiseSizeC;
					float _SpeedA;
					float _NoiseStrengthC;
					float _VertexColorMult;
					float _Fallof;
					float _textureDetail;
					float _NoiseStrengthA;
					#ifdef _TRANSMISSION_ASE
						float _TransmissionShadow;
					#endif
					#ifdef _TRANSLUCENCY_ASE
						float _TransStrength;
						float _TransNormal;
						float _TransScattering;
						float _TransDirect;
						float _TransAmbient;
						float _TransShadow;
					#endif
					#ifdef TESSELLATION_ON
						float _TessPhongStrength;
						float _TessValue;
						float _TessMin;
						float _TessMax;
						float _TessEdgeLength;
						float _TessMaxDisp;
					#endif
					CBUFFER_END


					float3 mod3D289(float3 x) { return x - floor(x / 289.0) * 289.0; }
					float4 mod3D289(float4 x) { return x - floor(x / 289.0) * 289.0; }
					float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
					float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }
					float snoise(float3 v)
					{
						const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
						float3 i = floor(v + dot(v, C.yyy));
						float3 x0 = v - i + dot(i, C.xxx);
						float3 g = step(x0.yzx, x0.xyz);
						float3 l = 1.0 - g;
						float3 i1 = min(g.xyz, l.zxy);
						float3 i2 = max(g.xyz, l.zxy);
						float3 x1 = x0 - i1 + C.xxx;
						float3 x2 = x0 - i2 + C.yyy;
						float3 x3 = x0 - 0.5;
						i = mod3D289(i);
						float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
						float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)
						float4 x_ = floor(j / 7.0);
						float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)
						float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
						float4 h = 1.0 - abs(x) - abs(y);
						float4 b0 = float4(x.xy, y.xy);
						float4 b1 = float4(x.zw, y.zw);
						float4 s0 = floor(b0) * 2.0 + 1.0;
						float4 s1 = floor(b1) * 2.0 + 1.0;
						float4 sh = -step(h, 0.0);
						float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
						float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
						float3 g0 = float3(a0.xy, h.x);
						float3 g1 = float3(a0.zw, h.y);
						float3 g2 = float3(a1.xy, h.z);
						float3 g3 = float3(a1.zw, h.w);
						float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
						g0 *= norm.x;
						g1 *= norm.y;
						g2 *= norm.z;
						g3 *= norm.w;
						float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
						m = m * m;
						m = m * m;
						float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
						return 42.0 * dot(m, px);
					}


					VertexOutput VertexFunction(VertexInput v)
					{
						VertexOutput o = (VertexOutput)0;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

						float3 objToWorldDir239 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime15 = _TimeParameters.x * _SpeedA;
						float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
						float simplePerlin3D3 = snoise(((_DirectionA * mulTime15) + (_3dNoiseSizeA * (ase_worldPos * _NoiseScaleA))));
						float temp_output_8_0 = (0.0 + (simplePerlin3D3 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
						float3 objToWorldDir213 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime75 = _TimeParameters.x * _SpeedB;
						float simplePerlin3D78 = snoise(((_DirectionB * mulTime75) + (_3dNoiseSizeB * (ase_worldPos * _NoiseScaleB))));
						float3 objToWorldDir257 = mul(GetObjectToWorldMatrix(), float4(float3(0,1,0), 0)).xyz;
						float mulTime248 = _TimeParameters.x * _SpeedC;
						float3 temp_output_252_0 = ((_DirectionC * mulTime248) + (_3dNoiseSizeC * (ase_worldPos * _NoiseScaleC)));
						float simplePerlin3D255 = snoise(temp_output_252_0);


						#ifdef ASE_ABSOLUTE_VERTEX_POS
							float3 defaultVertexValue = v.vertex.xyz;
						#else
							float3 defaultVertexValue = float3(0, 0, 0);
						#endif
						float3 vertexValue = ((objToWorldDir239 * _NoiseStrengthA * temp_output_8_0) + (objToWorldDir213 * (0.0 + (simplePerlin3D78 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthB) + (objToWorldDir257 * (0.0 + (simplePerlin3D255 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _NoiseStrengthC));
						#ifdef ASE_ABSOLUTE_VERTEX_POS
							v.vertex.xyz = vertexValue;
						#else
							v.vertex.xyz += vertexValue;
						#endif

						v.ase_normal = v.ase_normal;

						float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
						float4 positionCS = TransformWorldToHClip(positionWS);

						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						o.worldPos = positionWS;
						#endif

						#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							VertexPositionInputs vertexInput = (VertexPositionInputs)0;
							vertexInput.positionWS = positionWS;
							vertexInput.positionCS = positionCS;
							o.shadowCoord = GetShadowCoord(vertexInput);
						#endif

						o.clipPos = positionCS;
						return o;
					}

					#if defined(TESSELLATION_ON)
					struct VertexControl
					{
						float4 vertex : INTERNALTESSPOS;
						float3 ase_normal : NORMAL;

						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct TessellationFactors
					{
						float edge[3] : SV_TessFactor;
						float inside : SV_InsideTessFactor;
					};

					VertexControl vert(VertexInput v)
					{
						VertexControl o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_TRANSFER_INSTANCE_ID(v, o);
						o.vertex = v.vertex;
						o.ase_normal = v.ase_normal;

						return o;
					}

					TessellationFactors TessellationFunction(InputPatch<VertexControl,3> v)
					{
						TessellationFactors o;
						float4 tf = 1;
						float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
						float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
						#if defined(ASE_FIXED_TESSELLATION)
						tf = FixedTess(tessValue);
						#elif defined(ASE_DISTANCE_TESSELLATION)
						tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos);
						#elif defined(ASE_LENGTH_TESSELLATION)
						tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams);
						#elif defined(ASE_LENGTH_CULL_TESSELLATION)
						tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes);
						#endif
						o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
						return o;
					}

					[domain("tri")]
					[partitioning("fractional_odd")]
					[outputtopology("triangle_cw")]
					[patchconstantfunc("TessellationFunction")]
					[outputcontrolpoints(3)]
					VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
					{
					   return patch[id];
					}

					[domain("tri")]
					VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
					{
						VertexInput o = (VertexInput)0;
						o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
						o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;

						#if defined(ASE_PHONG_TESSELLATION)
						float3 pp[3];
						for (int i = 0; i < 3; ++i)
							pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
						float phongStrength = _TessPhongStrength;
						o.vertex.xyz = phongStrength * (pp[0] * bary.x + pp[1] * bary.y + pp[2] * bary.z) + (1.0f - phongStrength) * o.vertex.xyz;
						#endif
						UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
						return VertexFunction(o);
					}
					#else
					VertexOutput vert(VertexInput v)
					{
						return VertexFunction(v);
					}
					#endif

					half4 frag(VertexOutput IN) : SV_TARGET
					{
						UNITY_SETUP_INSTANCE_ID(IN);
						UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

						#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
						float3 WorldPosition = IN.worldPos;
						#endif
						float4 ShadowCoords = float4(0, 0, 0, 0);

						#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
							#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
								ShadowCoords = IN.shadowCoord;
							#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
								ShadowCoords = TransformWorldToShadowCoord(WorldPosition);
							#endif
						#endif



						float3 Albedo = float3(0.5, 0.5, 0.5);
						float Alpha = 1;
						float AlphaClipThreshold = 0.5;

						half4 color = half4(Albedo, Alpha);

						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif

						return color;
					}
					ENDHLSL
				}

		}

			CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
						Fallback "Hidden/InternalErrorShader"

}
/*ASEBEGIN
Version=18935
100;509.6;1536;815;-2345.655;-1956.823;3.550255;True;True
Node;AmplifyShaderEditor.CommentaryNode;242;4334.598,3400.093;Inherit;False;1681.438;904.9535;noise B;17;259;258;257;256;255;254;252;251;250;249;248;246;245;244;243;279;364;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;363;4392.098,1354.656;Inherit;False;1575.466;961.8254;Noise A;17;6;136;16;15;86;5;135;85;7;238;239;14;3;8;123;125;366;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;214;4317.949,2412.926;Inherit;False;1681.438;904.9535;noise B;16;79;143;74;75;144;80;76;87;77;78;149;151;148;212;213;145;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;6;4444.608,1946.557;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;245;4401.574,4121.045;Float;False;Property;_NoiseScaleC;NoiseScale C;11;0;Create;True;0;0;0;False;0;False;1,1,1;1,1,2.02;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;74;4367.949,2976.447;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;16;4442.098,1598.43;Float;False;Property;_SpeedA;Speed A;3;0;Create;True;0;0;0;False;0;False;0;0.55;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;243;4408.489,3739.104;Float;False;Property;_SpeedC;SpeedC;13;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;79;4391.839,2751.937;Float;False;Property;_SpeedB;Speed B;8;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;143;4384.924,3133.879;Float;False;Property;_NoiseScaleB;NoiseScale B;7;0;Create;True;0;0;0;False;0;False;1,1,1;1,1,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;136;4460.632,2132.481;Float;False;Property;_NoiseScaleA;NoiseScale A;1;0;Create;True;0;0;0;False;0;False;1,1,1;1,1,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;244;4384.599,3963.614;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;246;4534.873,3854.899;Float;False;Property;_3dNoiseSizeC;3dNoise Size C;12;0;Create;True;0;0;0;False;0;False;0;-1.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;15;4654.397,1604.73;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;279;4568.108,3554.552;Float;False;Property;_DirectionC;DirectionC;14;0;Create;True;0;0;0;False;0;False;1,0,0;1,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;86;4663.145,1404.656;Float;False;Property;_DirectionA;Direction A;4;0;Create;True;0;0;0;False;0;False;1,0,0;1.5,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;249;4612.297,4070.366;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;5;4615.051,1817.031;Float;False;Property;_3dNoiseSizeA;3dNoise Size A;2;0;Create;True;0;0;0;False;0;False;0;0.57;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;248;4605.451,3742.736;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;144;4595.647,3083.199;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;135;4710.632,2040.481;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;75;4588.801,2755.57;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;145;4561.48,2563.366;Float;False;Property;_DirectionB;Direction B;9;0;Create;True;0;0;0;False;0;False;1,0,0;1.48,-0.86,-0.36;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;80;4518.223,2867.733;Float;False;Property;_3dNoiseSizeB;3dNoise Size B;6;0;Create;True;0;0;0;False;0;False;0;1.49;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;4872.709,1875.656;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;251;4770.647,3923.612;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;85;4905.949,1551.789;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;250;4784.732,3693.801;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;4753.997,2936.445;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;87;4768.082,2706.634;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;77;4927.579,2845.2;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;5079.703,1854.33;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;252;4944.229,3832.367;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;212;5229.679,2503.926;Float;False;Constant;_Vector2;Vector 2;22;0;Create;True;0;0;0;False;0;False;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NoiseGeneratorNode;3;5268.188,1847.958;Inherit;False;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;238;5261.007,1515.841;Float;False;Constant;_Vector0;Vector 0;22;0;Create;True;0;0;0;False;0;False;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;254;5246.329,3491.093;Float;False;Constant;_Vector5;Vector 5;22;0;Create;True;0;0;0;False;0;False;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NoiseGeneratorNode;78;5216.059,2835.991;Inherit;False;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;255;5232.708,3823.158;Inherit;False;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;257;5442.313,3493.872;Inherit;False;Object;World;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformDirectionNode;213;5426.699,2515.469;Inherit;False;Object;World;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;8;5524.887,1844.041;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;125;5395.798,1727.332;Float;False;Property;_NoiseStrengthA;Noise Strength A;5;0;Create;True;0;0;0;False;0;False;0;0.472;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;258;5584.383,3849.455;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;256;5409.569,3694.243;Float;False;Property;_NoiseStrengthC;Noise Strength C;15;0;Create;True;0;0;0;False;0;False;0;0.071;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;149;5567.733,2862.288;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;239;5489.639,1516.662;Inherit;False;Object;World;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;148;5392.92,2707.076;Float;False;Property;_NoiseStrengthB;Noise Strength B;10;0;Create;True;0;0;0;False;0;False;0;0.182;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;123;5850.154,1830.067;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;151;5830.387,2641.828;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;259;5847.036,3628.995;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;281;4115.208,-38.51873;Inherit;False;1811.842;1319.317;COLOR;23;210;204;318;371;369;240;320;370;193;241;211;319;192;194;365;198;197;378;379;374;375;377;376;;1,1,1,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;376;4539.468,968.3293;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;366;5748.416,2067.205;Float;False;NoiseA;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;371;5362.035,55.19904;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;192;4743.418,11.88875;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;197;4170.305,475.2054;Float;False;Property;_Tiling;Tiling;18;0;Create;True;0;0;0;False;0;False;0,0;0.04,0.04;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TriplanarNode;194;4444.903,450.592;Inherit;True;Spherical;World;False;Top Texture 0;_TopTexture0;white;0;None;Mid Texture 0;_MidTexture0;white;-1;None;Bot Texture 0;_BotTexture0;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;241;4536.446,687.6609;Float;False;Property;_textureDetail;textureDetail;16;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;365;4156.804,383.9981;Inherit;False;364;NoiseWorldPos;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;374;4813.44,1007.488;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;370;4938.91,145.7891;Float;False;Property;_VertexColorMult;Vertex Color Mult;20;0;Create;True;0;0;0;False;0;False;1;1.12;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;193;4962.068,25.63408;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.454545;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;379;5017.517,974.5007;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0.25;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;240;5057.172,580.0022;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;356;6106.303,1660.543;Float;False;Property;_FresnelBSP;FresnelBSP;22;0;Create;True;0;0;0;False;0;False;0,0,0;0.22,1,1.45;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ColorNode;350;6366.394,1440.562;Float;False;Property;_RimColor;Rim Color;21;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.5188679,0.2388324,0.002447496,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;377;4677.468,1027.329;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;364;5080.631,4004.96;Float;False;NoiseWorldPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;318;5223.026,646.053;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;355;7268.551,1822.479;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;204;5104.326,283.5752;Float;False;Property;_TextureColor;Texture Color;17;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.509434,0.4349413,0.5047782,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;174;7780.481,2672.388;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;349;6658.039,1715.342;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;348;6953.387,1712.356;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;369;5191.03,44.91639;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;169;7176.476,2656.444;Float;False;Property;_blendfadedist;blend fade dist;23;0;Create;True;0;0;0;False;0;False;0;0.388;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;319;4582.11,791.982;Inherit;False;366;NoiseA;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;198;4168.964,640.3564;Float;False;Property;_Fallof;Fallof;19;0;Create;True;0;0;0;False;0;False;0;119.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;152;6311.289,2742.257;Inherit;True;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DepthFade;172;7490.476,2668.444;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;378;4482.468,1096.329;Float;False;Constant;_Float0;Float 0;25;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;320;4883.821,799.5373;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;346;6372.781,1679.91;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;211;4874.622,463.2088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;375;4343.468,937.3293;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;210;5583.373,301.7911;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;391;6608.578,2660.824;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;392;6608.578,2660.824;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;390;6608.578,2660.824;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;389;6608.578,2660.824;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;387;6608.578,2660.824;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;388;6608.578,2660.824;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;_Clouds/Clouds Unlit Vertex Color2;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;18;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;38;Workflow;1;0;Surface;0;0;  Refraction Model;0;0;  Blend;0;0;Two Sided;1;0;Fragment Normal Space,InvertActionOnDeselection;0;0;Transmission;0;0;  Transmission Shadow;0.5,False,-1;0;Translucency;0;0;  Translucency Strength;1,False,-1;0;  Normal Distortion;0.5,False,-1;0;  Scattering;2,False,-1;0;  Direct;0.9,False,-1;0;  Ambient;0.1,False,-1;0;  Shadow;0.5,False,-1;0;Cast Shadows;1;0;  Use Shadow Threshold;0;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;1;0;Built-in Fog;1;0;_FinalColorxAlpha;0;0;Meta Pass;1;0;Override Baked GI;0;0;Extra Pre Pass;0;0;DOTS Instancing;0;0;Tessellation;1;637796505514061556;  Phong;0;0;  Strength;0.5,False,-1;0;  Type;0;0;  Tess;16,False,-1;0;  Min;10,False,-1;0;  Max;25,False,-1;0;  Edge Length;16,False,-1;0;  Max Displacement;25,False,-1;0;Write Depth;0;0;  Early Z;0;0;Vertex Position,InvertActionOnDeselection;1;637796508831639653;0;6;False;True;True;True;True;True;False;;False;0
WireConnection;15;0;16;0
WireConnection;249;0;244;0
WireConnection;249;1;245;0
WireConnection;248;0;243;0
WireConnection;144;0;74;0
WireConnection;144;1;143;0
WireConnection;135;0;6;0
WireConnection;135;1;136;0
WireConnection;75;0;79;0
WireConnection;7;0;5;0
WireConnection;7;1;135;0
WireConnection;251;0;246;0
WireConnection;251;1;249;0
WireConnection;85;0;86;0
WireConnection;85;1;15;0
WireConnection;250;0;279;0
WireConnection;250;1;248;0
WireConnection;76;0;80;0
WireConnection;76;1;144;0
WireConnection;87;0;145;0
WireConnection;87;1;75;0
WireConnection;77;0;87;0
WireConnection;77;1;76;0
WireConnection;14;0;85;0
WireConnection;14;1;7;0
WireConnection;252;0;250;0
WireConnection;252;1;251;0
WireConnection;3;0;14;0
WireConnection;78;0;77;0
WireConnection;255;0;252;0
WireConnection;257;0;254;0
WireConnection;213;0;212;0
WireConnection;8;0;3;0
WireConnection;258;0;255;0
WireConnection;149;0;78;0
WireConnection;239;0;238;0
WireConnection;123;0;239;0
WireConnection;123;1;125;0
WireConnection;123;2;8;0
WireConnection;151;0;213;0
WireConnection;151;1;149;0
WireConnection;151;2;148;0
WireConnection;259;0;257;0
WireConnection;259;1;258;0
WireConnection;259;2;256;0
WireConnection;376;0;375;1
WireConnection;376;1;375;3
WireConnection;366;0;8;0
WireConnection;371;0;369;0
WireConnection;194;9;365;0
WireConnection;194;3;197;0
WireConnection;194;4;198;0
WireConnection;374;0;377;0
WireConnection;193;0;192;0
WireConnection;379;0;374;0
WireConnection;240;0;211;0
WireConnection;240;1;241;0
WireConnection;377;0;376;0
WireConnection;377;1;378;0
WireConnection;364;0;252;0
WireConnection;318;0;240;0
WireConnection;318;1;320;0
WireConnection;318;2;379;0
WireConnection;355;0;348;0
WireConnection;174;0;172;0
WireConnection;349;0;346;0
WireConnection;349;1;350;0
WireConnection;348;0;210;0
WireConnection;348;1;349;0
WireConnection;369;0;193;0
WireConnection;369;1;370;0
WireConnection;152;0;123;0
WireConnection;152;1;151;0
WireConnection;152;2;259;0
WireConnection;172;0;169;0
WireConnection;320;0;319;0
WireConnection;346;1;356;1
WireConnection;346;2;356;2
WireConnection;346;3;356;3
WireConnection;211;0;194;1
WireConnection;210;0;371;0
WireConnection;210;1;204;0
WireConnection;210;2;318;0
WireConnection;388;2;355;0
WireConnection;388;8;152;0
ASEEND*/
//CHKSM=085D4CA9ACFC73C5090011C643C5A239B356C3A3



//
////VORONOI MIT LICENSE
//Tutorials
//https ://www.iquilezles.org/www/articles/smoothvoronoi/smoothvoronoi.htm
//https://www.iquilezles.org/www/articles/voronoise/voronoise.htm
//https://thebookofshaders.com/12/
//
//https://www.shadertoy.com/view/ldB3zc
//// The MIT License
//// Copyright © 2014 Inigo Quilez
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//// Smooth Voronoi - avoiding aliasing, by replacing the usual min() function, which is
//// discontinuous, with a smooth version. That can help preventing some aliasing, and also
//// provides with more artistic control of the final procedural textures/models.
//
//// More Voronoi shaders:
////
//// Exact edges:  https://www.shadertoy.com/view/ldl3W8
//// Hierarchical: https://www.shadertoy.com/view/Xll3zX
//// Smooth:       https://www.shadertoy.com/view/ldB3zc
//// Voronoise:    https://www.shadertoy.com/view/Xd23Dh
//
//
//
//// Hierarchical: https://www.shadertoy.com/view/Xll3zX
//// The MIT License
//// Copyright © 2015 Inigo Quilez
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//// An attempt to hierarchical Voronoi.
//
//
//
//https://www.shadertoy.com/view/ldl3W8
//// The MIT License
//// Copyright © 2013 Inigo Quilez
//// https://www.youtube.com/c/InigoQuilez
//// https://iquilezles.org/
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//// I've not seen anybody out there computing correct cell interior distances for Voronoi
//// patterns yet. That's why they cannot shade the cell interior correctly, and why you've
//// never seen cell boundaries rendered correctly. 
////
//// However, here's how you do mathematically correct distances (note the equidistant and non
//// degenerated grey isolines inside the cells) and hence edges (in yellow):
////
//// http://www.iquilezles.org/www/articles/voronoilines/voronoilines.htm
//
//
//https://www.shadertoy.com/view/Xd23Dh
//// The MIT License
//// https://www.youtube.com/c/InigoQuilez
//// https://iquilezles.org/
//// Copyright © 2014 Inigo Quilez
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////
//Noise - Value - 2D - Periodic
//https ://www.shadertoy.com/view/3d2GRh
//// The MIT License
//// Copyright © 2019 Inigo Quilez
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

