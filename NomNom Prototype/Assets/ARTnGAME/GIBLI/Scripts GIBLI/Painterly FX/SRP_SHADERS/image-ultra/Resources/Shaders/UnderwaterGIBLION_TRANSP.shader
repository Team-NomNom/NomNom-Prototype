Shader "UltraEffects/UnderwaterGIBLION_TRANSP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_Strength("Strength", Float) = 0.01
		_WaterColour("Water Colour", Color) = (1.0, 1.0, 1.0, 1.0)
		_FogStrength("Fog Strength", Float) = 0.1

        _BumpAmt("Distortion", range(0,128)) = 10
        _BumpPower("Distortion Power", Float) = 1

        depthFade("depth Fade", Vector) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off //ZWrite Off ZTest Always

        Tags { "Queue" = "Transparent+1" "RenderType" = "Opaque" "IgnoreProjector" = "True"}
            ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 uvgrab : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

#if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
#else
                float scale = 1.0;
#endif

                o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
                o.uvgrab.zw = o.vertex.zw;


                return o;
            }

            float4 depthFade;

            uniform sampler2D _MainTex;

			uniform sampler2D _BumpMap;
			uniform float _Strength;

			uniform float4 _WaterColour;
			uniform float _FogStrength;
			uniform sampler2D _CameraDepthTexture;

            float _BumpAmt;
            float4 _BumpMap_ST;
            float4 _MainTex_ST;
            float _BumpPower;
            sampler2D _CameraOpaqueTexture;
            float4 _CameraOpaqueTexture_TexelSize;


            fixed4 frag (v2f i) : SV_Target
            {

                // calculate perturbed coordinates
                half3 bump = UnpackNormal(tex2D(_BumpMap, i.uv* _BumpMap_ST.xy + _BumpMap_ST.zw)).rgb; // we could optimize this by just reading the x & y without reconstructing the Z
                float2 offset = bump * _BumpAmt * _CameraOpaqueTexture_TexelSize.xy;

                i.uvgrab.xy = offset * i.uvgrab.z * 100 * _BumpPower + i.uvgrab.xy;
                half4 colA = tex2Dproj(_CameraOpaqueTexture, UNITY_PROJ_COORD(i.uvgrab));
                //return float4(colA.rgb*3,1);

				half3 normal = UnpackNormal(tex2D(_BumpMap, (i.uv * _BumpMap_ST.xy + _BumpMap_ST.zw + _Time.x) % 1.0));

				float2 uv = i.uv + normal * _Strength;

                fixed4 col = tex2D(_MainTex, uv* _MainTex_ST.xy + _MainTex_ST.zw);

				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uv));
				depth = Linear01Depth(depth);

				col = lerp(col+pow(colA, depthFade.x)* depthFade.y, _WaterColour, depth * _FogStrength);

                return col;
            }
            ENDCG
        }
    }
}
