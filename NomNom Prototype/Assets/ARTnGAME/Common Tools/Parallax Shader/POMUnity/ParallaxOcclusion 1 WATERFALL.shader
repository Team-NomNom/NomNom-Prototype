Shader "Custom/ParallaxOcclusion WATERFALL" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}

	_ErodeTex("_ErodeTex (RGB)", 2D) = "white" {}
	_ErodeTexA("_ErodeTex (RGB)", 2D) = "white" {}
	_ErodeTexB("_ErodeTex (RGB)", 2D) = "white" {}
	_Speed("_Speed", Float) = (1,1,1,1)
		_Intensity("_Intensity", Float) = (1,1,1,1)
		_Size("_Size", Float) = (1,1,1,1)

		_BumpMap ("Normal map (RGB)", 2D) = "bump" {}
		_BumpScale ("Bump scale", Range(0,1)) = 1
		_ParallaxMap ("Height map (R)", 2D) = "white" {}
		_Parallax ("Height scale", Range(0,1)) = 0.05
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_ParallaxMinSamples ("Parallax min samples", Range(2,100)) = 4
		_ParallaxMaxSamples ("Parallax max samples", Range(2,100)) = 20
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert

		#pragma target 3.0
		
		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _ParallaxMap;

		sampler2D _ErodeTex; sampler2D _ErodeTexA; sampler2D _ErodeTexB;
		float4 _Speed;
		float4 _Intensity;
		float4 _Size;

		struct Input {
			float2 texcoord;
			float3 eye;
			float sampleRatio;
		};

		half _Glossiness;
		half _Metallic;
		half _BumpScale;
		half _Parallax;
		fixed4 _Color;
		uint _ParallaxMinSamples;
		uint _ParallaxMaxSamples;
		
		#include<ParallaxOcclusion.cginc>
		
		void vert(inout appdata_full IN, out Input OUT) {

			float4 ErodeTex = tex2Dlod(_ErodeTex, float4(IN.texcoord.x, IN.texcoord.y,0,0) + float4(-0.75*_Time.y*_Speed.x, 0,0,0));
			float4 ErodeTex1 = tex2Dlod(_ErodeTex, float4(IN.texcoord.x, IN.texcoord.y + (0.3*cos(_Time.y*_Speed.y)),0,0) + float4(-0.45*_Time.y*_Speed.x, 0.1*_Time.y*_Speed.y,0,0));
			float4 ErodeTexA = tex2Dlod(_ErodeTexA, float4(IN.texcoord.x, IN.texcoord.y,0,0) + float4(-0.65*_Time.y*_Speed.x, 0,0,0));
			float4 ErodeTexB = tex2Dlod(_ErodeTexB, float4(IN.texcoord.x, IN.texcoord.y,0,0) + float4(-0.55*_Time.y*_Speed.x, 0,0,0));

			//IN.vertex.x += ErodeTex.r*0.944*_Speed.z;
			//IN.vertex.z += ErodeTex.r*0.944*_Speed.z;
			//IN.vertex.x += pow(ErodeTex.r,5)*0.944*_Speed.z;
			float AAA = 1 * 0.1*pow(3.9*(0.4* pow(ErodeTex.r, 2) + 0.54 * pow(ErodeTex1.r, 2)
				+ 0.54 *  pow(ErodeTexA.r, 1) + 0.4 *  pow(ErodeTexB.r, 1)) * (ErodeTex.r*0.6 + 0.1), 1);
			IN.vertex.y += AAA * _Speed.z;
			//IN.vertex.z += AAA * _Speed.z;

			parallax_vert( IN.vertex, IN.normal, IN.tangent, OUT.eye, OUT.sampleRatio );
			OUT.texcoord = IN.texcoord;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
		
			float4 ErodeTex = tex2D(_ErodeTex, float2(IN.texcoord.x, IN.texcoord.y*_Size.y) + float2(-0.85*_Time.y*_Speed.x,0));
			float4 ErodeTex1 = tex2D(_ErodeTex, float2(IN.texcoord.x, IN.texcoord.y+(0.3*cos(_Time.y*_Speed.y))) + float2(-0.55*_Time.y*_Speed.x, 0.1*_Time.y*_Speed.y));

			float4 ErodeTexA = tex2D(_ErodeTexA, float2(IN.texcoord.x, 3*IN.texcoord.y) + float2(-0.75*_Time.y*_Speed.x, 0));
			float4 ErodeTexA1 = tex2D(_ErodeTexA, float2(IN.texcoord.x, 3 * IN.texcoord.y*0.5) + float2(-0.75*_Time.y*_Speed.x*0.56, 0));
			float4 ErodeTexA2 = tex2D(_ErodeTexA, float2(IN.texcoord.x, 3 * IN.texcoord.y*0.5001) + float2(-0.75*_Time.y*_Speed.x*0.55999, 0));

			float4 ErodeTexB = tex2D(_ErodeTexB, float2(IN.texcoord.x, IN.texcoord.y) + float2(-0.65*_Time.y*_Speed.x, 0));

			float2 offset = parallax_offset (_Parallax, IN.eye, IN.sampleRatio, IN.texcoord, 
			_ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples );
			float2 uv = IN.texcoord + offset;
			fixed4 c = tex2D (_MainTex, uv) * _Color;
			o.Albedo = c.rgb;

			/*o.Albedo = c.rgb + 0.9*(0.4* pow(ErodeTex.r,2) + 0.54 * pow(ErodeTex1.r, 2)
				+ 0.54 *  pow(ErodeTexA.r,1) + 0.4 *  pow(ErodeTexB.r, 1));*/
			o.Albedo = c.rgb*0.5*_Intensity.x + _Intensity.y * 0.4*pow(3.9*(0.4* pow(ErodeTex.r, 2) + 0.54 * pow(ErodeTex1.r, 2)
				//+ 0.54 *  pow(ErodeTexA.r, 1) + 0.4 *  pow(ErodeTexB.r, 1)) * (ErodeTex.r*0.6+0.1),3 * _Intensity.z);
				+ 0.64 *  pow(ErodeTexA.r, 1.1) + 0.4 *  pow(ErodeTexB.r, 1)) * (ErodeTex.r*0.6 + 0.1), 3 * _Intensity.z)
				+ 0.84 *  pow(ErodeTexA1.r, 1.12) * float3(0.99,1,1) 
				+ 0.74 *  pow(ErodeTexA2.r, 1.12) * float3(0.69, 0.91, 0.98); //caustics bottom;

			o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		
		ENDCG
	}
	FallBack "Diffuse"
}
