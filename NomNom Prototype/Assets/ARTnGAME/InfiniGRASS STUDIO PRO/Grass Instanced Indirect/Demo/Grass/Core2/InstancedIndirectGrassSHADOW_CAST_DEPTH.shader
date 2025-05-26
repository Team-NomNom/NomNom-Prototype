Shader "MobileDrawMeshInstancedIndirect/SingleGrassSHADOW_CAST_DEPTH"
{
    Properties
    {
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
        _BaseColorTexture("_BaseColorTexture", 2D) = "white" {}
        _GroundColor("_GroundColor", Color) = (0.5,0.5,0.5)

        [Header(Grass Shape)]
        _GrassWidth("_GrassWidth", Float) = 1
        _GrassHeight("_GrassHeight", Float) = 1
		scaleDistantGrass("scaleDistantGrass", Float) = 1 //v0.4

        [Header(Wind)]
        _WindAIntensity("_WindAIntensity", Float) = 1.77
        _WindAFrequency("_WindAFrequency", Float) = 4
        _WindATiling("_WindATiling", Vector) = (0.1,0.1,0)
        _WindAWrap("_WindAWrap", Vector) = (0.5,0.5,0)

        _WindBIntensity("_WindBIntensity", Float) = 0.25
        _WindBFrequency("_WindBFrequency", Float) = 7.7
        _WindBTiling("_WindBTiling", Vector) = (.37,3,0)
        _WindBWrap("_WindBWrap", Vector) = (0.5,0.5,0)


        _WindCIntensity("_WindCIntensity", Float) = 0.125
        _WindCFrequency("_WindCFrequency", Float) = 11.7
        _WindCTiling("_WindCTiling", Vector) = (0.77,3,0)
        _WindCWrap("_WindCWrap", Vector) = (0.5,0.5,0)

        [Header(Lighting)]
        _RandomNormal("_RandomNormal", Float) = 0.15

        //make SRP batcher happy
        [HideInInspector]_PivotPosWS("_PivotPosWS", Vector) = (0,0,0,0)
        [HideInInspector]_BoundSize("_BoundSize", Vector) = (1,1,0)



		//v0.1
		//TERRAIN DEPTH
		_DepthCameraPos("Depth Camera Pos", Vector) = (240, 100, 250, 3.0)
		_ShoreContourTex("_ShoreContourTex", 2D) = "white" {}
		_ShoreFadeFactor("_ShoreFadeFactor", Float) = 1.0
		_TerrainScale("Terrain Scale", Float) = 1000.0
		//v2.0.7
		_grassHeight("grassHeight", Float) = 3
		//_grassNormal("grassNormal", Float) = 3
		_respectHeight("_respectHeight", Float) = 0
		//v2.0.8
		_InteractTexture("_Interact Texture", 2D) = "white" {}
		_InteractTexturePos("Interact Texture Pos", Vector) = (0, 5000, 0, 0)
		_InteractTexturePosY("Interact Texture Pos Y Shadows", Float) = 5000
		//v2.1.1
		_shapeOnlyHeight("Shape Only Height", Float) = 0
		//v2.1.11
		_BaseHeight("Base Height", Float) = 0
		//v2.1.20 - //INFINIGRASS - DENSITY EMULATING GRASS
		_NoiseAmplitude("_NoiseAmplitude", Float) = 0
		_NoiseFreqX("_NoiseFreqX ", Float) = 0
		_NoiseFreqZ("_NoiseFreqZ ", Float) = 0
		_NoiseOffsetY("_NoiseOffsetY ", Float) = 0
		//v2.1.20b
		_textureFeed("Use texture feed height", Float) = 1 //default is on
		//END INFINIGRASS 3

		//INFINIGRASS 5 - GRASS BURN
		//BURN 2.1.14
		_NoiseTexture("Noise Texture", 2D) = "white" {}
		_Burnfactor("Burn factor", Range(-1, 1)) = -1//6
		_Burnramp("Burn ramp", 2D) = "white" {}
		_BurnCenter("Ocean Center", Vector) = (0, 0, 0, 0)
		//END INFINIGRASS 5 - GRASS BURN

		//v1.2 - GRASS SHAPING
		//v2.0.8
		_InteractTextureA("_Interact Texture", 2D) = "white" {}
		_InteractTexturePosA("Interact Texture Pos", Vector) = (0, 5000, 0, 0)
		enableGrassShape("Enable Grass Shaping Texture", Float) = 0
			_erasedShadowFactor("Shape Erased Grass Shadow Factor", Float) = 1
    }

    SubShader
    {
       // Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}

        Pass
        {
            Cull Back //use default culling because this shader is billboard 
            ZTest Less
            Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" "LightMode" = "UniversalForwardOnly" }//2023

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Universal Render Pipeline keywords
            // When doing custom shaders you most often want to copy and paste these #pragmas
            // These multi_compile variants are stripped from the build depending on:
            // 1) Settings in the URP Asset assigned in the GraphicsSettings at build time
            // e.g If you disabled AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
            // will be stripped from build
            // 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
            // but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            // -------------------------------------

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
				float2 texcoord     : TEXCOORD0; //v0.1
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                half3 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float3 _PivotPosWS;
                float2 _BoundSize;

                float _GrassWidth;
                float _GrassHeight;

                float _WindAIntensity;
                float _WindAFrequency;
                float2 _WindATiling;
                float2 _WindAWrap;

                float _WindBIntensity;
                float _WindBFrequency;
                float2 _WindBTiling;
                float2 _WindBWrap;

                float _WindCIntensity;
                float _WindCFrequency;
                float2 _WindCTiling;
                float2 _WindCWrap;

                half3 _BaseColor;
                float4 _BaseColorTexture_ST;
                half3 _GroundColor;

                half _RandomNormal;

                StructuredBuffer<float3> _AllInstancesTransformBuffer;
                StructuredBuffer<uint> _VisibleInstanceOnlyTransformIDBuffer;
            CBUFFER_END

			//v1.2
			//v2.0.8
			sampler2D _InteractTextureA;
			float3 _InteractTexturePosA;
			float enableGrassShape;


			//INFINIGRASS 3
			//TERRAIN DEPTH
			sampler2D _ShoreContourTex;
			float3 _DepthCameraPos;
			float _ShoreFadeFactor;
			float _TerrainScale;
			//v2.0.7
			float _grassHeight;
			//float _grassNormal;
			float _respectHeight;
			//v2.0.8
			sampler2D _InteractTexture;
			float3 _InteractTexturePos;
			float _InteractTexturePosY;
			//v2.1.1
			float _shapeOnlyHeight;
			//v2.1.11
			float _BaseHeight;
			//v2.1.20
			float _NoiseAmplitude;
			float _NoiseFreqX;
			float _NoiseFreqZ;
			float _NoiseOffsetY;
			//v2.1.20b
			float _textureFeed;
			//v2.1.20			
			//END INFINIGRASS 3
			//INFINIGRASS 5
			//BURN 2.1.14
			uniform sampler2D _NoiseTexture; uniform float4 _NoiseTexture_ST;//
			uniform float _Burnfactor;
			uniform sampler2D _Burnramp; uniform float4 _Burnramp_ST;
			uniform float3 _BurnCenter;
			//END INFINIGRASS 5




            sampler2D _GrassBendingRT;
            sampler2D _BaseColorTexture;

            half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
            {
                half3 H = normalize(light.direction + V);

                //direct diffuse 
                half directDiffuse = dot(N, light.direction) * 0.5 + 0.5; //half lambert, to fake grass SSS

                //direct specular
                float directSpecular = saturate(dot(N,H));
                //pow(directSpecular,8)
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                //directSpecular *= directSpecular; //enable this line = change to pow(directSpecular,16)

                //add direct directSpecular to result
                directSpecular *= 0.1 * positionOSY;//only apply directSpecular to grass's top area, to simulate grass AO

                half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
                half3 result = (albedo * directDiffuse + directSpecular) * lighting;
                return result; 
            }

			//v0.4
			float3 _WorldSpaceCameraPosA;
			float scaleGrassHolder;
			float scaleDistantGrass;
            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;

                float3 perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];//we pre-transform to posWS in C# now

				//v0.4
				if (scaleGrassHolder != 0) {
					float scaleGrassbed = scaleGrassHolder * 2;// 111.8034 * 2;
					int floorX = floor(_WorldSpaceCameraPosA.x / (scaleGrassbed * 2));
					int floorZ = floor(_WorldSpaceCameraPosA.z / (scaleGrassbed * 2));
					//if ((perGrassPivotPosWS.x + (scaleGrassbed * 2)*floorX) < -scaleGrassbed + (scaleGrassbed * 2 * floorX) +  _WorldSpaceCameraPosA.x)
					if (_WorldSpaceCameraPosA.x > 0) {
						if (perGrassPivotPosWS.x < -scaleGrassbed + _WorldSpaceCameraPosA.x)
						{
							perGrassPivotPosWS.x += (scaleGrassbed * 2)*(floorX + 1); //perGrassPivotPosWS.x += scaleGrassbed * 2 + (scaleGrassbed * 2)*floorX;
						}
						if (perGrassPivotPosWS.x > scaleGrassbed + _WorldSpaceCameraPosA.x)
						{
							perGrassPivotPosWS.x -= scaleGrassbed * 2;
						}
					}
					if (_WorldSpaceCameraPosA.z > 0) {
						if (perGrassPivotPosWS.z < -scaleGrassbed + _WorldSpaceCameraPosA.z)
						{
							perGrassPivotPosWS.z += (scaleGrassbed * 2)*(floorZ + 1);
						}
						if (perGrassPivotPosWS.z > scaleGrassbed + _WorldSpaceCameraPosA.z)
						{
							perGrassPivotPosWS.z -= scaleGrassbed * 2;
						}
					}

					if (_WorldSpaceCameraPosA.x < 0) {
						if (perGrassPivotPosWS.x > scaleGrassbed + _WorldSpaceCameraPosA.x)
						{
							perGrassPivotPosWS.x += (scaleGrassbed * 2)*(floorX - 0); //perGrassPivotPosWS.x += scaleGrassbed * 2 + (scaleGrassbed * 2)*floorX;
						}
						if (perGrassPivotPosWS.x < -scaleGrassbed + _WorldSpaceCameraPosA.x)
						{
							perGrassPivotPosWS.x += scaleGrassbed * 2;
						}
					}
					if (_WorldSpaceCameraPosA.z < 0) {
						if (perGrassPivotPosWS.z > scaleGrassbed + _WorldSpaceCameraPosA.z)
						{
							perGrassPivotPosWS.z += (scaleGrassbed * 2)*(floorZ - 0);
						}
						if (perGrassPivotPosWS.z < -scaleGrassbed + _WorldSpaceCameraPosA.z)
						{
							perGrassPivotPosWS.z += scaleGrassbed * 2;
						}
					}// * scaleDistantGrass; //v0.4
				}

                float perGrassHeight = lerp(2,5,(sin(perGrassPivotPosWS.x*23.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55)) * _GrassHeight;

                //get "is grass stepped" data(bending) from RT
                float2 grassBendingUV = ((perGrassPivotPosWS.xz - _PivotPosWS.xz) / _BoundSize) * 0.5 + 0.5;//claculate where is this grass inside bound (can optimize to 2 MAD)
                float stepped = tex2Dlod(_GrassBendingRT, float4(grassBendingUV, 0, 0)).x;

                //rotation(make grass LookAt() camera just like a billboard)
                //=========================================
                float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;//UNITY_MATRIX_V[0].xyz == world space camera Right unit vector
                float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;//UNITY_MATRIX_V[1].xyz == world space camera Up unit vector
                float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;//UNITY_MATRIX_V[2].xyz == -1 * world space camera Forward unit vector

                //Expand Billboard (billboard Left+right)
                float3 positionOS = IN.positionOS.x * cameraTransformRightWS * _GrassWidth * (sin(perGrassPivotPosWS.x*95.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55);//random width from posXZ, min 0.1

                //Expand Billboard (billboard Up)
                positionOS += IN.positionOS.y * cameraTransformUpWS;         
                //=========================================

                //bending by RT (hard code)
                float3 bendDir = cameraTransformForwardWS;
                bendDir.xz *= 0.5; //make grass shorter when bending, looks better
                bendDir.y = min(-0.5,bendDir.y);//prevent grass become too long if camera forward is / near parallel to ground
                positionOS = lerp(positionOS.xyz + bendDir * positionOS.y / -bendDir.y, positionOS.xyz, stepped * 0.95 + 0.05);//don't fully bend, will produce ZFighting

                //per grass height scale
                positionOS.y *= perGrassHeight;

                //camera distance scale (make grass width larger if grass is far away to camera, to hide smaller than pixel size triangle flicker)        
                float3 viewWS = _WorldSpaceCameraPos - perGrassPivotPosWS;
                float ViewWSLength = length(viewWS);
                positionOS += cameraTransformRightWS * IN.positionOS.x * max(0, ViewWSLength * 0.0225) * scaleDistantGrass; //v0.4
                

                //move grass posOS -> posWS
                float3 positionWS = positionOS + perGrassPivotPosWS;
							   

				//v1.2 - grass shaper
				if (enableGrassShape == 1) {
					//v2.0.8
					half2 tileableUv = positionWS.xz;// mul(unity_ObjectToWorld, (v.vertex)).xz;
					float WorldScale = _InteractTexturePosA.y;
					float3 CamPos = float3(0, 0, 0);
					float3 Origin = float3(_InteractTexturePosA.x, _InteractTexturePosA.z, 0);
					float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
					float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
					float4 texInteract = tex2Dlod(_InteractTextureA, float4(ScaledTexPoint, 0.0, 0.0));
					//v2.0.8
					float3 posWORLD = positionWS;
					float2 shapeUVs = IN.texcoord;
					if (_shapeOnlyHeight == 0) { //v2.1.1
						posWORLD.y = posWORLD.y*clamp((1 - texInteract.r*texInteract.r  + 0.001)*1.5, 0, 1);
						posWORLD.y = posWORLD.y - texInteract.r * 2 ; //v2.1.14
						if (shapeUVs.y > 0.01) {
							if (texInteract.g >= 0.75) {
								posWORLD.x = posWORLD.x - (1 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
								posWORLD.z = posWORLD.z - (texInteract.g - 0.75)*shapeUVs.y * 42 * texInteract.b;
							}
							else if (texInteract.g >= 0.5) {	//negative angle region					
								posWORLD.x = posWORLD.x - (texInteract.g - 0.5)*shapeUVs.y * 42 * texInteract.b;
								posWORLD.z = posWORLD.z + (0.75 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
							}
							else if (texInteract.g >= 0.25) {
								posWORLD.x = posWORLD.x + (0.5 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
								posWORLD.z = posWORLD.z + (0.25 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
							}
							else if (texInteract.g > 0) {
								posWORLD.x = posWORLD.x + texInteract.g*shapeUVs.y * 42 * texInteract.b;
								posWORLD.z = posWORLD.z + (0.25 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
							}
						}
					}
					else {
						posWORLD.y = abs(posWORLD.y) - sign(posWORLD.y)*(1 - texInteract.r)*_shapeOnlyHeight;
					}
					positionWS = posWORLD;
				}


                //wind animation (biilboard Left Right direction only sin wave)            
                float wind = 0;
                wind += (sin(_Time.y * _WindAFrequency + perGrassPivotPosWS.x * _WindATiling.x + perGrassPivotPosWS.z * _WindATiling.y)*_WindAWrap.x+_WindAWrap.y) * _WindAIntensity; //windA
                wind += (sin(_Time.y * _WindBFrequency + perGrassPivotPosWS.x * _WindBTiling.x + perGrassPivotPosWS.z * _WindBTiling.y)*_WindBWrap.x+_WindBWrap.y) * _WindBIntensity; //windB
                wind += (sin(_Time.y * _WindCFrequency + perGrassPivotPosWS.x * _WindCTiling.x + perGrassPivotPosWS.z * _WindCTiling.y)*_WindCWrap.x+_WindCWrap.y) * _WindCIntensity; //windC
                wind *= IN.positionOS.y; //wind only affect top region, don't affect root region
                float3 windOffset = cameraTransformRightWS * wind; //swing using billboard left right direction
                positionWS.xyz += windOffset;



				//////// InfiniGRASS Terrain ADAPT
				float heightDepthCam = _DepthCameraPos.y;
				_TerrainScale = heightDepthCam * 2;//URP
				half2 tileableUv = float2(positionWS.x, positionWS.z);// positionWS.xz;// mul(unity_ObjectToWorld, (v.vertex)).xz;
				float WorldScale = 500;
				WorldScale = _TerrainScale;
				float3 CamPos = float3(250, 0, 250);//_WorldSpaceCameraPos;
				CamPos = _DepthCameraPos;//_WorldSpaceCameraPos;
				float2 Origin = float2(CamPos.x - WorldScale / 2.0, CamPos.z - WorldScale / 2.0);
				float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
				float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
				float4 tex = tex2Dlod(_ShoreContourTex, float4(float2(ScaledTexPoint.x, ScaledTexPoint.y), 0.0, 0.0));
				//URP			
				tex.r = 1 - tex.r;
				//v2.0.8
				WorldScale = _InteractTexturePos.y;
				CamPos = float3(0, 0, 0);//_DepthCameraPos;//_WorldSpaceCameraPos;
				Origin = float2(0, 0);//float2(CamPos.x - WorldScale/2.0 , CamPos.z - WorldScale/2.0);
				UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
				ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
				float4 texInteract = tex2Dlod(_InteractTexture, float4(ScaledTexPoint, 0.0, 0.0));
				if (_textureFeed == 1) { //v2.1.20b
					positionWS.y = positionWS.y*_respectHeight + (pow(abs(heightDepthCam - tex.r*heightDepthCam), _ShoreFadeFactor) + IN.texcoord.y*_grassHeight);
					positionWS.y = positionWS.y + _BaseHeight + _DepthCameraPos.y - heightDepthCam; //v2.1.16
				}

				//NOISE
				float noise = tex2Dlod(_NoiseTexture, float4(ScaledTexPoint * _NoiseFreqX, 0.0, 0.0));
				float influence = 1.0 - _NoiseAmplitude;
				float d2 = (1.0 - influence) / 2.0;
				noise -= d2;
				noise *= 1.0 / influence;
				//noise = clamp(noise, 0.0, 1.0);
				//if (IN.texcoord.y > 0) {
					positionWS.y = positionWS.y + noise * _NoiseFreqZ;
				//}


                
                //vertex position logic done, complete posWS -> posCS
                OUT.positionCS = TransformWorldToHClip(positionWS);

                /////////////////////////////////////////////////////////////////////
                //lighting & color
                /////////////////////////////////////////////////////////////////////

                //lighting data
                Light mainLight;
#if _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
                mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
#else
                mainLight = GetMainLight();
#endif
                half3 randomAddToN = (_RandomNormal* sin(perGrassPivotPosWS.x * 82.32523 + perGrassPivotPosWS.z) + wind * -0.25) * cameraTransformRightWS;//random normal per grass 
                //default grass's normal is pointing 100% upward in world space, it is an important but simple grass normal trick
                //-apply random to normal else lighting is too uniform
                //-apply cameraTransformForwardWS to normal because grass is billboard
                half3 N = normalize(half3(0,1,0) + randomAddToN - cameraTransformForwardWS*0.5);

                half3 V = viewWS / ViewWSLength;

                half3 baseColor = tex2Dlod(_BaseColorTexture, float4(TRANSFORM_TEX(positionWS.xz,_BaseColorTexture),0,0)) * _BaseColor;//sample mip 0 only
                half3 albedo = lerp(_GroundColor,baseColor, IN.positionOS.y);

                //indirect
                half3 lightingResult = SampleSH(0) * albedo;

                //main direct light
                lightingResult += ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);

                // Additional lights loop
#if _ADDITIONAL_LIGHTS

                // Returns the amount of lights affecting the object being renderer.
                // These lights are culled per-object in the forward renderer
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    // Similar to GetMainLight, but it takes a for-loop index. This figures out the
                    // per-object light index and samples the light buffer accordingly to initialized the
                    // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
                    Light light = GetAdditionalLight(i, positionWS);

                    // Same functions used to shade the main light.
                    lightingResult += ApplySingleDirectLight(light, N, V, albedo, positionOS.y);
                }
#endif

                //fog
                float fogFactor = ComputeFogFactor(OUT.positionCS.z);
                // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
                // with a custom one.
                OUT.color = MixFog(lightingResult, fogFactor);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.color,1);
            }
            ENDHLSL
        }

        //copy pass, change LightMode to ShadowCaster will make grass cast shadow
        //copy pass, change LightMode to DepthOnly will make grass render into _CameraDepthTexture

		///// START SHADOW
		Pass
		{
			Cull Back //use default culling because this shader is billboard 
			ZTest Less
			Tags{ "LightMode" = "ShadowCaster" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// -------------------------------------
			// Universal Render Pipeline keywords
			// When doing custom shaders you most often want to copy and paste these #pragmas
			// These multi_compile variants are stripped from the build depending on:
			// 1) Settings in the URP Asset assigned in the GraphicsSettings at build time
			// e.g If you disabled AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
			// will be stripped from build
			// 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
			// but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile_fog
			// -------------------------------------

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
		{
			float4 positionOS   : POSITION;
			float2 texcoord     : TEXCOORD0; //v0.1
		};

		struct Varyings
		{
			float4 positionCS  : SV_POSITION;
			half3 color        : COLOR;
		};

		CBUFFER_START(UnityPerMaterial)
			float3 _PivotPosWS;
		float2 _BoundSize;

		float _GrassWidth;
		float _GrassHeight;

		float _WindAIntensity;
		float _WindAFrequency;
		float2 _WindATiling;
		float2 _WindAWrap;

		float _WindBIntensity;
		float _WindBFrequency;
		float2 _WindBTiling;
		float2 _WindBWrap;

		float _WindCIntensity;
		float _WindCFrequency;
		float2 _WindCTiling;
		float2 _WindCWrap;

		half3 _BaseColor;
		half3 _GroundColor;

		half _RandomNormal;

		StructuredBuffer<float3> _AllInstancesTransformBuffer;
		StructuredBuffer<uint> _VisibleInstanceOnlyTransformIDBuffer;
		CBUFFER_END


		//v1.2
		//v2.0.8
		sampler2D _InteractTextureA;
		float3 _InteractTexturePosA;
		float enableGrassShape;
		float _erasedShadowFactor;

			//INFINIGRASS 3
			//TERRAIN DEPTH
			sampler2D _ShoreContourTex;
		float3 _DepthCameraPos;
		float _ShoreFadeFactor;
		float _TerrainScale;
		//v2.0.7
		float _grassHeight;
		//float _grassNormal;
		float _respectHeight;
		//v2.0.8
		sampler2D _InteractTexture;
		float3 _InteractTexturePos;
		float _InteractTexturePosY;
		//v2.1.1
		float _shapeOnlyHeight;
		//v2.1.11
		float _BaseHeight;
		//v2.1.20
		float _NoiseAmplitude;
		float _NoiseFreqX;
		float _NoiseFreqZ;
		float _NoiseOffsetY;
		//v2.1.20b
		float _textureFeed;
		//v2.1.20			
		//END INFINIGRASS 3
		//INFINIGRASS 5
		//BURN 2.1.14
		uniform sampler2D _NoiseTexture; uniform float4 _NoiseTexture_ST;//
		uniform float _Burnfactor;
		uniform sampler2D _Burnramp; uniform float4 _Burnramp_ST;
		uniform float3 _BurnCenter;
		//END INFINIGRASS 5


			sampler2D _GrassBendingRT;

		half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
		{
			half3 H = normalize(light.direction + V);

			//direct diffuse 
			half directDiffuse = dot(N, light.direction) * 0.5 + 0.5; //half lambert, to fake grass SSS

																	  //direct specular
			float directSpecular = saturate(dot(N,H));
			//pow(directSpecular,8)
			directSpecular *= directSpecular;
			directSpecular *= directSpecular;
			directSpecular *= directSpecular;
			//directSpecular *= directSpecular; //enable this line = change to pow(directSpecular,16)

			//add direct directSpecular to result
			directSpecular *= 0.1 * positionOSY;//only apply directSpecular to grass's top area, to simulate grass AO

			half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
			half3 result = (albedo * directDiffuse + directSpecular) * lighting;
			return result;
		}

		//v0.4
		float3 _WorldSpaceCameraPosA;
		float scaleGrassHolder;
		float scaleDistantGrass;
		Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
		{
			Varyings OUT;

			float3 perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];//we pre-transform to posWS in C# now

			//v0.4
			if (scaleGrassHolder != 0) {
				float scaleGrassbed = scaleGrassHolder * 2;// 111.8034 * 2;
				int floorX = floor(_WorldSpaceCameraPosA.x / (scaleGrassbed * 2));
				int floorZ = floor(_WorldSpaceCameraPosA.z / (scaleGrassbed * 2));
				//if ((perGrassPivotPosWS.x + (scaleGrassbed * 2)*floorX) < -scaleGrassbed + (scaleGrassbed * 2 * floorX) +  _WorldSpaceCameraPosA.x)
				if (_WorldSpaceCameraPosA.x > 0) {
					if (perGrassPivotPosWS.x < -scaleGrassbed + _WorldSpaceCameraPosA.x)
					{
						perGrassPivotPosWS.x += (scaleGrassbed * 2)*(floorX + 1); //perGrassPivotPosWS.x += scaleGrassbed * 2 + (scaleGrassbed * 2)*floorX;
					}
					if (perGrassPivotPosWS.x > scaleGrassbed + _WorldSpaceCameraPosA.x)
					{
						perGrassPivotPosWS.x -= scaleGrassbed * 2;
					}
				}
				if (_WorldSpaceCameraPosA.z > 0) {
					if (perGrassPivotPosWS.z < -scaleGrassbed + _WorldSpaceCameraPosA.z)
					{
						perGrassPivotPosWS.z += (scaleGrassbed * 2)*(floorZ + 1);
					}
					if (perGrassPivotPosWS.z > scaleGrassbed + _WorldSpaceCameraPosA.z)
					{
						perGrassPivotPosWS.z -= scaleGrassbed * 2;
					}
				}

				if (_WorldSpaceCameraPosA.x < 0) {
					if (perGrassPivotPosWS.x > scaleGrassbed + _WorldSpaceCameraPosA.x)
					{
						perGrassPivotPosWS.x += (scaleGrassbed * 2)*(floorX - 0); //perGrassPivotPosWS.x += scaleGrassbed * 2 + (scaleGrassbed * 2)*floorX;
					}
					if (perGrassPivotPosWS.x < -scaleGrassbed + _WorldSpaceCameraPosA.x)
					{
						perGrassPivotPosWS.x += scaleGrassbed * 2;
					}
				}
				if (_WorldSpaceCameraPosA.z < 0) {
					if (perGrassPivotPosWS.z > scaleGrassbed + _WorldSpaceCameraPosA.z)
					{
						perGrassPivotPosWS.z += (scaleGrassbed * 2)*(floorZ - 0);
					}
					if (perGrassPivotPosWS.z < -scaleGrassbed + _WorldSpaceCameraPosA.z)
					{
						perGrassPivotPosWS.z += scaleGrassbed * 2;
					}
				}// * scaleDistantGrass; //v0.4
			}

			float perGrassHeight = lerp(2,5,(sin(perGrassPivotPosWS.x*23.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55)) * _GrassHeight;

			//get "is grass stepped" data(bending) from RT
			float2 grassBendingUV = ((perGrassPivotPosWS.xz - _PivotPosWS.xz) / _BoundSize) * 0.5 + 0.5;//claculate where is this grass inside bound (can optimize to 2 MAD)
			float stepped = tex2Dlod(_GrassBendingRT, float4(grassBendingUV, 0, 0)).x;

			//rotation(make grass LookAt() camera just like a billboard)
			//=========================================
			float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;//UNITY_MATRIX_V[0].xyz == world space camera Right unit vector
			float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;//UNITY_MATRIX_V[1].xyz == world space camera Up unit vector
			float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;//UNITY_MATRIX_V[2].xyz == -1 * world space camera Forward unit vector

																	 //Expand Billboard (billboard Left+right)
			float3 positionOS = IN.positionOS.x * cameraTransformRightWS * _GrassWidth * (sin(perGrassPivotPosWS.x*95.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55);//random width from posXZ, min 0.1

																																								  //Expand Billboard (billboard Up)
			positionOS += IN.positionOS.y * cameraTransformUpWS;
			//=========================================

			//bending by RT (hard code)
			float3 bendDir = cameraTransformForwardWS;
			bendDir.xz *= 0.5; //make grass shorter when bending, looks better
			bendDir.y = min(-0.5,bendDir.y);//prevent grass become too long if camera forward is / near parallel to ground
			positionOS = lerp(positionOS.xyz + bendDir * positionOS.y / -bendDir.y, positionOS.xyz, stepped * 0.95 + 0.05);//don't fully bend, will produce ZFighting

																														   //per grass height scale
			positionOS.y *= perGrassHeight;

			//camera distance scale (make grass width larger if grass is far away to camera, to hide smaller than pixel size triangle flicker)        
			float3 viewWS = _WorldSpaceCameraPos - perGrassPivotPosWS;
			float ViewWSLength = length(viewWS);
			positionOS += cameraTransformRightWS * IN.positionOS.x * max(0, ViewWSLength * 0.0225) * scaleDistantGrass; //v0.4


			//move grass posOS -> posWS
			float3 positionWS = positionOS + perGrassPivotPosWS;


			//v1.2 - grass shaper
			if (enableGrassShape == 1) {
				//v2.0.8
				half2 tileableUv = positionWS.xz;// mul(unity_ObjectToWorld, (v.vertex)).xz;
				float WorldScale = _InteractTexturePosA.y;
				float3 CamPos = float3(0, 0, 0);
				float3 Origin = float3(_InteractTexturePosA.x, _InteractTexturePosA.z, 0);
				float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
				float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
				float4 texInteract = tex2Dlod(_InteractTextureA, float4(ScaledTexPoint, 0.0, 0.0));
				//v2.0.8
				float3 posWORLD = positionWS;
				float2 shapeUVs = IN.texcoord;
				if (_shapeOnlyHeight == 0) { //v2.1.1
					posWORLD.y = posWORLD.y*clamp((1 - texInteract.r*texInteract.r * _erasedShadowFactor + 0.001)*1.5, 0, 1);
					posWORLD.y = posWORLD.y - texInteract.r * 2 * _erasedShadowFactor; //v2.1.14
					if (shapeUVs.y > 0.01) {
						if (texInteract.g >= 0.75) {
							posWORLD.x = posWORLD.x - (1 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
							posWORLD.z = posWORLD.z - (texInteract.g - 0.75)*shapeUVs.y * 42 * texInteract.b;
						}
						else if (texInteract.g >= 0.5) {	//negative angle region					
							posWORLD.x = posWORLD.x - (texInteract.g - 0.5)*shapeUVs.y * 42 * texInteract.b;
							posWORLD.z = posWORLD.z + (0.75 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
						}
						else if (texInteract.g >= 0.25) {
							posWORLD.x = posWORLD.x + (0.5 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
							posWORLD.z = posWORLD.z + (0.25 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
						}
						else if (texInteract.g > 0) {
							posWORLD.x = posWORLD.x + texInteract.g*shapeUVs.y * 42 * texInteract.b;
							posWORLD.z = posWORLD.z + (0.25 - texInteract.g)*shapeUVs.y * 42 * texInteract.b;
						}
					}
				}
				else {
					posWORLD.y = abs(posWORLD.y) - sign(posWORLD.y)*(1 - texInteract.r)*_shapeOnlyHeight;
				}
				positionWS = posWORLD;
			}


			//wind animation (biilboard Left Right direction only sin wave)            
			float wind = 0;
			wind += (sin(_Time.y * _WindAFrequency + perGrassPivotPosWS.x * _WindATiling.x + perGrassPivotPosWS.z * _WindATiling.y)*_WindAWrap.x + _WindAWrap.y) * _WindAIntensity; //windA
			wind += (sin(_Time.y * _WindBFrequency + perGrassPivotPosWS.x * _WindBTiling.x + perGrassPivotPosWS.z * _WindBTiling.y)*_WindBWrap.x + _WindBWrap.y) * _WindBIntensity; //windB
			wind += (sin(_Time.y * _WindCFrequency + perGrassPivotPosWS.x * _WindCTiling.x + perGrassPivotPosWS.z * _WindCTiling.y)*_WindCWrap.x + _WindCWrap.y) * _WindCIntensity; //windC
			wind *= IN.positionOS.y; //wind only affect top region, don't affect root region
			float3 windOffset = cameraTransformRightWS * wind; //swing using billboard left right direction
			positionWS.xyz += windOffset;



			//////// InfiniGRASS Terrain ADAPT
			float heightDepthCam = _DepthCameraPos.y;
			_TerrainScale = heightDepthCam * 2;//URP
			half2 tileableUv = float2(positionWS.x, positionWS.z);// positionWS.xz;// mul(unity_ObjectToWorld, (v.vertex)).xz;
			float WorldScale = 500;
			WorldScale = _TerrainScale;
			float3 CamPos = float3(250, 0, 250);//_WorldSpaceCameraPos;
			CamPos = _DepthCameraPos;//_WorldSpaceCameraPos;
			float2 Origin = float2(CamPos.x - WorldScale / 2.0, CamPos.z - WorldScale / 2.0);
			float2 UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
			float2 ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
			float4 tex = tex2Dlod(_ShoreContourTex, float4(float2(ScaledTexPoint.x, ScaledTexPoint.y), 0.0, 0.0));
			//URP			
			tex.r = 1 - tex.r;
			//v2.0.8
			WorldScale = _InteractTexturePos.y;
			CamPos = float3(0, 0, 0);//_DepthCameraPos;//_WorldSpaceCameraPos;
			Origin = float2(0, 0);//float2(CamPos.x - WorldScale/2.0 , CamPos.z - WorldScale/2.0);
			UnscaledTexPoint = float2(tileableUv.x - Origin.x, tileableUv.y - Origin.y);
			ScaledTexPoint = float2(UnscaledTexPoint.x / WorldScale, UnscaledTexPoint.y / WorldScale);
			float4 texInteract = tex2Dlod(_InteractTexture, float4(ScaledTexPoint, 0.0, 0.0));
			if (_textureFeed == 1) { //v2.1.20b
				positionWS.y = positionWS.y*_respectHeight + (pow(abs(heightDepthCam - tex.r*heightDepthCam), _ShoreFadeFactor) + IN.texcoord.y*_grassHeight);
				positionWS.y = positionWS.y + _BaseHeight + _DepthCameraPos.y - heightDepthCam; //v2.1.16
			}

			//NOISE
			float noise = tex2Dlod(_NoiseTexture, float4(ScaledTexPoint * _NoiseFreqX, 0.0, 0.0));
			float influence = 1.0 - _NoiseAmplitude;
			float d2 = (1.0 - influence) / 2.0;
			noise -= d2;
			noise *= 1.0 / influence;
			//noise = clamp(noise, 0.0, 1.0);
			//if (IN.texcoord.y > 0) {
			positionWS.y = positionWS.y + noise * _NoiseFreqZ;
			//}



			//vertex position logic done, complete posWS -> posCS
			OUT.positionCS = TransformWorldToHClip(positionWS);

			/////////////////////////////////////////////////////////////////////
			//lighting & color
			/////////////////////////////////////////////////////////////////////

			//lighting data
			Light mainLight;
#if _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_CASCADE
			mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
#else
			mainLight = GetMainLight();
#endif
			half3 randomAddToN = (_RandomNormal* sin(perGrassPivotPosWS.x * 82.32523 + perGrassPivotPosWS.z) + wind * -0.25) * cameraTransformRightWS;//random normal per grass 
																																					  //default grass's normal is pointing 100% upward in world space, it is an important but simple grass normal trick
																																					  //-apply random to normal else lighting is too uniform
																																					  //-apply cameraTransformForwardWS to normal because grass is billboard
			half3 N = normalize(half3(0,1,0) + randomAddToN - cameraTransformForwardWS * 0.5);

			half3 V = viewWS / ViewWSLength;
			half3 albedo = lerp(_GroundColor,_BaseColor, IN.positionOS.y);//you can use texture if you wish to

																		  //indirect
			half3 lightingResult = SampleSH(0) * albedo;

			//main direct light
			lightingResult += ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);

			// Additional lights loop
#if _ADDITIONAL_LIGHTS

			// Returns the amount of lights affecting the object being renderer.
			// These lights are culled per-object in the forward renderer
			int additionalLightsCount = GetAdditionalLightsCount();
			for (int i = 0; i < additionalLightsCount; ++i)
			{
				// Similar to GetMainLight, but it takes a for-loop index. This figures out the
				// per-object light index and samples the light buffer accordingly to initialized the
				// Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
				Light light = GetAdditionalLight(i, positionWS);

				// Same functions used to shade the main light.
				lightingResult += ApplySingleDirectLight(light, N, V, albedo, positionOS.y);
			}
#endif

			//fog
			float fogFactor = ComputeFogFactor(OUT.positionCS.z);
			// Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
			// with a custom one.
			OUT.color = MixFog(lightingResult, fogFactor);

			return OUT;
		}

		half4 frag(Varyings IN) : SV_Target
		{
			return half4(IN.color,1);
		}
			ENDHLSL

		}
			//////////// END SHADOW
    }
}