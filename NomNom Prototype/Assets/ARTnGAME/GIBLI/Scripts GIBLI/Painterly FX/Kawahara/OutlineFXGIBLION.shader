Shader "ARTnGAME/GIBLION/OutlinePaintingFX"
{
    Properties
    {
		//2.water color
        _MainTex ("Texture", 2D) = "white" {}
		_KernelSize("Kernel Size (N)", Int) = 17

			//////1. OUTLINE
			OutlineThickness("OutlineThickness",Float) = 3
				DepthSensitivity("DepthSensitivity", Float) = 0.5
				NormalsSensitivity("NormalsSensitivity", Float) = 0.44
				ColorSensitivity("ColorSensitivity", Float) = 0.1
				OutlineColor("OutlineColor", Vector) = (0, 0, 0, 0)
				OutlineControls("OutlineControls", Vector) = (0, 0, 0, 0)
			TexelScale("TexelScale", Float) = 0.1

			////////// 3. "UltraEffects/UnderwaterGIBLION"
			_BumpMap("Normal Map", 2D) = "bump" {}
			_WaterColourStrength("Strength", Float) = 0.01
			_WaterColour("Water Colour", Color) = (1.0, 1.0, 1.0, 1.0)
			_FogStrength("Fog Strength", Float) = 0.1
			UnderwaterFactor("Underwater Factor", Float) = 0

			////////// 4. "SMO/Complete/PixelSNES"
			SNESFactor("SNES Factor", Vector) = (1.0, 0.0, 0.0, 0.0)

			////////5. "SMO/Complete/Neon"
			NeonFactor("Neon Factor", Vector) = (1.0, 0.0, 0.0, 0.0)

			//////6. shaders-pmd - Hatching
			_SmudgeStrengthHatching("Hatching Smudge Strength", Float) = 0.002
			_DrawingStrengthHatching("Hatching Drawing Strength", Float) = 0.35
			_HatchingTex("Hatching Texture", 2D) = "white" {}
			_TilingOffsetHatching("Hatching Tiling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
			_HatchingSpeed("Hatching Speed", Float) = 2.5
			HatchFactor("Hatching Factor",  Vector) = (1.0, 0.0, 0.0, 0.0)

			//////7.shaders-retro - GameBoyRamp
			_GameboyRampTex("Gameboy Ramp Tex", 2D) = "white" {}
			GameboyFactor("Gameboy Factor",  Vector) = (1.0, 0.0, 0.0, 0.0)

			///DEPTH
			worldPosScaler("world Position Scaler",  Vector) = (1.0, 1.0, 1.0, 1.0)

			////8.KNITTING
			_KnitwearMap("_Knitwear Map", 2D) = "white" {}
			_KnitwearDivision("_KnitwearDivision", Float) = 1
			_KnitwearAspect("_KnitwearAspect", Float) = 1
			_KnitwearShear("_KnitwearShear", Float) = 1
			_KnitwearDistortionStrength("_KnitwearDistortionStrength", Float) = 1
			KnitFactor("Knit Factor",  Vector) = (1.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        // No culling or depth
        //Cull Off //ZWrite Off ZTest Always



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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			//2. water color
			int _KernelSize;
			float4 worldPosScaler;

			//////7.shaders-retro - GameBoyRamp
			sampler2D _GameboyRampTex;
			float4 GameboyFactor;

			//////6. shaders-pmd - Hatching
			float _SmudgeStrengthHatching;
			float _DrawingStrengthHatching;
			sampler2D _HatchingTex;
			float4 _TilingOffsetHatching;
			float _HatchingSpeed;
			float4 HatchFactor;
			
			////////5. "SMO/Complete/Neon"
			float4 NeonFactor;
			float3 sobel(float2 uv)
			{
				float x = 0;
				float y = 0;

				float2 texelSize = _MainTex_TexelSize;

				x += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, 0)) * -2.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) * -1.0;

				x += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) *  1.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, 0)) *  2.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				y += tex2D(_MainTex, uv + float2(0, -texelSize.y)) * -2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) * -1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) *  1.0;
				y += tex2D(_MainTex, uv + float2(0, texelSize.y)) *  2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				return sqrt(x * x + y * y);
			}
			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 rgb2hsv(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
				float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);

				float d = q.x - min(q.w, q.y);
				float e = 1.0e-10;
				return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 hsv2rgb(float3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}
			///END5.NEON
			

			
			////////// 4. "SMO/Complete/PixelSNES"
			float4  _MainTex_ST;
			static const float EPSILON = 1e-10;
			float4 SNESFactor;

			////////// 3. "UltraEffects/UnderwaterGIBLION"
			uniform sampler2D _BumpMap;
			uniform float _WaterColourStrength;
			uniform float4 _WaterColour;
			uniform float _FogStrength;
			float UnderwaterFactor;

			///////////////////////// 1. OUTLINE /////////////////////////////////
			float TexelScale ;
			float OutlineThickness ;
			float DepthSensitivity ;
			float NormalsSensitivity ;
			float ColorSensitivity ;
			float4 OutlineColor ;
			float4 OutlineControls ;
			sampler2D _CameraColorTexture;//TEXTURE2D(_CameraColorTexture);
			//SAMPLER(sampler_CameraColorTexture);
			float4 _CameraColorTexture_TexelSize;

			sampler2D _CameraDepthTexture;
			//TEXTURE2D(_CameraDepthTexture);
			//SAMPLER(sampler_CameraDepthTexture);
			float4 sampler_CameraDepthTexture;

			sampler2D _CameraDepthNormalsTextureGIBLI;
			//TEXTURE2D(_CameraDepthNormalsTextureGIBLI);
			//SAMPLER(sampler_CameraDepthNormalsTextureGIBLI);
		

			float3 DecodeNormal(float4 enc)
			{
				float kScale = 1.7777;
				float3 nn = enc.xyz*float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
				float g = 2.0 / dot(nn.xyz, nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}

			float4 Outline(float2 UV, float OutlineThickness, float DepthSensitivity, float NormalsSensitivity, float ColorSensitivity, float4 OutlineColor, float4 OutlineControls)
			{
				float halfScaleFloor = floor(OutlineThickness * 0.5);
				float halfScaleCeil = ceil(OutlineThickness * 0.5);
				float2 Texel = (1.0) / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);
				Texel = (1.0*TexelScale) * float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);///NEW1

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
						/*depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTextureGIBLI, sampler_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);*/

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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

					float4 original = tex2D(_MainTex, uvSamples[0]); //SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeDepth, edgeDepth, edgeDepth, 1);// ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / edgeDepth;//
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

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
					}

					// Depth
					float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
					float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
					float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
					float depthThreshold = (1 / DepthSensitivity) * depthSamples[0];
					edgeDepth = edgeDepth > depthThreshold ? 1 : 0;


					//REGULATE BY DEPTH
					Texel = (depthThreshold + 0.5*OutlineControls.y) / float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
					uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
					uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
					uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
					uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);
					for (int i = 0; i < 4; i++)
					{

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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

					float4 original = tex2D(_MainTex, uvSamples[0]); //SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeDepth, edgeDepth, edgeDepth, 1);// ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / edgeDepth;//
					//Out = float4(edgeNormalA1, edgeNormalA1, edgeNormalA1, 1);//
					//Out = float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth;//
					//	Out = original* (1- OutlineControls.y) + (OutlineControls.y)*original * float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / (edgeDepth + 0.1*OutlineControls.z);//
				}
				else if (OutlineControls.x == 3) {

					uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
					uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
					uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
					uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

					for (int i = 0; i < 4; i++)
					{

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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
						+ dot(normalFiniteDifference1, depthFiniteDifference1*(1 - OutlineControls.y) + normalFiniteDifference1 * (OutlineControls.y))
					);
					float edgeNormalA = edgeNormal > (1 / NormalsSensitivity) ? 1 : 0;

					//float edgeNormalA1 = edgeNormal > (1 / edgeDepth) ? 1 : 0;

					// Color
					float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
					float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
					float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
					edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;

					float edge = max(edgeDepth, max(edgeNormalA, edgeColor));

					float4 original = tex2D(_MainTex, uvSamples[0]); // SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
				}
				else {

					uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
					uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
					uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
					uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

					for (int i = 0; i < 4; i++)
					{

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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

					float4 original = tex2D(_MainTex, uvSamples[0]); //SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					//return float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth*edgeNormalA*edgeColor;//
					//return float4(edgeNormalA, edgeNormalA, edgeNormalA, 1);//
					//Out = float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth;//


					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
				}
			}
			//////////////////////// END OUTLINE //////////////////////////////


			struct region
			{
				float3 mean;
				float variance;
			};

			region calcRegion(int2 lower, int2 upper, int samples, float2 uv)
			{
				region r;
				float3 sum = 0.0;
				float3 squareSum = 0.0;

				for (int x = lower.x; x <= upper.x; ++x)
				{
					for (int y = lower.y; y <= upper.y; ++y)
					{
						float2 offset = float2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y);
						float3 tex = tex2D(_MainTex, uv + offset);


						/////OUTLINE
						/*float4 outliner = Outline(uv + offset, OutlineThickness,
							DepthSensitivity, NormalsSensitivity,
							ColorSensitivity, OutlineColor,
							OutlineControls);
						tex =outliner;*/


						sum += tex;
						squareSum += tex * tex;
					}
				}

				r.mean = sum / samples;
				float3 variance = abs((squareSum / samples) - (r.mean * r.mean));
				r.variance = length(variance);

				return r;
			}

			//DEPTH WORLD SPACE
			//https://forum.unity.com/threads/upgrading-shader.819558/
			//float4x4 _InvProjMatrix;//URP
			///WORLD SPACE
			float3 DepthToWorld(float2 uv, float depth) {
				float z = (1 - depth) * 2.0 - 1.0;
				float4 clipSpacePos = float4(uv * 2.0 - 1.0, z, 1.0);
				float4 viewSpacePos = mul(unity_CameraInvProjection, clipSpacePos);// _CameraInverseProjection, clipSpacePos); unity_CameraInvProjection
				viewSpacePos /= viewSpacePos.w;
				float4 worldSpacePos = mul(unity_ObjectToWorld, viewSpacePos);
				return worldSpacePos.xyz;
			}


			////8.KNITTING
			sampler2D _KnitwearMap;
			float4 KnitFactor;
			half _KnitwearDivision;
			half _KnitwearAspect;
			half _KnitwearShear;
			half _KnitwearDistortionStrength;
			float2 GradientNoiseDir(float2 p)
			{
				p = p % 289;
				float x = (34 * p.x + 1) * p.x % 289 + p.y;
				x = (34 * x + 1) * x % 289;
				x = frac(x / 41) * 2 - 1;
				return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
			}
			float GradientNoise(float2 p)
			{
				float2 ip = floor(p);
				float2 fp = frac(p);
				float d00 = dot(GradientNoiseDir(ip), fp);
				float d01 = dot(GradientNoiseDir(ip + float2(0, 1)), fp - float2(0, 1));
				float d10 = dot(GradientNoiseDir(ip + float2(1, 0)), fp - float2(1, 0));
				float d11 = dot(GradientNoiseDir(ip + float2(1, 1)), fp - float2(1, 1));
				fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
				return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
			}
			void ApplyDistortion(inout float2 uv, half division, half distortion)
			{
				float noise = GradientNoise(uv * division * 0.005);
				noise *= distortion * 50 / division;

				float noiseDetail = GradientNoise(uv * division * 0.04);
				noiseDetail *= distortion * 6.25 / division;

				uv += noise + noiseDetail;
			}
			void KnitwearCoordinate(inout float2 uv, out float2 cell, half division, half shear, half distortion = 0)
			{
				//#if defined(_KNITWEAR_DISTORTION_ON)
								ApplyDistortion(uv, division, distortion);
				//#endif

				float verticalOffset = distance(frac(uv.x), 0.5) * shear;
				uv.y += verticalOffset;

				cell = floor(uv * float2(2.0, 1.0));
				cell += float2(0.5, 0.5);
				cell *= float2(0.5, 1.0);
			}
			





            float4 frag (v2f i) : SV_Target
            {
				float4 texA = tex2D(_MainTex, i.uv);
				//float4 outlinerA = Outline(i.uv, OutlineThickness,
				//	DepthSensitivity, NormalsSensitivity,
				//	ColorSensitivity, OutlineColor,
				//	OutlineControls);

				//return outlinerA;// float4(col, 1.0)*1.5 + outliner * col.rgbb;

				int upper = (_KernelSize - 1) / 2;
				int lower = -upper;

				int samples = (upper + 1) * (upper + 1);

				region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, i.uv);
				region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, i.uv);
				region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, i.uv);
				region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, i.uv);

				fixed3 col = regionA.mean;
				fixed minVar = regionA.variance;

				float testVal;

				testVal = step(regionB.variance, minVar);
				col = lerp(col, regionB.mean, testVal);
				minVar = lerp(minVar, regionB.variance, testVal);

				testVal = step(regionC.variance, minVar);
				col = lerp(col, regionC.mean, testVal);
				minVar = lerp(minVar, regionC.variance, testVal);

				testVal = step(regionD.variance, minVar);
				col = lerp(col, regionD.mean, testVal);


				/// 1. OUTLINE
				/*void Outline_float(float2 UV, float OutlineThickness, 
					float DepthSensitivity, float NormalsSensitivity, 
					float ColorSensitivity, float4 OutlineColor, 
					float4 OutlineControls, out float4 Out)*/				
				float4 outliner = Outline(i.uv, OutlineThickness,
					DepthSensitivity, NormalsSensitivity,
					ColorSensitivity, OutlineColor,
					OutlineControls);


			

				////////// 3. "UltraEffects/UnderwaterGIBLION"
				half3 normal = UnpackNormal(tex2D(_BumpMap, (i.uv + _Time.x) % 1.0));
				float2 uv = i.uv + normal * _WaterColourStrength;
				float4 colC = tex2D(_MainTex, uv);
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uv));
				depth = Linear01Depth(depth);
				colC = lerp(colC, _WaterColour, depth * _FogStrength);
				col += UnderwaterFactor * colC;

				////////// 4. "SMO/Complete/PixelSNES"
				/*	To achieve the thresholding of colour values, integer 	truncation is used.		*/
				int r = (texA.r - EPSILON) * 6;
				int g = (texA.g - EPSILON) * 6;
				int b = (texA.b - EPSILON) * 6;
				/*	Divide by 5, not 6, because we're dividing by the maximum value of each channel integer (which is 6).
					value of each channel integer (which is 6). */
				float4 colD = float4(r / 5.0, g / 5.0, b / 5.0, 1.0);
				col = SNESFactor.x *col + SNESFactor.y * colD + SNESFactor.z * colD*col;


				////////5. "SMO/Complete/Neon"
				float3 s = sobel(i.uv);
				float3 texE = texA;// tex2D(_MainTex, i.uv);

				float3 hsvTex = rgb2hsv(texE);
				hsvTex.y = 1.0;		// Modify saturation.
				hsvTex.z = 1.0;		// Modify lightness/value.
				float3 colE = hsv2rgb(hsvTex) * (s + NeonFactor.w);
				col = NeonFactor.x *col + NeonFactor.y * colE + NeonFactor.z * colE*col;
				//return float4(col * s, 1.0);

				//////6. shaders-pmd - Hatching				
				// _SmudgeStrengthHatching;
				// _DrawingStrengthHatching;
				// _HatchingTex;
				// _TilingOffsetHatching;
				// _HatchingSpeed;
				float scrrenDivide = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
				float2 HatchingUVOffseted = i.uv * scrrenDivide * _TilingOffsetHatching.xy + _TilingOffsetHatching.zw + (0.5 * floor(fmod(_Time.y * _HatchingSpeed,2)));///NEW1
				float2 HatchingTexAUV = float2(0.37, 0.37) * HatchingUVOffseted;
				float4 HatchingTexA = tex2D(_HatchingTex, HatchingTexAUV);
				float4 HatchingTexB = tex2D(_HatchingTex, HatchingUVOffseted);
				float4 HatchingValA = lerp(HatchingTexA, HatchingTexB,0.5);
				float4 HatchingVal = HatchingValA -0.5;
				float2 LerpIn1UV = i.uv + (_SmudgeStrengthHatching*HatchingVal.xy);
				float4 LerpIn1 = tex2D(_MainTex, LerpIn1UV);
				float4 LerpIn2 = LerpIn1 * HatchingVal;
				float4 colF = lerp( LerpIn1, LerpIn2,_DrawingStrengthHatching);
				col = HatchFactor.x *col + HatchFactor.y * colF + HatchFactor.z * colF*col;


			


				//WORLD POS from DEPTH
				//https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/writing-shaders-urp-reconstruct-world-position.html
				float depthA = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
				depthA = Linear01Depth(depthA)* worldPosScaler.y;
				float3 depthPOS = DepthToWorld(i.uv, depthA);// *_WorldSpaceCameraPos;
				// The following part creates the checkerboard effect. Scale is the inverse size of the squares.
				float3 scale = 0.01 * worldPosScaler.x;
				// Scale, mirror and snap the coordinates.
				uint3 worldIntPos = uint3(abs(depthPOS.xyz * scale));
				// Divide the surface into squares. Calculate the color ID value.
				bool white = ((worldIntPos.x) & 1) ^ (worldIntPos.y & 1) ^ (worldIntPos.z & 1);
				// Color the square based on the ID value (black or white).
				half4 colorCHBRD = white ? half4(1, 1, 1, 1) : half4(0, 0, 0, 1);
				//return float4(depthPOS,1);
				//col = col * colorCHBRD;
				//return colorCHBRD;

				//GET LIGHTING
				/*if SHADOWS_SCREEN
					half4 clipPos = TransformWorldToHClip(depthPOS);
					half4 shadowCoord = ComputeScreenPos(clipPos);
				#else
				half4 shadowCoord = TransformWorldToShadowCoord(depthPOS);
				#endif
				Light mainLight = GetMainLight(shadowCoord);*/


				//////7.shaders-retro - GameBoyRamp
				float4 colG = tex2D(_GameboyRampTex, colorCHBRD*worldIntPos);
				col = GameboyFactor.x *col + GameboyFactor.y * colG + GameboyFactor.z * colG * col;


				/////8. KNITTING
				float2 uvsORIGIN = i.uv;
				float2 texCoord = i.uv;
				float2 dtdx = ddx(texCoord);
				float2 dtdy = ddy(texCoord);
				half2 scaleK = _KnitwearDivision / half2(_KnitwearAspect, 1.0);
				uvsORIGIN *= scaleK;
				float2 duvdx = dtdx * scaleK;
				float2 duvdy = dtdy * scaleK;
				KnitwearCoordinate(uvsORIGIN, texCoord, _KnitwearDivision, _KnitwearShear, _KnitwearDistortionStrength);
				texCoord /= scaleK;
				/*	half4 SampleAlbedoAlphaGrad(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap), float2 dpdx, float2 dpdy)
				{
					return SAMPLE_TEXTURE2D_GRAD(albedoAlphaMap, sampler_albedoAlphaMap, uv, dpdx, dpdy);
				}
				half3 SampleKnitwear(float2 uv, float2 dpdx, float2 dpdy)
				{
					return SAMPLE_TEXTURE2D_GRAD(_KnitwearMap, sampler_KnitwearMap, uv, dpdx, dpdy).rgb;
				}*/
				//https://forum.unity.com/threads/how-to-use-samplegrad-in-unity-shader.430857/
				half4 albedoAlpha = tex2Dgrad(_MainTex, texCoord, dtdx, dtdy);//SampleAlbedoAlphaGrad(texCoord, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), dtdx, dtdy);
				//outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
				half3 knitwearColor  = tex2Dgrad(_KnitwearMap, uvsORIGIN, duvdx, duvdy); //SampleKnitwear(uvsORIGIN, duvdx, duvdy);
				//half3 knitwearColor = SampleKnitwear(uvsORIGIN, duvdx, duvdy);
				//outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb * knitwearColor;
				float4 knittingFinal = float4(albedoAlpha.rgb * knitwearColor,1);
				col = KnitFactor.x *col + KnitFactor.y * knittingFinal + KnitFactor.z * knittingFinal * col;


				//2. col = image-ultra - UltraEffects/Painting
				//return float4(col, 1.0);
				return float4(outliner.rgb - texA,1) + float4(col, 1.0);// float4(col, 1.0)*1.5 + outliner * col.rgbb;
            }
            ENDCG
        }




		
			///////////////////////////////////////// PASS 1 /////////////////////////////////////////
		//// GRAPH
		Pass //6 BLIT
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
					#include "BlitGIBLION.hlsl"//v0.2

					#pragma vertex VertABC
					#pragma fragment FragNearest
				ENDHLSL
		}
		///////////////////////////////////////// PASS 2 /////////////////////////////////////////
		Pass //7 BLIT BACKGROUND
		{
				Name "ColorBlitPasss"
				HLSLPROGRAM
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
				#include "BlitGIBLION.hlsl"//v0.2
				#pragma vertex VertABC
				#pragma fragment Frag
				//
				float4 Frag(VaryingsB input) : SV_Target0
				{
					
					//return half4(1, 0, 0, 0.5);
					// this is needed so we account XR platform differences in how they handle texture arrays
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
					// sample the texture using the SAMPLE_TEXTURE2D_X_LOD
					float2 uv = input.texcoord.xy;
					half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
					// Inverts the sampled color
					//return half4(1, 1, 1, 1) - color;
					return color*2;//2.5;
				}
				ENDHLSL
		}

		/////////////////////////////// PASS 3 !!!
		 Pass
        {
            CGPROGRAM
            #pragma vertex VertABC//vert
            #pragma fragment frag

            #include "UnityCG.cginc"
						
			/*
			  struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
			  v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
			*/

			//START GRAPH
			uniform float4 _BlitScaleBias;
			uniform float4 _BlitScaleBiasRt;
			uniform float _BlitMipLevel;
			uniform float2 _BlitTextureSize;
			uniform uint _BlitPaddingSize;
			uniform int _BlitTexArraySlice;
			uniform float4 _BlitDecodeInstructions;			          

			// Generates a triangle in homogeneous clip space, s.t.
			// v0 = (-1, -1, 1), v1 = (3, -1, 1), v2 = (-1, 3, 1).
			float2 GetFullScreenTriangleTexCoord(uint vertexID)
			{
				#if UNITY_UV_STARTS_AT_TOP
					return float2((vertexID << 1) & 2, 1.0 - (vertexID & 2));
				#else
					return float2((vertexID << 1) & 2, vertexID & 2);
				#endif
			}

			float4 GetFullScreenTriangleVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
			{
				// note: the triangle vertex position coordinates are x2 so the returned UV coordinates are in range -1, 1 on the screen.
				float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
				float4 pos = float4(uv * 2.0 - 1.0, z, 1.0);
			#ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
				pos = ApplyPretransformRotation(pos);
			#endif
				return pos;
			}

			#if SHADER_API_GLES
				struct AttributesB
				{
					float4 positionOS       : POSITION;
					float2 uv               : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
				#else
				struct AttributesB
				{
					uint vertexID : SV_VertexID;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
			#endif

			struct VaryingsB
			{
					float4 positionCS : SV_POSITION;
					float2 texcoord   : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
			};
			VaryingsB VertABC(AttributesB input)
			{
				VaryingsB output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if SHADER_API_GLES
					float4 pos = input.positionOS;
					float2 uv  = input.uv;
				#else
					float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
					float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
				#endif

				output.positionCS = pos;
				output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
				return output;
			}
			//END GRAPH

          
            sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			//2. water color
			int _KernelSize;
			float4 worldPosScaler;

			//////7.shaders-retro - GameBoyRamp
			sampler2D _GameboyRampTex;
			float4 GameboyFactor;

			//////6. shaders-pmd - Hatching
			float _SmudgeStrengthHatching;
			float _DrawingStrengthHatching;
			sampler2D _HatchingTex;
			float4 _TilingOffsetHatching;
			float _HatchingSpeed;
			float4 HatchFactor;
			
			////////5. "SMO/Complete/Neon"
			float4 NeonFactor;
			float3 sobel(float2 uv)
			{
				float x = 0;
				float y = 0;

				float2 texelSize = _MainTex_TexelSize;

				x += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, 0)) * -2.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) * -1.0;

				x += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) *  1.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, 0)) *  2.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				y += tex2D(_MainTex, uv + float2(0, -texelSize.y)) * -2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) * -1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) *  1.0;
				y += tex2D(_MainTex, uv + float2(0, texelSize.y)) *  2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				return sqrt(x * x + y * y);
			}
			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 rgb2hsv(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
				float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);

				float d = q.x - min(q.w, q.y);
				float e = 1.0e-10;
				return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 hsv2rgb(float3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}
			///END5.NEON
			

			
			////////// 4. "SMO/Complete/PixelSNES"
			float4  _MainTex_ST;
			static const float EPSILON = 1e-10;
			float4 SNESFactor;

			////////// 3. "UltraEffects/UnderwaterGIBLION"
			uniform sampler2D _BumpMap;
			uniform float _WaterColourStrength;
			uniform float4 _WaterColour;
			uniform float _FogStrength;
			float UnderwaterFactor;

			///////////////////////// 1. OUTLINE /////////////////////////////////
			float TexelScale ;
			float OutlineThickness ;
			float DepthSensitivity ;
			float NormalsSensitivity ;
			float ColorSensitivity ;
			float4 OutlineColor ;
			float4 OutlineControls ;
			sampler2D _CameraColorTexture;//TEXTURE2D(_CameraColorTexture);
			//SAMPLER(sampler_CameraColorTexture);
			float4 _CameraColorTexture_TexelSize;

			sampler2D _CameraDepthTexture;
			//TEXTURE2D(_CameraDepthTexture);
			//SAMPLER(sampler_CameraDepthTexture);
			float4 sampler_CameraDepthTexture;

			sampler2D _CameraDepthNormalsTextureGIBLI;
			//TEXTURE2D(_CameraDepthNormalsTextureGIBLI);
			//SAMPLER(sampler_CameraDepthNormalsTextureGIBLI);
		

			float3 DecodeNormal(float4 enc)
			{
				float kScale = 1.7777;
				float3 nn = enc.xyz*float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
				float g = 2.0 / dot(nn.xyz, nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}

			float4 Outline(float2 UV, float OutlineThickness, float DepthSensitivity, float NormalsSensitivity, float ColorSensitivity, float4 OutlineColor, float4 OutlineControls)
			{
				float halfScaleFloor = floor(OutlineThickness * 0.5);
				float halfScaleCeil = ceil(OutlineThickness * 0.5);
				float2 Texel = (1.0) / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);
				Texel = (1.0*TexelScale) * float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);///NEW1

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
						/*depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTextureGIBLI, sampler_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);*/

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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

					float4 original = tex2D(_MainTex, uvSamples[0]); //SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeDepth, edgeDepth, edgeDepth, 1);// ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / edgeDepth;//
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

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
					}

					// Depth
					float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
					float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
					float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
					float depthThreshold = (1 / DepthSensitivity) * depthSamples[0];
					edgeDepth = edgeDepth > depthThreshold ? 1 : 0;


					//REGULATE BY DEPTH
					Texel = (depthThreshold + 0.5*OutlineControls.y) / float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
					uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
					uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
					uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
					uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);
					for (int i = 0; i < 4; i++)
					{

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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

					float4 original = tex2D(_MainTex, uvSamples[0]); //SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeDepth, edgeDepth, edgeDepth, 1);// ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
					return float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / edgeDepth;//
					//Out = float4(edgeNormalA1, edgeNormalA1, edgeNormalA1, 1);//
					//Out = float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth;//
					//	Out = original* (1- OutlineControls.y) + (OutlineControls.y)*original * float4(edgeNormalA, edgeNormalA, edgeNormalA, 1) / (edgeDepth + 0.1*OutlineControls.z);//
				}
				else if (OutlineControls.x == 3) {

					uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
					uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
					uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
					uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

					for (int i = 0; i < 4; i++)
					{

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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
						+ dot(normalFiniteDifference1, depthFiniteDifference1*(1 - OutlineControls.y) + normalFiniteDifference1 * (OutlineControls.y))
					);
					float edgeNormalA = edgeNormal > (1 / NormalsSensitivity) ? 1 : 0;

					//float edgeNormalA1 = edgeNormal > (1 / edgeDepth) ? 1 : 0;

					// Color
					float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
					float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
					float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
					edgeColor = edgeColor > (1 / ColorSensitivity) ? 1 : 0;

					float edge = max(edgeDepth, max(edgeNormalA, edgeColor));

					float4 original = tex2D(_MainTex, uvSamples[0]); // SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
				}
				else {

					uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
					uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
					uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
					uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);

					for (int i = 0; i < 4; i++)
					{

						depthSamples[i] = tex2D(_CameraDepthTexture, uvSamples[i]).r;
						normalSamples[i] = DecodeNormal(tex2D(_CameraDepthNormalsTextureGIBLI, uvSamples[i]));
						colorSamples[i] = tex2D(_MainTex, uvSamples[i]);
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

					float4 original = tex2D(_MainTex, uvSamples[0]); //SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);

					//return float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth*edgeNormalA*edgeColor;//
					//return float4(edgeNormalA, edgeNormalA, edgeNormalA, 1);//
					//Out = float4(depthThreshold, depthThreshold, depthThreshold, 1)*edgeDepth;//


					return ((1 - edge) * original) + (edge * lerp(original, OutlineColor, OutlineColor.a));
				}
			}
			//////////////////////// END OUTLINE //////////////////////////////


			struct region
			{
				float3 mean;
				float variance;
			};

			region calcRegion(int2 lower, int2 upper, int samples, float2 uv)
			{
				region r;
				float3 sum = 0.0;
				float3 squareSum = 0.0;

				for (int x = lower.x; x <= upper.x; ++x)
				{
					for (int y = lower.y; y <= upper.y; ++y)
					{
						float2 offset = float2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y);
						float3 tex = tex2D(_MainTex, uv + offset);


						/////OUTLINE
						/*float4 outliner = Outline(uv + offset, OutlineThickness,
							DepthSensitivity, NormalsSensitivity,
							ColorSensitivity, OutlineColor,
							OutlineControls);
						tex =outliner;*/


						sum += tex;
						squareSum += tex * tex;
					}
				}

				r.mean = sum / samples;
				float3 variance = abs((squareSum / samples) - (r.mean * r.mean));
				r.variance = length(variance);

				return r;
			}

			//DEPTH WORLD SPACE
			//https://forum.unity.com/threads/upgrading-shader.819558/
			//float4x4 _InvProjMatrix;//URP
			///WORLD SPACE
			float3 DepthToWorld(float2 uv, float depth) {
				float z = (1 - depth) * 2.0 - 1.0;
				float4 clipSpacePos = float4(uv * 2.0 - 1.0, z, 1.0);
				float4 viewSpacePos = mul(unity_CameraInvProjection, clipSpacePos);// _CameraInverseProjection, clipSpacePos); unity_CameraInvProjection
				viewSpacePos /= viewSpacePos.w;
				float4 worldSpacePos = mul(unity_ObjectToWorld, viewSpacePos);
				return worldSpacePos.xyz;
			}


			////8.KNITTING
			sampler2D _KnitwearMap;
			float4 KnitFactor;
			half _KnitwearDivision;
			half _KnitwearAspect;
			half _KnitwearShear;
			half _KnitwearDistortionStrength;
			float2 GradientNoiseDir(float2 p)
			{
				p = p % 289;
				float x = (34 * p.x + 1) * p.x % 289 + p.y;
				x = (34 * x + 1) * x % 289;
				x = frac(x / 41) * 2 - 1;
				return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
			}
			float GradientNoise(float2 p)
			{
				float2 ip = floor(p);
				float2 fp = frac(p);
				float d00 = dot(GradientNoiseDir(ip), fp);
				float d01 = dot(GradientNoiseDir(ip + float2(0, 1)), fp - float2(0, 1));
				float d10 = dot(GradientNoiseDir(ip + float2(1, 0)), fp - float2(1, 0));
				float d11 = dot(GradientNoiseDir(ip + float2(1, 1)), fp - float2(1, 1));
				fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
				return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
			}
			void ApplyDistortion(inout float2 uv, half division, half distortion)
			{
				float noise = GradientNoise(uv * division * 0.005);
				noise *= distortion * 50 / division;

				float noiseDetail = GradientNoise(uv * division * 0.04);
				noiseDetail *= distortion * 6.25 / division;

				uv += noise + noiseDetail;
			}
			void KnitwearCoordinate(inout float2 uv, out float2 cell, half division, half shear, half distortion = 0)
			{
				//#if defined(_KNITWEAR_DISTORTION_ON)
								ApplyDistortion(uv, division, distortion);
				//#endif

				float verticalOffset = distance(frac(uv.x), 0.5) * shear;
				uv.y += verticalOffset;

				cell = floor(uv * float2(2.0, 1.0));
				cell += float2(0.5, 0.5);
				cell *= float2(0.5, 1.0);
			}
			


			float4 frag (VaryingsB i) : SV_Target//float4 frag (v2f i) : SV_Target
            {
				float4 texA = tex2D(_MainTex, i.texcoord);
				//float4 outlinerA = Outline(i.uv, OutlineThickness,
				//	DepthSensitivity, NormalsSensitivity,
				//	ColorSensitivity, OutlineColor,
				//	OutlineControls);

				//return outlinerA;// float4(col, 1.0)*1.5 + outliner * col.rgbb;

				int upper = (_KernelSize - 1) / 2;
				int lower = -upper;

				int samples = (upper + 1) * (upper + 1);

				region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, i.texcoord);
				region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, i.texcoord);
				region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, i.texcoord);
				region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, i.texcoord);

				fixed3 col = regionA.mean;
				fixed minVar = regionA.variance;

				float testVal;

				testVal = step(regionB.variance, minVar);
				col = lerp(col, regionB.mean, testVal);
				minVar = lerp(minVar, regionB.variance, testVal);

				testVal = step(regionC.variance, minVar);
				col = lerp(col, regionC.mean, testVal);
				minVar = lerp(minVar, regionC.variance, testVal);

				testVal = step(regionD.variance, minVar);
				col = lerp(col, regionD.mean, testVal);


				/// 1. OUTLINE
				/*void Outline_float(float2 UV, float OutlineThickness, 
					float DepthSensitivity, float NormalsSensitivity, 
					float ColorSensitivity, float4 OutlineColor, 
					float4 OutlineControls, out float4 Out)*/				
				float4 outliner = Outline(i.texcoord, OutlineThickness,
					DepthSensitivity, NormalsSensitivity,
					ColorSensitivity, OutlineColor,
					OutlineControls);			

				////////// 3. "UltraEffects/UnderwaterGIBLION"
				half3 normal = UnpackNormal(tex2D(_BumpMap, (i.texcoord + _Time.x) % 1.0));
				float2 uv = i.texcoord + normal * _WaterColourStrength;
				float4 colC = tex2D(_MainTex, uv);
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uv));
				depth = Linear01Depth(depth);
				colC = lerp(colC, _WaterColour, depth * _FogStrength);
				col += UnderwaterFactor * colC;

				////////// 4. "SMO/Complete/PixelSNES"
				/*	To achieve the thresholding of colour values, integer 	truncation is used.		*/
				int r = (texA.r - EPSILON) * 6;
				int g = (texA.g - EPSILON) * 6;
				int b = (texA.b - EPSILON) * 6;
				/*	Divide by 5, not 6, because we're dividing by the maximum value of each channel integer (which is 6).
					value of each channel integer (which is 6). */
				float4 colD = float4(r / 5.0, g / 5.0, b / 5.0, 1.0);
				col = SNESFactor.x *col + SNESFactor.y * colD + SNESFactor.z * colD*col;


				////////5. "SMO/Complete/Neon"
				float3 s = sobel(i.texcoord);
				float3 texE = texA;// tex2D(_MainTex, i.texcoord);

				float3 hsvTex = rgb2hsv(texE);
				hsvTex.y = 1.0;		// Modify saturation.
				hsvTex.z = 1.0;		// Modify lightness/value.
				float3 colE = hsv2rgb(hsvTex) * (s + NeonFactor.w);
				col = NeonFactor.x *col + NeonFactor.y * colE + NeonFactor.z * colE*col;
				//return float4(col * s, 1.0);

				//////6. shaders-pmd - Hatching				
				// _SmudgeStrengthHatching;
				// _DrawingStrengthHatching;
				// _HatchingTex;
				// _TilingOffsetHatching;
				// _HatchingSpeed;
				float scrrenDivide = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
				float2 HatchingUVOffseted = i.texcoord * scrrenDivide * _TilingOffsetHatching.xy + _TilingOffsetHatching.zw + (0.5 * floor(fmod(_Time.y * _HatchingSpeed,2)));///NEW1
				float2 HatchingTexAUV = float2(0.37, 0.37) * HatchingUVOffseted;
				float4 HatchingTexA = tex2D(_HatchingTex, HatchingTexAUV);
				float4 HatchingTexB = tex2D(_HatchingTex, HatchingUVOffseted);
				float4 HatchingValA = lerp(HatchingTexA, HatchingTexB,0.5);
				float4 HatchingVal = HatchingValA -0.5;
				float2 LerpIn1UV = i.texcoord + (_SmudgeStrengthHatching*HatchingVal.xy);
				float4 LerpIn1 = tex2D(_MainTex, LerpIn1UV);
				float4 LerpIn2 = LerpIn1 * HatchingVal;
				float4 colF = lerp( LerpIn1, LerpIn2,_DrawingStrengthHatching);
				col = HatchFactor.x *col + HatchFactor.y * colF + HatchFactor.z * colF*col;		


				//WORLD POS from DEPTH
				//https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/writing-shaders-urp-reconstruct-world-position.html
				float depthA = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.texcoord));
				depthA = Linear01Depth(depthA)* worldPosScaler.y;
				float3 depthPOS = DepthToWorld(i.texcoord, depthA);// *_WorldSpaceCameraPos;
				// The following part creates the checkerboard effect. Scale is the inverse size of the squares.
				float3 scale = 0.01 * worldPosScaler.x;
				// Scale, mirror and snap the coordinates.
				uint3 worldIntPos = uint3(abs(depthPOS.xyz * scale));
				// Divide the surface into squares. Calculate the color ID value.
				bool white = ((worldIntPos.x) & 1) ^ (worldIntPos.y & 1) ^ (worldIntPos.z & 1);
				// Color the square based on the ID value (black or white).
				half4 colorCHBRD = white ? half4(1, 1, 1, 1) : half4(0, 0, 0, 1);
				//return float4(depthPOS,1);
				//col = col * colorCHBRD;
				//return colorCHBRD;

				//GET LIGHTING
				/*if SHADOWS_SCREEN
					half4 clipPos = TransformWorldToHClip(depthPOS);
					half4 shadowCoord = ComputeScreenPos(clipPos);
				#else
				half4 shadowCoord = TransformWorldToShadowCoord(depthPOS);
				#endif
				Light mainLight = GetMainLight(shadowCoord);*/


				//////7.shaders-retro - GameBoyRamp
				float4 colG = tex2D(_GameboyRampTex, colorCHBRD*worldIntPos);
				col = GameboyFactor.x *col + GameboyFactor.y * colG + GameboyFactor.z * colG * col;


				/////8. KNITTING
				float2 uvsORIGIN = i.texcoord;
				float2 texCoord = i.texcoord;
				float2 dtdx = ddx(texCoord);
				float2 dtdy = ddy(texCoord);
				half2 scaleK = _KnitwearDivision / half2(_KnitwearAspect, 1.0);
				uvsORIGIN *= scaleK;
				float2 duvdx = dtdx * scaleK;
				float2 duvdy = dtdy * scaleK;
				KnitwearCoordinate(uvsORIGIN, texCoord, _KnitwearDivision, _KnitwearShear, _KnitwearDistortionStrength);
				texCoord /= scaleK;
				/*	half4 SampleAlbedoAlphaGrad(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap), float2 dpdx, float2 dpdy)
				{
					return SAMPLE_TEXTURE2D_GRAD(albedoAlphaMap, sampler_albedoAlphaMap, uv, dpdx, dpdy);
				}
				half3 SampleKnitwear(float2 uv, float2 dpdx, float2 dpdy)
				{
					return SAMPLE_TEXTURE2D_GRAD(_KnitwearMap, sampler_KnitwearMap, uv, dpdx, dpdy).rgb;
				}*/
				//https://forum.unity.com/threads/how-to-use-samplegrad-in-unity-shader.430857/
				half4 albedoAlpha = tex2Dgrad(_MainTex, texCoord, dtdx, dtdy);//SampleAlbedoAlphaGrad(texCoord, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), dtdx, dtdy);
				//outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
				half3 knitwearColor  = tex2Dgrad(_KnitwearMap, uvsORIGIN, duvdx, duvdy); //SampleKnitwear(uvsORIGIN, duvdx, duvdy);
				//half3 knitwearColor = SampleKnitwear(uvsORIGIN, duvdx, duvdy);
				//outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb * knitwearColor;
				float4 knittingFinal = float4(albedoAlpha.rgb * knitwearColor,1);
				col = KnitFactor.x *col + KnitFactor.y * knittingFinal + KnitFactor.z * knittingFinal * col;
				
				//2. col = image-ultra - UltraEffects/Painting
				//return float4(col, 1.0);
				return float4(outliner.rgb - texA,1) + float4(col, 1.0);// float4(col, 1.0)*1.5 + outliner * col.rgbb;
            }
            ENDCG
        }



    }
}
