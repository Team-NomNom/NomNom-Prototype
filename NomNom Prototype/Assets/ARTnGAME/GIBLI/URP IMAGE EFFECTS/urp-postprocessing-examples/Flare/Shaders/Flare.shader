Shader "Hidden/Toguchi/PostProcessing/Flare"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

    TEXTURE2D_X(_MainTex);

    float4 _FlareVector;
    float4 _FlareColor;
    float4 _ParaVector;
    float4 _ParaColor;

    half3 ApplyFlare(half3 color, float2 screenPos)
    {
        float2 flarePos = _FlareVector.xy;
        float2 paraPos = _ParaVector.xy;

        float flare = 1.0 - clamp(length(flarePos - screenPos) * _FlareVector.z, 0, 1);
        float para = 1.0 - clamp(length(paraPos - screenPos) * _ParaVector.z, 0, 1);

        color = color * lerp(float3(1, 1, 1), _ParaColor, para) + lerp(float3(0, 0, 0), _FlareColor, flare);
        return color;
    }

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	struct AttributesB
	{
		float4 positionOS       : POSITION;
		float2 uv               : TEXCOORD0;
	};

	v2f VertA(AttributesB v) {//v2f vert(AttributesDefault v) { 		
		v2f o = (v2f)0;
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
		o.pos = float4(vertexInput.positionCS.xy, 0.0, 1.0);
		float2 uv = v.uv;
#if !UNITY_UV_STARTS_AT_TOP
		uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif
		o.uv = uv;
#if !UNITY_UV_STARTS_AT_TOP
		o.uv = uv.xy;//o.uv1 = uv.xy;
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1 - o.uv.y;//o.uv1.y = 1 - o.uv1.y;
#endif	
		return o;
	}

	struct VaryingsA
	{
		float4 positionCS    : SV_POSITION;
		float2 uv            : TEXCOORD0;
	};

    half4 Frag(VaryingsA input) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp , input.uv);
        color.rgb = ApplyFlare(color.rgb, (input.uv - 0.5) * 2.0);

        return color;
    }
ENDHLSL
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Flare"

            HLSLPROGRAM
                #pragma vertex VertA
                #pragma fragment Frag
            ENDHLSL
        }
    }
}