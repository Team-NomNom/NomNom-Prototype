// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Crosshatch 2019"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[ASEBegin]_HatchTexture("HatchTexture", 2D) = "white" {}
		[ASEEnd]_Tiling("Tiling", Vector) = (0,0,0,0)

			//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
			//_TransStrength( "Trans Strength", Range( 0, 50 ) ) = 1
			//_TransNormal( "Trans Normal Distortion", Range( 0, 1 ) ) = 0.5
			//_TransScattering( "Trans Scattering", Range( 1, 50 ) ) = 2
			//_TransDirect( "Trans Direct", Range( 0, 1 ) ) = 0.9
			//_TransAmbient( "Trans Ambient", Range( 0, 1 ) ) = 0.1
			//_TransShadow( "Trans Shadow", Range( 0, 1 ) ) = 0.5
			//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
			//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
			//_TessMin( "Tess Min Distance", Float ) = 10
			//_TessMax( "Tess Max Distance", Float ) = 25
			//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
			//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

		SubShader
		{
			LOD 0



			Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
			Cull Back
			AlphaToMask Off
			HLSLINCLUDE
			#pragma target 2.0

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
				Tags { "LightMode" = "UniversalForward" }

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
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

				#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				#pragma multi_compile _ _SHADOWS_SOFT
				#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

				#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
				#pragma multi_compile _ SHADOWS_SHADOWMASK

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

				#define ASE_NEEDS_FRAG_SCREEN_POSITION
				#define ASE_NEEDS_FRAG_WORLD_NORMAL
				#define ASE_NEEDS_FRAG_WORLD_POSITION
				#define ASE_NEEDS_FRAG_SHADOWCOORDS


				struct VertexInput
				{
					float4 vertex : POSITION;
					float3 ase_normal : NORMAL;
					float4 ase_tangent : TANGENT;
					float4 texcoord1 : TEXCOORD1;
					float4 texcoord : TEXCOORD0;

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

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				CBUFFER_START(UnityPerMaterial)
				float3 _Tiling;
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
				sampler2D _HatchTexture;



				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.ase_normal = v.ase_normal;

					float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
					float3 positionVS = TransformWorldToView(positionWS);
					float4 positionCS = TransformWorldToHClip(positionWS);

					VertexNormalInputs normalInput = GetVertexNormalInputs(v.ase_normal, v.ase_tangent);

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

					float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
					ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float4 break12 = ase_screenPosNorm;
					float2 appendResult4_g1 = (float2(break12.x , break12.y));
					float2 temp_output_17_0 = ((appendResult4_g1 * 4.0) / break12.w);
					float dotResult19 = dot(WorldNormal , _MainLightPosition.xyz);
					float ase_lightAtten = 0;
					Light ase_lightAtten_mainLight = GetMainLight(ShadowCoords);
					ase_lightAtten = ase_lightAtten_mainLight.distanceAttenuation * ase_lightAtten_mainLight.shadowAttenuation;
					float temp_output_24_0 = saturate(((dotResult19 * ase_lightAtten) * length(_MainLightColor.rgb)));
					float lerpResult34 = lerp(tex2D(_HatchTexture, (_Tiling.y * temp_output_17_0)).g , tex2D(_HatchTexture, (_Tiling.z * temp_output_17_0)).b , temp_output_24_0);
					float lerpResult35 = lerp(lerpResult34 , tex2D(_HatchTexture, (_Tiling.x * temp_output_17_0)).r , temp_output_24_0);
					float3 temp_cast_0 = (saturate(lerpResult35)).xxx;

					float3 Albedo = float3(0.5, 0.5, 0.5);
					float3 Normal = float3(0, 0, 1);
					float3 Emission = temp_cast_0;
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

					inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.clipPos);
					inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUVOrVertexSH.xy);

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
						float3 refractionOffset = (RefractionIndex - 1.0) * mul(UNITY_MATRIX_V, WorldNormal).xyz * (1.0 - dot(WorldNormal, WorldViewDirection));
						projScreenPos.xy += refractionOffset.xy;
						float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR(projScreenPos) * RefractionColor;
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
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

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
				float3 _Tiling;
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



				float3 _LightDirection;

				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
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
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

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
				float3 _Tiling;
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



				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
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
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

				#pragma vertex vert
				#pragma fragment frag

				#define SHADERPASS_META

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

				#define ASE_NEEDS_VERT_NORMAL
				#define ASE_NEEDS_FRAG_WORLD_POSITION
				#define ASE_NEEDS_FRAG_SHADOWCOORDS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _SHADOWS_SOFT


				#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

				struct VertexInput
				{
					float4 vertex : POSITION;
					float3 ase_normal : NORMAL;
					float4 texcoord1 : TEXCOORD1;
					float4 texcoord2 : TEXCOORD2;

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
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				CBUFFER_START(UnityPerMaterial)
				float3 _Tiling;
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
				sampler2D _HatchTexture;



				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
					float4 screenPos = ComputeScreenPos(ase_clipPos);
					o.ase_texcoord2 = screenPos;
					float3 ase_worldNormal = TransformObjectToWorldNormal(v.ase_normal);
					o.ase_texcoord3.xyz = ase_worldNormal;


					//setting value to unused interpolator channels and avoid initialization warnings
					o.ase_texcoord3.w = 0;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
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

					float4 screenPos = IN.ase_texcoord2;
					float4 ase_screenPosNorm = screenPos / screenPos.w;
					ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float4 break12 = ase_screenPosNorm;
					float2 appendResult4_g1 = (float2(break12.x , break12.y));
					float2 temp_output_17_0 = ((appendResult4_g1 * 4.0) / break12.w);
					float3 ase_worldNormal = IN.ase_texcoord3.xyz;
					float dotResult19 = dot(ase_worldNormal , _MainLightPosition.xyz);
					float ase_lightAtten = 0;
					Light ase_lightAtten_mainLight = GetMainLight(ShadowCoords);
					ase_lightAtten = ase_lightAtten_mainLight.distanceAttenuation * ase_lightAtten_mainLight.shadowAttenuation;
					float temp_output_24_0 = saturate(((dotResult19 * ase_lightAtten) * length(_MainLightColor.rgb)));
					float lerpResult34 = lerp(tex2D(_HatchTexture, (_Tiling.y * temp_output_17_0)).g , tex2D(_HatchTexture, (_Tiling.z * temp_output_17_0)).b , temp_output_24_0);
					float lerpResult35 = lerp(lerpResult34 , tex2D(_HatchTexture, (_Tiling.x * temp_output_17_0)).r , temp_output_24_0);
					float3 temp_cast_0 = (saturate(lerpResult35)).xxx;


					float3 Albedo = float3(0.5, 0.5, 0.5);
					float3 Emission = temp_cast_0;
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
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

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
				float3 _Tiling;
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



				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);



					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
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


			Pass
			{

				Name "DepthNormals"
				Tags { "LightMode" = "DepthNormals" }

				ZWrite On
				Blend One Zero
				ZTest LEqual
				ZWrite On

				HLSLPROGRAM
				#define _NORMAL_DROPOFF_TS 1
				#pragma multi_compile_instancing
				#pragma multi_compile _ LOD_FADE_CROSSFADE
				#pragma multi_compile_fog
				#define ASE_FOG 1
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

				#pragma vertex vert
				#pragma fragment frag

				#define SHADERPASS_DEPTHNORMALSONLY

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
					float3 worldNormal : TEXCOORD2;

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				CBUFFER_START(UnityPerMaterial)
				float3 _Tiling;
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



				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif

					v.ase_normal = v.ase_normal;
					float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
					float3 normalWS = TransformObjectToWorldNormal(v.ase_normal);
					float4 positionCS = TransformWorldToHClip(positionWS);

					#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.worldPos = positionWS;
					#endif

					o.worldNormal = normalWS;

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

					return float4(PackNormalOctRectEncode(TransformWorldToViewDir(IN.worldNormal, true)), 0.0, 0.0);
				}
				ENDHLSL
			}


			Pass
			{

				Name "GBuffer"
				Tags { "LightMode" = "UniversalGBuffer" }

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
				#define _EMISSION
				#define ASE_SRP_VERSION 100302

				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				#pragma multi_compile _ _SHADOWS_SOFT
				#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
				#pragma multi_compile _ _GBUFFER_NORMALS_OCT

				#pragma multi_compile _ DIRLIGHTMAP_COMBINED
				#pragma multi_compile _ LIGHTMAP_ON

				#pragma vertex vert
				#pragma fragment frag

				#define SHADERPASS SHADERPASS_GBUFFER

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

				#if ASE_SRP_VERSION <= 70108
				#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				#endif

				#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
					#define ENABLE_TERRAIN_PERPIXEL_NORMAL
				#endif

				#define ASE_NEEDS_FRAG_SCREEN_POSITION
				#define ASE_NEEDS_FRAG_WORLD_NORMAL
				#define ASE_NEEDS_FRAG_WORLD_POSITION
				#define ASE_NEEDS_FRAG_SHADOWCOORDS


				struct VertexInput
				{
					float4 vertex : POSITION;
					float3 ase_normal : NORMAL;
					float4 ase_tangent : TANGENT;
					float4 texcoord1 : TEXCOORD1;
					float4 texcoord : TEXCOORD0;

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

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				CBUFFER_START(UnityPerMaterial)
				float3 _Tiling;
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
				sampler2D _HatchTexture;



				VertexOutput VertexFunction(VertexInput v)
				{
					VertexOutput o = (VertexOutput)0;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.ase_normal = v.ase_normal;

					float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
					float3 positionVS = TransformWorldToView(positionWS);
					float4 positionCS = TransformWorldToHClip(positionWS);

					VertexNormalInputs normalInput = GetVertexNormalInputs(v.ase_normal, v.ase_tangent);

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
				FragmentOutput frag(VertexOutput IN
									#ifdef ASE_DEPTH_WRITE_ON
									,out float outputDepth : ASE_SV_DEPTH
									#endif
									 )
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

					float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
					ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float4 break12 = ase_screenPosNorm;
					float2 appendResult4_g1 = (float2(break12.x , break12.y));
					float2 temp_output_17_0 = ((appendResult4_g1 * 4.0) / break12.w);
					float dotResult19 = dot(WorldNormal , _MainLightPosition.xyz);
					float ase_lightAtten = 0;
					Light ase_lightAtten_mainLight = GetMainLight(ShadowCoords);
					ase_lightAtten = ase_lightAtten_mainLight.distanceAttenuation * ase_lightAtten_mainLight.shadowAttenuation;
					float temp_output_24_0 = saturate(((dotResult19 * ase_lightAtten) * length(_MainLightColor.rgb)));
					float lerpResult34 = lerp(tex2D(_HatchTexture, (_Tiling.y * temp_output_17_0)).g , tex2D(_HatchTexture, (_Tiling.z * temp_output_17_0)).b , temp_output_24_0);
					float lerpResult35 = lerp(lerpResult34 , tex2D(_HatchTexture, (_Tiling.x * temp_output_17_0)).r , temp_output_24_0);
					float3 temp_cast_0 = (saturate(lerpResult35)).xxx;

					float3 Albedo = float3(0.5, 0.5, 0.5);
					float3 Normal = float3(0, 0, 1);
					float3 Emission = temp_cast_0;
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

					BRDFData brdfData;
					InitializeBRDFData(Albedo, Metallic, Specular, Smoothness, Alpha, brdfData);
					half4 color;
					color.rgb = GlobalIllumination(brdfData, inputData.bakedGI, Occlusion, inputData.normalWS, inputData.viewDirectionWS);
					color.a = Alpha;

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
						float3 refractionOffset = (RefractionIndex - 1.0) * mul(UNITY_MATRIX_V, WorldNormal).xyz * (1.0 - dot(WorldNormal, WorldViewDirection));
						projScreenPos.xy += refractionOffset.xy;
						float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR(projScreenPos) * RefractionColor;
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

					return BRDFDataToGbuffer(brdfData, inputData, Smoothness, Emission + color.rgb);
				}

				ENDHLSL
			}

		}
			/*ase_lod*/
					CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
					Fallback "Hidden/InternalErrorShader"

}
/*ASEBEGIN
Version=18900
1536.8;73.6;911.2;965;3058.23;971.8258;1.760552;True;False
Node;AmplifyShaderEditor.ScreenPosInputsNode;61;-2628.442,-382.86;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;12;-2414.136,-371.5701;Inherit;True;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;16;-2075.277,-279.9718;Inherit;False;Constant;_Float0;Float 0;0;0;Create;True;0;0;0;False;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;14;-2173.277,-383.9718;Inherit;False;MakeFloat2;-1;;1;8fda03c5ec029854aa45bd33bc82e849;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldNormalVector;18;-1713.607,544.9077;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;10;-1718.714,727.5667;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DotProductOpNode;19;-1435.661,572.7463;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-1928.277,-379.9718;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LightAttenuation;8;-1692.074,999.9548;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;9;-1680.714,869.5667;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.LengthOpNode;21;-1219.76,973.0026;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;25;-1593.123,-538.5649;Inherit;False;Property;_Tiling;Tiling;1;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-1204.004,703.5172;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;17;-1738.277,-238.9718;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-1339.5,-314.877;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-927.7599,862.0026;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-1341.5,-220.8769;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;33;-1171.354,21.11322;Inherit;True;Property;_HatchTexture;HatchTexture;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;32;-781.0523,22.48339;Inherit;True;Property;_TextureSample2;Texture Sample 2;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;31;-783.2623,-169.7834;Inherit;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1339.123,-408.5649;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;24;-750.76,852.0026;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;30;-780.5507,-368.0919;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;34;-244.5632,463.9653;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;35;24.74853,472.7375;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;37;257.6992,475.2905;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;6;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthNormals;0;6;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=DepthNormals;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;5;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;7;-1302.332,192.2906;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;GBuffer;0;7;GBuffer;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalGBuffer;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;-1302.332,192.2906;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;462.3939,428.4371;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;Crosshatch;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;18;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;38;Workflow;1;Surface;0;  Refraction Model;0;  Blend;0;Two Sided;1;Fragment Normal Space,InvertActionOnDeselection;0;Transmission;0;  Transmission Shadow;0.5,False,-1;Translucency;0;  Translucency Strength;1,False,-1;  Normal Distortion;0.5,False,-1;  Scattering;2,False,-1;  Direct;0.9,False,-1;  Ambient;0.1,False,-1;  Shadow;0.5,False,-1;Cast Shadows;1;  Use Shadow Threshold;0;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;_FinalColorxAlpha;0;Meta Pass;1;Override Baked GI;0;Extra Pre Pass;0;DOTS Instancing;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Write Depth;0;  Early Z;0;Vertex Position,InvertActionOnDeselection;1;0;8;False;True;True;True;True;True;True;True;False;;False;0
WireConnection;12;0;61;0
WireConnection;14;1;12;0
WireConnection;14;2;12;1
WireConnection;19;0;18;0
WireConnection;19;1;10;0
WireConnection;15;0;14;0
WireConnection;15;1;16;0
WireConnection;21;0;9;1
WireConnection;20;0;19;0
WireConnection;20;1;8;0
WireConnection;17;0;15;0
WireConnection;17;1;12;3
WireConnection;28;0;25;2
WireConnection;28;1;17;0
WireConnection;22;0;20;0
WireConnection;22;1;21;0
WireConnection;29;0;25;3
WireConnection;29;1;17;0
WireConnection;32;0;33;0
WireConnection;32;1;29;0
WireConnection;31;0;33;0
WireConnection;31;1;28;0
WireConnection;27;0;25;1
WireConnection;27;1;17;0
WireConnection;24;0;22;0
WireConnection;30;0;33;0
WireConnection;30;1;27;0
WireConnection;34;0;31;2
WireConnection;34;1;32;3
WireConnection;34;2;24;0
WireConnection;35;0;34;0
WireConnection;35;1;30;1
WireConnection;35;2;24;0
WireConnection;37;0;35;0
WireConnection;1;2;37;0
ASEEND*/
//CHKSM=40263BA30DCE80AB8C473F59EE7B0FFC8582ADB7