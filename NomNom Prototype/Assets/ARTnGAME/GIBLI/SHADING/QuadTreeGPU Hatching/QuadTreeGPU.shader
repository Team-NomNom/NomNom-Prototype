//https://www.shadertoy.com/view/wslcWf //MIT LICENSE
Shader "Unlit/QuadTree GPU"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		lineThickness("lineThickness", Float) = 1
			treeSpread("treeSpread", Float) = 1
			hatchingA("hatchingA", Float) = 1
			blackenA("blackenA", Float) = 1
			colorSmoothX("colorSmoothX", Float) = 1
				colorSmoothY("colorSmoothY", Float) = 1
			addOriginal("addOriginal", Float) = 0
			brightness("brightness", Float) = 1
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				float lineThickness;
		float treeSpread;
		float hatchingA;
		float blackenA;
		float colorSmoothX;
		float colorSmoothY;
		float addOriginal;
		float brightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;

			
			// the number of divisions at the start
			#define MIN_DIVISIONS 4.0

			// the numer of possible quad divisions
			#define MAX_ITERATIONS 6

			// the number of samples picked fter each quad division
			#define SAMPLES_PER_ITERATION 30
			#define F_SAMPLES_PER_ITERATION 30.

			// useless, kept it for reference for a personal usage 
			#define MAX_SAMPLES 200

			// threshold min, max given the mouse.x
			#define THRESHOLD_MIN 0.0001
			#define THRESHOLD_MAX 0.01

			// taken from http://glslsandbox.com/e#41197.0
			float2 hash22(float2 p) {
				float n = sin(dot(p, float2(41, 289)));
				return frac(float2(262144, 32768)*n);
			}
			// Computes the color variation on a quad division of the space
			// Basically, this method takes n random samples in a given quad, compute the average 
			// of each color component of the samples.
			// Then, it computes the variance of the samples
			// This is the way I thought for computing the color variation, there might be others,
			// and there must be better ones
			float4 quadColorVariation(in float2 center, in float size) {
				// this array will store the grayscale of the samples
				float3 samplesBuffer[SAMPLES_PER_ITERATION];

				// the average of the color components
				float3 avg = float3(0,0,0);

				// we sample the current space by picking pseudo random samples in it 
				for (int i = 0; i < SAMPLES_PER_ITERATION; i++) {
					float fi = float(i);
					// pick a random 2d point using the center of the active quad as input
					// this ensures that for every point belonging to the active quad, we pick the same samples
					float2 r = hash22(center.xy + float2(fi, 0.0)) - 0.5;
					float3 sp = tex2D(_MainTex, center + r * size).rgb;									//////////texture(iChannel0, center + r * size).rgb;
					avg += sp;
					samplesBuffer[i] = sp;
				}

				avg /= F_SAMPLES_PER_ITERATION;

				// estimate the color variation on the active quad by computing the variance
				float3 var = float3(0, 0, 0);
				for (int i = 0; i < SAMPLES_PER_ITERATION; i++) {
					var += pow(samplesBuffer[i], float3(2 * hatchingA,0,0));////////////////////// CHECK
				}
				var /= F_SAMPLES_PER_ITERATION;
				var -= pow(avg, float3(2, 0, 0));///////////////////// CHECK

				return float4(avg, (var.x + var.y + var.z) / 3.0);
			}








			// ---------------------------------------- VORONOI ----------------------------------------
			//VORONOI
			//https://www.shadertoy.com/view/ldl3W8
			// The MIT License
			#define ANIMATE
			float2 hash2(float2 p)
			{
				// texture based white noise
				return tex2D(_MainTex, float2((p + 0.5) / 256.0)).xy; //textureLod(iChannel0, (p + 0.5) / 256.0, 0.0).xy;

				// procedural white noise	
				//return fract(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
			}

			float3 voronoi(in float2 x)
			{
				float2 n = floor(x);
				float2 f = frac(x);

				//----------------------------------
				// first pass: regular voronoi
				//----------------------------------
				float2 mg, mr;

				float md = 8.0;
				for (int j = -1; j <= 1; j++)
					for (int i = -1; i <= 1; i++)
					{
						float2 g = float2(float(i), float(j));
						float2 o = hash2(n + g);
#ifdef ANIMATE
						o = 0.5 + 0.5*sin(_Time.y + 6.2831*o);
#endif	
						float2 r = g + o - f;
						float d = dot(r, r);

						if (d < md)
						{
							md = d;
							mr = r;
							mg = g;
						}
					}

				//----------------------------------
				// second pass: distance to borders
				//----------------------------------
				md = 8.0;
				for (int j = -2; j <= 2; j++)
					for (int i = -2; i <= 2; i++)
					{
						float2 g = mg + float2(float(i), float(j));
						float2 o = hash2(n + g);
#ifdef ANIMATE
						o = 0.5 + 0.5*sin(_Time.y + 6.2831*o);
#endif	
						float2 r = g + o - f;

						if (dot(mr - r, mr - r) > 0.00001)
							md = min(md, dot(0.5*(mr + r), normalize(r - mr)));
					}

				return float3(md, mr);
			}
			// ---------------------------------------- END VORONOI ----------------------------------------





            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {

				//RADIAL SHEAR
				//i.uv.x = cos(i.uv.y);
				//i.uv.y = sin(i.uv.x);
				//i.uv.x = cos(i.uv.x*10.5 - 0.5);
				//i.uv.y = sin(i.uv.y*10.5 - 0.5);
				float radius =1.85;
				//i.uv.x = i.uv.x*radius * cos(i.uv.x-0.5);
				//i.uv.y = i.uv.y*radius * sin(i.uv.y - 0.5);
				//i.uv.x = i.uv.y*radius * cos(i.uv.x);
				//i.uv.y = i.uv.x*radius * sin(i.uv.x);
				radius = -1.5;
				//	i.uv.x = i.uv.x*radius * cos(i.uv.x );
				//	i.uv.y = i.uv.y*radius * sin(i.uv.x );
				/*i.uv.x = i.uv.x * 2 - 1;
				i.uv.y = i.uv.y * 2 - 1;
				i.uv.x *= radius*cos(i.uv.x);
				i.uv.y *= radius*sin(i.uv.y);
				i.uv.x = (i.uv.x + 1) / 2 ;
				i.uv.y = (i.uv.y + 1) / 2 ;*/
				float swirlradius = 2.08;
				float angleOfswirl = 1 * 3.14;
				swirlradius = 1.08;
				angleOfswirl = 2 * 3.14;
				float2 centerOfswirl = float2(0,0);
				float2 uvSwirling = i.uv - centerOfswirl;
				float distance = length(i.uv * float2(1, 1) - float2(0.45,0.45));
				float tan = smoothstep(swirlradius, 0, distance) * angleOfswirl + atan2(i.uv.y, i.uv.x);
				// smoothstep(swirlradius, 0.4, distance) * angleOfswirl + atan2(i.uv.y, i.uv.x);
				float distanceA = length(i.uv);// +distance;
				float2 finalUVSwirl = centerOfswirl + float2(cos(tan)*distanceA, sin(tan)*distanceA);
				//if (i.uv.x%0.2 < 0.05){// && i.uv.y%0.2 < 0.01) {
					//i.uv.x = finalUVSwirl.x;// %0.2;
					//i.uv.y = finalUVSwirl.y;// %0.2;
				//}
				if (i.uv.x%0.2 < 0.05){// && i.uv.y%0.2 < 0.01) {
					//i.uv.x = finalUVSwirl.x;// %0.2;
					//i.uv.y = finalUVSwirl.y;// %0.2;
				}
				else {
					//i.uv.x = finalUVSwirl.x%0.2;
					//i.uv.y = finalUVSwirl.y%0.2;
				}


			










                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
					   

				// Normalized pixel coordinates (from 0 to 1)
				float2 uv = i.uv;// fragCoord / iResolution.xy;///////////////////// CHECK

				float threshold = lerp(THRESHOLD_MIN, THRESHOLD_MAX, 1/treeSpread);// iMouse.x / iResolution.x);///////////////////// CHECK

				// number of space divisions
				float divs = MIN_DIVISIONS;

				// the center of the active quad - we initialze with 2 divisions
				float2 quadCenter = (floor(uv * divs) + 0.5) / divs;
				float quadSize = 1. / divs; // the length of a side of the active quad

				// we store average and variance here
				float4 quadInfos = float4(0,0,0,0);

				for (int i = 0; i < MAX_ITERATIONS; i++) {
					quadInfos = quadColorVariation(quadCenter, quadSize);

					// if the variance is lower than the threshold, current quad is outputted
					if (quadInfos.w < threshold) break;

					// otherwise, we divide the space again
					divs *= 2.0 * blackenA;
					quadCenter = (floor(uv * divs) + 0.5) / divs;
					quadSize /= 2.0;
				}




				



				float4 color = tex2D(_MainTex, uv); //texture(iChannel0, uv);

				// the coordinates of the quad
				float2 nUv = frac(uv * divs);

				// we create lines from the uv coordinates
				float2 lWidth = float2(uv.x/1500, uv.y/1500)*lineThickness;//  i.uv;/////  float2(1. / iResolution.x, 1. / iResolution.y);///////////////////// CHECK
				float2 uvAbs = abs(nUv - 0.5);
				float s = step(0.5 - uvAbs.x, lWidth.x*divs) + step(0.5 - uvAbs.y, lWidth.y*divs);

				// we smooth the color between average and texture initial
				color.rgb = lerp(color.rgb, quadInfos.rgb, uv.x);

				// we smooth the lines over the x axis
				s*= pow(1. - uv.x, 4.0 * colorSmoothX);
				s *= pow(1. - uv.y, 4.0 * colorSmoothY);
				// for black lines, we just subtract
				color -= s;

				// Output to screen
				//fragColor = color;

				col = col * addOriginal + color * brightness;





				//VORONOI
			//https://www.shadertoy.com/view/ldl3W8
			// The MIT License
			// Copyright © 2013 Inigo Quilez
			// https://www.youtube.com/c/InigoQuilez
			// https://iquilezles.org/
			// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
			/*to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
			and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
			The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
			THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
			FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
			LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
			IN THE SOFTWARE.*/
				
				float2 pA = uv.xy;
				float3 c = voronoi(188.0*pA);
				// isolines
				float3 colVOIRONOI_A = c.x*(0.5 + 0.5*sin(64.0*c.x))*float3(1.0,1,1);
				// borders	
				colVOIRONOI_A = lerp(float3(1.0, 0.6, 0.0), colVOIRONOI_A, smoothstep(0.04, 0.07, c.x));
				// feature points
				float dd = length(c.yz);
				colVOIRONOI_A = lerp(float3(1.0, 0.6, 0.1), colVOIRONOI_A, smoothstep(0.0, 0.12, dd));
				colVOIRONOI_A += float3(1.0, 0.6, 0.1)*(1.0 - smoothstep(0.0, 0.04, dd));
				float4 colorV = tex2D(_MainTex, uv + 0.005*colVOIRONOI_A.rg);
				if (colVOIRONOI_A.r < 0.5) {
					if (colorV.r > 0.12 && colorV.g > 0.12 && colorV.b > 0.15) {
						col.rgb = colorV.rgb + 1*colorV.rgb* colVOIRONOI_A.rgb * 1;//
					}
					else {
						col.rgb = colorV.rgb + 0.7*colorV.rgb* colVOIRONOI_A.rgb * 2;//
					}
				}




                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

//
//// @license MIT
//// @author ciphrd
//// 
//// This algorithm is sort of a probabilistic quad tree construction where quad divisions are 
//// added if the color variation (variance) in a quad is too important.
//// 
//// The color variation is computed by taking n samples within the quad, and then we compute the 
//// variance on each color component of the samples.
////
////
//// Limitations
//// 
//// If a certain area, which is large, has a small section of it being detailed while the rest
//// is pretty much linear, divisions might not be added. Because this algorithm picks random
//// points in the quad, the small detailed section has little to no infuence on the overall
//// variations of the colors in the quad.
//// You can observe this behavior on the Google logo when it appears.
////
//
//
//// the number of divisions at the start
//#define MIN_DIVISIONS 4.0
//
//// the numer of possible quad divisions
//#define MAX_ITERATIONS 6
//
//// the number of samples picked fter each quad division
//#define SAMPLES_PER_ITERATION 30
//#define F_SAMPLES_PER_ITERATION 30.
//
//// useless, kept it for reference for a personal usage 
//#define MAX_SAMPLES 200
//
//// threshold min, max given the mouse.x
//#define THRESHOLD_MIN 0.0001
//#define THRESHOLD_MAX 0.01
//
//
//// taken from http://glslsandbox.com/e#41197.0
//vec2 hash22(vec2 p) {
//	float n = sin(dot(p, vec2(41, 289)));
//	return fract(vec2(262144, 32768)*n);
//}
//
//
//// Computes the color variation on a quad division of the space
//// Basically, this method takes n random samples in a given quad, compute the average 
//// of each color component of the samples.
//// Then, it computes the variance of the samples
//// This is the way I thought for computing the color variation, there might be others,
//// and there must be better ones
//vec4 quadColorVariation(in vec2 center, in float size) {
//	// this array will store the grayscale of the samples
//	vec3 samplesBuffer[SAMPLES_PER_ITERATION];
//
//	// the average of the color components
//	vec3 avg = vec3(0);
//
//	// we sample the current space by picking pseudo random samples in it 
//	for (int i = 0; i < SAMPLES_PER_ITERATION; i++) {
//		float fi = float(i);
//		// pick a random 2d point using the center of the active quad as input
//		// this ensures that for every point belonging to the active quad, we pick the same samples
//		vec2 r = hash22(center.xy + vec2(fi, 0.0)) - 0.5;
//		vec3 sp = texture(iChannel0, center + r * size).rgb;
//		avg += sp;
//		samplesBuffer[i] = sp;
//	}
//
//	avg /= F_SAMPLES_PER_ITERATION;
//
//	// estimate the color variation on the active quad by computing the variance
//	vec3 var = vec3(0);
//	for (int i = 0; i < SAMPLES_PER_ITERATION; i++) {
//		var += pow(samplesBuffer[i], vec3(2.0));
//	}
//	var /= F_SAMPLES_PER_ITERATION;
//	var -= pow(avg, vec3(2.0));
//
//	return vec4(avg, (var.x + var.y + var.z) / 3.0);
//}

//
//void mainImage(out vec4 fragColor, in vec2 fragCoord)
//{
//	// Normalized pixel coordinates (from 0 to 1)
//	vec2 uv = fragCoord / iResolution.xy;
//
//	float threshold = mix(THRESHOLD_MIN, THRESHOLD_MAX, iMouse.x / iResolution.x);
//
//	// number of space divisions
//	float divs = MIN_DIVISIONS;
//
//	// the center of the active quad - we initialze with 2 divisions
//	vec2 quadCenter = (floor(uv * divs) + 0.5) / divs;
//	float quadSize = 1. / divs; // the length of a side of the active quad
//
//	// we store average and variance here
//	vec4 quadInfos = vec4(0);
//
//	for (int i = 0; i < MAX_ITERATIONS; i++) {
//		quadInfos = quadColorVariation(quadCenter, quadSize);
//
//		// if the variance is lower than the threshold, current quad is outputted
//		if (quadInfos.w < threshold) break;
//
//		// otherwise, we divide the space again
//		divs *= 2.0;
//		quadCenter = (floor(uv * divs) + 0.5) / divs;
//		quadSize /= 2.0;
//	}
//
//
//
//	vec4 color = texture(iChannel0, uv);
//
//	// the coordinates of the quad
//	vec2 nUv = fract(uv * divs);
//
//	// we create lines from the uv coordinates
//	vec2 lWidth = vec2(1. / iResolution.x, 1. / iResolution.y);
//	vec2 uvAbs = abs(nUv - 0.5);
//	float s = step(0.5 - uvAbs.x, lWidth.x*divs) + step(0.5 - uvAbs.y, lWidth.y*divs);
//
//	// we smooth the color between average and texture initial
//	//color.rgb = mix(color.rgb, quadInfos.rgb, uv.x);
//
//	// we smooth the lines over the x axis
//	//s*= pow(1. - uv.x, 4.0);
//
//	// for black lines, we just subtract
//	color -= s;
//
//	// Output to screen
//	fragColor = color;
//}