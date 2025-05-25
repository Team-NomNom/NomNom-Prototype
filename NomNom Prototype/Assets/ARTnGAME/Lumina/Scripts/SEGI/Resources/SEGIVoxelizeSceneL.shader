// Upgrade NOTE: replaced 'defined USE_FORWARD_PLUS' with 'defined (USE_FORWARD_PLUS)'
// Upgrade NOTE: replaced 'defined _ADDITIONAL_LIGHTS' with 'defined (_ADDITIONAL_LIGHTS)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "SEGI/SEGIVoxelizeSceneL" {
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0,1)) = 0.333
		_BlockerValue("Blocker Value", Range(0, 10)) = 0
	}
		SubShader
		{
			Cull Off
			ZTest Always
			Tags {"LightMode" = "ForwardBase"}// "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			//v0.1
			Lighting On

			Pass
			{
				CGPROGRAM

					#pragma target 5.0
					#pragma vertex vert
					#pragma fragment frag
					#pragma geometry geom
					#include "UnityCG.cginc"

				#pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

					//v0.1
					#include "UnityLightingCommon.cginc"
					#include "UnityShaderVariables.cginc"
					#include "AutoLight.cginc"
					#include "UnityDeferredLibrary.cginc"

				//#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
				//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
				//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			//COKKIES
			#define MAX_VISIBLE_LIGHTS 16
			#define URP_LIGHT_TYPE_SPOT        0
#define URP_LIGHT_TYPE_DIRECTIONAL 1
#define URP_LIGHT_TYPE_POINT       2
			// Match  UnityEngine.TextureWrapMode
#define URP_TEXTURE_WRAP_MODE_REPEAT       0
#define URP_TEXTURE_WRAP_MODE_CLAMP        1
#define URP_TEXTURE_WRAP_MODE_MIRROR       2
#define URP_TEXTURE_WRAP_MODE_MIRROR_ONCE  3
			// Types
#define URP_LIGHT_COOKIE_FORMAT_NONE (-1)
#define URP_LIGHT_COOKIE_FORMAT_RGB (0)
#define URP_LIGHT_COOKIE_FORMAT_ALPHA (1)
#define URP_LIGHT_COOKIE_FORMAT_RED (2)
			// Textures
			#define TEXTURE2D(textureName)                  Texture2D textureName
			#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) tex2Dlod(textureName, float4(coord2, 0.0, lod))
		//	SAMPLER(sampler_LinearClamp);
		SamplerState sampler_LinearClamp;
TEXTURE2D(_MainLightCookieTexture);
//TEXTURE2D(_AdditionalLightsCookieAtlasTexture);
sampler2D _AdditionalLightsCookieAtlasTexture;
float _AdditionalLightsCookieAtlasTextureFormat;
			  float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS]; // (xy: uv size, zw: uv offset)
			 float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];
			 float _AdditionalLightsCookieEnableBits[(MAX_VISIBLE_LIGHTS + 31) / 32];
		float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS];
		float4 GetLightCookieAtlasUVRect(int lightIndex)
		{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
			return _AdditionalLightsCookieAtlasUVRectBuffer[lightIndex];
#else
			return _AdditionalLightsCookieAtlasUVRects[lightIndex];
#endif
		}
		float4x4 GetLightCookieWorldToLightMatrix(int lightIndex)
		{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
			return _AdditionalLightsWorldToLightBuffer[lightIndex];
#else
			return _AdditionalLightsWorldToLights[lightIndex];
#endif
		}
		int GetLightCookieLightType(int lightIndex)
		{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
			return _AdditionalLightsLightTypeBuffer[lightIndex];
#else
			return _AdditionalLightsLightTypes[lightIndex];
#endif
		}
			bool IsLightCookieEnabled(int lightBufferIndex)
{
#if 0
	float4 uvRect = GetLightCookieAtlasUVRect(lightBufferIndex);
	return any(uvRect != 0);
#else
			// 2^5 == 32, bit mask for a float/uint.
			uint elemIndex = ((uint)lightBufferIndex) >> 5;
			uint bitOffset = (uint)lightBufferIndex & ((1 << 5) - 1);

			#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
				uint elem = asuint(_AdditionalLightsCookieEnableBitsBuffer[elemIndex]);
			#else
				uint elem = asuint(_AdditionalLightsCookieEnableBits[elemIndex]);
			#endif

			return (elem & (1u << bitOffset)) != 0u;
		#endif
		}
			float2 PackNormalOctQuadEncode(float3 n)
			{
				//float l1norm    = dot(abs(n), 1.0);
				//float2 res0     = n.xy * (1.0 / l1norm);

				//float2 val      = 1.0 - abs(res0.yx);
				//return (n.zz < float2(0.0, 0.0) ? (res0 >= 0.0 ? val : -val) : res0);

				// Optimized version of above code:
				n *= rcp(max(dot(abs(n), 1.0), 1e-6));
				float t = saturate(-n.z);
				return n.xy + float2(n.x >= 0.0 ? t : -t, n.y >= 0.0 ? t : -t);
			}
			float2 ComputeLightCookieUVSpot(float4x4 worldToLightPerspective, float3 samplePositionWS, float4 atlasUVRect)
			{
				// Translate, rotate and project 'positionWS' into the light clip space.
				float4 positionCS = mul(worldToLightPerspective, float4(samplePositionWS, 1));
				float2 positionNDC = positionCS.xy / positionCS.w;

				// Remap NDC to the texture coordinates, from NDC [-1, 1]^2 to [0, 1]^2.
				float2 positionUV = saturate(positionNDC * 0.5 + 0.5);

				// Remap into rect in the atlas texture
				float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV)+atlasUVRect.zw;

				return positionAtlasUV;
			}
			float2 ComputeLightCookieUVPoint(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect)
			{
				// Translate and rotate 'positionWS' into the light space.
				float4 positionLS = mul(worldToLight, float4(samplePositionWS, 1));

				float3 sampleDirLS = normalize(positionLS.xyz / positionLS.w);

				// Project direction to Octahederal quad UV.
				float2 positionUV = saturate(PackNormalOctQuadEncode(sampleDirLS) * 0.5 + 0.5);

				// Remap to atlas texture
				float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV)+atlasUVRect.zw;

				return positionAtlasUV;
			}
			float2 ComputeLightCookieUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect, uint2 uvWrap)
			{
				// Translate and rotate 'positionWS' into the light space.
				// Project point to light "view" plane, i.e. discard Z.
				float2 positionLS = mul(worldToLight, float4(samplePositionWS, 1)).xy;

				// Remap [-1, 1] to [0, 1]
				// (implies the transform has ortho projection mapping world space box to [-1, 1])
				float2 positionUV = positionLS * 0.5 + 0.5;

				// Tile texture for cookie in repeat mode
				positionUV.x = (uvWrap.x == URP_TEXTURE_WRAP_MODE_REPEAT) ? frac(positionUV.x) : positionUV.x;
				positionUV.y = (uvWrap.y == URP_TEXTURE_WRAP_MODE_REPEAT) ? frac(positionUV.y) : positionUV.y;
				positionUV.x = (uvWrap.x == URP_TEXTURE_WRAP_MODE_CLAMP) ? saturate(positionUV.x) : positionUV.x;
				positionUV.y = (uvWrap.y == URP_TEXTURE_WRAP_MODE_CLAMP) ? saturate(positionUV.y) : positionUV.y;

				// Remap to atlas texture
				float2 positionAtlasUV = atlasUVRect.xy * float2(positionUV)+atlasUVRect.zw;

				return positionAtlasUV;
			}
			float4 SampleAdditionalLightsCookieAtlasTexture(float2 uv)
			{
				// No mipmap support
				//return SAMPLE_TEXTURE2D_LOD(_AdditionalLightsCookieAtlasTexture, sampler_LinearClamp, uv, 0);
				return tex2Dlod(_AdditionalLightsCookieAtlasTexture, float4(uv,0,0));
			}
			bool IsAdditionalLightsCookieAtlasTextureRGBFormat()
			{
				return _AdditionalLightsCookieAtlasTextureFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
			}
			bool IsAdditionalLightsCookieAtlasTextureAlphaFormat()
			{
				return _AdditionalLightsCookieAtlasTextureFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
			}


			float3 SampleAdditionalLightCookie(int perObjectLightIndex, float3 samplePositionWS)
{
	if (!IsLightCookieEnabled(perObjectLightIndex))
		return float3(1,1,1);

	int lightType = GetLightCookieLightType(perObjectLightIndex);
	int isSpot = lightType == URP_LIGHT_TYPE_SPOT;
	int isDirectional = lightType == URP_LIGHT_TYPE_DIRECTIONAL;

	float4x4 worldToLight = GetLightCookieWorldToLightMatrix(perObjectLightIndex);
	float4 uvRect = GetLightCookieAtlasUVRect(perObjectLightIndex);

	float2 uv;
	if (isSpot)
	{
		uv = ComputeLightCookieUVSpot(worldToLight, samplePositionWS, uvRect);
	}
	else if (isDirectional)
	{
		uv = ComputeLightCookieUVDirectional(worldToLight, samplePositionWS, uvRect, URP_TEXTURE_WRAP_MODE_REPEAT);
	}
	else
	{
		uv = ComputeLightCookieUVPoint(worldToLight, samplePositionWS, uvRect);
	}

	float4 color = SampleAdditionalLightsCookieAtlasTexture(uv);

	return IsAdditionalLightsCookieAtlasTextureRGBFormat() ? color.rgb
			: IsAdditionalLightsCookieAtlasTextureAlphaFormat() ? color.aaa
			: color.rrr;
}

			//SHADOWS
			half MixRealtimeAndBakedShadows(half realtimeShadow, half bakedShadow, half shadowFade)
{
#if defined(LIGHTMAP_SHADOW_MIXING)
	return min(lerp(realtimeShadow, 1, shadowFade), bakedShadow);
#else
	return lerp(realtimeShadow, bakedShadow, shadowFade);
#endif
}
			half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS, half3 lightDirection)
{
	#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
		ShadowSamplingData shadowSamplingData = GetAdditionalLightShadowSamplingData(lightIndex);

		half4 shadowParams = GetAdditionalLightShadowParams(lightIndex);

		int shadowSliceIndex = shadowParams.w;
		if (shadowSliceIndex < 0)
			return 1.0;

		half isPointLight = shadowParams.z;

		UNITY_BRANCH
		if (isPointLight)
		{
			// This is a point light, we have to find out which shadow slice to sample from
			float cubemapFaceId = CubeMapFaceID(-lightDirection);
			shadowSliceIndex += cubemapFaceId;
		}

		#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
			float4 shadowCoord = mul(_AdditionalLightsWorldToShadow_SSBO[shadowSliceIndex], float4(positionWS, 1.0));
		#else
			float4 shadowCoord = mul(_AdditionalLightsWorldToShadow[shadowSliceIndex], float4(positionWS, 1.0));
		#endif

		return SampleShadowmap(TEXTURE2D_ARGS(_AdditionalLightsShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, true);
	#else
		return half(1.0);
	#endif
}
			half AdditionalLightShadow(int lightIndex, float3 positionWS, half3 lightDirection, half4 shadowMask, half4 occlusionProbeChannels)
{
	half realtimeShadow = AdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection);

#ifdef CALCULATE_BAKED_SHADOWS
	half bakedShadow = BakedShadow(shadowMask, occlusionProbeChannels);
#else
	half bakedShadow = half(1.0);
#endif

#ifdef ADDITIONAL_LIGHT_CALCULATE_SHADOWS
	half shadowFade = GetAdditionalLightShadowFade(positionWS);
#else
	half shadowFade = half(1.0);
#endif

	return MixRealtimeAndBakedShadows(realtimeShadow, bakedShadow, shadowFade);
}
			
	
			#define HALF_MIN 6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Sm…
