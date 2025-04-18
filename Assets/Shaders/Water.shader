Shader "PF/Water/Water"
{
	Properties
	{
		// main settings
		_MaskTex("Masks RG = Flow map, B = Flow noise, A = Foam mask", 2D) = "grey" {}
		_BumpMap("Bump 1", 2D) = "bump" {}
		_BumpMap1("Bump 2", 2D) = "bump" {}
		_BaseColor("Base Color", Color) = (1,1,1,1)
		_AmbientColor("Ambient Color", Color) = (0,0,0,1)

		// procedural flow
		_FlowDirections ("XY - Flow direction 1 ZW - Flow direction 2", Vector) = (0,0,0,0)
		_FlowSpeed("Flow Speed", Range(-5, 5)) = 1
		_WavesLerpSpeed("WavesLerpSpeed", Range(0, 2)) = 0.5
		
		// foam
		_MainTex("Foam Texture", 2D) = "white" {}
		_FoamRamp("Foam Ramp", 2D) = "white" {}
		_FoamColor("Foam Color", Color) = (1,1,1,1)
		_FoamStrength ("Foam Strength", Range(0, 10)) = 1
		_FoamPower ("Foam Power", Range(0, 10)) = 1
		_FoamMaskScale("Foam Mask Scale", Range(0, 2)) = 1
		
		// lighting
		_Distortion("Distortion", Range(0, 1)) = 1
		_SpecularPower("Specular Power", Range(0,1)) = 1
		_SpecularIntensity("Specular Intensity", Range(0,10)) = 1
		_Density("Density", Range(0, 1)) = 1
		_ShorePower("Shore Power", Range(0, 1)) = 0

		// flow map
		_FlowNoiseStrength("Flow Noise Strength", Range(0, 2)) = 1

		[ToggleOff] _FogOfWarMaterialFlag("Fog Of War Affected", float) = 1

		_FluidIntensity("Fluid Intensity", Range(0, 2)) = 1

		[Header(Reflections Common)]
		_FresnelPower("Fresnel Power", Range(0.1, 10)) = 1

		[Header(Planar Reflections)]
		_ReflectionsColor("Reflections Color", Color) = (1,1,1,1)

		// screen space reflections
		[ToggleOff] _AllowBackwardsRays("Allow Backward Rays", float) = 0
		_MaxRayTraceDistance("Max Distance", Range(0.1, 300)) = 100
		_LayerThickness("Width Modifier", float) = .5
		_MaxSteps("Iterations Count", Range(16, 1024)) = 256
		_RayStepSize("Step Size", Range(1, 16)) = 3
		_FadeDistance("Fade Distance", Range(0, 1000)) = 100
		_FresnelFade("Fresnel Fade", Range(0, 1)) = 1
		_ScreenEdgeFading("Screen Edge Fading", Range(0, 1)) = 0.03
	}

	CGINCLUDE
	#define BUMP_ON
	#define FOG_OF_WAR_ON
	#define NEED_SCREEN_POS
	#include "Includes/PFCore.cginc"
	#include "Includes/ScreenSpaceRaytrace.cginc"

	sampler2D _RefractionTexWater;
	sampler2D _ReflectionTex;
	sampler2D _BumpMap1;
	sampler2D _WaterMask;
	sampler2D _WaterFluidMask;
	sampler2D _MaskTex;
	sampler2D _FoamRamp;

	CBUFFER_START(WaterBuffer)
	float4 _BumpMap1_ST;
	float4 _BumpMap_ST;
	float4 _MaskTex_ST;
	float4 _WaterMask_ST;
	float4 _WaterFluidMask_ST;
	float _FluidFactor;
	float _FlowSpeed;
	float _WavesLerpSpeed;
	float4 _FlowDirections;
	float _FlowNoiseStrength;
	float4 _BaseColor;
	float4 _AmbientColor;
	float _Density;
	float _Distortion;
	float _ShorePower;
	float _WaterLocalMapFlag;
	float _FresnelPower;
	float4 _ReflectionsColor;
	float _FluidIntensity;
	float _FoamStrength;
	float4 _FoamColor;
	float _FoamPower;
	float _FoamMaskScale;
	CBUFFER_END

	float3 FetchNormalWorld(sampler2D bumpMap, float2 uv, half4 tangentToWorld[3])
	{
		half3 tangent = tangentToWorld[0].xyz;
        half3 binormal = tangentToWorld[1].xyz;
        half3 normal = tangentToWorld[2].xyz;

        #if UNITY_TANGENT_ORTHONORMALIZE
            normal = normalize(normal);

            // ortho-normalize Tangent
            tangent = normalize (tangent - normal * dot(tangent, normal));

            // recalculate Binormal
            half3 newB = cross(normal, tangent);
            binormal = newB * sign (dot (newB, binormal));
        #endif

        half3 normalTangent = UnpackNormalFromAtlas(tex2D(bumpMap, uv));
        half3 normalWorld = normalize(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z); // @TODO: see if we can squeeze this normalize on SM2.0 as well
		return normalWorld;
	}

	float3 GetNormal(float2 uv, half4 tangentToWorld[3], float4 flowDirections)
	{
		half3 normal = tangentToWorld[2].xyz;
		float3 n = normal;
		#if FLOW_ON
			float2 flowDir = flowDirections.xy;

			float phase0 = flowDirections.z;
			float phase1 = flowDirections.w;

			float3 tex0 = FetchNormalWorld(_BumpMap, uv * _BumpMap_ST.xy + flowDir.xy * phase0, tangentToWorld);
			float3 tex1 = FetchNormalWorld(_BumpMap1, uv * _BumpMap1_ST.xy + flowDir.xy * phase1, tangentToWorld);

			float flowLerp = abs(0.5 - phase0) / 0.5;
			float3 bump = lerp(tex0, tex1, flowLerp);
			n = normalize(bump);
		#else
			float3 tex0 = FetchNormalWorld(_BumpMap, uv * _BumpMap_ST.xy + _Time[1] * flowDirections.xy, tangentToWorld);
			float3 tex1 = FetchNormalWorld(_BumpMap1, uv * _BumpMap1_ST.xy + _Time[1] * flowDirections.zw, tangentToWorld);
			n = lerp(tex0, tex1, .5);
			//float3 n = normalize(float3(tex0.xy + tex1.xy, tex0.z * tex1.z));
		#endif

		return lerp(normal, n, _Distortion);
	}

	float4 GetFlowDirection(float4 maskTex)
	{
		#if FLOW_ON
			float2 flowDir = maskTex.rg * float2(-2.0, 2.0) - float2(-1.0, 1.0);
			flowDir *= _FlowSpeed;

			float noise = maskTex.b * _FlowNoiseStrength;

			float phase0 = frac(_Time[1] * _WavesLerpSpeed + 0.5f + noise);
			float phase1 = frac(_Time[1] * _WavesLerpSpeed + noise);

			return float4(flowDir.xy, phase0, phase1);
		#else
			return float4(normalize(_FlowDirections.xy), normalize(_FlowDirections.zw)) * _FlowSpeed * .5;
		#endif
	}

	float4 GetFoam(float2 uv, float4 flowDirections)
	{
		#if FLOW_ON
			float2 flowDir = flowDirections.xy;

			float phase0 = flowDirections.z;
			float phase1 = flowDirections.w;

			float foamSpeedScale0 = _MainTex_ST.xy / _BumpMap_ST.xy;
			float foamSpeedScale1 = _MainTex_ST.xy / _BumpMap1_ST.xy;
			float4 tex0 = tex2D(_MainTex, uv * _MainTex_ST.xy + foamSpeedScale0 * flowDir.xy * phase0 - .5);
			float4 tex1 = tex2D(_MainTex, uv * _MainTex_ST.xy + foamSpeedScale1 * flowDir.xy * phase1 - .5);

			float flowLerp = abs(0.5 - phase0) / 0.5;
			return lerp(tex0, tex1, flowLerp);
		#else
			float foamSpeedScale0 = _MainTex_ST.xy / _BumpMap_ST.xy;
			float4 tex0 = tex2D(_MainTex, uv * _MainTex_ST.xy + foamSpeedScale0 * _Time[1] * flowDirections.xy);
			return tex0;
		#endif
	}

	// Shlick approximation of pow() a^b = a / (b - b * a + a);
	float ShlickPow(float x, float pow)
	{
		float a = x;
		float b = pow;
		return a / (b - b * a + a);
	}

	float4 ShlickPow(float4 x, float pow)
	{
		float4 a = x;
		float4 b = pow;
		return a / (b - b * a + a);
	}

	// ************ Screen Space Reflections *************
	float2 _ScreenSize;
	float4 _ProjInfo;
	float4x4 _WorldToCameraMatrix;
	float4x4 _ProjectToPixelMatrix;
	float _AllowBackwardsRays;
	float _LayerThickness;
	float _MaxRayTraceDistance;
	int _MaxSteps;
	float _RayStepSize;
	float3 _CameraClipInfo;
	float _FadeDistance;
	float _FresnelFade;
	float _ScreenEdgeFading;

	float3 ReconstructCSPosition(float2 S, float z)
	{
		float linEyeZ = -LinearEyeDepth(z);
		return float3((((S.xy * _ScreenSize)) * _ProjInfo.xy + _ProjInfo.zw) * linEyeZ, linEyeZ);
	}

	float3 csMirrorVector(float3 csPosition, float3 csN)
	{
		float3 csE = -normalize(csPosition.xyz);
		float cos_o = dot(csN, csE);
		float3 c_mi = normalize((csN * (2.0 * cos_o)) - csE);

		return c_mi;
	}

	inline float  Pow2(float  x) { return x * x; }
	inline float2 Pow2(float2 x) { return x * x; }
	inline float3 Pow2(float3 x) { return x * x; }
	inline float4 Pow2(float4 x) { return x * x; }

	float applyEdgeFade(float2 tsP, float fadeStrength)
	{
		float maxFade = 0.1;

		float2 itsP = float2(1.0, 1.0) - tsP;
		float dist = min(min(itsP.x, itsP.y), min(tsP.x, tsP.x));
		float fade = dist / (maxFade*fadeStrength + 0.001);
		fade = max(min(fade, 1.0), 0.0);
		fade = pow(fade, 0.2);

		return fade;
	}

	float4 GetScreenSpaceReflections(float2 screenSpacePos, float depth, float3 normalWorld)
	{
		float3 csPosition = ReconstructCSPosition(screenSpacePos, depth);

		if (csPosition.z < -100.0)
		{
			return float4(0.0, 0.0, 0.0, 0.0);
		}

		float3 csN = mul((float3x3)(_WorldToCameraMatrix), normalWorld);

		float3 csRayDirection = csMirrorVector(csPosition, csN);

		if (_AllowBackwardsRays == 0 && csRayDirection.z > 0.0)
		{
			return float4(0.0, 0.0, 0.0, 0.0);
		}

		float maxRayTraceDistance = _MaxRayTraceDistance;
		float jitterFraction = 0.0f;
		float layerThickness = _LayerThickness;

		int maxSteps = _MaxSteps;

		// Bump the ray more in world space as it gets farther away (and so each pixel covers more WS distance)
		float rayBump = max(-0.01*csPosition.z, 0.001);
		float2 hitPixel;
		float3 csHitPoint;
		float stepCount;
		float stepRate = _RayStepSize;

		bool wasHit = castDenseScreenSpaceRay
		(csPosition + (csN)* rayBump,
			csRayDirection,
			_ProjectToPixelMatrix,
			_ScreenSize,
			_CameraClipInfo,
			jitterFraction,
			maxSteps,
			layerThickness,
			maxRayTraceDistance,
			hitPixel,
			stepRate,
			false, //_TraceBehindObjects == 1,
			csHitPoint,
			stepCount);

		float2 tsPResult = hitPixel / _ScreenSize;
		float rayDist = dot(csHitPoint - csPosition, csRayDirection);
		float confidence = 0.0;

		if (wasHit)
		{
			confidence = Pow2(1.0 - max(2.0*float(stepCount) / float(maxSteps) - 1.0, 0.0));
			confidence *= clamp(((_MaxRayTraceDistance - rayDist) / _FadeDistance), 0.0, 1.0);

			// Fake fresnel fade
			float3 csE = -normalize(csPosition.xyz);
			confidence *= max(0.0, lerp(pow(abs(dot(csRayDirection, -csE)), _FresnelPower), 1, 1.0 - _FresnelFade));
		}

		// Fade out reflections that hit near edge of screen,
		// to prevent abrupt appearance/disappearance when object go off screen
		float vignette = applyEdgeFade(tsPResult, _ScreenEdgeFading);
		confidence *= vignette;
		confidence *= vignette;

		float4 reflections = tex2D(_RefractionTexWater, tsPResult) * confidence;
		return float4(reflections.rgb, confidence);
	}
	// ************ Screen Space Reflections *************

	// ************ Index Lighting ********************
	float3 ShadeFourPointLightsWater(
		float4 lightPosX, float4 lightPosY, float4 lightPosZ,
		float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
		float4 lightAttenSq,
		float4 invRangeSq,
		float3 pos, float3 normal,
		float3 viewDir,
		float3 baseColor,
		float shoreBlend)
	{
		// to light vectors
		float4 toLightX = lightPosX - pos.x;
		float4 toLightY = lightPosY - pos.y;
		float4 toLightZ = lightPosZ - pos.z;
		// squared lengths
		float4 lengthSq = 0;
		lengthSq += toLightX * toLightX;
		lengthSq += toLightY * toLightY;
		lengthSq += toLightZ * toLightZ;
		// don't produce NaNs if some vertex position overlaps with the light
		lengthSq = max(lengthSq, 0.000001);

		// NdotL
		float4 ndotl = 0;
		ndotl += toLightX * normal.x;
		ndotl += toLightY * normal.y;
		ndotl += toLightZ * normal.z;
		// correct NdotL
		float4 length = sqrt(lengthSq);
		ndotl = max(float4(0, 0, 0, 0), ndotl / length);
		// attenuation
		float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);

		// !!!!!!!!! DON'T DELETE !!!!!!!!!
		// !!! attenuation correction from Engine
		/*static const float kConstantFac = 1.000f;
		static const float kQuadraticFac = 25.0f;
		// where the falloff down to zero should start
		static const float kToZeroFadeStart = 0.8f * 0.8f;
		float LightAttenuateNormalized(float distSqr)
		{
		// match the vertex lighting falloff
		float atten = 1 / (kConstantFac + CalculateLightQuadFac(1.0f) * distSqr);

		// ...but vertex one does not falloff to zero at light's range;
		// So force it to falloff to zero at the edges.
		if (distSqr >= kToZeroFadeStart)
		{
		if (distSqr > 1)
		atten = 0;
		else
		atten *= 1 - (distSqr - kToZeroFadeStart) / (1 - kToZeroFadeStart);
		}

		return atten;
		}*/

		float4 normalizedSqrDist = lengthSq * invRangeSq;
		float4 kToZeroFadeStart = 0.64f; // .8f * .8f
		float4 lerpFactor = normalizedSqrDist > kToZeroFadeStart;
		atten *= lerp(1, 1 - (normalizedSqrDist - kToZeroFadeStart) / (1 - kToZeroFadeStart), lerpFactor);
		atten *= normalizedSqrDist > 1 ? 0 : 1;

		ndotl *= shoreBlend;
		
		// half vectors
		float4 hX = viewDir.x + toLightX / length;
		float4 hY = viewDir.y + toLightY / length;
		float4 hZ = viewDir.z + toLightZ / length;

		float3 h0 = normalize(float3(hX.x, hY.x, hZ.x));
		float3 h1 = normalize(float3(hX.y, hY.y, hZ.y));
		float3 h2 = normalize(float3(hX.z, hY.z, hZ.z));
		float3 h3 = normalize(float3(hX.w, hY.w, hZ.w));

		float4 ndoth = 0;
		ndoth.x = dot(normal, h0);
		ndoth.y = dot(normal, h1);
		ndoth.z = dot(normal, h2);
		ndoth.w = dot(normal, h3);
		float4 specular = ShlickPow(ndoth, 1024.0 * _SpecularPower) * _SpecularIntensity;

		// final color
		float3 col = 0;
		col += (baseColor * ndotl.x + specular.x) * lightColor0 * atten.x;
		col += (baseColor * ndotl.y + specular.y) * lightColor1 * atten.y;
		col += (baseColor * ndotl.z + specular.z) * lightColor2 * atten.z;
		col += (baseColor * ndotl.w + specular.w) * lightColor3 * atten.w;

		return col;
	}

	float3 ClusteredLightingWater(FragmentInputData IN, float3 posWS, float3 normalWS, float3 viewDir, float3 baseColor, float shoreBlend)
	{
		float depth = LinearViewDepth(IN.posVS.xyz, IN.pos.xyz);

		float2 screenUv = IN.pos.xy / _ScreenParams.xy;

		#if UNITY_UV_STARTS_AT_TOP
			screenUv.y = 1 - screenUv.y;
		#endif

		float depthSliceIndex = depth * _Clusters.z;
		int4 clustersUv = int4(screenUv.xy * _Clusters.xy, depthSliceIndex, 0);
		uint lightMask = asuint(_ClustersRT.Load(clustersUv));

		int count = 0;
		float d = 0;
		float4 posAndRange[4];
		float4 colorAndAtten[4];
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			posAndRange[i] = 0;
			colorAndAtten[i] = 0;
		}
		while (lightMask && count < 4)
		{
			// Extract a light from the mask and disable that bit
			int mapIndex = firstbitlow(lightMask);
			lightMask &= ~(1 << mapIndex);

			// Get light index from index map
			int offset = _ZBins[depthSliceIndex].x;
			int lightIndex = _PunctualLightIndexMap[offset + mapIndex];

			posAndRange[count] = _LightPosAndRange[lightIndex];
			colorAndAtten[count] = _LightColorAndAtten[lightIndex];

			count++;
		}

		return ShadeFourPointLightsWater(
			float4(posAndRange[0].x, posAndRange[1].x, posAndRange[2].x, posAndRange[3].x),
			float4(posAndRange[0].y, posAndRange[1].y, posAndRange[2].y, posAndRange[3].y),
			float4(posAndRange[0].z, posAndRange[1].z, posAndRange[2].z, posAndRange[3].z),
			colorAndAtten[0].xyz, colorAndAtten[1].xyz, colorAndAtten[2].xyz, colorAndAtten[3].xyz,
			float4(colorAndAtten[0].w, colorAndAtten[1].w, colorAndAtten[2].w, colorAndAtten[3].w),
			float4(posAndRange[0].w, posAndRange[1].w, posAndRange[2].w, posAndRange[3].w),
			posWS, normalWS, viewDir, baseColor, shoreBlend);
	}
	// ************ Index Lighting ********************
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="AlphaTest+10" }
		LOD 100

		// Grab the screen behind the object into _RefractionTexWater
        GrabPass
        {
            "_RefractionTexWater"
        }

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_fwdbase_fullshadows
			#pragma skip_variants VERTEXLIGHT_ON
			#pragma multi_compile_fog
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
			#pragma shader_feature __ FLOW_ON
			#pragma shader_feature __ FOAM_ON
			#pragma shader_feature __ FOAM_DEPTH
			#pragma shader_feature __ FOAM_MASK
			#pragma multi_compile __ WATER_INTERACTION
			#pragma multi_compile __ FLUID_ON
			#pragma multi_compile __ SCREEN_SPACE_REFLECTIONS

			#define SPECULAR_ON
			#pragma target 3.5

			FragmentInputData vert(VertexInputData v)
			{
				FragmentInputData o = VertexCommon(v);

				float3 posWorld = float3(o.tangentToWorldAndWorldPos[0].w, o.tangentToWorldAndWorldPos[1].w, o.tangentToWorldAndWorldPos[2].w);
				float3 normalWorld = o.tangentToWorldAndWorldPos[2].xyz;

				o.uv = v.uv.xyxy;

				#if defined(WATER_INTERACTION)
					o.uv.zw = TRANSFORM_TEX(posWorld.xz, _WaterMask);
				#endif

				#if defined(FLUID_ON) && defined(FOG_OF_WAR_ON)
					o.fogOfWarCoords.zw = TRANSFORM_TEX(posWorld.xz, _WaterFluidMask);
				#endif

				o.screenPos = ComputeScreenPos(o.pos);

				return o;
			}
			
			float4 frag (FragmentInputData i) : SV_Target
			{
				float4 maskTex = 0;
				#if defined(FLOW_ON) || defined(FOAM_ON)
					maskTex = tex2D(_MaskTex, i.uv);
				#endif

				float4 flowDir = GetFlowDirection(maskTex);

				float3 posWorld = float3(i.tangentToWorldAndWorldPos[0].w, i.tangentToWorldAndWorldPos[1].w, i.tangentToWorldAndWorldPos[2].w);
				float3 normalWorld = GetNormal(i.uv.xy, i.tangentToWorldAndWorldPos, flowDir);
				float3 viewDir = normalize(posWorld.xyz - _WorldSpaceCameraPos.xyz);

				float4 baseColor = _BaseColor;
				float density = _Density;
				UnityLight mainLight = MainLight();

				#if defined(WATER_INTERACTION)
					float4 interactionMask = tex2D(_WaterMask, i.uv.zw);
					//return baseColor;
					//density = lerp(density, 10, fluidMask.a);
					//return waterMask.a;
					// TODO: перенести расчет нормали в функцию GetNormal
					interactionMask.rgb = interactionMask.rgb * 2.0 - 1.0;
					normalWorld = normalize(float3(normalWorld.xy + interactionMask.xy, normalWorld.z * interactionMask.z));
				#endif

				float fluidFactor = 0;
				#if defined(FLUID_ON)
					float4 fluidMask = tex2D(_WaterFluidMask, i.fogOfWarCoords.zw);
					fluidFactor = saturate(fluidMask.a * _FluidFactor * _FluidIntensity);
					baseColor.rgb = lerp(baseColor.rgb, fluidMask.rgb, fluidFactor);
					//return baseColor;
					//return fluidFactor;
				#endif

				float2 ssP = i.screenPos.xy / i.screenPos.w;

				/*#if UNITY_UV_STARTS_AT_TOP
                    if (_ProjectionParams.x < 1)
                        ssP.y = 1 - ssP.y;
                #endif*/
				
				// depth
				float2 waterDepth2 = i.screenPos.zw;
				float sceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, ssP);
				float waterDepth = 0;
				if (unity_OrthoParams.w > 0)
				{
					sceneDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, sceneDepth);
					waterDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, waterDepth2.x);
				}
				else
				{
					sceneDepth = LinearEyeDepth(sceneDepth);
					waterDepth = waterDepth2.y;
				}

				float linearDepth = sceneDepth - waterDepth;
				float expDepth = saturate(1.0 - exp(-density * linearDepth));
				float shoreBlend = saturate(1.0 - exp(-_ShorePower * 10 * linearDepth));

				// foam
				float foamResult = 0;
				#if defined(FOAM_ON)
					float4 foam = GetFoam(i.uv, flowDir);
					float foamBlend = 0;
					#if defined(FOAM_DEPTH)
						foamBlend = saturate(exp(-_FoamPower * linearDepth)) * shoreBlend;
					#endif

					#if defined(FOAM_MASK)
						maskTex.a *= _FoamMaskScale * shoreBlend;
						foamBlend = max(foamBlend, maskTex.a);
						//foamBlend *= maskTex.a;
					#endif

						//return foamBlend;
					
					#if defined(WATER_INTERACTION)
						foamBlend = max(interactionMask.a, foamBlend);
					#endif

						//return foamValue;
					float4 foamRamp = tex2D(_FoamRamp, float2(foamBlend, .5));
					foamResult = saturate(dot(foam.rgb, foamRamp.rgb) * _FoamStrength);
					//return foamResult;
					baseColor.rgb = lerp(baseColor.rgb, foamResult * _FoamColor, foamResult * (1 - fluidFactor));
				#endif

				float invFoamResult = 1 - foamResult;

				// lighting
				// specular
				float3 h = normalize(-viewDir + mainLight.dir);
				float NdotH = dot(normalWorld, h);
				float3 specular = ShlickPow(NdotH, 1024.0 * _SpecularPower);
				specular = specular * mainLight.color * _SpecularIntensity;
				// diffuse
				float NdotL = max(0.0, dot(normalWorld, mainLight.dir));
				float3 diffuse = baseColor.rgb * mainLight.color * NdotL;
				
				float3 lightedColor = (diffuse + _AmbientColor * (1 - fluidFactor)) + specular * invFoamResult;

				if (_GlobalClusterLightingEnabled > 0)
				{
					lightedColor += ClusteredLightingWater(i, posWorld, normalWorld, -viewDir, baseColor.rgb, shoreBlend);
				}

				baseColor.rgb = lightedColor;

				// distortion
				float2 distort = normalWorld.xz * _Distortion * .1;
				float distortedDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, ssP + distort);
				distortedDepth = LinearEyeDepth(distortedDepth);
				float distortedExpDepth = saturate(1.0 - exp(-density * (distortedDepth - waterDepth)));
				if (waterDepth > distortedDepth)
				{
					distort = 0;
					distortedExpDepth = expDepth;
				}

				#if defined(SCREEN_SPACE_REFLECTIONS)
					float4 reflections = GetScreenSpaceReflections(ssP, i.screenPos.z / i.screenPos.w, normalWorld);	
				#else
					//Fresnel
					float fresnelLookup = dot(normalWorld, -viewDir);
					float bias = 0.06;
					float fresnelTemp = 1.0 - fresnelLookup;
					float f = bias + (1.0 - bias) * pow(fresnelTemp, _FresnelPower);
					f = saturate(f);

					float4 reflections = tex2D(_ReflectionTex, ssP + distort.xy);
					reflections.a *= f;
					reflections.rgb = reflections.a * _ReflectionsColor;
				#endif

				reflections *= shoreBlend;
				float4 finalColor = lerp(baseColor, reflections, saturate(reflections.a * invFoamResult));

				UNITY_APPLY_FOG(i.fogCoord, finalColor);
				APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor, i.uv.xy);

				float4 refraction = tex2D(_RefractionTexWater, ssP + distort.xy * shoreBlend);
				float refractionBlend = saturate(distortedExpDepth + _WaterLocalMapFlag + foamResult + reflections.a);
				//return refractionBlend;
				finalColor = lerp(refraction, finalColor, refractionBlend);

				return finalColor;
			}
			ENDCG
		}

		Pass
		{
			Name "ForwarAdd"
			Tags{ "LightMode" = "ForwardAdd" }
			Blend One One
			Fog{ Color(0,0,0,0) }
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
			#pragma shader_feature __ FLOW_ON
			#pragma shader_feature __ FOAM_ON
			#pragma shader_feature __ FOAM_DEPTH
			#pragma shader_feature __ FOAM_MASK
			#pragma multi_compile __ WATER_INTERACTION
			#pragma multi_compile __ FLUID_ON

			FragmentInputData vert(VertexInputData v)
			{
				FragmentInputData o = VertexCommon(v);

				float3 posWorld = float3(o.tangentToWorldAndWorldPos[0].w, o.tangentToWorldAndWorldPos[1].w, o.tangentToWorldAndWorldPos[2].w);

				o.uv = v.uv.xyxy;

				#if defined(WATER_INTERACTION)
					o.uv.zw = TRANSFORM_TEX(posWorld.xz, _WaterMask);
				#endif

				#if defined(FLUID_ON) && defined(FOG_OF_WAR_ON)
					o.fogOfWarCoords.zw = TRANSFORM_TEX(posWorld.xz, _WaterFluidMask);
				#endif

				o.screenPos = ComputeScreenPos(o.pos);

				return o;
			}

			float4 frag (FragmentInputData i) : SV_Target
			{
				float4 maskTex = 0;
				#if defined(FLOW_ON) || defined(FOAM_ON)
					maskTex = tex2D(_MaskTex, i.uv);
				#endif

				float4 flowDir = GetFlowDirection(maskTex);

				float3 posWorld = float3(i.tangentToWorldAndWorldPos[0].w, i.tangentToWorldAndWorldPos[1].w, i.tangentToWorldAndWorldPos[2].w);
				float3 normalWorld = GetNormal(i.uv.xy, i.tangentToWorldAndWorldPos, flowDir);
				float3 viewDir = normalize(posWorld.xyz - _WorldSpaceCameraPos.xyz);

				float4 baseColor = _BaseColor;
				float density = _Density;

				#if defined(WATER_INTERACTION)
					float4 interactionMask = tex2D(_WaterMask, i.uv.zw);
					//return baseColor;
					//density = lerp(density, 10, fluidMask.a);
					//return waterMask.a;
					// TODO: перенести расчет нормали в функцию GetNormal
					interactionMask.rgb = interactionMask.rgb * 2.0 - 1.0;
					normalWorld = normalize(float3(normalWorld.xy + interactionMask.xy, normalWorld.z * interactionMask.z));
				#endif

				float fluidFactor = 0;
				#if defined(FLUID_ON)
					float4 fluidMask = tex2D(_WaterFluidMask, i.fogOfWarCoords.zw);
					fluidFactor = saturate(fluidMask.a * _FluidFactor);
					baseColor.rgb = lerp(baseColor.rgb, fluidMask.rgb, fluidFactor);
					//return fluidFactor;
				#endif

				float2 ssP = i.screenPos.xy / i.screenPos.w;
				
				// depth
				float2 waterDepth2 = i.screenPos.zw;
				float sceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, ssP);
				float waterDepth = 0;
				if (unity_OrthoParams.w > 0)
				{
					sceneDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, sceneDepth);
					waterDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, waterDepth2.x);
				}
				else
				{
					sceneDepth = LinearEyeDepth(sceneDepth);
					waterDepth = waterDepth2.y;
				}

				float linearDepth = sceneDepth - waterDepth;
				float expDepth = saturate(1.0 - exp(-density * linearDepth));

				// foam
				float foamResult = 0;
				#if defined(FOAM_ON)
					float4 foam = GetFoam(i.uv, flowDir);
					float foamBlend = 0;
					#if defined(FOAM_DEPTH)
						foamBlend = saturate(exp(-_FoamPower * linearDepth));
					#endif

					#if defined(FOAM_MASK)
						maskTex.a *= _FoamMaskScale;
						foamBlend = max(foamBlend, maskTex.a);
					#endif
					
					#if defined(WATER_INTERACTION)
						foamBlend = max(interactionMask.a, foamBlend);
					#endif
						//return foamValue;
					float4 foamRamp = tex2D(_FoamRamp, float2(foamBlend, .5));
					foamResult = saturate(dot(foam.rgb, foamRamp.rgb) * _FoamStrength);
					//return foamResult;
					baseColor.rgb = lerp(baseColor.rgb, foamResult * _FoamColor, foamResult);
				#endif

				// lighting
				// specular
				float3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - posWorld.xyz, _WorldSpaceLightPos0.w));
				float3 lightColor = _LightColor0.rgb;
				float3 h = normalize(-viewDir + lightDir);
				float NdotH = dot(normalWorld, h);
				float3 specular = ShlickPow(NdotH, 1024.0 * _SpecularPower);
				specular = specular * _SpecularIntensity;
				// diffuse
				float NdotL = max(0.0, dot(normalWorld, lightDir));
				float3 diffuse = baseColor.rgb * NdotL;

				float attenuation = 1.0f;
				#if defined (POINT) || defined (SPOT)
					float3 posLight = mul(unity_WorldToLight, float4(posWorld, 1));
					attenuation = tex2D(_LightTexture0, dot(posLight, posLight).xx).UNITY_ATTEN_CHANNEL;
				#endif

				baseColor.rgb = (diffuse + specular) * lightColor * attenuation;

				float4 finalColor = baseColor;

				UNITY_APPLY_FOG_COLOR(i.fogCoord, finalColor, fixed4(0, 0, 0, 0));
				APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor, i.uv.xy);

				finalColor.rgb *= expDepth;
				
				return finalColor;
			}
			ENDCG
		}
	}

	CustomEditor "WaterShaderGUI"
}
