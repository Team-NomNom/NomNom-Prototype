Shader "SEGI/SEGIVoxelizeSceneVERTL" {
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
					#include "UnityCG.cginc"

					//v0.1
					#include "UnityLightingCommon.cginc"
					#include "UnityShaderVariables.cginc"
					#include "AutoLight.cginc"
					#include "UnityDeferredLibrary.cginc"

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

					//v0.1
					//struct g2f
					//{
					//	float4 pos : SV_POSITION;
					//	half4 uv : TEXCOORD0;
					//	float3 normal : TEXCOORD1;
					//	float angle : TEXCOORD2;
					//	half4 worldPos : TEXCOORD3;	//v0.1
					//	half4 attenUV : TEXCOORD4;	//v0.1
					//};

					half4 _Color;

					v2g vert(appdata_full v)
					{
						v2g o;
						UNITY_INITIALIZE_OUTPUT(v2g, o);

						float4 vertex = v.vertex;

						o.normal = UnityObjectToWorldNormal(v.normal);
						float3 absNormal = abs(o.normal);

						//METHOD2
						o.vertexLighting = float3(0.0, 0.0, 0.0);
						float3 worldN = mul((float3x3)unity_ObjectToWorld, SCALED_NORMAL);

						//MAC 
						v2g p[3];
						int i = 0;
						for (i = 0; i < 3; i++)
						{
							//p[i] = input[i];
							p[i].pos = mul(unity_ObjectToWorld, vertex);

							p[i].uv = v.texcoord;
							p[i].normal = absNormal;
							p[i].angle = 0;

							////v0.1
							//p[i].worldPos = mul(unity_ObjectToWorld, p[i].pos);
							//p[i].attenUV.x = attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), p[i].worldPos.xyz);
							//p[i].attenUV.y = attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), p[i].worldPos.xyz);
							//p[i].attenUV.z = attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), p[i].worldPos.xyz);
							//p[i].attenUV.w = attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), p[i].worldPos.xyz);
						}

						float3 realNormal = float3(0.0, 0.0, 0.0);

						float3 V = p[1].pos.xyz - p[0].pos.xyz;
						float3 W = p[2].pos.xyz - p[0].pos.xyz;

						realNormal.x = (V.y * W.z) - (V.z * W.y);
						realNormal.y = (V.z * W.x) - (V.x * W.z);
						realNormal.z = (V.x * W.y) - (V.y * W.x);

						absNormal = abs(realNormal);

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

							p[i].pos = mul(UNITY_MATRIX_P, p[i].pos);

#if defined(UNITY_REVERSED_Z)
							p[i].pos.z = 1.0 - p[i].pos.z;
#else 
							p[i].pos.z *= -1.0;
#endif

							p[i].angle = (float)angle;

							//METHOD3
							//p[i].vertexLighting += Shade4PointLights(
							//	unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							//	unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							//	unity_4LightAtten0, p[i].pos, realNormal//absNormal//realNormal
							//);

							//v0.1
							/*p[i].worldPos = mul(unity_ObjectToWorld, p[i].pos);
							p[i].attenUV.x = attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), p[i].worldPos.xyz);
							p[i].attenUV.y = attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), p[i].worldPos.xyz);
							p[i].attenUV.z = attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), p[i].worldPos.xyz);
							p[i].attenUV.w = attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), p[i].worldPos.xyz);*/
						}


						//o.pos = vertex;
						o.pos = mul(unity_ObjectToWorld, p[0].pos);

						o.uv = float4(TRANSFORM_TEX(v.texcoord.xy, _MainTex), 1.0, 1.0);

						//v0.1
						o.worldPos = mul(unity_ObjectToWorld, v.vertex);
						o.attenUV.x = (1 - shadowlessLocalOcclusion) * attenUV(unity_4LightAtten0.x, float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), o.worldPos.xyz);
						o.attenUV.y = (1 - shadowlessLocalOcclusion) * attenUV(unity_4LightAtten0.y, float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), o.worldPos.xyz);
						o.attenUV.z = (1 - shadowlessLocalOcclusion) * attenUV(unity_4LightAtten0.z, float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), o.worldPos.xyz);
						o.attenUV.w = (1 - shadowlessLocalOcclusion) * attenUV(unity_4LightAtten0.w, float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), o.worldPos.xyz);

						//METHOD3
						//#ifdef VERTEXLIGHT_ON
						o.vertexLighting += Shade4PointLights(
							unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							unity_4LightAtten0, o.worldPos, worldN
						);
						//#endif // VERTEXLIGHT_ON

						return o;
					}

					int SEGIVoxelResolution;

					

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

					float4 frag(v2g input) : SV_TARGET //v0.1
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
							result.rgb += unity_LightColor[0].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x), input.worldPos.xyz)) * _atten.x;
							result.rgb += unity_LightColor[1].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y), input.worldPos.xyz)) * _atten.y;
							result.rgb += unity_LightColor[2].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z), input.worldPos.xyz)) * _atten.z;
							result.rgb += unity_LightColor[3].rgb *shadowlessLocalPower* (1 / distance(float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w), input.worldPos.xyz)) * _atten.w;
						}

						//METHOD3
						if (shadowedLocalPower != 0) {
							//float atten = LIGHT_ATTENUATION(input);
							result.rgb += float3(input.vertexLighting.rgb*0.02 * shadowedLocalPower);
						}

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
			FallBack Off
}