#define HALF_MIN_SQRT 0.0078125  // 2^-7 == sqrt(HALF_MIN), useful for ensuring HALF_MIN after x^2
			half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsOcclusionProbes[MAX_VISIBLE_LIGHTS];
float _AdditionalLightsLayerMasks[MAX_VISIBLE_LIGHTS];
			float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
			struct Light
{
	half3   direction;
	half3   color;
	float   distanceAttenuation; // full-float precision required on some platforms
	half    shadowAttenuation;
	uint    layerMask;
};

			// Matches Unity Vanilla HINT_NICE_QUALITY attenuation
// Attenuation smoothly decreases to light range.
			float DistanceAttenuation(float distanceSqr, half2 distanceAttenuation)
			{
				// We use a shared distance attenuation for additional directional and puctual lights
				// for directional lights attenuation will be 1
				float lightAtten = rcp(distanceSqr);
				float2 distanceAttenuationFloat = float2(distanceAttenuation);

				// Use the smoothing factor also used in the Unity lightmapper.
				half factor = half(distanceSqr * distanceAttenuationFloat.x);
				half smoothFactor = saturate(half(1.0) - factor * factor);
				smoothFactor = smoothFactor * smoothFactor;

				return lightAtten * smoothFactor;
			}

			half AngleAttenuation(half3 spotDirection, half3 lightDirection, half2 spotAttenuation)
			{
				// Spot Attenuation with a linear falloff can be defined as
				// (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
				// This can be rewritten as
				// invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
				// SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
				// SdotL * spotAttenuation.x + spotAttenuation.y

				// If we precompute the terms in a MAD instruction
				half SdotL = dot(spotDirection, lightDirection);
				half atten = saturate(SdotL * spotAttenuation.x + spotAttenuation.y);
				return atten * atten;
			}

		Light GetAdditionalPerObjectLight(int perObjectLightIndex, float3 positionWS)
		{
			// Abstraction over Light input constants
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
			float4 lightPositionWS = _AdditionalLightsBuffer[perObjectLightIndex].position;
			half3 color = _AdditionalLightsBuffer[perObjectLightIndex].color.rgb;
			half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[perObjectLightIndex].attenuation;
			half4 spotDirection = _AdditionalLightsBuffer[perObjectLightIndex].spotDirection;
			uint lightLayerMask = _AdditionalLightsBuffer[perObjectLightIndex].layerMask;
#else
			float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
			half3 color = _AdditionalLightsColor[perObjectLightIndex].rgb;
			half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
			half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
			uint lightLayerMask = asuint(_AdditionalLightsLayerMasks[perObjectLightIndex]);
#endif

			// Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
			// This way the following code will work for both directional and punctual lights.
			float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
			float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

			half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
			// full-float precision required on some platforms
			float attenuation = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);

			Light light;
			light.direction = lightDirection;
			light.distanceAttenuation = attenuation;
			light.shadowAttenuation = 1.0; // This value can later be overridden in GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
			light.color = color;
			light.layerMask = lightLayerMask;

			return light;
		}

					//v1.6
					float3 _CutoffGI;
					//sampler2D _CameraDepthTexture;

					RWTexture3D<uint> RG0;

					int LayerToVisualize;

					float4x4 SEGIVoxelViewFront;
					float4x4 SEGIVoxelViewLeft;
					float4x4 SEGIVoxelViewTop;

					sampler2D _MainTex;
					sampler2D _EmissionMap;
					float _Cutoff;
					float4 _MainTex_ST;
					half4 _EmissionColor;

					float SEGISecondaryBounceGain;

					float _BlockerValue;

					//v0.2
					float shadowedLocalPower;
					float shadowlessLocalPower;
					float shadowlessLocalOcclusion;

					struct v2g
					{
						float4 pos : SV_POSITION;
						half4 uv : TEXCOORD0;
						float3 normal : TEXCOORD1;
						float angle : TEXCOORD2;
						half4 worldPos : TEXCOORD3;	//v0.1
						half4 attenUV : TEXCOORD4;	//v0.1

						//METHOD2
						LIGHTING_COORDS(5, 6)
						float3  vertexLighting : TEXCOORD7;
					};

					//v0.1
					float attenUV(float lightAtten0, float3 _4LightPos, float3 _worldPos) : SV_Target{
						float range = (0.005 * sqrt(1000000 - lightAtten0)) / sqrt(lightAtten0);
						return distance(_4LightPos, _worldPos) / range;
					}
					float atten(float _attenUV) : SV_Target{
						return saturate(1.0 / (1.0 + 25.0*_attenUV*_attenUV) * saturate((1 - _attenUV) * 5.0));
					}
					float attenTex(sampler2D _LightTextureB, float _attenUV) : SV_Target{
						return tex2D(_LightTextureB, (_attenUV * _attenUV).xx).UNITY_ATTEN_CHANNEL;
					}



					struct g2f
					{
						float4 pos : SV_POSITION;
						half4 uv : TEXCOORD0;
						float3 normal : TEXCOORD1;
						float angle : TEXCOORD2;
						half4 worldPos : TEXCOORD3;	//v0.1
						half4 attenUV : TEXCOORD4;	//v0.1

						//METHOD2
						LIGHTING_COORDS(5, 6)
						float3  vertexLighting : TEXCOORD7;
					};

					half4 _Color;
					int _visibleLightsCount;

					v2g vert(appdata_full v)
					{
						v2g o;
						UNITY_INITIALIZE_OUTPUT(v2g, o);

						float4 vertex = v.vertex;

						o.normal = UnityObjectToWorldNormal(v.normal);
						float3 absNormal = abs(o.normal);

						o.pos = vertex;

						o.uv = float4(TRANSFORM_TEX(v.texcoord.xy, _MainTex), 1.0, 1.0);

						//v0.1
						float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
						o.worldPos = worldPos;// mul(unity_ObjectToWorld, v.vertex);
						o.attenUV.x = attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), o.worldPos.xyz);
						o.attenUV.y = attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), o.worldPos.xyz);
						o.attenUV.z = attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), o.worldPos.xyz);
						o.attenUV.w = attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), o.worldPos.xyz);

						//METHOD2
						//v0.1a
						//if (1 == 1)//(spotLight)
						//{
						/*
						for (int index = 0; index < 4; index++)
						{
							float4 lightPosition = float4(unity_4LightPosX0[index],
								unity_4LightPosY0[index],
								unity_4LightPosZ0[index], 1.0);
							float3 vertexToLightSource = float3(lightPosition.xyz - worldPos);
							float rho = max(0, dot(vertexToLightSource, unity_SpotDirection[index].xyz));
							float spotAtt = (rho - unity_LightAtten[index].x) * unity_LightAtten[index].y;
							if (index == 0) {
								o.attenUV.x = saturate(spotAtt);
							}
							if (index == 1) {
								o.attenUV.y = saturate(spotAtt);
							}
							if (index == 2) {
								o.attenUV.z = saturate(spotAtt);
							}
							if (index == 3) {
								o.attenUV.w = saturate(spotAtt);
							}


						}
						//}
						*/

						o.vertexLighting = float3(0.0, 0.0, 0.0);
						float3 worldN = mul((float3x3)unity_ObjectToWorld, SCALED_NORMAL);
						
						//#ifdef VERTEXLIGHT_ON						
						/*
						for (int index = 0; index < 4; index++)
						{
							float4 lightPosition = float4(unity_4LightPosX0[index],
								unity_4LightPosY0[index],
								unity_4LightPosZ0[index], 1.0);

							//float3 toLight = unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;

							float3 vertexToLightSource = float3(lightPosition.xyz - worldPos);

							float3 lightDirection = normalize(vertexToLightSource);

							float squaredDistance = dot(vertexToLightSource, vertexToLightSource);

							float attenuation = 1.0 / (1.0 + unity_4LightAtten0[index] * squaredDistance);

							//if (1==1)//(spotLight)
							//{
							//	float rho = max(0, dot(vertexToLightSource, float3(1,0,1)));
							//	float spotAtt = rho*11111;
							//	attenuation *= saturate(spotAtt);
							//}

							float3 diffuseReflection = attenuation * float3(unity_LightColor[index].rgb)
								* float3(_Color.rgb)* max(0.0, dot(worldN, lightDirection));

							o.vertexLighting = o.vertexLighting + diffuseReflection * 2;
						}
						//#endif
						*/

						//METHOD3
						//#ifdef VERTEXLIGHT_ON
							/*o.vertexLighting += Shade4PointLights(
								unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
								unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
								unity_4LightAtten0, worldPos, worldN
							);*/
						//#endif // VERTEXLIGHT_ON

							//o.vertexLighting =1* ShadeVertexLightsFull(
							//	v.vertex, v.normal, 8, true//absNormal//realNormal
							//);

						TRANSFER_VERTEX_TO_FRAGMENT(o);

						return o;
					}

					int SEGIVoxelResolution;

					[maxvertexcount(3)]
					void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
					{
						v2g p[3];
						int i = 0;
						for (i = 0; i < 3; i++)
						{
							p[i] = input[i];
							p[i].pos = mul(unity_ObjectToWorld, p[i].pos);


							//v0.1
						/*	p[i].worldPos = p[i].pos;
							p[i].attenUV.x = attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), p[i].worldPos.xyz);
							p[i].attenUV.y = attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), p[i].worldPos.xyz);
							p[i].attenUV.z = attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), p[i].worldPos.xyz);
							p[i].attenUV.w = attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), p[i].worldPos.xyz);*/
						}


						float3 realNormal = float3(0.0, 0.0, 0.0);

						float3 V = p[1].pos.xyz - p[0].pos.xyz;
						float3 W = p[2].pos.xyz - p[0].pos.xyz;

						realNormal.x = (V.y * W.z) - (V.z * W.y);
						realNormal.y = (V.z * W.x) - (V.x * W.z);
						realNormal.z = (V.x * W.y) - (V.y * W.x);

						float3 absNormal = abs(realNormal);


						//v0.1a
						//	p[i].worldPos = p[i].pos;
						////v0.1
						//p[i].worldPos = p[i].pos;
						for (i = 0; i < 3; i++)
						{
							//p[i] = input[i];
							if (shadowlessLocalOcclusion != 0) {
								p[i].attenUV.x = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), p[i].pos.xyz);
								p[i].attenUV.y = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), p[i].pos.xyz);
								p[i].attenUV.z = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), p[i].pos.xyz);
								p[i].attenUV.w = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), p[i].pos.xyz);
								//p[i].attenUV = input[i].attenUV;
								
							
							
							}

							//METHOD3
							if (shadowedLocalPower != 0) {
								//p[i].vertexLighting = Shade4PointLights(
								//	unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
								//	unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
								//	unity_4LightAtten0, p[i].pos, realNormal//absNormal//realNormal
								//);


								//int _visibleLightsCount = 16;
#if defined (_ADDITIONAL_LIGHTS) || defined (USE_FORWARD_PLUS)
								[loop] //v1.9.9.8 - Ethereal v1.1.8h
								for (int k = 0; k < _visibleLightsCount-1; k++) //v1.9.9.8 - Ethereal v1.1.8h
								{
									//LIGHT 1
									float distToRayStartA = length(_WorldSpaceCameraPos - p[i].pos);//v1.9.9.8 - Ethereal v1.1.8h

		//#ifdef URP11//v1.8
									Light light = GetAdditionalPerObjectLight(k, p[i].pos);
									light.shadowAttenuation = AdditionalLightShadow(k, p[i].pos, light.direction, half4(1, 1, 1, 1), half4(0, 0, 0, 0));

									//COOKIE
									//#if defined(_LIGHT_COOKIES)
									float3 cookieColor = SampleAdditionalLightCookie(k, p[i].pos);
									light.color *= cookieColor;
									//#endif
		//#else
		//							Light light = GetAdditionalPerObjectLight(k, pos);// GetAdditionalLight(k, pos); //v0.4 URP 10 need an extra shadowmask variable
		//#endif
									p[i].vertexLighting += light.color  *pow(light.shadowAttenuation, 1)* pow(light.distanceAttenuation, 1);
									//result.rgb *= light.shadowAttenuation;
								}
#endif

								//
								// ShadeVertexLightsFull (float4 vertex, float3 normal, int lightCount, bool spotLight)
								//p[i].vertexLighting += input[i].vertexLighting;
								//	101*ShadeVertexLightsFull(
								//	p[i].pos.xyzz, realNormal, 8, true//absNormal//realNormal
								//);
							}
						}


						int angle = 0;
						if (absNormal.z > absNormal.y && absNormal.z > absNormal.x)
						{
							angle = 0;
						}
						else if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
						{
							angle = 1;
						}
						else if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
						{
							angle = 2;
						}
						else
						{
							angle = 0;
						}

						for (i = 0; i < 3; i++)
						{
							//p[i].worldPos = p[i].pos;

							///*
							if (angle == 0)
							{
								p[i].pos = mul(SEGIVoxelViewFront, p[i].pos);
							}
							else if (angle == 1)
							{
								p[i].pos = mul(SEGIVoxelViewLeft, p[i].pos);
							}
							else
							{
								p[i].pos = mul(SEGIVoxelViewTop, p[i].pos);
							}

							//METHOD3
							/*p[i].vertexLighting = 1*Shade4PointLights(
								unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
								unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
								unity_4LightAtten0, p[i].pos, absNormal
							);*/

							p[i].pos = mul(UNITY_MATRIX_P, p[i].pos);

							#if defined(UNITY_REVERSED_Z)
							p[i].pos.z = 1.0 - p[i].pos.z;
							#else 
							p[i].pos.z *= -1.0;
							#endif

							p[i].angle = (float)angle;

							/*
							//v0.1
							//	p[i].worldPos = p[i].pos;
							////v0.1
							//p[i].worldPos = p[i].pos;
							if (shadowlessLocalOcclusion != 0) {
								p[i].attenUV.x = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), p[i].pos.xyz);
								p[i].attenUV.y = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), p[i].pos.xyz);
								p[i].attenUV.z = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), p[i].pos.xyz);
								p[i].attenUV.w = shadowlessLocalOcclusion * attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), p[i].pos.xyz);
							}

							//METHOD3
							if (shadowedLocalPower != 0) {
								p[i].vertexLighting += Shade4PointLights(
									unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
									unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
									unity_4LightAtten0, p[i].pos, realNormal//absNormal//realNormal
								);
							}
							*/
							//METHOD3
							
							
						}

						triStream.Append(p[0]);
						triStream.Append(p[1]);
						triStream.Append(p[2]);
					}

					float3 rgb2hsv(float3 c)
					{
						float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
						float4 p = lerp(float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g));
						float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

						float d = q.x - min(q.w, q.y);
						float e = 1.0e-10;

						return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
					}

					float3 hsv2rgb(float3 c)
					{
						float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
						float3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
						return c.z * lerp(k.xxx, saturate(p - k.xxx), c.y);
					}

					float4 DecodeRGBAuint(uint value)
					{
						uint ai = value & 0x0000007F;
						uint vi = (value / 0x00000080) & 0x000007FF;
						uint si = (value / 0x00040000) & 0x0000007F;
						uint hi = value / 0x02000000;

						float h = float(hi) / 127.0;
						float s = float(si) / 127.0;
						float v = (float(vi) / 2047.0) * 10.0;
						float a = ai * 2.0;

						v = pow(v, 3.0);

						float3 color = hsv2rgb(float3(h, s, v));

						return float4(color.rgb, a);
					}

					uint EncodeRGBAuint(float4 color)
					{
						//7[HHHHHHH] 7[SSSSSSS] 11[VVVVVVVVVVV] 7[AAAAAAAA]
						float3 hsv = rgb2hsv(color.rgb);
						hsv.z = pow(hsv.z, 1.0 / 3.0);

						uint result = 0;

						uint a = min(127, uint(color.a / 2.0));
						uint v = min(2047, uint((hsv.z / 10.0) * 2047));
						uint s = uint(hsv.y * 127);
						uint h = uint(hsv.x * 127);

						result += a;
						result += v * 0x00000080; // << 7
						result += s * 0x00040000; // << 18
						result += h * 0x02000000; // << 25

						return result;
					}

					void interlockedAddFloat4(RWTexture3D<uint> destination, int3 coord, float4 value)
					{
						uint writeValue = EncodeRGBAuint(value);
						uint compareValue = 0;
						uint originalValue;

						[allow_uav_condition]
						for (int i = 0; i < 18; i++)// while (true)
						{
							InterlockedCompareExchange(destination[coord], compareValue, writeValue, originalValue);
							if (compareValue == originalValue)
								break;
							compareValue = originalValue;
							float4 originalValueFloats = DecodeRGBAuint(originalValue);
							writeValue = EncodeRGBAuint(originalValueFloats + value);
						}
					}

					void interlockedAddFloat4b(RWTexture3D<uint> destination, int3 coord, float4 value)
					{
						uint writeValue = EncodeRGBAuint(value);
						uint compareValue = 0;
						uint originalValue;

						[allow_uav_condition]
						for (int i = 0; i < 18; i++)// while (true)
						{
							InterlockedCompareExchange(destination[coord], compareValue, writeValue, originalValue);
							if (compareValue == originalValue)
								break;
							compareValue = originalValue;
							float4 originalValueFloats = DecodeRGBAuint(originalValue);
							writeValue = EncodeRGBAuint(originalValueFloats + value);
						}
					}

					float4x4 SEGIVoxelToGIProjection;
					float4x4 SEGIVoxelProjectionInverse;
					sampler2D SEGISunDepth;
					float4 SEGISunlightVector;
					float4 GISunColor;
					float4 SEGIVoxelSpaceOriginDelta;

					sampler3D SEGIVolumeTexture1;

					int SEGIInnerOcclusionLayers;

					#define VoxelResolution (SEGIVoxelResolution * (1 + SEGIVoxelAA))

					int SEGIVoxelAA;

					float4 frag(g2f input) : SV_TARGET
					{
						int3 coord = int3((int)(input.pos.x), (int)(input.pos.y), (int)(input.pos.z * VoxelResolution));

						float3 absNormal = abs(input.normal);

						int angle = 0;

						angle = (int)input.angle;

						if (angle == 1)
						{
							coord.xyz = coord.zyx;
							coord.z = VoxelResolution - coord.z - 1;
						}
						else if (angle == 2)
						{
							coord.xyz = coord.xzy;
							coord.y = VoxelResolution - coord.y - 1;
						}

						float3 fcoord = (float3)(coord.xyz) / VoxelResolution;

						float4 shadowPos = mul(SEGIVoxelProjectionInverse, float4(fcoord * 2.0 - 1.0, 0.0));
						shadowPos = mul(SEGIVoxelToGIProjection, shadowPos);
						shadowPos.xyz = shadowPos.xyz * 0.5 + 0.5;

						float sunDepth = tex2Dlod(SEGISunDepth, float4(shadowPos.xy, 0, 0)).x;
						#if defined(UNITY_REVERSED_Z)
						sunDepth = 1.0 - sunDepth;
						#endif

						float sunVisibility = saturate((sunDepth - shadowPos.z + 0.2525) * 1000.0);


						float sunNdotL = saturate(dot(input.normal, -SEGISunlightVector.xyz));

						float4 tex = tex2D(_MainTex, input.uv.xy);
						float4 emissionTex = tex2D(_EmissionMap, input.uv.xy);

						float4 color = _Color;

						if (length(_Color.rgb) < 0.0001)
						{
							color.rgb = float3(1, 1, 1);
						}

						//v0.7
						float3 col = sunVisibility.xxx * sunNdotL * color.rgb * tex.rgb * GISunColor.rgb * GISunColor.a
							+ _EmissionColor.rgb * (0.1 + (1 - color.a) * 5) * emissionTex.rgb; //1.0g
						//float3 col = sunVisibility.xxx * sunNdotL * color.rgb * tex.rgb * GISunColor.rgb * GISunColor.a + _EmissionColor.rgb * 0.9 * emissionTex.rgb;

						float4 prevBounce = tex3D(SEGIVolumeTexture1, fcoord + SEGIVoxelSpaceOriginDelta.xyz);
						col.rgb += prevBounce.rgb * 1.6 * SEGISecondaryBounceGain * tex.rgb * color.rgb;

						float4 result = float4(col.rgb, 2.0);


						const float sqrt2 = sqrt(2.0) * 1.0;

						coord /= (uint)SEGIVoxelAA + 1u;

						//v0.1 - 1.6
						float depthA = 1 - Linear01Depth(tex2D(_CameraDepthTexture, input.uv.xy).x);
						if (depthA + 0.02*_CutoffGI.x > length(_WorldSpaceCameraPos - coord.xyz) / (VoxelResolution*_CutoffGI.y)) {
							result.a += 90.0 * _CutoffGI.z;
						}

						if (_BlockerValue > 0.01)
						{
							result.a += 20.0;
							result.a += _BlockerValue;
							result.rgb = float3(0.0, 0.0, 0.0);
						}

						//v0.1
						float4 _atten = 0;
						//float _atten.x = attenTex(_LightTextureB0, f.attenUV.x);
						_atten.x = atten(input.attenUV.x);
						_atten.y = atten(input.attenUV.y);
						_atten.z = atten(input.attenUV.z);
						_atten.w = atten(input.attenUV.w);
						//fixed4 col = tex2D(_MainTex, input.uv.xy) * input.color;
						if (shadowlessLocalPower != 0) {
							result.rgb += color.rgb * tex.rgb * unity_LightColor[0].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), input.worldPos.xyz)) * _atten.x;
							result.rgb += color.rgb * tex.rgb * unity_LightColor[1].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), input.worldPos.xyz)) * _atten.y;
							result.rgb += color.rgb * tex.rgb * unity_LightColor[2].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), input.worldPos.xyz)) * _atten.z;
							result.rgb += color.rgb * tex.rgb * unity_LightColor[3].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), input.worldPos.xyz)) * _atten.w;
						}
						//result.rgb += unity_LightColor[0].rgb * _atten.x;
						//result.rgb += unity_LightColor[1].rgb * _atten.y;
						//result.rgb += unity_LightColor[2].rgb * _atten.z;
						//result.rgb += unity_LightColor[3].rgb * _atten.w;

						//METHOD3
						if (shadowedLocalPower != 0) {
							//float atten = LIGHT_ATTENUATION(input);
							//result.rgb += color.rgb * float3(input.vertexLighting.rgb*0.02 * shadowedLocalPower) * tex.rgb* atten;
							result.rgb += color.rgb * float3(input.vertexLighting.rgb * 0.01 * shadowedLocalPower) * tex.rgb * 1;
						}
						float atten = LIGHT_ATTENUATION(input);
						//result.rgb += ShadeVertexLightsFull(fcoord.xyzz, input.normal, 4, true) * shadowedLocalPower * tex.rgb * atten;
						

					


						//Light mainLight = GetMainLight(input.shadowCoord);


						interlockedAddFloat4(RG0, coord, result);

						if (SEGIInnerOcclusionLayers > 0)
						{
							interlockedAddFloat4b(RG0, coord - int3((int)(input.normal.x * sqrt2 * 1.0), (int)(input.normal.y * sqrt2 * 1.0), (int)(input.normal.z * sqrt2 * 1.0)), float4(0.0, 0.0, 0.0, 8.0));
						}

						if (SEGIInnerOcclusionLayers > 1)
						{
							interlockedAddFloat4b(RG0, coord - int3((int)(input.normal.x * sqrt2 * 2.0), (int)(input.normal.y * sqrt2 * 2.0), (int)(input.normal.z * sqrt2 * 2.0)), float4(0.0, 0.0, 0.0, 22.0));
						}

						return float4(0.0, 0.0, 0.0, 0.0);
					}

				ENDCG
			}
		}
		FallBack Off//FallBack "Diffuse"//FallBack Off
}
