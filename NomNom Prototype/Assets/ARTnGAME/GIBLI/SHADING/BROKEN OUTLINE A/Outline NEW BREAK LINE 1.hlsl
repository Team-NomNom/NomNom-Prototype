TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

void Outline_float(float4 UV, out float Out)
{    
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, UV.xy).r;	
	//Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV), _ZBufferParams);

//#if defined(REQUIRE_DEPTH_TEXTURE)
//	float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
//	return Linear01Depth(rawDepth, _ZBufferParams);
//#endif
	float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UV);
	Out = LinearEyeDepth(rawDepth, _ZBufferParams);
	Out = depth;
   // Out = LinearEyeDepth(depth, _ZBufferParams);
}