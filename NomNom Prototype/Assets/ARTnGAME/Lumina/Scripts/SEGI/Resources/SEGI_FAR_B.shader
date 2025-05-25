Shader "Hidden/SEGI_FAR_B" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
//
CGINCLUDE
#include "UnityCG.cginc"
#include "SEGI.cginc"
#pragma target 5.0

sampler2D PreviousGITexture_FAR_B;
float SEGIVoxelScaleFactor_FAR_B;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;	
		
		#if UNITY_UV_STARTS_AT_TOP
		half4 uv2 : TEXCOORD1;
		#endif
	};
	
	v2f vert(appdata_img v)
	{
		v2f o;
		
		o.pos = UnityObjectToClipPos (v.vertex);
		o.uv = float4(v.texcoord.xy, 1, 1);		
		
		#if UNITY_UV_STARTS_AT_TOP
			o.uv2 = float4(v.texcoord.xy, 1, 1);				
			if (_MainTex_TexelSize.y < 0.0)
				o.uv.y = 1.0 - o.uv.y;
		#endif
	        	
		return o; 
	}

	#define PI 3.147159265


ENDCG


SubShader
{
	ZTest Off
	Cull Off
	ZWrite Off
	Fog { Mode off }
		
	Pass //0
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4x4 CameraToWorld;
			
			sampler2D _CameraGBufferTexture2;
			
			//v0.4
			sampler2D _GBuffer0;
			sampler2D _GBuffer1;
			sampler2D _GBuffer2;
			sampler2D _GBuffer3;
			
			int FrameSwitch;
			int HalfResolution;
			
			
			sampler3D SEGIVolumeTexture1;

			sampler2D NoiseTexture;
			
			//v2.1.34 - COLOR - DITHER - v0.7
			float4 ditherControl;// = float4(0, 1, 1, 1); //enable Dirther - Trace Directions Divider - Trece Steps Devider - Reflections Steps Divider
			
			//v1.2
			float smoothNormals;

			float4 frag(v2f input) : SV_Target
			{
				#if UNITY_UV_STARTS_AT_TOP
					float2 coord = input.uv2.xy;
				#else
					float2 coord = input.uv.xy;
				#endif

				//Get view space position and view vector
				float4 viewSpacePosition = GetViewSpacePosition(coord);
				float3 viewVector = normalize(viewSpacePosition.xyz);

				//Get voxel space position
				float4 voxelSpacePosition = mul(CameraToWorld, viewSpacePosition);
				voxelSpacePosition = mul(SEGIWorldToVoxel, voxelSpacePosition);
				voxelSpacePosition = mul(SEGIVoxelProjection, voxelSpacePosition);
				voxelSpacePosition.xyz = voxelSpacePosition.xyz * 0.5 + 0.5;
				

				//Prepare for cone trace
				float2 dither = rand(coord + (float)FrameSwitch * 0.011734);
				
				//v0.4
				//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
				float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb* 2.0 - 1.0	), 1);
				float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb), 1);
				//float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1); //v1.2
				//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
				float depthA = 1;
				if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
					depthA = 0;
				}

				//v1.2
				//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;// pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1) * worldNormalA;
				float3 worldNormal = tex2D(_GBuffer2, coord).rgb*(depthA) + worldNormalA * (1 - depthA); //v1.2b
				//return float4(depth, depth, depth, depth);

				//v1.2
				if(smoothNormals != 1){//if (ditherControl.x != 0) {
					float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1);
					worldNormal = worldNormal * smoothNormals 
					+ (1- smoothNormals)*( normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA);
				}

				//v2.1.34 - COLOR - v0.7
				//float3 fakeGI = saturate(dot(worldNormal, float3(0, 1, 0)) * 0.5 + 0.5) * SEGISkyColor.rgb * 5.0;
				//float4 ditherControl = float4(1, 1, 1, 1);
				//if (mod(voxelSpacePosition.z, 0.01) < 0.002 && mod(voxelSpacePosition.z, 0.01) > 0.001) {
				if (ditherControl.x != 0 && dither.x > 0.001 * ditherControl.x) {
					//if (mod(input.uv.x, 0.1) == 0.004) {
					//discard;
					//float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1);
					//return float4(fakeGI *1, 1.0);
					TraceDirections = TraceDirections / ditherControl.y;// *saturate(dither.x);
					TraceSteps = TraceSteps / ditherControl.z;
				}


				float3 voxelOrigin = voxelSpacePosition.xyz + worldNormal.xyz * 0.003 * ConeTraceBias * 1.25 / SEGIVoxelScaleFactor_FAR_B;

				float3 gi = float3(0.0, 0.0, 0.0);
				float4 traceResult = float4(0,0,0,0);

				const float phi = 1.618033988;
				const float gAngle = phi * PI * 1.0;

				//Get blue noise
				float2 noiseCoord = (input.uv.xy * _MainTex_TexelSize.zw) / (64.0).xx;
				float blueNoise = tex2Dlod(NoiseTexture, float4(noiseCoord, 0.0, 0.0)).x;
				
				//Trace GI cones
				int numSamples = TraceDirections;
				for (int i = 0; i < numSamples; i++)
				{
					float fi = (float)i + blueNoise * StochasticSampling;
					float fiN = fi / numSamples;
					float longitude = gAngle * fi;
					float latitude = asin(fiN * 2.0 - 1.0);
					
					float3 kernel;
					kernel.x = cos(latitude) * cos(longitude);
					kernel.z = cos(latitude) * sin(longitude);
					kernel.y = sin(latitude);
					
					kernel = normalize(kernel + worldNormal.xyz * 1.0);

					traceResult += ConeTrace(voxelOrigin.xyz, kernel.xyz, worldNormal.xyz, coord, dither.y, TraceSteps, ConeSize, 1.0, 1.0);
				}
				
				traceResult /= numSamples;
				gi = traceResult.rgb * 20.0;

				//v0.1
				float4 voxelSpacePositionA = mul(CameraToWorld, viewSpacePosition);
				float diff = 15 * ditherControl.w - length(voxelSpacePositionA - _WorldSpaceCameraPos);
				if (0 < diff) {
					gi = gi / (1 + diff);
				}

				float fadeout = saturate((distance(voxelSpacePosition.xyz, float3(0.5, 0.5, 0.5)) - 0.5f) * 5.0);

				float3 fakeGI = saturate(dot(worldNormal, float3(0, 1, 0)) * 0.5 + 0.5) * SEGISkyColor.rgb * 5.0 * (1+SEGISkyColor.a*2); //v2.0.5

				gi.rgb = lerp(gi.rgb, fakeGI, fadeout);

				gi *= 0.75 + (float)HalfResolution * 0.25;

				 
				return float4(gi, 1.0);
			}
			
		ENDCG
	}
	
	Pass //1 Bilateral Blur
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float2 Kernel;
			
			float DepthTolerance;
			
			sampler2D DepthNormalsLow;
			sampler2D _CameraGBufferTexture2;
			sampler2D DepthLow;
			int SourceScale;
			
			//v0.4
			sampler2D _GBuffer0;
			sampler2D _GBuffer1;
			sampler2D _GBuffer2;
			sampler2D _GBuffer3;

			//v1.2
			float smoothNormals;
					
			float4 frag(v2f input) : COLOR0
			{
				float4 blurred = float4(0.0, 0.0, 0.0, 0.0);
				float validWeights = 0.0;
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, input.uv.xy).x);





				//v0.4
				//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
				float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb* 2.0 - 1.0), 1);
				float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb), 1);
				float3 worldNormalB = pow((tex2D(_GBuffer2, input.uv.xy).rgb * 2.0 - 1.0), 1);
				//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
				float depthA = 1;
				if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
					depthA = 0;
				}
				/////////float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//
				//half3 normal = normalize(tex2D(_CameraGBufferTexture2, input.uv.xy).rgb * 2.0 - 1.0);
				//half3 normal = normalize(tex2D(_CameraGBufferTexture2, input.uv.xy).rgb * 0.2 - 0.1 + worldNormalA)*(worldNormalB*(depthA)+0.88);
				half3 normal = tex2D(_GBuffer2, input.uv.xy).rgb * depthA; 

				//v1.2
				if(smoothNormals != 1){//if (ditherControl.x != 0) {
					normal = normalize(tex2D(_CameraGBufferTexture2, input.uv.xy).rgb * 0.2 - 0.1)*(worldNormalB*(depthA + worldNormalA) + 0.88);
				}



				float thresh = 0.26;
				
				float3 viewPosition = GetViewSpacePosition(input.uv.xy).xyz;
				float3 viewVector = normalize(viewPosition);
				
				float NdotV = 1.0 / (saturate(dot(-viewVector, normal.xyz)) + 0.1);
				thresh *= 1.0 + NdotV * 2.0;
				
				for (int i = -4; i <= 4; i++)
				{
					float2 offs = Kernel.xy * (i) * _MainTex_TexelSize.xy * 1.0;
					float sampleDepth = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, float4(input.uv.xy + offs.xy * 1, 0, 0)).x);

					//v1.2
					half3 sampleNormal = tex2Dlod(_GBuffer2, float4(input.uv.xy + offs.xy * 1, 0, 0)).rgb;// 

					//v1.2
					if (smoothNormals != 1) {
						sampleNormal = normalize(tex2Dlod(_CameraGBufferTexture2, float4(input.uv.xy + offs.xy * 1, 0, 0)).rgb * 2.0 - 1.0);
					}
					
					float weight = saturate(1.0 - abs(depth - sampleDepth) / thresh);
					weight *= pow(saturate(dot(sampleNormal, normal)), 24.0);
					
					float4 blurSample = tex2Dlod(_MainTex, float4(input.uv.xy + offs.xy, 0, 0)).rgba;
					blurred += blurSample * weight;
					validWeights += weight;
				}
				
				blurred /= validWeights + 0.001;
				
				return blurred;
			}		
		
		ENDCG
	}		
	
	Pass //2 Blend with scene
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _CameraGBufferTexture2;
			sampler2D _CameraGBufferTexture1;
			sampler2D GITexture;
			sampler2D Reflections;
			
			//v0.4
			sampler2D _GBuffer0;
			sampler2D _GBuffer1;
			sampler2D _GBuffer2;
			sampler2D _GBuffer3;
			
			float4x4 CameraToWorld;
			
			int DoReflections;

			//v0.4
			float contrastA;
			float4 ReflectControl;

			int HalfResolution;
					
			float4 frag(v2f input) : COLOR0
			{
#if UNITY_UV_STARTS_AT_TOP
				float2 coord = input.uv2.xy;
#else
				float2 coord = input.uv.xy;
#endif


				//v0.4
				float4 albedoSEGI = tex2D(_GBuffer3, input.uv.xy);
				float4 albedoSEGIA = tex2D(_GBuffer1, input.uv.xy);
				//float4 albedoTex = (1-contrastA)*albedoSEGI +(contrastA * albedoSEGIA*albedoSEGI);// tex2D(_CameraGBufferTexture0, input.uv.xy);

				float contrastA1 = contrastA;

				if (contrastA > 5) {
					contrastA = 5;
				}
				float4 albedoTex = (1 - contrastA)*albedoSEGI*albedoSEGI + (contrastA * albedoSEGIA*albedoSEGI*2);//v1.2f
				
				//v1.2fa
				float gialpha = 1;
				float3 scene = tex2D(_MainTex, input.uv.xy).rgb;
				float4 albedoSEGIB = tex2D(_GBuffer0, input.uv.xy);
				if (contrastA1 >= 20) {
					albedoTex = (1 - (contrastA1-20)) * albedoSEGI + ((contrastA1-20) * albedoSEGIA * albedoSEGI);
					albedoTex.r = scene.r;
					albedoTex = albedoTex * albedoSEGIB;
				}
				else
				if (contrastA1 >= 10) {
					albedoTex = (1 - (contrastA1 - 10)) * albedoSEGI + (contrastA1 - 10) * albedoSEGIA * albedoSEGI;
					albedoTex.r = scene.r;
					albedoTex = albedoTex * albedoSEGIB;
					gialpha = 1 - (albedoTex.a * albedoSEGIB.a);
				}
				else {
					//v1.2f
					//float4 albedoSEGIB = tex2D(_GBuffer0, input.uv.xy);
					albedoTex = albedoTex + albedoTex * albedoSEGIB;
				}

				float3 albedo = albedoTex.rgb;
				float3 gi = tex2D(GITexture, input.uv.xy).rgb;
				//float3 scene = tex2D(_MainTex, input.uv.xy).rgb;
				float3 reflections = tex2D(Reflections, input.uv.xy).rgb;
				
				//float3 result = scene + gi * albedoTex.a * albedoTex.rgb; //v1.2f
				float3 result = scene + albedo * gi * albedoTex.a * albedoTex.rgb * 10;

				if (contrastA1 >= 20) {
					gi = saturate(gi* albedoSEGIB);
					result = scene + gi;
				}
				else
				if (contrastA1 >= 10) {
					result = scene + (gi*scene * gialpha);
				}
				else
				if (contrastA1 >= 5) {
					result = scene + albedoSEGIB * gi * contrastA1 * scene + albedoSEGIB * gi * 0.01 * contrastA1;
				}

				if (DoReflections > 0)
				{

					//v0.4
					//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
					float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb* 2.0 - 1.0), 1);
					float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb), 1);
					float3 worldNormalB = pow((tex2D(_GBuffer2, input.uv.xy).rgb * 2.0 - 1.0), 1);
					//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
					float depthA = 1;
					if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
						depthA = 0;
					}
					/////////float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//


					float4 viewSpacePosition = GetViewSpacePosition(coord);
					float3 viewVector = normalize(viewSpacePosition.xyz);
					float4 worldViewVector = mul(CameraToWorld, float4(viewVector.xyz, 0.0));

					//v0.4
					//float4 spec = tex2D(_CameraGBufferTexture1, coord);
					float4 spec = tex2D(_GBuffer0, coord)* ReflectControl.y*(depthA)+ tex2D(_GBuffer1, coord)* ReflectControl.z*(depthA); //tex2D(_GBuffer1, coord); //v0.7

					float smoothness = spec.a;
					float3 specularColor = spec.rgb * ReflectControl.x;

					//v0.4
					//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
					float3 worldNormal = worldNormalB*(depthA)+worldNormalA1;
					
					float3 reflectionKernel = reflect(worldViewVector.xyz, worldNormal);

					float3 fresnel = pow(saturate(dot(worldViewVector.xyz, reflectionKernel.xyz)) * (smoothness * 0.5 + 0.5), 5.0);
					fresnel = lerp(fresnel, (1.0).xxx, specularColor.rgb);

					fresnel *= saturate(smoothness * 4.0);

					result = lerp(result, reflections, fresnel);
				}

				return float4(result, 1.0);
			}		
		
		ENDCG
	}	
	
	Pass //3 Temporal blend (with unity motion vectors)
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D GITexture;
			sampler2D PreviousDepth;
			sampler2D CurrentDepth;
			sampler2D PreviousLocalWorldPos;
			
			
			float4 CameraPosition;
			float4 CameraPositionPrev;
			float4x4 ProjectionPrev;
			float4x4 ProjectionPrevInverse;
			float4x4 WorldToCameraPrev;
			float4x4 CameraToWorldPrev;
			float4x4 CameraToWorld;
			float DeltaTime;
			float BlendWeight;
			
			float4 frag(v2f input) : COLOR0
			{
				float3 gi = tex2D(_MainTex, input.uv.xy).rgb;
				

				float depth = GetDepthTexture(input.uv.xy);
				
				float4 currentPos = float4(input.uv.x * 2.0 - 1.0, input.uv.y * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
				
				float4 fragpos = mul(ProjectionMatrixInverse, currentPos);
				fragpos = mul(CameraToWorld, fragpos); 
				fragpos /= fragpos.w;
				float4 thisWorldPosition = fragpos;
				
				


				float2 motionVectors = tex2Dlod(_CameraMotionVectorsTexture, float4(input.uv.xy, 0.0, 0.0)).xy;
				float2 reprojCoord = input.uv.xy - motionVectors.xy;
				

				
				float prevDepth = (tex2Dlod(PreviousDepth, float4(reprojCoord + _MainTex_TexelSize.xy * 0.0, 0.0, 0.0)).x);
				#if defined(UNITY_REVERSED_Z)
				prevDepth = 1.0 - prevDepth;
				#endif

				float4 previousWorldPosition = mul(ProjectionPrevInverse, float4(reprojCoord.xy * 2.0 - 1.0, prevDepth * 2.0 - 1.0, 1.0));
				previousWorldPosition = mul(CameraToWorldPrev, previousWorldPosition);
				previousWorldPosition /= previousWorldPosition.w;


				float blendWeight = BlendWeight;

				float posSimilarity = saturate(1.0 - distance(previousWorldPosition.xyz, thisWorldPosition.xyz) * 1.0);
				blendWeight = lerp(1.0, blendWeight, posSimilarity);




				float3 minPrev = float3(10000, 10000, 10000);
				float3 maxPrev = float3(0, 0, 0);

				float3 s0 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(0.5, 0.5), 0, 0)).rgb;
				minPrev = s0;
				maxPrev = s0;
				s0 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(0.5, -0.5), 0, 0)).rgb;
				minPrev = min(minPrev, s0);
				maxPrev = max(maxPrev, s0);
				s0 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(-0.5, 0.5), 0, 0)).rgb;
				minPrev = min(minPrev, s0);
				maxPrev = max(maxPrev, s0);
				s0 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(-0.5, -0.5), 0, 0)).rgb;
				minPrev = min(minPrev, s0);
				maxPrev = max(maxPrev, s0);


				
				float3 prevGI = tex2Dlod(PreviousGITexture_FAR_B, float4(reprojCoord, 0.0, 0.0)).rgb;
				prevGI = lerp(prevGI, clamp(prevGI, minPrev, maxPrev), 0.25);
				
				gi = lerp(prevGI, gi, float3(blendWeight, blendWeight, blendWeight));
				
				float3 result = gi;
				return float4(result, 1.0);
			}	
		
		ENDCG
	}
	
	Pass //4 Specular/reflections trace
	{
		ZTest Always
	
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4x4 CameraToWorld;
			
			
			sampler2D _CameraGBufferTexture1;
			sampler2D _CameraGBufferTexture2;
			
			//v0.4
			sampler2D _GBuffer0;
			sampler2D _GBuffer1;
			sampler2D _GBuffer2;
			sampler2D _GBuffer3;
			
			sampler3D SEGIVolumeTexture1;
			
			int FrameSwitch;

			//v2.1.34 - COLOR - DITHER - v0.7
			float4 ditherControl;// = float4(0, 1, 1, 1); //enable Dirther - Trace Directions Divider - Trece Steps Devider - Reflections Steps Divider
			
			//v1.2
			float smoothNormals;

			float4 frag(v2f input) : SV_Target
			{
				#if UNITY_UV_STARTS_AT_TOP
					float2 coord = input.uv2.xy;
				#else
					float2 coord = input.uv.xy;
				#endif
				
				float4 spec = tex2D(_GBuffer0, coord);
				//float4 spec = tex2D(_CameraGBufferTexture1, coord);

				float4 viewSpacePosition = GetViewSpacePosition(coord);
				float3 viewVector = normalize(viewSpacePosition.xyz);
				float4 worldViewVector = mul(CameraToWorld, float4(viewVector.xyz, 0.0));

				
				float4 voxelSpacePosition = mul(CameraToWorld, viewSpacePosition);
				float3 worldPosition = voxelSpacePosition.xyz;
				voxelSpacePosition = mul(SEGIWorldToVoxel, voxelSpacePosition);
				voxelSpacePosition = mul(SEGIVoxelProjection, voxelSpacePosition);
				voxelSpacePosition.xyz = voxelSpacePosition.xyz * 0.5 + 0.5;
				

				//v0.4
					//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
				float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb* 2.0 - 1.0), 1);
				float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb), 1);
				float3 worldNormalB = pow((tex2D(_GBuffer2, input.uv.xy).rgb * 2.0 - 1.0), 1);
				//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
				float depthA = 1;
				if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
					depthA = 0;
				}
				/////////float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//

				//v0.4
				//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;
				// worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);

				//v1.2
				float3 worldNormal = saturate(normalize(tex2D(_GBuffer2, coord).rgb)) * depthA;

				//v1.2
				if (smoothNormals != 1){//if (ditherControl.x != 0) {
					worldNormal = saturate(normalize(tex2D(_GBuffer2, coord).rgb * 1 - 0.0) * 1 * (depthA)+worldNormalA * (depthA)); //1.0g
				}

				
				float3 voxelOrigin = voxelSpacePosition.xyz + worldNormal.xyz * 0.006 * ConeTraceBias * 1.25 / SEGIVoxelScaleFactor_FAR_B;

				float2 dither = rand(coord + (float)FrameSwitch * 0.11734);
				

				//v2.1.34 - COLOR - v0.7
				if (ditherControl.x != 0 && dither.x > 0.001 * ditherControl.x) {
					ReflectionSteps = ReflectionSteps / ditherControl.w; //float2 dither = rand(coord + (float)FrameSwitch * 0.011734);
				}


				float smoothness = spec.a * 0.5;
				float3 specularColor = spec.rgb;
				
				float4 reflection = (0.0).xxxx;
				
				float3 reflectionKernel = reflect(worldViewVector.xyz, worldNormal);

				float3 fresnel = pow(saturate(dot(worldViewVector.xyz, reflectionKernel.xyz)) * (smoothness * 0.5 + 0.5), 5.0);
				fresnel = lerp(fresnel, (1.0).xxx, specularColor.rgb);
				
				voxelOrigin += worldNormal.xyz * 0.002 * 1.25 / SEGIVoxelScaleFactor_FAR_B;
				reflection = SpecularConeTrace(voxelOrigin.xyz, reflectionKernel.xyz, worldNormal.xyz, smoothness, coord, dither.x);

				float3 skyReflection = (reflection.a * 1.0 * SEGISkyColor);
				
				reflection.rgb = reflection.rgb * 0.7 + skyReflection.rgb * 2.4015 * SkyReflectionIntensity;
				
				return float4(reflection.rgb, 1.0);
			}
			
		ENDCG
	}
	
	Pass //5 Get camera depth texture
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4 frag(v2f input) : COLOR0
			{
				float2 coord = input.uv.xy;
				float4 tex = tex2D(_CameraDepthTexture, coord);				
				return tex;
			}	
		
		ENDCG
	}
	
	Pass //6 Get camera normals texture
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			//TEXTURE2D(_CameraNormalsTexture);
			//SAMPLER(sampler_CameraNormalsTexture);
			sampler2D _CameraNormalsTexture;
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
			//float DecodeFloatRG(float2 enc)
			//{
			//	float2 kDecodeDot = float2(1.0, 1 / 255.0);
			//	return dot(enc, kDecodeDot);
			//}
			//void DecodeDepthNormal(float4 enc, out float depth, out float3 normal)
			//{
			//	depth = DecodeFloatRG(enc.zw);
			//	normal = DecodeNormal(enc);
			//}
			sampler2D _GBuffer2;
			
			float4 frag(v2f input) : COLOR0
			{
				float2 coord = input.uv.xy;
				float4 tex = tex2D(_CameraDepthNormalsTexture, coord);

				//v1.5
				float3 normal = 0;
				float depth = 0;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, coord), depth, normal);
				if(coord.x > 0.5){
					normal= tex2D(_CameraNormalsTexture, coord).rgb*2.0 -1.0;
				}
				//float4 tex = tex2D(_CameraNormalsTexture, coord);
				
				//normal= tex2D(_GBuffer2, coord).rgb;
				return float4(normal,1);
			}	
		
		ENDCG
	}	
	
	
	Pass //7 Visualize GI
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D GITexture;
					
			float4 frag(v2f input) : COLOR0
			{
				float4 albedoTex = tex2D(_CameraGBufferTexture0, input.uv.xy);
				float3 albedo = albedoTex.rgb;
				float3 gi = tex2D(GITexture, input.uv.xy).rgb;
				return float4(gi, 1.0);
			}		
		
		ENDCG
	}	
	
	
	
	Pass //8 Write black
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4 frag(v2f input) : COLOR0
			{
				return float4(0.0, 0.0, 0.0, 1.0);
			}
			
		ENDCG
	}
	
	Pass //9 Visualize slice of GI Volume (CURRENTLY UNUSED)
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float LayerToVisualize;
			int MipLevelToVisualize;
			
			sampler3D SEGIVolumeTexture1;
			
			float4 frag(v2f input) : COLOR0
			{
				return float4(tex3D(SEGIVolumeTexture1, float3(input.uv.xy, LayerToVisualize)).rgb, 1.0);
			}
			
		ENDCG
	}
	
	
	Pass //10 Visualize voxels (trace through GI volumes)
	{
ZTest Always
	
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4x4 CameraToWorld;
			
			//sampler2D _CameraGBufferTexture2;
			
			float4 CameraPosition;
			
			float4 frag(v2f input) : SV_Target
			{
				#if UNITY_UV_STARTS_AT_TOP
					float2 coord = input.uv2.xy;
				#else
					float2 coord = input.uv.xy;
				#endif
				
				float4 viewSpacePosition = GetViewSpacePosition(coord);
				float3 viewVector = normalize(viewSpacePosition.xyz);
				float4 worldViewVector = mul(CameraToWorld, float4(viewVector.xyz, 0.0));

				float4 voxelCameraPosition = mul(SEGIWorldToVoxel, float4(CameraPosition.xyz, 1.0));
					   voxelCameraPosition = mul(SEGIVoxelProjection, voxelCameraPosition);
					   voxelCameraPosition.xyz = voxelCameraPosition.xyz * 0.5 + 0.5;
				
				float4 result = VisualConeTrace(voxelCameraPosition.xyz, worldViewVector.xyz);
				
				return float4(result.rgb, 1.0);
			}
			
		ENDCG
	}
	
	Pass //11 Bilateral upsample
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float2 Kernel;
			
			float DepthTolerance;
			
			sampler2D DepthNormalsLow;
			sampler2D DepthLow;
			int SourceScale;
			sampler2D CurrentDepth;
			sampler2D CurrentNormal;
			
					
			float4 frag(v2f input) : COLOR0
			{
				float4 blurred = float4(0.0, 0.0, 0.0, 0.0);
				float4 blurredDumb = float4(0.0, 0.0, 0.0, 0.0);
				float validWeights = 0.0;
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, input.uv.xy).x);
				half3 normal = DecodeViewNormalStereo(tex2D(_CameraDepthNormalsTexture, input.uv.xy));
				float thresh = 0.26;
				
				float3 viewPosition = GetViewSpacePosition(input.uv.xy).xyz;
				float3 viewVector = normalize(viewPosition);
				
				float NdotV = 1.0 / (saturate(dot(-viewVector, normal.xyz)) + 0.1);
				thresh *= 1.0 + NdotV * 2.0;
				
				float4 sample00 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(0.0, 0.0) * 1.0, 0.0, 0.0));
				float4 sample10 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(1.0, 0.0) * 1.0, 0.0, 0.0));
				float4 sample11 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(1.0, 1.0) * 1.0, 0.0, 0.0));
				float4 sample01 = tex2Dlod(_MainTex, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(0.0, 1.0) * 1.0, 0.0, 0.0));
				
				float4 depthSamples = float4(0,0,0,0);
				depthSamples.x = LinearEyeDepth(tex2Dlod(CurrentDepth, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(0.0, 0.0), 0, 0)).x);
				depthSamples.y = LinearEyeDepth(tex2Dlod(CurrentDepth, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(1.0, 0.0), 0, 0)).x);
				depthSamples.z = LinearEyeDepth(tex2Dlod(CurrentDepth, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(1.0, 1.0), 0, 0)).x);
				depthSamples.w = LinearEyeDepth(tex2Dlod(CurrentDepth, float4(input.uv.xy + _MainTex_TexelSize.xy * float2(0.0, 1.0), 0, 0)).x);
				
				half3 normal00 = DecodeViewNormalStereo(tex2D(CurrentNormal, input.uv.xy + _MainTex_TexelSize.xy * float2(0.0, 0.0)));
				half3 normal10 = DecodeViewNormalStereo(tex2D(CurrentNormal, input.uv.xy + _MainTex_TexelSize.xy * float2(1.0, 0.0)));
				half3 normal11 = DecodeViewNormalStereo(tex2D(CurrentNormal, input.uv.xy + _MainTex_TexelSize.xy * float2(1.0, 1.0)));
				half3 normal01 = DecodeViewNormalStereo(tex2D(CurrentNormal, input.uv.xy + _MainTex_TexelSize.xy * float2(0.0, 1.0)));
				
				float4 depthWeights = saturate(1.0 - abs(depthSamples - depth.xxxx) / thresh);
				
				float4 normalWeights = float4(0,0,0,0);
				normalWeights.x = pow(saturate(dot(normal00, normal)), 24.0);
				normalWeights.y = pow(saturate(dot(normal10, normal)), 24.0);
				normalWeights.z = pow(saturate(dot(normal11, normal)), 24.0);
				normalWeights.w = pow(saturate(dot(normal01, normal)), 24.0);
				
				float4 weights = depthWeights * normalWeights;
				
				float weightSum = dot(weights, float4(1.0, 1.0, 1.0, 1.0));				
								
				if (weightSum < 0.01)
				{
					weightSum = 4.0;
					weights = (1.0).xxxx;
				}
				
				weights /= weightSum;
				
				float2 fractCoord = frac(input.uv.xy * _MainTex_TexelSize.zw * 1.0);
				
				float4 filteredX0 = lerp(sample00 * weights.x, sample10 * weights.y, fractCoord.x);
				float4 filteredX1 = lerp(sample01 * weights.w, sample11 * weights.z, fractCoord.x);
				
				float4 filtered = lerp(filteredX0, filteredX1, fractCoord.y);
				
				
				return filtered * 3.0;
				
				return blurred;
			}		
		
		ENDCG
	}

	Pass //12 Temporal blending without motion vectors (for legacy support)
	{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D GITexture;
			sampler2D PreviousDepth;
			sampler2D CurrentDepth;
			sampler2D PreviousLocalWorldPos;
			
			
			float4 CameraPosition;
			float4 CameraPositionPrev;
			float4x4 ProjectionPrev;
			float4x4 ProjectionPrevInverse;
			float4x4 WorldToCameraPrev;
			float4x4 CameraToWorldPrev;
			float4x4 CameraToWorld;
			float DeltaTime;
			float BlendWeight;
			
			float4 frag(v2f input) : COLOR0
			{
				float3 gi = tex2D(_MainTex, input.uv.xy).rgb;
				
				float2 depthLookupCoord = round(input.uv.xy * _MainTex_TexelSize.zw) * _MainTex_TexelSize.xy;
				depthLookupCoord = input.uv.xy;
				//float depth = tex2Dlod(_CameraDepthTexture, float4(depthLookupCoord, 0.0, 0.0)).x;
				float depth = GetDepthTexture(depthLookupCoord);
				
				float4 currentPos = float4(input.uv.x * 2.0 - 1.0, input.uv.y * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
				
				float4 fragpos = mul(ProjectionMatrixInverse, currentPos);
				float4 thisViewPos = fragpos;
				fragpos = mul(CameraToWorld, fragpos); 
				fragpos /= fragpos.w;
				float4 thisWorldPosition = fragpos;
				fragpos.xyz += CameraPosition.xyz * DeltaTime;
				
				float4 prevPos = fragpos;
				prevPos.xyz -= CameraPositionPrev.xyz * DeltaTime;
				prevPos = mul(WorldToCameraPrev, prevPos);
				prevPos = mul(ProjectionPrev, prevPos);
				prevPos /= prevPos.w;
				
				float2 diff = currentPos.xy - prevPos.xy;
				
				float2 reprojCoord = input.uv.xy - diff.xy * 0.5;
				float2 previousTexcoord = input.uv.xy + diff.xy * 0.5;
				

				float blendWeight = BlendWeight;
				
				float prevDepth = (tex2Dlod(PreviousDepth, float4(reprojCoord + _MainTex_TexelSize.xy * 0.0, 0.0, 0.0)).x);
				
				float4 previousWorldPosition = mul(ProjectionPrevInverse, float4(reprojCoord.xy * 2.0 - 1.0, prevDepth * 2.0 - 1.0, 1.0));
				previousWorldPosition = mul(CameraToWorldPrev, previousWorldPosition);
				previousWorldPosition /= previousWorldPosition.w;
				
				if (distance(previousWorldPosition.xyz, thisWorldPosition.xyz) > 0.1 || reprojCoord.x > 1.0 || reprojCoord.x < 0.0 || reprojCoord.y > 1.0 || reprojCoord.y < 0.0)
				{
					blendWeight = 1.0;
				}
				
				float3 prevGI = tex2D(PreviousGITexture_FAR_B, reprojCoord).rgb;
				
				gi = lerp(prevGI, gi, float3(blendWeight, blendWeight, blendWeight));
				
				float3 result = gi;
				return float4(result, 1.0);
			}	
		
		ENDCG
	}
	



		////// END BLEND	
				Pass //13 BLIT
			{
				Name "ColorBlitPass"
				HLSLPROGRAM
				// Core.hlsl includes URP basic variables needed for any shader. The Blit.hlsl provides a
				//Vert and Fragment function that abstracts platform differences when handling a full screen shader pass.
				//It also declares a _BlitTex texture that is bound by the Blitter API.
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
				// This is a simple read shader so we use the default provided Vert and FragNearest
				//functions. If you would like to do a bilinear sample you could use the FragBilinear functions instead.
				#include "BlitLumina.hlsl"//v0.2

				#pragma vertex Vert
				#pragma fragment FragNearest
			ENDHLSL
			}

				Pass //14 BLIT BACKGROUND
			{
				Name "ColorBlitPasss"
				HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
				#include "BlitLumina.hlsl"//v0.2
				#pragma vertex Vert
				#pragma fragment Frag
				//
				float4 Frag(VaryingsB input) : SV_Target0
				{
					// this is needed so we account XR platform differences in how they handle texture arrays
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			// sample the texture using the SAMPLE_TEXTURE2D_X_LOD
			float2 uv = input.texcoord.xy;
			half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
			// Inverts the sampled color
			//return half4(1, 1, 1, 1) - color;
			return color;
		}
		ENDHLSL
			}
				Pass //15 Get camera depth texture 2
		{
			Name "DepthBlitPasss"
				HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			#include "BlitLumina.hlsl"//v0.2
			#pragma vertex Vert
			#pragma fragment Frag
			//

			sampler2D _CameraDepthTexture;

			float4 Frag(VaryingsB input) : SV_Target0
			{
				// this is needed so we account XR platform differences in how they handle texture arrays
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			// sample the texture using the SAMPLE_TEXTURE2D_X_LOD
			float2 uv = input.texcoord.xy;
			float4 tex = tex2D(_CameraDepthTexture, uv);
			return tex;
		}
		ENDHLSL
			/*
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				float4 frag(v2f input) : COLOR0
				{
					float2 coord = input.uv.xy;
					float4 tex = tex2D(_CameraDepthTexture, coord);
					return tex;
				}

			ENDCG
			*/
		}
			Pass //16 Get camera normals texture 2
		{
				Name "DepthBlitPasss"
				HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			#include "BlitLumina.hlsl"//v0.2
			#pragma vertex Vert
			#pragma fragment Frag
			//
			sampler2D _GBuffer2;

			sampler2D _CameraNormalsTexture;
			//sampler2D _CameraDepthNormalsTexture;

			float3 DecodeNormal(float4 enc)
			{
				float kScale = 1.7777;
				float3 nn = enc.xyz * float3(2 * kScale,2 * kScale,0) + float3(-kScale,-kScale,1);
				float g = 2.0 / dot(nn.xyz,nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}
			 float DecodeFloatRGBA(float4 enc)
			{
				float4 kDecodeDot = float4(1.0, 1 / 255.0, 1 / 65025.0, 1 / 16581375.0);
				return dot(enc, kDecodeDot);
			}
			float DecodeFloatRG(float2 enc)
			{
				float2 kDecodeDot = float2(1.0, 1 / 255.0);
				return dot(enc, kDecodeDot);
			}
			float3 DecodeViewNormalStereo(float4 enc4)
			{
				float kScale = 1.7777;
				float3 nn = enc4.xyz * float3(2 * kScale,2 * kScale,0) + float3(-kScale,-kScale,1);
				float g = 2.0 / dot(nn.xyz,nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}
			void DecodeDepthNormal(float4 enc, out float depth, out float3 normal)
			{
				depth = DecodeFloatRG(enc.zw);
				normal = DecodeViewNormalStereo(enc);
			}
			float4 Frag(VaryingsB input) : SV_Target0
			{
				// this is needed so we account XR platform differences in how they handle texture arrays
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			// sample the texture using the SAMPLE_TEXTURE2D_X_LOD
			float2 coord = input.texcoord.xy;
			float4 tex = tex2D(_CameraNormalsTexture, coord);

			//v1.5
			float3 normal = 0;
			float depth = 0;
			DecodeDepthNormal(tex2D(_CameraNormalsTexture, coord), depth, normal);
			//if(coord.x > 0.5){
				//normal= tex2D(_CameraNormalsTexture, coord).rgb*2.0 -1.0;
			//	normal= tex2D(_CameraDepthNormalsTexture, coord).rgb*2.0 -1.0;
			//}
			//float4 tex = tex2D(_CameraNormalsTexture, coord);

			//normal= tex2D(_GBuffer2, coord).rgb;
			return float4(normal,1);
		}
		ENDHLSL

			/*
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				//TEXTURE2D(_CameraNormalsTexture);
				//SAMPLER(sampler_CameraNormalsTexture);
				sampler2D _CameraNormalsTexture;
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
				//float DecodeFloatRG(float2 enc)
				//{
				//	float2 kDecodeDot = float2(1.0, 1 / 255.0);
				//	return dot(enc, kDecodeDot);
				//}
				//void DecodeDepthNormal(float4 enc, out float depth, out float3 normal)
				//{
				//	depth = DecodeFloatRG(enc.zw);
				//	normal = DecodeNormal(enc);
				//}
				sampler2D _GBuffer2;

				float4 frag(v2f input) : COLOR0
				{
					float2 coord = input.uv.xy;
					float4 tex = tex2D(_CameraDepthNormalsTexture, coord);

					//v1.5
					float3 normal = 0;
					float depth = 0;
					DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, coord), depth, normal);
					if(coord.x > 0.5){
						normal= tex2D(_CameraNormalsTexture, coord).rgb*2.0 -1.0;
					}
					//float4 tex = tex2D(_CameraNormalsTexture, coord);

					//normal= tex2D(_GBuffer2, coord).rgb;
					return float4(normal,1);
				}

			ENDCG
			*/
		}	//END PASS 16

			Pass //17 == 0
		{

			Name "Trace step HLSL"
				HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			#include "BlitLumina.hlsl"//v0.2

			#include "SEGI.cginc"

			#pragma vertex Vert
			#pragma fragment Frag

				float SEGIVoxelScaleFactor_FAR_B;

			sampler2D _CameraNormalsTexture;

			float4x4 CameraToWorld;

			sampler2D _CameraGBufferTexture2;

			//v0.4
			sampler2D _GBuffer0;
			sampler2D _GBuffer1;
			sampler2D _GBuffer2;
			sampler2D _GBuffer3;

			int FrameSwitch;
			int HalfResolution;


			sampler3D SEGIVolumeTexture1;

			sampler2D NoiseTexture;

			//v2.1.34 - COLOR - DITHER - v0.7
			float4 ditherControl;// = float4(0, 1, 1, 1); //enable Dirther - Trace Directions Divider - Trece Steps Devider - Reflections Steps Divider

			//v1.2
			float smoothNormals;

			float4 Frag(VaryingsB input) : SV_Target0
			{
				// this is needed so we account XR platform differences in how they handle texture arrays
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			// sample the texture using the SAMPLE_TEXTURE2D_X_LOD
			//float2 coord = input.texcoord.xy;


			#if UNITY_UV_STARTS_AT_TOP
				float2 coord = input.texcoord.xy;
			#else
				float2 coord = input.texcoord.xy;
			#endif

				//Get view space position and view vector
				float4 viewSpacePosition = GetViewSpacePosition(coord);
				float3 viewVector = normalize(viewSpacePosition.xyz);

				//Get voxel space position
				float4 voxelSpacePosition = mul(CameraToWorld, viewSpacePosition);
				voxelSpacePosition = mul(SEGIWorldToVoxel, voxelSpacePosition);
				voxelSpacePosition = mul(SEGIVoxelProjection, voxelSpacePosition);
				voxelSpacePosition.xyz = voxelSpacePosition.xyz * 0.5 + 0.5;


				//Prepare for cone trace
				float2 dither = rand(coord + (float)FrameSwitch * 0.011734);

				//v0.4
				//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
				float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0), 1);
				float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb), 1);
				//float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1); //v1.2
				//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
				float depthA = 1;
				if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
					depthA = 0;
				}

				//v1.2
				//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;// pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1) * worldNormalA;
				float3 worldNormal = tex2D(_GBuffer2, coord).rgb * (depthA)+worldNormalA * (1 - depthA); //v1.2b
				//return float4(depth, depth, depth, depth);

				//v1.2
				if (smoothNormals != 1) {//if (ditherControl.x != 0) {
					float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1);
					worldNormal = worldNormal * smoothNormals
					+ (1 - smoothNormals) * (normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0) * worldNormalB * (depthA)+worldNormalA);
				}

				//v2.1.34 - COLOR - v0.7
				//float3 fakeGI = saturate(dot(worldNormal, float3(0, 1, 0)) * 0.5 + 0.5) * SEGISkyColor.rgb * 5.0;
				//float4 ditherControl = float4(1, 1, 1, 1);
				//if (mod(voxelSpacePosition.z, 0.01) < 0.002 && mod(voxelSpacePosition.z, 0.01) > 0.001) {
				if (ditherControl.x != 0 && dither.x > 0.001 * ditherControl.x) {
					//if (mod(input.uv.x, 0.1) == 0.004) {
					//discard;
					//float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1);
					//return float4(fakeGI *1, 1.0);
					TraceDirections = TraceDirections / ditherControl.y;// *saturate(dither.x);
					TraceSteps = TraceSteps / ditherControl.z;
				}


				float3 voxelOrigin = voxelSpacePosition.xyz + worldNormal.xyz * 0.003 * ConeTraceBias * 1.25 / SEGIVoxelScaleFactor_FAR_B;

				float3 gi = float3(0.0, 0.0, 0.0);
				float4 traceResult = float4(0,0,0,0);

				const float phi = 1.618033988;
				const float gAngle = phi * PI * 1.0;

				//Get blue noise
				float2 noiseCoord = (input.texcoord.xy * _MainTex_TexelSize.zw) / (64.0).xx;
				float blueNoise = tex2Dlod(NoiseTexture, float4(noiseCoord, 0.0, 0.0)).x;

				//Trace GI cones
				int numSamples = TraceDirections;
				for (int i = 0; i < numSamples; i++)
				{
					float fi = (float)i + blueNoise * StochasticSampling;
					float fiN = fi / numSamples;
					float longitude = gAngle * fi;
					float latitude = asin(fiN * 2.0 - 1.0);

					float3 kernel;
					kernel.x = cos(latitude) * cos(longitude);
					kernel.z = cos(latitude) * sin(longitude);
					kernel.y = sin(latitude);

					kernel = normalize(kernel + worldNormal.xyz * 1.0);

					traceResult += ConeTrace(voxelOrigin.xyz, kernel.xyz, worldNormal.xyz, coord, dither.y, TraceSteps, ConeSize, 1.0, 1.0);
				}

				traceResult /= numSamples;
				gi = traceResult.rgb * 20.0;


				float fadeout = saturate((distance(voxelSpacePosition.xyz, float3(0.5, 0.5, 0.5)) - 0.5f) * 5.0);

				float3 fakeGI = saturate(dot(worldNormal, float3(0, 1, 0)) * 0.5 + 0.5) * SEGISkyColor.rgb * 5.0;

				gi.rgb = lerp(gi.rgb, fakeGI, fadeout);

				gi *= 0.75 + (float)HalfResolution * 0.25;


				return float4(gi, 1.0);

			}
			ENDHLSL

				/*
								CGPROGRAM
									#pragma vertex vert
									#pragma fragment frag

									float4x4 CameraToWorld;

									sampler2D _CameraGBufferTexture2;

									//v0.4
									sampler2D _GBuffer0;
									sampler2D _GBuffer1;
									sampler2D _GBuffer2;
									sampler2D _GBuffer3;

									int FrameSwitch;
									int HalfResolution;


									sampler3D SEGIVolumeTexture1;

									sampler2D NoiseTexture;

									//v2.1.34 - COLOR - DITHER - v0.7
									float4 ditherControl;// = float4(0, 1, 1, 1); //enable Dirther - Trace Directions Divider - Trece Steps Devider - Reflections Steps Divider

									//v1.2
									float smoothNormals;

									float4 frag(v2f input) : SV_Target
									{
										#if UNITY_UV_STARTS_AT_TOP
											float2 coord = input.uv2.xy;
										#else
											float2 coord = input.uv.xy;
										#endif

										//Get view space position and view vector
										float4 viewSpacePosition = GetViewSpacePosition(coord);
										float3 viewVector = normalize(viewSpacePosition.xyz);

										//Get voxel space position
										float4 voxelSpacePosition = mul(CameraToWorld, viewSpacePosition);
										voxelSpacePosition = mul(SEGIWorldToVoxel, voxelSpacePosition);
										voxelSpacePosition = mul(SEGIVoxelProjection, voxelSpacePosition);
										voxelSpacePosition.xyz = voxelSpacePosition.xyz * 0.5 + 0.5;


										//Prepare for cone trace
										float2 dither = rand(coord + (float)FrameSwitch * 0.011734);

										//v0.4
										//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
										float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb* 2.0 - 1.0	), 1);
										float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb), 1);
										//float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1); //v1.2
										//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
										float depthA = 1;
										if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
											depthA = 0;
										}

										//v1.2
										//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;// pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1) * worldNormalA;
										float3 worldNormal = tex2D(_GBuffer2, coord).rgb*(depthA) + worldNormalA * (1 - depthA); //v1.2b
										//return float4(depth, depth, depth, depth);

										//v1.2
										if(smoothNormals != 1){//if (ditherControl.x != 0) {
											float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1);
											worldNormal = worldNormal * smoothNormals
											+ (1- smoothNormals)*( normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA);
										}

										//v2.1.34 - COLOR - v0.7
										//float3 fakeGI = saturate(dot(worldNormal, float3(0, 1, 0)) * 0.5 + 0.5) * SEGISkyColor.rgb * 5.0;
										//float4 ditherControl = float4(1, 1, 1, 1);
										//if (mod(voxelSpacePosition.z, 0.01) < 0.002 && mod(voxelSpacePosition.z, 0.01) > 0.001) {
										if (ditherControl.x != 0 && dither.x > 0.001 * ditherControl.x) {
											//if (mod(input.uv.x, 0.1) == 0.004) {
											//discard;
											//float3 worldNormalB = pow((tex2D(_GBuffer2, coord).rgb * 2.0 - 1.0), 1);
											//return float4(fakeGI *1, 1.0);
											TraceDirections = TraceDirections / ditherControl.y;// *saturate(dither.x);
											TraceSteps = TraceSteps / ditherControl.z;
										}


										float3 voxelOrigin = voxelSpacePosition.xyz + worldNormal.xyz * 0.003 * ConeTraceBias * 1.25 / SEGIVoxelScaleFactor;

										float3 gi = float3(0.0, 0.0, 0.0);
										float4 traceResult = float4(0,0,0,0);

										const float phi = 1.618033988;
										const float gAngle = phi * PI * 1.0;

										//Get blue noise
										float2 noiseCoord = (input.uv.xy * _MainTex_TexelSize.zw) / (64.0).xx;
										float blueNoise = tex2Dlod(NoiseTexture, float4(noiseCoord, 0.0, 0.0)).x;

										//Trace GI cones
										int numSamples = TraceDirections;
										for (int i = 0; i < numSamples; i++)
										{
											float fi = (float)i + blueNoise * StochasticSampling;
											float fiN = fi / numSamples;
											float longitude = gAngle * fi;
											float latitude = asin(fiN * 2.0 - 1.0);

											float3 kernel;
											kernel.x = cos(latitude) * cos(longitude);
											kernel.z = cos(latitude) * sin(longitude);
											kernel.y = sin(latitude);

											kernel = normalize(kernel + worldNormal.xyz * 1.0);

											traceResult += ConeTrace(voxelOrigin.xyz, kernel.xyz, worldNormal.xyz, coord, dither.y, TraceSteps, ConeSize, 1.0, 1.0);
										}

										traceResult /= numSamples;
										gi = traceResult.rgb * 20.0;


										float fadeout = saturate((distance(voxelSpacePosition.xyz, float3(0.5, 0.5, 0.5)) - 0.5f) * 5.0);

										float3 fakeGI = saturate(dot(worldNormal, float3(0, 1, 0)) * 0.5 + 0.5) * SEGISkyColor.rgb * 5.0;

										gi.rgb = lerp(gi.rgb, fakeGI, fadeout);

										gi *= 0.75 + (float)HalfResolution * 0.25;


										return float4(gi, 1.0);
									}

								ENDCG
								*/

		}//END PASS 17 ==0

			Pass //18 == 2 Blend with scene 2
			{

				Name "Blend step HLSL"
					HLSLPROGRAM
					#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
				#include "BlitLumina.hlsl"//v0.2

				#include "SEGI.cginc"

				#pragma vertex Vert
				#pragma fragment Frag

				//sampler2D _CameraNormalsTexture;

				sampler2D _CameraGBufferTexture2;
				sampler2D _CameraGBufferTexture1;
				sampler2D GITexture;
				sampler2D Reflections;

				//v0.4
				sampler2D _GBuffer0;
				sampler2D _GBuffer1;
				sampler2D _GBuffer2;
				sampler2D _GBuffer3;

				float4x4 CameraToWorld;

				int DoReflections;

				//v0.4
				float contrastA;
				float4 ReflectControl;

				int HalfResolution;

				float4 Frag(VaryingsB input) : SV_Target0
				{
					// this is needed so we account XR platform differences in how they handle texture arrays
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
					#if UNITY_UV_STARTS_AT_TOP
						float2 coord = input.texcoord.xy;
					#else
						float2 coord = input.texcoord.xy;
					#endif


						//v0.4
						float4 albedoSEGI = tex2D(_GBuffer3, input.texcoord.xy);
						float4 albedoSEGIA = tex2D(_GBuffer1, input.texcoord.xy);
						//float4 albedoTex = (1-contrastA)*albedoSEGI +(contrastA * albedoSEGIA*albedoSEGI);// tex2D(_CameraGBufferTexture0, input.uv.xy);

						float contrastA1 = contrastA;

						if (contrastA > 5) {
							contrastA = 5;
						}
						float4 albedoTex = (1 - contrastA) * albedoSEGI * albedoSEGI + (contrastA * albedoSEGIA * albedoSEGI * 2);//v1.2f

						//v1.2fa
						float gialpha = 1;

						//float3 scene = tex2D(_BlitTexture, input.texcoord.xy).rgb;
						float3 scene = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, coord,	_BlitMipLevel);

						float4 albedoSEGIB = tex2D(_GBuffer0, input.texcoord.xy);
						if (contrastA1 >= 20) {
							albedoTex = (1 - (contrastA1 - 20)) * albedoSEGI + ((contrastA1 - 20) * albedoSEGIA * albedoSEGI);
							albedoTex.r = scene.r;
							albedoTex = albedoTex * albedoSEGIB;
						}
						else
						if (contrastA1 >= 10) {
							albedoTex = (1 - (contrastA1 - 10)) * albedoSEGI + (contrastA1 - 10) * albedoSEGIA * albedoSEGI;
							albedoTex.r = scene.r;
							albedoTex = albedoTex * albedoSEGIB;
							gialpha = 1 - (albedoTex.a * albedoSEGIB.a);
						}
						else {
							//v1.2f
							//float4 albedoSEGIB = tex2D(_GBuffer0, input.uv.xy);
							albedoTex = albedoTex + albedoTex * albedoSEGIB;
						}

						float3 albedo = albedoTex.rgb;
						float3 gi = tex2D(GITexture, input.texcoord.xy).rgb;
						//float3 scene = tex2D(_MainTex, input.uv.xy).rgb;
						float3 reflections = tex2D(Reflections, input.texcoord.xy).rgb;

						//float3 result = scene + gi * albedoTex.a * albedoTex.rgb; //v1.2f
						float3 result = scene + albedo * gi * albedoTex.a * albedoTex.rgb * 10;

						if (contrastA1 >= 20) {
							gi = saturate(gi * albedoSEGIB);
							result = scene + gi;
						}
						else
						if (contrastA1 >= 10) {
							result = scene + (gi * scene * gialpha);
						}
						else
						if (contrastA1 >= 5) {
							result = scene + albedoSEGIB * gi * contrastA1 * scene + albedoSEGIB * gi * 0.01 * contrastA1;
						}

						if (DoReflections > 0)
						{

							//v0.4
							//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
							float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, input.texcoord.xy).rgb * 2.0 - 1.0), 1);
							float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, input.texcoord.xy).rgb), 1);
							float3 worldNormalB = pow((tex2D(_GBuffer2, input.texcoord.xy).rgb * 2.0 - 1.0), 1);
							//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
							float depthA = 1;
							if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
								depthA = 0;
							}
							/////////float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//


							float4 viewSpacePosition = GetViewSpacePosition(coord);
							float3 viewVector = normalize(viewSpacePosition.xyz);
							float4 worldViewVector = mul(CameraToWorld, float4(viewVector.xyz, 0.0));

							//v0.4
							//float4 spec = tex2D(_CameraGBufferTexture1, coord);
							float4 spec = tex2D(_GBuffer0, coord) * ReflectControl.y * (depthA)+tex2D(_GBuffer1, coord) * ReflectControl.z * (depthA); //tex2D(_GBuffer1, coord); //v0.7

							float smoothness = spec.a;
							float3 specularColor = spec.rgb * ReflectControl.x;

							//v0.4
							//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
							float3 worldNormal = worldNormalB * (depthA)+worldNormalA1;

							float3 reflectionKernel = reflect(worldViewVector.xyz, worldNormal);

							float3 fresnel = pow(saturate(dot(worldViewVector.xyz, reflectionKernel.xyz)) * (smoothness * 0.5 + 0.5), 5.0);
							fresnel = lerp(fresnel, (1.0).xxx, specularColor.rgb);

							fresnel *= saturate(smoothness * 4.0);

							result = lerp(result, reflections, fresnel);
						}

						return float4(result, 1.0);

					}
					ENDHLSL
						/*
					CGPROGRAM
						#pragma vertex vert
						#pragma fragment frag

						sampler2D _CameraGBufferTexture2;
						sampler2D _CameraGBufferTexture1;
						sampler2D GITexture;
						sampler2D Reflections;

						//v0.4
						sampler2D _GBuffer0;
						sampler2D _GBuffer1;
						sampler2D _GBuffer2;
						sampler2D _GBuffer3;

						float4x4 CameraToWorld;

						int DoReflections;

						//v0.4
						float contrastA;
						float4 ReflectControl;

						int HalfResolution;

						float4 frag(v2f input) : COLOR0
						{
			#if UNITY_UV_STARTS_AT_TOP
							float2 coord = input.uv2.xy;
			#else
							float2 coord = input.uv.xy;
			#endif


							//v0.4
							float4 albedoSEGI = tex2D(_GBuffer3, input.uv.xy);
							float4 albedoSEGIA = tex2D(_GBuffer1, input.uv.xy);
							//float4 albedoTex = (1-contrastA)*albedoSEGI +(contrastA * albedoSEGIA*albedoSEGI);// tex2D(_CameraGBufferTexture0, input.uv.xy);

							float contrastA1 = contrastA;

							if (contrastA > 5) {
								contrastA = 5;
							}
							float4 albedoTex = (1 - contrastA)*albedoSEGI*albedoSEGI + (contrastA * albedoSEGIA*albedoSEGI*2);//v1.2f

							//v1.2fa
							float gialpha = 1;
							float3 scene = tex2D(_MainTex, input.uv.xy).rgb;
							float4 albedoSEGIB = tex2D(_GBuffer0, input.uv.xy);
							if (contrastA1 >= 20) {
								albedoTex = (1 - (contrastA1-20)) * albedoSEGI + ((contrastA1-20) * albedoSEGIA * albedoSEGI);
								albedoTex.r = scene.r;
								albedoTex = albedoTex * albedoSEGIB;
							}
							else
							if (contrastA1 >= 10) {
								albedoTex = (1 - (contrastA1 - 10)) * albedoSEGI + (contrastA1 - 10) * albedoSEGIA * albedoSEGI;
								albedoTex.r = scene.r;
								albedoTex = albedoTex * albedoSEGIB;
								gialpha = 1 - (albedoTex.a * albedoSEGIB.a);
							}
							else {
								//v1.2f
								//float4 albedoSEGIB = tex2D(_GBuffer0, input.uv.xy);
								albedoTex = albedoTex + albedoTex * albedoSEGIB;
							}

							float3 albedo = albedoTex.rgb;
							float3 gi = tex2D(GITexture, input.uv.xy).rgb;
							//float3 scene = tex2D(_MainTex, input.uv.xy).rgb;
							float3 reflections = tex2D(Reflections, input.uv.xy).rgb;

							//float3 result = scene + gi * albedoTex.a * albedoTex.rgb; //v1.2f
							float3 result = scene + albedo * gi * albedoTex.a * albedoTex.rgb * 10;

							if (contrastA1 >= 20) {
								gi = saturate(gi* albedoSEGIB);
								result = scene + gi;
							}
							else
							if (contrastA1 >= 10) {
								result = scene + (gi*scene * gialpha);
							}

							if (DoReflections > 0)
							{

								//v0.4
								//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
								float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb* 2.0 - 1.0), 1);
								float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, input.uv.xy).rgb), 1);
								float3 worldNormalB = pow((tex2D(_GBuffer2, input.uv.xy).rgb * 2.0 - 1.0), 1);
								//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
								float depthA = 1;
								if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
									depthA = 0;
								}
								/////////float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//


								float4 viewSpacePosition = GetViewSpacePosition(coord);
								float3 viewVector = normalize(viewSpacePosition.xyz);
								float4 worldViewVector = mul(CameraToWorld, float4(viewVector.xyz, 0.0));

								//v0.4
								//float4 spec = tex2D(_CameraGBufferTexture1, coord);
								float4 spec = tex2D(_GBuffer0, coord)* ReflectControl.y*(depthA)+ tex2D(_GBuffer1, coord)* ReflectControl.z*(depthA); //tex2D(_GBuffer1, coord); //v0.7

								float smoothness = spec.a;
								float3 specularColor = spec.rgb * ReflectControl.x;

								//v0.4
								//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
								float3 worldNormal = worldNormalB*(depthA)+worldNormalA1;

								float3 reflectionKernel = reflect(worldViewVector.xyz, worldNormal);

								float3 fresnel = pow(saturate(dot(worldViewVector.xyz, reflectionKernel.xyz)) * (smoothness * 0.5 + 0.5), 5.0);
								fresnel = lerp(fresnel, (1.0).xxx, specularColor.rgb);

								fresnel *= saturate(smoothness * 4.0);

								result = lerp(result, reflections, fresnel);
							}

							return float4(result, 1.0);
						}

					ENDCG
					*/
			}	//END PASS 18 == 2


			////////////////// TEMPORAL AA

			///// TEMPORAL AA
		////////////// TEMPORAL - https://github.com/cdrinmatane/SSRT - MIT LICENSE
			//https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@13.1/manual/renderer-features/how-to-fullscreen-blit-in-xr-spi.html
						Pass // 19
					{
						Name "TemporalReproj1"
						HLSLPROGRAM

						//NEW

						//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
						//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

								//v0.2 - shadows
						//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
						//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
						//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookie.hlsl"

						//#pragma vertex vert
						#include "BlitLumina.hlsl"//v0.2
						#pragma vertex Vert //vert//v0.2

						#pragma fragment frag


						//NEW
						TEXTURE2D(_ColorBuffer);
						SAMPLER(sampler_ColorBuffer);
						TEXTURE2D(_CameraDepthTexture);
						SAMPLER(sampler_CameraDepthTexture);
						half4 _CameraDepthTexture_ST;
						float _TemporalResponse;
						float _TemporalGain;

						struct AttributesA
						{
							float4 positionHCS   : POSITION;
							float2 uv           : TEXCOORD0;
							UNITY_VERTEX_INPUT_INSTANCE_ID
						};

						struct VaryingsA
						{
							float4  positionCS  : SV_POSITION;
							float2  uv          : TEXCOORD0;
							UNITY_VERTEX_OUTPUT_STEREO
						};

						VaryingsA vert(AttributesA input)
						{
							VaryingsA output;
							UNITY_SETUP_INSTANCE_ID(input);
							UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

							// Note: The pass is setup with a mesh already in clip
							// space, that's why, it's enough to just output vertex
							// positions
							output.positionCS = float4(input.positionHCS.xyz, 1.0);

							#if UNITY_UV_STARTS_AT_TOP
							output.positionCS.y *= -1;
							#endif

							output.uv = input.uv;
							return output;
						}

						TEXTURE2D_X(_PreviousColor);
						SAMPLER(sampler_PreviousColor);
						TEXTURE2D_X(_PreviousDepth);
						SAMPLER(sampler_PreviousDepth);

						// Transformation matrices
						float4x4 _CameraToWorldMatrix;
						float4x4 _InverseProjectionMatrix;
						float4x4 _LastFrameViewProjectionMatrix;
						float4x4 _InverseViewProjectionMatrix;
						float4x4 _LastFrameInverseViewProjectionMatrix;
						//float _TemporalResponse;

						//NEW TAA
						//float4x4 _invP;//_InverseProjectionMatrix
						float4x4 _FrameMatrix;

						float LinearEyeDepthB(float z)
						{
							return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
						}
						float4 frag(VaryingsA  input) : SV_Target
						{
							//UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);				

							float2 uv = input.uv.xy;
							float2 oneOverResolution = (1.0 / _ScreenParams.xy);

							float4 gi = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, input.uv.xy);// tex2D(_MainTex, input.uv.xy);

							//DEBUG
							//float4 giP = SAMPLE_TEXTURE2D(_PreviousColor, sampler_PreviousColor, input.uv.xy);
							//return giP;//

							float4 depthIMG = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy);
							float depth = LinearEyeDepthB(depthIMG.x);// depthIMG.x; //LinearEyeDepth(depthIMG.x);
							float4 currentPos = float4(input.uv.x * 2.0 - 1.0, input.uv.y * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);

							float4 fragpos = mul(_InverseViewProjectionMatrix, float4(float3(uv * 2 - 1, depth), 1));
							fragpos.xyz /= fragpos.w;
							float4 thisWorldPosition = fragpos;

							float2 motionVectors = float2(0,0);// tex2Dlod(_CameraMotionVectorsTexture, float4(input.uv.xy, 0.0, 0.0)).xy;
							float2 reprojCoord = input.uv.xy - motionVectors.xy;

							//NEW TAA
							//temporal reprojection
							float d0 = depth;//sampleDepth(i.uv);
							float d01 = (d0 * (_ProjectionParams.z - _ProjectionParams.y) + _ProjectionParams.y) / _ProjectionParams.z;
							float3 pos = float3(reprojCoord * 2.0 - 1.0, 1.0);
							float4 rd = mul(_InverseProjectionMatrix, float4(pos, 1));
							rd.xyz /= rd.w;
							float4 temporalUV = mul(_FrameMatrix, float4(rd.xyz * d01, 1));
							temporalUV /= temporalUV.w;
							reprojCoord = temporalUV.xy * 1;
							if (reprojCoord.x > 0.99) {
								reprojCoord.x = 0.99;
							}
							if (reprojCoord.y * 1 > 0.99) {
								reprojCoord.y = 0.99;
							}
							if (reprojCoord.x < -0.99) {
								reprojCoord.x = -0.99;
							}
							if (reprojCoord.y * 1 < -0.99) {
								reprojCoord.y = -0.99;
							}
							/*
							if(reprojCoord.x*0.5+0.5 > 1){
								reprojCoord.x = 1;
							}
							if(reprojCoord.y*0.5+0.5 > 1){
								reprojCoord.y = 1;
							}
							if(reprojCoord.x*0.5+0.5 < -1){
								reprojCoord.x = -1;
							}
							if(reprojCoord.y*0.5+0.5 < -1){
								reprojCoord.y = -1;
							}
							*/
							float4 prevGI = SAMPLE_TEXTURE2D(_PreviousColor, sampler_PreviousColor, float2(reprojCoord * 0.5 + 0.5));
							if (abs(temporalUV.x) > 1 || abs(temporalUV.y) > 1)
							{
								prevGI = gi;
							}

							float prevDepth = SAMPLE_DEPTH_TEXTURE(_PreviousDepth, sampler_PreviousDepth, float2(reprojCoord * 0.5 + 0.5 + oneOverResolution * 0.0)).x;

						prevDepth = LinearEyeDepthB(prevDepth);

						//float4 previousWorldPosition = mul(_LastFrameInverseViewProjectionMatrix, float4(reprojCoord.xy * 2.0 - 1.0, prevDepth, 1.0)); //prevDepth * 2.0 - 1.0, 1.0));
						float4 previousWorldPosition = mul(_LastFrameInverseViewProjectionMatrix, float4(reprojCoord * 0.5 + 0.5, prevDepth, 1.0));
						previousWorldPosition /= previousWorldPosition.w;

						float blendWeight = 0.15 * _TemporalResponse;

						float posSimilarity = saturate(1.0 - distance(previousWorldPosition.xyz, thisWorldPosition.xyz) * 1.0);
						blendWeight = lerp(1.0, blendWeight, posSimilarity);

						float4 minPrev = float4(10000, 10000, 10000, 10000);
						float4 maxPrev = float4(0, 0, 0, 0);

						float4 s0 = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, float2(input.uv.xy + oneOverResolution * float2(0.5, 0.5)));// tex2Dlod(_MainTex, float4(input.uv.xy + oneOverResolution * float2(0.5, 0.5), 0, 0));
						minPrev = s0;
						maxPrev = s0;
						s0 = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, float2(input.uv.xy + oneOverResolution * float2(0.5, -0.5)));
						minPrev = min(minPrev, s0);
						maxPrev = max(maxPrev, s0);
						s0 = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, float2(input.uv.xy + oneOverResolution * float2(-0.5, 0.5)));
						minPrev = min(minPrev, s0);
						maxPrev = max(maxPrev, s0);
						s0 = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, float2(input.uv.xy + oneOverResolution * float2(-0.5, -0.5)));
						minPrev = min(minPrev, s0);
						maxPrev = max(maxPrev, s0);

						//float4 prevGI = SAMPLE_TEXTURE2D(_PreviousColor, sampler_PreviousColor, float2(reprojCoord*0.5+0.5));				
						prevGI = lerp(prevGI, clamp(prevGI, minPrev, maxPrev), 0.25);
						prevGI = clamp(prevGI, minPrev, maxPrev); ///BASIC

						///BASIC
						float temporalMultiplier = exp(-100 * length(pos.xy - temporalUV));
						//float3 finalCol = lerp(curCol, lastCol, _TemporalFade * temporalMultiplier);

						gi = lerp(prevGI, gi, float4(blendWeight, blendWeight, blendWeight, blendWeight) * 0.65 * _TemporalGain);

						return gi;
					}
						ENDHLSL
					}
						Pass //20
					{
						Name"GetDepth2"
						HLSLPROGRAM//CGPROGRAM
						//#pragma vertex vert
						#include "BlitLumina.hlsl"//v0.2
						#pragma vertex Vert //vert//v0.2

						#pragma fragment frag


							TEXTURE2D(_CameraDepthTexture);
						SAMPLER(sampler_CameraDepthTexture);
						half4 _CameraDepthTexture_ST;

						struct AttributesA
						{
							float4 positionHCS   : POSITION;
							float2 uv           : TEXCOORD0;
							UNITY_VERTEX_INPUT_INSTANCE_ID
						};

						struct VaryingsA
						{
							float4  positionCS  : SV_POSITION;
							float2  uv          : TEXCOORD0;
							UNITY_VERTEX_OUTPUT_STEREO
						};

						VaryingsA vert(AttributesA input)
						{
							VaryingsA output;
							UNITY_SETUP_INSTANCE_ID(input);
							UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
							output.positionCS = float4(input.positionHCS.xyz, 1.0);

							#if UNITY_UV_STARTS_AT_TOP
							output.positionCS.y *= -1;
							#endif

							output.uv = input.uv;
							return output;
						}
						float4 frag(VaryingsA input) : COLOR0
						{
							float2 coord = input.uv.xy + (1.0 / _ScreenParams.xy) * 0.5;
							float4 tex = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, coord);
							return tex;
						}
						ENDHLSL
					}
						///// END TEMPORAL AA

						///////////////// END TEMPORAL AA




								////PASS REFL
							Pass //4 Specular/reflections trace //PASS 21
						{
							ZTest Always

							HLSLPROGRAM

								#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
							//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
							#include "BlitLumina.hlsl"//v0.2

							#include "SEGI.cginc"

							#pragma vertex Vert
							#pragma fragment frag

							float SEGIVoxelScaleFactor_FAR_B;

							/*#pragma vertex vert
							#pragma fragment frag*/

							float4x4 CameraToWorld;


							sampler2D _CameraGBufferTexture1;
							sampler2D _CameraGBufferTexture2;

							//v0.4
							sampler2D _GBuffer0;
							sampler2D _GBuffer1;
							sampler2D _GBuffer2;
							sampler2D _GBuffer3;

							sampler3D SEGIVolumeTexture1;

							int FrameSwitch;

							//v2.1.34 - COLOR - DITHER - v0.7
							float4 ditherControl;// = float4(0, 1, 1, 1); //enable Dirther - Trace Directions Divider - Trece Steps Devider - Reflections Steps Divider

							//v1.2
							float smoothNormals;

							float4 frag(VaryingsB input) : SV_Target
							{
								#if UNITY_UV_STARTS_AT_TOP
									float2 coord = input.texcoord.xy;
								#else
									float2 coord = input.texcoord.xy;
								#endif

								float4 spec = tex2D(_GBuffer0, coord);
								//float4 spec = tex2D(_CameraGBufferTexture1, coord);

								float4 viewSpacePosition = GetViewSpacePosition(coord);
								float3 viewVector = normalize(viewSpacePosition.xyz);
								float4 worldViewVector = mul(CameraToWorld, float4(viewVector.xyz, 0.0));


								float4 voxelSpacePosition = mul(CameraToWorld, viewSpacePosition);
								float3 worldPosition = voxelSpacePosition.xyz;
								voxelSpacePosition = mul(SEGIWorldToVoxel, voxelSpacePosition);
								voxelSpacePosition = mul(SEGIVoxelProjection, voxelSpacePosition);
								voxelSpacePosition.xyz = voxelSpacePosition.xyz * 0.5 + 0.5;


								//v0.4
									//float3 worldNormal = pow((tex2D(_CameraDepthNormalsTexture, coord).rgb * 2.0 - 1.0),1);   //normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);
								float3 worldNormalA = pow((tex2D(_CameraDepthNormalsTexture, input.texcoord.xy).rgb * 2.0 - 1.0), 1);
								float3 worldNormalA1 = pow((tex2D(_CameraDepthNormalsTexture, input.texcoord.xy).rgb), 1);
								float3 worldNormalB = pow((tex2D(_GBuffer2, input.texcoord.xy).rgb * 2.0 - 1.0), 1);
								//float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, coord).x);
								float depthA = 1;
								if (worldNormalA1.r == 0 && worldNormalA1.g == 0 && worldNormalA1.b == 0) {
									depthA = 0;
								}
								/////////float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;//

								//v0.4
								//float3 worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0)*worldNormalB*(depthA)+worldNormalA;
								// worldNormal = normalize(tex2D(_CameraGBufferTexture2, coord).rgb * 2.0 - 1.0);

								//v1.2
								float3 worldNormal = saturate(normalize(tex2D(_GBuffer2, coord).rgb)) * depthA;

								//v1.2
								if (smoothNormals != 1) {//if (ditherControl.x != 0) {
									worldNormal = saturate(normalize(tex2D(_GBuffer2, coord).rgb * 1 - 0.0) * 1 * (depthA)+worldNormalA * (depthA)); //1.0g
								}


								float3 voxelOrigin = voxelSpacePosition.xyz + worldNormal.xyz * 0.006 * ConeTraceBias * 1.25 / SEGIVoxelScaleFactor_FAR_B;

								float2 dither = rand(coord + (float)FrameSwitch * 0.11734);


								//v2.1.34 - COLOR - v0.7
								if (ditherControl.x != 0 && dither.x > 0.001 * ditherControl.x) {
									ReflectionSteps = ReflectionSteps / ditherControl.w; //float2 dither = rand(coord + (float)FrameSwitch * 0.011734);
								}


								float smoothness = spec.a * 0.5;
								float3 specularColor = spec.rgb;

								float4 reflection = (0.0).xxxx;

								float3 reflectionKernel = reflect(worldViewVector.xyz, worldNormal);

								float3 fresnel = pow(saturate(dot(worldViewVector.xyz, reflectionKernel.xyz)) * (smoothness * 0.5 + 0.5), 5.0);
								fresnel = lerp(fresnel, (1.0).xxx, specularColor.rgb);

								voxelOrigin += worldNormal.xyz * 0.002 * 1.25 / SEGIVoxelScaleFactor_FAR_B;
								reflection = SpecularConeTrace(voxelOrigin.xyz, reflectionKernel.xyz, worldNormal.xyz, smoothness, coord, dither.x);

								float3 skyReflection = (reflection.a * 1.0 * SEGISkyColor);

								reflection.rgb = reflection.rgb * 0.7 + skyReflection.rgb * 2.4015 * SkyReflectionIntensity;

								return float4(reflection.rgb, 1.0);
							}

						ENDHLSL
						}
							////END PASS RELF

	
}

Fallback off

}