Shader "Unlit/testForwardPlusDefferedMapsGIBLI"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        choice("choice", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

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

            sampler2D _DeferredPass_Albedo_Texture;
            sampler2D _DeferredPass_Specular_Texture;
            sampler2D _DeferredPass_WorldPosition_Texture;
            sampler2D _DeferredPass_DepthNormals_Texture;
            sampler2D _CameraDepthTexture;

            sampler2D _GBuffer0;
            sampler2D _GBuffer1;
            sampler2D _GBuffer2;
            sampler2D _GrassBendingRT;
            sampler2D _MaxZMaskTexture;

            int choice;

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
                // sample the texture
                fixed4 col = tex2D(_DeferredPass_DepthNormals_Texture, i.uv);

                if (choice == 0) {
                    col = tex2D(_DeferredPass_Albedo_Texture, i.uv);
                }
                if (choice == 1) {
                    col = tex2D(_DeferredPass_Specular_Texture, i.uv);
                }
                if (choice == 2) {
                    col = tex2D(_DeferredPass_WorldPosition_Texture, i.uv);
                }
                if (choice == 3) {
                    col = tex2D(_DeferredPass_DepthNormals_Texture, i.uv);
                }
                if (choice == 4) {
                    col = tex2D(_CameraDepthTexture, i.uv);
                }
                if (choice == 5) {
                    col = tex2D(_DeferredPass_DepthNormals_Texture, i.uv).a;
                }

                if (choice == 6) {
                    col = tex2D(_GBuffer0, i.uv);
                }
                if (choice == 7) {
                    col = tex2D(_GBuffer1, i.uv);
                }
                if (choice == 8) {
                    col = tex2D(_GBuffer2, i.uv);
                }
                 if (choice == 9) {
                                    col = tex2D(_MaxZMaskTexture, i.uv);
                                }
                 if (choice == 10) {
                     col = 1-pow(tex2D(_GrassBendingRT, i.uv).r,1)/1;
                 }

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col*2;
            }
            ENDCG
        }
    }
}
