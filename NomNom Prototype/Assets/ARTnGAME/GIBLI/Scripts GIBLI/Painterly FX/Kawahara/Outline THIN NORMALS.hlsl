TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);
float4 _CameraColorTexture_TexelSize;

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

TEXTURE2D(_CameraDepthNormalsTexture);
SAMPLER(sampler_CameraDepthNormalsTexture);
 
float3 DecodeNormal(float4 enc)
{
    float kScale = 1.7777;
    float3 nn = enc.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    float3 n;
    n.xy = g*nn.xy;
    n.z = g-1;
    return n;
}

void Outline_float(float2 UV, float OutlineThickness, float DepthSensitivity, float NormalsSensitivity, float ColorSensitivity, float4 OutlineColor, float4 OutlineControls, out float4 Out)
{
    float halfScaleFloor = floor(OutlineThickness * 0.5);
    float halfScaleCeil = ceil(OutlineThickness * 0.5);
    float2 Texel = (1.0) / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);

    float2 uvSamples[4];
    float depthSamples[4];
    float3 normalSamples[4], colorSamples[4];

  

	if (OutlineControls.x == 1) {

		uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
		uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
		uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
		uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

		for (int i = 0; i < 4; i++)
		{
			depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
			normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uvSamples[i]));
			colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
		}

		// Depth
		float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
		float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
		float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
		float depthThreshold = (1 / DepthSensitivity) * depthSamples[0];
		edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

		// Normals
		float3 normalFiniteDifference0 = (normalSamples[1] - normalSamples[0]);// / (1 - edgeDepth);
		float3 normalFiniteDifference1 = (normalSamples[3] - normalSamples[2]);// / (1 - edgeDepth);
		//float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(edgeDepth+0.7)) + dot(normalFiniteDifference1, normalFiniteDifference1*(edgeDepth + 0.7)));
		float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(1)) + dot(normalFiniteDifference1, normalFiniteDifference1*(1)));
		float edgeNormalA = edgeNormal > (1 / NormalsSensitivity) ? 1 : 0;

		float edgeNormalA1 = edgeNormal > (1 / edgeDepth) ? 1 : 0;

		// Color
		float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
		float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
		float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
		edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;

		float edge = max(edgeDepth, max(edgeNormalA, edgeColor));

		float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

		Out = ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
		Out = float4(edgeDepth, edgeDepth, edgeDepth, 1);// ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
		Out = float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / edgeDepth;//
		//Out = float4(edgeNormalA1, edgeNormalA1, edgeNormalA1, 1);//
		//Out = float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth;//
		//	Out = original* (1- OutlineControls.y) + (OutlineControls.y)*original * float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / (edgeDepth + 0.1*OutlineControls.z);//
	}
	else if (OutlineControls.x == 2) {

		uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
		uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
		uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
		uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

		for (int i = 0; i < 4; i++)
		{
			depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
			normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uvSamples[i]));
			colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
		}

		// Depth
		float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
		float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
		float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
		float depthThreshold = (1 / DepthSensitivity) * depthSamples[0];
		edgeDepth = edgeDepth > depthThreshold ? 1 : 0;


		//REGULATE BY DEPTH
		Texel = (depthThreshold+0.5*OutlineControls.y) / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);
		uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
		uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
		uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
		uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);
		for (int i = 0; i < 4; i++)
		{
			depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
			normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uvSamples[i]));
			colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
		}


		// Normals
		float3 normalFiniteDifference0 = (normalSamples[1] - normalSamples[0]);// / (1 - edgeDepth);
		float3 normalFiniteDifference1 = (normalSamples[3] - normalSamples[2]);// / (1 - edgeDepth);
		//float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(edgeDepth+0.7)) + dot(normalFiniteDifference1, normalFiniteDifference1*(edgeDepth + 0.7)));
		float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(1)) + dot(normalFiniteDifference1, normalFiniteDifference1*(1)));
		float edgeNormalA = edgeNormal > (1 / NormalsSensitivity) ? 1 : 0;

		float edgeNormalA1 = edgeNormal > (1 / edgeDepth) ? 1 : 0;

		// Color
		float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
		float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
		float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
		edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;

		float edge = max(edgeDepth, max(edgeNormalA, edgeColor));

		float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

		Out = ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
		Out = float4(edgeDepth, edgeDepth, edgeDepth, 1);// ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
		Out = float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / edgeDepth;//
		//Out = float4(edgeNormalA1, edgeNormalA1, edgeNormalA1, 1);//
		//Out = float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth;//
		//	Out = original* (1- OutlineControls.y) + (OutlineControls.y)*original * float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / (edgeDepth + 0.1*OutlineControls.z);//
	}
	else if(OutlineControls.x == 3) {

	uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
	uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
	uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
	uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

	for (int i = 0; i < 4; i++)
	{
		depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
		normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uvSamples[i]));
		colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
	}

	// Depth
	float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
	float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
	float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
	float depthThreshold = (1 / DepthSensitivity) * depthSamples[0];
	edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

	// Normals
	float3 normalFiniteDifference0 = (normalSamples[1] - normalSamples[0]);// / (1 - edgeDepth);
	float3 normalFiniteDifference1 = (normalSamples[3] - normalSamples[2]);// / (1 - edgeDepth);
	//float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(edgeDepth+0.7)) + dot(normalFiniteDifference1, normalFiniteDifference1*(edgeDepth + 0.7)));
	float edgeNormal = sqrt(
		dot(normalFiniteDifference0*(1 - OutlineControls.z), normalFiniteDifference0*(1 - OutlineControls.y)) 
		+ dot(normalFiniteDifference1, depthFiniteDifference1*(1-OutlineControls.y) + normalFiniteDifference1*(OutlineControls.y))
	);
	float edgeNormalA = edgeNormal > (1 / NormalsSensitivity) ? 1 : 0;

	//float edgeNormalA1 = edgeNormal > (1 / edgeDepth) ? 1 : 0;

	// Color
	float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
	float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
	float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
	edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;

	float edge = max(edgeDepth, max(edgeNormalA, edgeColor));

	float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

	Out = ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
	}
	else {

		uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
		uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
		uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
		uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

		for (int i = 0; i < 4; i++)
		{
			depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
			normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uvSamples[i]));
			colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
		}	

		// Depth
		float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
		float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
		float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
		float depthThreshold = (1 / DepthSensitivity) * depthSamples[0];
		edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

		// Normals
		float3 normalFiniteDifference0 = (normalSamples[1] - normalSamples[0]);// / (1 - edgeDepth);
		float3 normalFiniteDifference1 = (normalSamples[3] - normalSamples[2]);// / (1 - edgeDepth);
		//float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(edgeDepth+0.7)) + dot(normalFiniteDifference1, normalFiniteDifference1*(edgeDepth + 0.7)));
		float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0*(1)) + dot(normalFiniteDifference1, normalFiniteDifference1*(1)));
		float edgeNormalA = edgeNormal > (1 / NormalsSensitivity) ? 1 : 0;

		//float edgeNormalA1 = edgeNormal > (1 / edgeDepth) ? 1 : 0;

		// Color
		float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
		float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
		float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
		edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;

		float edge = max(edgeDepth, max(edgeNormalA, edgeColor));

		float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

		Out = ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
	}

}


