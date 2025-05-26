//StructuredBuffer<float4x4> _AdditionalLightsWorldToShadow_SSBO;
//float4x4    _AdditionalLightsWorldToShadow[MAX_PUNCTUAL_LIGHT_SHADOW_SLICES_IN_UBO];
//half4       _AdditionalShadowOffset0;
//half4       _AdditionalShadowOffset1;
//half4       _AdditionalShadowOffset2;
//half4       _AdditionalShadowOffset3;
//half4       _AdditionalShadowFadeParams; // x: additional light fade scale, y: additional light fade bias, z: 0.0, w: 0.0)
//float4      _AdditionalShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
float4x4    _AdditionalLightsWorldToShadow[16]; 
//ShadowSamplingData GetAdditionalLightShadowSamplingData()
//{
//	ShadowSamplingData shadowSamplingData;
//
//	// shadowOffsets are used in SampleShadowmapFiltered #if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
//	//shadowSamplingData.shadowOffset0 = _AdditionalShadowOffset0;
//	//shadowSamplingData.shadowOffset1 = _AdditionalShadowOffset1;
//	//shadowSamplingData.shadowOffset2 = _AdditionalShadowOffset2;
//	//shadowSamplingData.shadowOffset3 = _AdditionalShadowOffset3;
//
//	// shadowmapSize is used in SampleShadowmapFiltered for other platforms
//	shadowSamplingData.shadowmapSize = _AdditionalShadowmapSize;
//
//	return shadowSamplingData;
//}

ShadowSamplingData GetAdditionalLightShadowSamplingDataA(int index)
{
	ShadowSamplingData shadowSamplingData = (ShadowSamplingData)0;

#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
	// shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
	shadowSamplingData.shadowOffset0 = _AdditionalShadowOffset0;
	shadowSamplingData.shadowOffset1 = _AdditionalShadowOffset1;

	// shadowmapSize is used in SampleShadowmapFiltered otherwise.
	shadowSamplingData.shadowmapSize = _AdditionalShadowmapSize;
	shadowSamplingData.softShadowQuality = _AdditionalShadowParams[index].y;
#endif

	return shadowSamplingData;
}


//#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shadows.hlsl"
void Shadows_float(float3 WorldPos, float3 WorldNormal, float3  WorldView, out float3 AdditionalShadows, out float3 AdditionalLightsDiffuse, out float3 MainLightShadow)
{
	// Shader graph preview defaults;
	AdditionalShadows = 1.0;
	MainLightShadow = 1.0;
	AdditionalLightsDiffuse = 1.0;

	#ifndef SHADERGRAPH_PREVIEW
		//
		// spot light shadow
		//
		half4 shadowCoord = mul(_AdditionalLightsWorldToShadow[0], float4(WorldPos, 1.0));
		ShadowSamplingData a_shadowSamplingData = GetAdditionalLightShadowSamplingData(0);
		half4 shadowParams = GetAdditionalLightShadowParams(0);

		AdditionalShadows = SampleShadowmap(TEXTURE2D_ARGS(_AdditionalLightsShadowmapTexture, sampler_AdditionalLightsShadowmapTexture), shadowCoord, a_shadowSamplingData, shadowParams, true);

		float3 diffuseColor = 0;

		WorldNormal = normalize(WorldNormal);
		WorldView = SafeNormalize(WorldView);
		int pixelLightCount = GetAdditionalLightsCount();

		for (int i = 0; i < pixelLightCount; ++i)
		{
			Light light = GetAdditionalLight(i, WorldPos);
			float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
			diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
		}

		AdditionalLightsDiffuse = diffuseColor;
		
		//
		// main light
		//
		half4 m_shadowCoord = TransformWorldToShadowCoord(WorldPos);

		ShadowSamplingData m_shadowSamplingData = GetMainLightShadowSamplingData();
		half shadowStrength = GetMainLightShadowStrength();
		MainLightShadow = SampleShadowmap(m_shadowCoord, TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), m_shadowSamplingData, shadowStrength, false);
	#endif
}