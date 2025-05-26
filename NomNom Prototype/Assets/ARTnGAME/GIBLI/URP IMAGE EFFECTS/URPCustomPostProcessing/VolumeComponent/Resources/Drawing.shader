Shader "SleeplessOwl/Post-Processing/Drawing"
{
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


			struct appdata
			{
				uint vertexID : SV_VertexID;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uvShift[9] : TEXCOORD1;
			};


			//v0.1
			TEXTURE2D(_DrawingTex);
			float _OverlayOffset;
			float _Strength;
			float _Tiling;
			float _Smudge;
			float _DepthThreshold;


			TEXTURE2D(_PostSource);
			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_PostSource);

			float4 _PostSource_TexelSize;
			float _sampleDistance;
			float _strengthPow;

			float Sobel(float2 uv[9])
			{
				const float gx[9] =
				{
					-1, -2, -1,
					0,  0,  0,
					1,  2,  1
				};

				const float gy[9] =
				{
					1, 0, -1,
					2, 0, -2,
					1, 0, -1
				};

				float edgeX, edgeY;
				for (int j = 0; j < 9; j++)
				{
					float3 col = SAMPLE_TEXTURE2D(_PostSource, sampler_PostSource, uv[j]).rgb;

					float lum = (col.r + col.g + col.b) * 0.3333333;

					edgeX += lum * gx[j];
					edgeY += lum * gy[j];
				}
				return 1 - abs(edgeX) - abs(edgeY);
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				o.uv.xy = GetFullScreenTriangleTexCoord(v.vertexID);

				//_MainTex_TexelSize => Vector4(1 / width, 1 / height, width, height)
				float2 uvShift = _PostSource_TexelSize.xy * _sampleDistance;
				o.uvShift[0] = o.uv + float2(-1, -1) * uvShift;
				o.uvShift[1] = o.uv + float2(0, -1) * uvShift;
				o.uvShift[2] = o.uv + float2(1, -1) * uvShift;
				o.uvShift[3] = o.uv + float2(-1, 0) * uvShift;
				o.uvShift[4] = o.uv + float2(0, 0) * uvShift;
				o.uvShift[5] = o.uv + float2(1, 0) * uvShift;
				o.uvShift[6] = o.uv + float2(-1, 1) * uvShift;
				o.uvShift[7] = o.uv + float2(0, 1) * uvShift;
				o.uvShift[8] = o.uv + float2(1, 1) * uvShift;
				return o;
			}

			inline float Linear01Depth(float z)
			{
				return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
			}

			float4 frag(v2f i) : SV_Target
			{
				float outline = Sobel(i.uvShift);
				outline = pow(outline, _strengthPow);
				outline = step(0.5, outline);

				float3 colOri = LOAD_TEXTURE2D(_PostSource, i.uv.xy * _ScreenParams.xy).rgb;
				float depth = Linear01Depth(LOAD_TEXTURE2D(_CameraDepthTexture, i.uv.xy * _ScreenParams.xy).r);
				float3 colOutline = min(1, (outline + .8)) * colOri.rgb;

				////ignore mid view
				//depth *= 4;
				//float value = pow(depth - .5, 8) * 256;
				//value = pow(saturate(value + .2), 2);
				//return float4(lerp(colOri, colOutline, value), 1);

				//v0.1
				float2 drawingUV = i.uv * _Tiling + _OverlayOffset;
				drawingUV.y *= _ScreenParams.y / _ScreenParams.x;
				float4 drawingCol = (LOAD_TEXTURE2D(_DrawingTex, drawingUV) +  //tex2D(_DrawingTex, drawingUV) +
					LOAD_TEXTURE2D(_DrawingTex, drawingUV / 3.0f)) / 2.0f;//tex2D(_DrawingTex, drawingUV / 3.0f)) / 2.0f;

				float2 texUV = i.uv * _ScreenParams.xy + drawingCol * _Smudge;
				float4 col = LOAD_TEXTURE2D(_PostSource, texUV  );// tex2D(_MainTex, texUV); //URP

				float lum = dot(col, float3(0.3f, 0.59f, 0.11f));
				float4 drawing = lerp(col, drawingCol * col, (1.0f - lum) * _Strength);

				//float depth = tex2D(_CameraDepthTexture, i.uv).r;
				//depth = Linear01Depth(depth);
				
				return depth < _DepthThreshold ? (drawing * float4(colOutline, 1)) : (col * float4(colOutline, 1));
				//END v0.1



				return float4(colOutline, 1);
			}
			ENDHLSL
		}


		//v0.2
		// Wobbing
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			struct appdata
			{
				uint vertexID : SV_VertexID;
			};

			struct v2f
			{
				//float4 vertex : SV_POSITION;
				//float2 uv : TEXCOORD0;	

				float4 vertex : SV_POSITION;
				float2 uv_Main : TEXCOORD0;
				float2 uv_Wobb : TEXCOORD1;				
			};

			//v0.1
			TEXTURE2D(_WobbTex);			
			float _WobbScale;
			float _WobbPower;
			float4 ColorMod(float4 c, float d) {
				return c - (c - c * c) * (d - 1);
			}

			TEXTURE2D(_PostSource);
			//TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_PostSource);
			float4 _PostSource_TexelSize;							

			v2f vert(appdata v)
			{
				v2f o;
				//o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				//o.uv.xy = GetFullScreenTriangleTexCoord(v.vertexID);

				float aspect = _ScreenParams.x / _ScreenParams.y;
				//v2f_wobb o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);// UnityObjectToClipPos(v.vertex);
				o.uv_Main = GetFullScreenTriangleTexCoord(v.vertexID); ;// v.uv;
				o.uv_Wobb = GetFullScreenTriangleTexCoord(v.vertexID)* float2(aspect, 1) * _WobbScale;//   v.uv * float2(aspect, 1) * _WobbScale;
				//return o;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{				
				//float3 colOri = LOAD_TEXTURE2D(_PostSource, i.uv.xy * _ScreenParams.xy).rgb;

				float2 wobb = LOAD_TEXTURE2D(_WobbTex, i.uv_Wobb * _ScreenParams.xy).wy * 2 - 1; // tex2D(_WobbTex, i.uv_Wobb).wy * 2 - 1;
				//TEST
				float4 src1 = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + wobb * _WobbPower); // tex2D(_PostSource, i.uv_Main);
				//return float4(1, 0, 0, 1);// src1 / 21;
				return src1;

				//float4 src = tex2D(_PostSource, i.uv_Main + wobb * _WobbPower);				
				//return float4(colOutline, 1);
			}
			ENDHLSL
		}//WOBBNG HLSL
		//Pass{
		//	CGPROGRAM
		//	#pragma vertex vert_wobb
		//	#pragma fragment frag

		//	sampler2D _PostSource;
		//	float4 _PostSource_TexelSize;

		//	struct v2f_wobb {
		//		float2 uv_Main : TEXCOORD0;
		//		float2 uv_Wobb : TEXCOORD1;
		//		float4 vertex : SV_POSITION;
		//	};
		//	struct appdata {
		//		float4 vertex : POSITION;
		//		float2 uv : TEXCOORD0;
		//	};
		//	sampler2D _WobbTex;
		//	float _WobbScale;
		//	float _WobbPower;
		//	float4 ColorMod(float4 c, float d) {
		//		return c - (c - c * c) * (d - 1);
		//	}
		//	v2f_wobb vert_wobb(appdata v) {
		//		float aspect = _ScreenParams.x / _ScreenParams.y;

		//		v2f_wobb o;
		//		o.vertex = UnityObjectToClipPos(v.vertex);
		//		o.uv_Main = v.uv;
		//		o.uv_Wobb = v.uv * float2(aspect, 1) * _WobbScale;
		//		return o;
		//	}

		//	fixed4 frag(v2f_wobb i) : SV_Target {
		//		fixed2 wobb = tex2D(_WobbTex, i.uv_Wobb).wy * 2 - 1;

		//		//TEST
		//		fixed4 src1 = tex2D(_PostSource, i.uv_Main);
		//		return float4(1, 0, 0, 1);// src1 / 21;


		//		fixed4 src = tex2D(_PostSource, i.uv_Main + wobb * _WobbPower);
		//		return src;
		//	}
		//	ENDCG
		//}

			// Edge Darkening
		// Edge Darkening
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				uint vertexID : SV_VertexID;
			};

			struct v2f
			{
				//float4 vertex : SV_POSITION;
				//float2 uv : TEXCOORD0;	
				float4 vertex : SV_POSITION;
				float2 uv_Main : TEXCOORD0;
				//float2 uv_Wobb : TEXCOORD1;
			};

			//v0.1
			//TEXTURE2D(_WobbTex);
			float _EdgeSize;
			float _EdgePower;
			float4 ColorMod(float4 c, float d) {
				return c - (c - c * c) * (d - 1);
			}

			TEXTURE2D(_PostSource);
			//TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_PostSource);
			float4 _PostSource_TexelSize;

			v2f vert(appdata v)
			{
				v2f o;
				//o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				//o.uv.xy = GetFullScreenTriangleTexCoord(v.vertexID);

				float aspect = _ScreenParams.x / _ScreenParams.y;
				//v2f_wobb o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);// UnityObjectToClipPos(v.vertex);
				o.uv_Main = GetFullScreenTriangleTexCoord(v.vertexID);// v.uv;
				//o.uv_Wobb = GetFullScreenTriangleTexCoord(v.vertexID)* float2(aspect, 1) * _WobbScale;//   v.uv * float2(aspect, 1) * _WobbScale;
				//return o;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				//float2 wobb = LOAD_TEXTURE2D(_WobbTex, i.uv_Wobb * _ScreenParams.xy).wy * 2 - 1; 
				//float4 src1 = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + wobb * _WobbPower); 
				//return src1;

				float2 uv_offset = _PostSource_TexelSize.xy * _EdgeSize;
				float4 src_l = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + float2(-uv_offset.x, 0));
				float4 src_r = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + float2(+uv_offset.x, 0));
				float4 src_b = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + float2(0, -uv_offset.y));
				float4 src_t = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + float2(0, +uv_offset.y));
				float4 src = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy);

				float4 grad = abs(src_r - src_l) + abs(src_b - src_t);
				float intens = saturate(0.333 * (grad.x + grad.y + grad.z));
				float d = _EdgePower * intens + 1;
				return ColorMod(src, d);
			}
			ENDHLSL
		}//Edge Darkening HLSL
				/*Pass{
					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag

					sampler2D _PostSource;
					float4 _PostSource_TexelSize;


					struct appdata {
						float4 vertex : POSITION;
						float2 uv : TEXCOORD0;
					};
					struct v2f {
						float2 uv_Main : TEXCOORD0;
						float4 vertex : SV_POSITION;
					};
					v2f vert(appdata v) {
						v2f o;
						o.vertex = UnityObjectToClipPos(v.vertex);
						o.uv_Main = v.uv;
						return o;
					}

					float _EdgeSize;
					float _EdgePower;
					float4 ColorMod(float4 c, float d) {
						return c - (c - c * c) * (d - 1);
					}
					fixed4 frag(v2f i) : SV_Target {
						float2 uv_offset = _PostSource_TexelSize.xy * _EdgeSize;
						fixed4 src_l = tex2D(_PostSource, i.uv_Main + float2(-uv_offset.x, 0));
						fixed4 src_r = tex2D(_PostSource, i.uv_Main + float2(+uv_offset.x, 0));
						fixed4 src_b = tex2D(_PostSource, i.uv_Main + float2(0, -uv_offset.y));
						fixed4 src_t = tex2D(_PostSource, i.uv_Main + float2(0, +uv_offset.y));
						fixed4 src = tex2D(_PostSource, i.uv_Main);

						fixed4 grad = abs(src_r - src_l) + abs(src_b - src_t);
						float intens = saturate(0.333 * (grad.x + grad.y + grad.z));
						float d = _EdgePower * intens + 1;
						return ColorMod(src, d);
					}
					ENDCG
			}*/

				// Paper Layer
		// Paper Layer
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				uint vertexID : SV_VertexID;
			};

			struct v2f
			{
				//float4 vertex : SV_POSITION;
				//float2 uv : TEXCOORD0;	

				float4 vertex : SV_POSITION;
				float2 uv_Main : TEXCOORD0;
				float2 uv_Paper : TEXCOORD1;
			};

			//v0.1
			TEXTURE2D(_PaperTex);
			float _PaperScale;
			float _PaperPower;
			float4 ColorMod(float4 c, float d) {
				return c - (c - c * c) * (d - 1);
			}

			TEXTURE2D(_PostSource);
			//TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_PostSource);
			float4 _PostSource_TexelSize;

			v2f vert(appdata v)
			{
				v2f o;
				//o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				//o.uv.xy = GetFullScreenTriangleTexCoord(v.vertexID);

				float aspect = _ScreenParams.x / _ScreenParams.y;
				//v2f_wobb o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);// UnityObjectToClipPos(v.vertex);
				o.uv_Main = GetFullScreenTriangleTexCoord(v.vertexID); ;// v.uv;
				//o.uv_Wobb = GetFullScreenTriangleTexCoord(v.vertexID)* float2(aspect, 1) * _WobbScale;//   v.uv * float2(aspect, 1) * _WobbScale;
				o.uv_Paper = GetFullScreenTriangleTexCoord(v.vertexID)* float2(aspect, 1) * _PaperScale;//v.uv * float2(aspect, 1) * _PaperScale;
				//return o;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				//float2 wobb = LOAD_TEXTURE2D(_WobbTex, i.uv_Wobb * _ScreenParams.xy).wy * 2 - 1;
				//float4 src1 = LOAD_TEXTURE2D(_PostSource, i.uv_Main * _ScreenParams.xy + wobb * _WobbPower); 
				//return src1;

				float4 src = LOAD_TEXTURE2D(_PostSource, i.uv_Main* _ScreenParams.xy);
				float paper = LOAD_TEXTURE2D(_PaperTex, i.uv_Paper* _ScreenParams.xy).x;
				float d = _PaperPower * (paper - 0.5) + 1;
				return ColorMod(src, d);
			}
			ENDHLSL
		}//Paper Layer HLSL
						/*Pass{
							CGPROGRAM
							#pragma vertex vert_paper
							#pragma fragment frag

							sampler2D _PostSource;
							float4 _PostSource_TexelSize;

							float4 ColorMod(float4 c, float d) {
								return c - (c - c * c) * (d - 1);
							}
							struct v2f_paper {
								float2 uv_Main : TEXCOORD0;
								float2 uv_Paper : TEXCOORD1;
								float4 vertex : SV_POSITION;
							};
							struct appdata {
								float4 vertex : POSITION;
								float2 uv : TEXCOORD0;
							};

							sampler2D _PaperTex;
							float _PaperScale;
							float _PaperPower;

							v2f_paper vert_paper(appdata v) {
								float aspect = _ScreenParams.x / _ScreenParams.y;

								v2f_paper o;
								o.vertex = UnityObjectToClipPos(v.vertex);
								o.uv_Main = v.uv;
								o.uv_Paper = v.uv * float2(aspect, 1) * _PaperScale;
								return o;
							}

							fixed4 frag(v2f_paper i) : SV_Target {
								fixed4 src = tex2D(_PostSource, i.uv_Main);
								fixed paper = tex2D(_PaperTex, i.uv_Paper).x;

								float d = _PaperPower * (paper - 0.5) + 1;
								return ColorMod(src, d);
							}

							ENDCG
					}*/
		//END v0.2


		//v0.3
		Pass
		{
			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment Frag
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			///#include "../../Shaders/StdLib.hlsl"
			#include "../../Shaders/XPostProcessing.hlsl"

			TEXTURE2D(_PostSource);
			//TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_PostSource);
			float4 _PostSource_TexelSize;

			half2 _Params;
			half4 _EdgeColor;
			half4 _BackgroundColor;

			#define _EdgeWidth _Params.x
			#define _BackgroundFade _Params.y

			struct appdata
			{
				uint vertexID : SV_VertexID;
			};
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv_Main : TEXCOORD0;
			};
			v2f vert(appdata v)
			{
				v2f o;
				float aspect = _ScreenParams.x / _ScreenParams.y;				
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				o.uv_Main = GetFullScreenTriangleTexCoord(v.vertexID); ;
				return o;
			}

			float intensity(in float4 color)
			{
				return sqrt((color.x * color.x) + (color.y * color.y) + (color.z * color.z));
			}

			float sobel(float stepx, float stepy, float2 center)
			{
				// get samples around pixel
				float topLeft = intensity(SAMPLE_TEXTURE2D(_PostSource, sampler_PostSource, center + float2(-stepx, stepy)));
				float bottomLeft = intensity(SAMPLE_TEXTURE2D(_PostSource, sampler_PostSource, center + float2(-stepx, -stepy)));
				float topRight = intensity(SAMPLE_TEXTURE2D(_PostSource, sampler_PostSource, center + float2(stepx, stepy)));
				float bottomRight = intensity(SAMPLE_TEXTURE2D(_PostSource, sampler_PostSource, center + float2(stepx, -stepy)));

				// Roberts Operator
				//X = -1   0      Y = 0  -1
				//     0   1          1   0

				// Gx = sum(kernelX[i][j]*image[i][j])
				float Gx = -1.0 * topLeft + 1.0 * bottomRight;

				// Gy = sum(kernelY[i][j]*image[i][j]);
				float Gy = -1.0 * topRight + 1.0 * bottomLeft;


				float sobelGradient = sqrt((Gx * Gx) + (Gy * Gy));
				return sobelGradient;
			}
					   
			half4 Frag(v2f i) : SV_Target//   VaryingsDefault i) : SV_Target
			{
				half4 sceneColor = SAMPLE_TEXTURE2D(_PostSource, sampler_PostSource, i.uv_Main* 1);
				float sobelGradient = sobel(_EdgeWidth / _ScreenParams.x, _EdgeWidth / _ScreenParams.y, i.uv_Main* 1);
				half4 backgroundColor = lerp(sceneColor, _BackgroundColor, _BackgroundFade);
				float3 edgeColor = lerp(backgroundColor.rgb, _EdgeColor.rgb, sobelGradient);
				return float4(edgeColor, 1); //return float4(1,0,0, 1);
			}
			ENDHLSL
		}
		//END v0.3
	}
}