﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PF/Particles"
{
	Properties
	{
		_TintColor("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_UV0Speed("UV0 Speed", Vector) = (0,0,0,0)
		_AlphaScale("Alpha Scale", float) = 1
		_HDRColorScale("HDR Color Scale", float) = 1.0
		_HDRColorClamp("HDR Clamp", float) = 100.0
		_MainTex1("Texture", 2D) = "white" {}
		_UV1Speed("UV1 Speed", Vector) = (0,0,0,0)
		[ToggleOff] _ApplyTexSheetUvTex1("Apply Texture Sheet Animation", float) = 1
		_MainTex1Weight("Main Tex 1 Weight", float) = 1.0
		[Enum(Lerp RGBA,1,Lerp RGB Multiply A,2)] _Tex1MixMode("Mix Mode", float) = 1.0
		_BumpMap("Bump Map", 2D) = "bump" {}
		_UVBumpSpeed("UV Bump Speed", Vector) = (0,0,0,0)
		[ToggleOff] _ApplyTexSheetUvBump("Apply Texture Sheet Animation", float) = 1
		_Distortion ("Distortion", float) = 1.0
		_DistortionColorFactor("Distortion Color Factor", Range(0,1)) = .5
		_DistortionOffset("Distortion UV Offset", Vector) = (0,0,0,0)
		_Noise0Tex("Noise 0", 2D) = "black" {}
		_Noise1Tex("Noise 1", 2D) = "black" {}
		_Noise0Scale("Noise 0 Scale", float) = 0.0
		_Noise0IDSpeedScale("Noise 0 ID Scale", float) = 1.0
		_Noise0Speed("Noise 0 Speed", Vector) = (0,0,0,0)
		[ToggleOff] _ApplyTexSheetUvNoise0("Apply Texture Sheet Animation", float) = 1
		_Noise1Scale("Noise 1 Scale", float) = 1.0
		_Noise1IDSpeedScale("Noise 1 ID Scale", float) = 0.0
		_Noise1Speed("Noise 1 Speed", Vector) = (0,0,0,0)
		[ToggleOff] _ApplyTexSheetUvNoise1("Apply Texture Sheet Animation", float) = 1
		[ToggleOff] _RandomizeNoiseOffset("Apply Texture Sheet Animation", float) = 0
		_ColorAlphaRamp("Color Alpha Ramp", 2D) = "white" {}
		_RampScrollSpeed("Ramp Scroll Speed", float) = 0
		_RampAlbedoWeight("Ramp -> Albedo x Ramp Lerp", Range(0, 1)) = 0
		_Cutout("Cutout", Range(0, 1)) = .001
		_Softness("Мягонькость", float) = .5
		[ToggleOff] _SubstractSoftness ("Отнять мягонькость от альфы", float) = 0
		_VirtualOffset("Virtual Offset", float) = 0
		_OpacityFalloff("Opacity Falloff", Range(0, 10)) = 1
		[ToggleOff] _SubstractVertexAlpha("Substract Vertex Alpha", float) = 0
		[ToggleOff] _FogOfWarMaterialFlag("Fog Of War Affected", float) = 1
		_FogInfluence("Unity Fog Influence", Range(0, 1)) = .5
		_LightingBackfaceFactor("Lighting backface factor", Range(0,1)) = .5
		_LightingWrapFactor("Half Lambert Amount", Range(1,.5)) = .5
		_LightingVertexInfluence("Vertex Lighting Influence", Range(0, 1)) = .5

		[HideInInspector]
		_TexSheetEnabled("__texSheet", float) = 0.0

		[HideInInspector]
		_Type("__type", float) = 0.0
		[HideInInspector]
		_SrcBlend ("__src", float) = 1.0
		[HideInInspector]
		_DstBlend ("__dst", float) = 0.0
		[HideInInspector] 
		_ZWrite ("__zw", float) = 1.0
		[HideInInspector]
		_CullMode ("__cull", float) = 0.0
		//[HideInInspector]
		_Queue("__queue", float) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent+30" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }
			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]
			Cull [_CullMode]
			ColorMask RGB
			Stencil
			{
                Ref 0
                Comp always
                Pass replace
            }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_fwdbase
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
			#define VERTEXLIGHT_ON
			#pragma shader_feature __ ALPHABLEND_ON ALPHAPREMULTIPLY_ON ALPHABLENDMULTIPLY_ON
			#pragma shader_feature __ SOFT_PARTICLES
			#pragma shader_feature __ NOISE0_ON
			#pragma shader_feature __ NOISE1_ON
			#pragma shader_feature __ NOISE_UV_CORRECTION
			#pragma shader_feature __ COLOR_ALPHA_RAMP
			#pragma shader_feature __ DISTORTION_ON
			#pragma shader_feature __ PARTICLES_LIGHTING_ON
			#pragma shader_feature __ TEXTURE1_ON
			#pragma shader_feature __ BUMP_ON
			#pragma shader_feature __ OVERRIDE_NORMAL_ON
			#pragma shader_feature __ OPACITY_FALLOFF
			#pragma shader_feature __ INVERT_OPACITY_FALLOFF
			#pragma shader_feature __ FLUID_FOG
			#pragma shader_feature __ WORLD_UV_XZ
			#pragma shader_feature __ LIGHT_PROBES_ONLY
			#pragma target 4.0

			#define FOG_OF_WAR_ON
			//#define FLUID2D_PARTICLES

			#include "Includes/PFCore.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL0;
				#if defined(PARTICLES_LIGHTING_ON)
					float4 tangent : TANGENT0;
				#endif
				fixed4 color : COLOR0;
				float4 uv : TEXCOORD0;
				#if defined(FLUID2D_PARTICLES)
					float2 uv2 : TEXCOORD1;
				#endif
				#if defined(NOISE0_ON) || defined(NOISE1_ON) || defined(COLOR_ALPHA_RAMP)
					float4 particleID : TEXCOORD1;
				#endif
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR0;
				float4 uv : TEXCOORD0;
				#if defined(TEXTURE1_ON) || defined(BUMP_ON)
					float4 uv1 : TEXCOORD1;
				#endif
				UNITY_FOG_COORDS(2)
				FOG_OF_WAR_COORDS(3)
				#if defined(SOFT_PARTICLES) || defined(DISTORTION_ON)
					float4 screenPos : TEXCOORD4;
				#endif
				#if defined(NOISE0_ON) || defined(NOISE1_ON)
					float4 noiseUV : TEXCOORD5;
				#endif
				#if defined(PARTICLES_LIGHTING_ON)
					float3 basisCol0 : TEXCOORD6;
					float3 basisCol1 : TEXCOORD7;
					float3 basisCol2 : TEXCOORD8;
					float3 lightProbe : TEXCOORD9;
				#endif
				#if defined(OPACITY_FALLOFF) || defined(PARTICLES_LIGHTING_ON)
					float4 lightDir : TEXCOORD10;
				#endif
				#if defined(FLUID_FOG) || defined(COLOR_ALPHA_RAMP)
					float3 fluidAndRampUv : TEXCOORD11;
				#endif
			};

			float2 _UV0Speed;

			sampler2D _MainTex1;
			float4 _MainTex1_ST;
			float2 _UV1Speed;
			float _Tex1MixMode;
			float _MainTex1Weight;
			float _ApplyTexSheetUvTex1;

			float _HDRColorScale;
			float _HDRColorClamp;

			float4 _BumpMap_ST;
			float2 _UVBumpSpeed;
			float _ApplyTexSheetUvBump;

			float _Distortion;
			float _DistortionColorFactor;
			float4 _DistortionOffset;
			sampler2D _CameraDepthTexture;
			sampler2D _ColorAlphaRamp;
			float4 _ColorAlphaRamp_ST;
			float _RampAlbedoWeight;
			float _RampScrollSpeed;
			float _RandomizeRampOffset;
			
			sampler2D _Noise0Tex;
			float4 _Noise0Tex_ST;
			float _Noise0Scale;
			float _Noise0IDSpeedScale;
			float2 _Noise0Speed;
			float _ApplyTexSheetUvNoise0;
			float _RandomizeNoiseOffset;
			float _TexSheetEnabled;

			sampler2D _Noise1Tex;
			float4 _Noise1Tex_ST;
			float _Noise1Scale;
			float _Noise1IDSpeedScale;
			float2 _Noise1Speed;
			float _ApplyTexSheetUvNoise1;

			float _SubstractVertexAlpha;
			float _Softness;
			float _SubstractSoftness;
			float _TimeEditor;
			float _VirtualOffset;
			float _OpacityFalloff;

			sampler2D _RefractionTex;
			sampler2D _Fluid2DParticlesPosTex;
			float4 _Fluid2DParticlesPosTex_TexelSize;

			float _Fluid2DParticlesParticleSize;

			float _LightingBackfaceFactor;
			float _LightingWrapFactor;
			float _LightingVertexInfluence;

			sampler2D _FogFluidMask;
			float4 _FogFluidMask_ST;

			/*float3 _HL2Basis0 = float3(sqrt(2.0 / 3.0), 0, 1.0 / sqrt(3.0));
			float3 _HL2Basis1 = float3(-1.0 / sqrt(6.0), 1.0 / sqrt(2.0), 1.0 / sqrt(3.0));
			float3 _HL2Basis2 = float3(-1.0 / sqrt(6.0), -1.0 / sqrt(2.0), 1.0 / sqrt(3.0));*/

			const float3 _HL2Basis0 = float3(0.8164, 0, 0.57735);
			const float3 _HL2Basis1 = float3(-0.4082, 0.7071, 0.57735);
			const float3 _HL2Basis2 = float3(-0.4082, -0.7071, 0.57735);

			//Сделано по статье
			//http://roxlu.com/downloads/scholar/008.rendering.practical_particle_lighting.pdf
			//Позволяет сделать нормал-маппинг для вертексных(!) источников света
			float3x3 HL2Lighting(
				float4 lightPosX, float4 lightPosY, float4 lightPosZ,
				float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
				float4 lightAttenSq,
				float3 pos, float3 normal)
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

				float4 dot0 = 0;
				float4 dot1 = 0;
				float4 dot2 = 0;

				const float3 _HL2Basis0 = float3(0.8164, 0, 0.57735);
				const float3 _HL2Basis1 = float3(-0.4082, 0.7071, 0.57735);
				const float3 _HL2Basis2 = float3(-0.4082, -0.7071, 0.57735);

				dot0 += -toLightX * _HL2Basis0.x;
				dot0 += -toLightY * _HL2Basis0.y;
				dot0 += -toLightZ * _HL2Basis0.z;

				dot1 += -toLightX * _HL2Basis1.x;
				dot1 += -toLightY * _HL2Basis1.y;
				dot1 += -toLightZ * _HL2Basis1.z;

				dot2 += -toLightX * _HL2Basis2.x;
				dot2 += -toLightY * _HL2Basis2.y;
				dot2 += -toLightZ * _HL2Basis2.z;

				dot0 *= _LightingWrapFactor + (1 - _LightingWrapFactor);
				dot1 *= _LightingWrapFactor + (1 - _LightingWrapFactor);
				dot2 *= _LightingWrapFactor + (1 - _LightingWrapFactor);

				// don't produce NaNs if some vertex position overlaps with the light
				lengthSq = max(lengthSq, 0.000001);
				float4 rangeSq = 25 / lightAttenSq;
				// attenuation
				float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);
				float4 invLength = rsqrt(lengthSq);
				//double-sided lighting
				dot0 = abs(dot0 * invLength);
				dot1 = abs(dot1 * invLength);
				dot2 = abs(dot2 * invLength);
				
				float4 normalizedSqrDist = lengthSq / rangeSq;
				float4 kToZeroFadeStart = 0.64f; // .8f * .8f
				float4 lerpFactor = normalizedSqrDist > kToZeroFadeStart;
				atten *= lerp(1, 1 - (normalizedSqrDist - kToZeroFadeStart) / (1 - kToZeroFadeStart), lerpFactor);
				atten *= normalizedSqrDist > 1 ? 0 : 1;

				float3x3 result;
				result[0] = lightColor0 * dot0.x * atten.x + lightColor1 * dot0.y * atten.y + lightColor2 * dot0.z * atten.z + lightColor3 * dot0.w * atten.w;
				result[1] = lightColor0 * dot1.x * atten.x + lightColor1 * dot1.y * atten.y + lightColor2 * dot1.z * atten.z + lightColor3 * dot1.w * atten.w;
				result[2] = lightColor0 * dot2.x * atten.x + lightColor1 * dot2.y * atten.y + lightColor2 * dot2.z * atten.z + lightColor3 * dot2.w * atten.w;

				return result;
			}

			float3 GetNormal(float2 uv)
			{
				#ifdef BUMP_ON
					return UnpackNormal(tex2D(_BumpMap, uv));
				#else
					return float3(0, 0, 1);
				#endif
			}

			float4 ApplyFogOfWarOnFx(float4 finalColor, float2 fogOfWarCoords, float2 uv, float treshold)
			{
				float enable = _FogOfWarGlobalFlag * _FogOfWarMaterialFlag;
				if (enable > 0)
				{
					float4 fogOfWar = tex2D(_FogOfWarMask, fogOfWarCoords);
					#if !defined(PARTICLES_LIGHTING_ON)
						fogOfWar.g = treshold > 0 ? 1 : fogOfWar.g * _FogOfWarColor.a;
					#else
						fogOfWar.g = fogOfWar.g * _FogOfWarColor.a;
					#endif
					float mask = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
					mask = saturate(mask);
					finalColor.rgb = lerp(_FogOfWarColor.rgb, finalColor.rgb, mask);
				}

				return finalColor;
			}
						
			v2f vert (appdata v)
			{
				v2f o = (v2f)0;

				#if defined(FLUID2D_PARTICLES)
					float2 uv = v.uv2 + _Fluid2DParticlesPosTex_TexelSize.xy * .5;
					float4 pos = tex2Dlod(_Fluid2DParticlesPosTex, float4(uv, 0, 0));
					v.vertex.xyz *= _Fluid2DParticlesParticleSize;
					v.vertex.xz += pos.xy * 50;
				#endif

				o.pos = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o, o.pos);
				float2 offsetPos = o.pos.zw + float2(UNITY_MATRIX_P[2][2], UNITY_MATRIX_P[3][2]) * _VirtualOffset;
				o.pos.z = (offsetPos.x / offsetPos.y) * o.pos.w;

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

				#if defined(WORLD_UV_XZ)
					v.uv.xy = worldPos.xz;
				#endif

				o.color = v.color * _TintColor * float4(_HDRColorScale, _HDRColorScale, _HDRColorScale,1);
				o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex) + _UV0Speed.xy * (_Time.yy + _TimeEditor);

				o.uv.zw = _TexSheetEnabled > 0 ? v.uv.zw : v.uv.xy;

				#if defined(TEXTURE1_ON)
					float2 tex1Uv = _ApplyTexSheetUvTex1 > 0 ? v.uv.xy : v.uv.zw;
					o.uv1.xy = TRANSFORM_TEX(tex1Uv, _MainTex1) + _UV1Speed.xy * (_Time.yy + _TimeEditor);
				#endif

				#if defined(BUMP_ON)
					float2 bumpUv = _ApplyTexSheetUvBump > 0 ? v.uv.xy : v.uv.zw;
					o.uv1.zw = TRANSFORM_TEX(bumpUv, _BumpMap) + _UVBumpSpeed.xy * (_Time.yy + _TimeEditor);
				#endif

				#if defined(NOISE0_ON)
					float2 noise0Uv = _ApplyTexSheetUvNoise0 > 0 ? v.uv.xy : v.uv.zw;
					o.noiseUV.xy = TRANSFORM_TEX(noise0Uv, _Noise0Tex) + _Noise0Speed.xy * (_Time.yy + _TimeEditor) + lerp(v.uv.xx, v.particleID.xx, _RandomizeNoiseOffset) * _Noise0IDSpeedScale;
				#endif

				#if defined(NOISE1_ON)
					float2 noise1Uv = _ApplyTexSheetUvNoise1 > 0 ? v.uv.xy : v.uv.zw;
					o.noiseUV.zw = TRANSFORM_TEX(noise1Uv, _Noise1Tex) + _Noise1Speed.xy * (_Time.yy + _TimeEditor) + lerp(v.uv.xx, v.particleID.xx, _RandomizeNoiseOffset) * _Noise1IDSpeedScale;
				#endif

				#if defined(SOFT_PARTICLES) || defined(DISTORTION_ON)
					o.screenPos = ComputeScreenPos(o.pos);
					o.screenPos.z = COMPUTE_EYEDEPTH(o.screenPos.z);
				#endif

				#if defined(PARTICLES_LIGHTING_ON) || defined(OPACITY_FALLOFF)
					half3 eyeVec = normalize(_WorldSpaceCameraPos - worldPos.xyz);
					#if defined(OVERRIDE_NORMAL_ON)
						float3 worldNormal = eyeVec;
					#else
						float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					#endif
				#endif

				#if defined(PARTICLES_LIGHTING_ON)
					#ifdef UNITY_SHOULD_SAMPLE_SH
						// ambient is included in SH
						o.lightProbe = saturate(ShadeSH9(half4(worldNormal, 1.0)));
					#endif

					#if defined(VERTEXLIGHT_ON)
						float3x3 basisCol = HL2Lighting(
							unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							unity_4LightAtten0, worldPos.xyz, worldNormal);
						o.basisCol0 = basisCol[2];
						o.basisCol1 = basisCol[1];
						o.basisCol2 = basisCol[0];
					#endif
					
					float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - worldPos.xyz, _WorldSpaceLightPos0.w));
					float3 viewDir;
					TangentSpaceLightingInput(worldNormal, -v.tangent, lightDirection, eyeVec, o.lightDir.xyz, viewDir);
				#endif

				#if defined(OPACITY_FALLOFF)
					float VdotN = dot(eyeVec.xyz, worldNormal);
					float vertexNormalsSlope = max(0.0, abs(VdotN));
					#if defined(INVERT_OPACITY_FALLOFF)
						float2 madCoeff = float2(1, -1);
					#else
						float2 madCoeff = float2(0, 1);
					#endif
						vertexNormalsSlope = saturate(madCoeff.x + madCoeff.y * vertexNormalsSlope);
					o.lightDir.w = pow(vertexNormalsSlope, _OpacityFalloff);
				#endif

				TRANSFER_FOG_OF_WAR(o, worldPos.xz);

				#if defined(FLUID_FOG)
					o.fluidAndRampUv.xy = TRANSFORM_TEX(worldPos.xz, _FogFluidMask);
				#endif

				#if defined(COLOR_ALPHA_RAMP)
					o.fluidAndRampUv.z = v.particleID.y * _RandomizeRampOffset;
				#endif

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				#if defined(SOFT_PARTICLES)
					float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos))) + _VirtualOffset;
					float softFactor = saturate(1.0 - exp(-_Softness * (sceneZ - i.screenPos.z)));
					//return softFactor;
				#endif

				float2 mainUV = i.uv;
				float2 noiseUV0 = 0;
				float2 noiseUV1 = 0;
				float4 uv1Bump = 0;

				#if (defined(NOISE_UV_CORRECTION) && (defined(NOISE0_ON) || defined(NOISE1_ON)))
					float2 uvCorrection = i.uv.zw * 2.0 - 1.0;
					float noiseFade = saturate(1 - length(uvCorrection));
				#endif

				#if defined(NOISE0_ON)
					noiseUV0 = tex2D(_Noise0Tex, i.noiseUV.xy).rg * 2.0 - 1.0;
					#if defined(NOISE_UV_CORRECTION)
						_Noise0Scale *= noiseFade;
					#endif
					noiseUV0 *= _Noise0Scale;
				#endif

				#if defined(NOISE1_ON)
					noiseUV1 = tex2D(_Noise1Tex, i.noiseUV.zw).rg * 2.0 - 1.0;
					#if defined(NOISE_UV_CORRECTION)
						_Noise1Scale *= noiseFade;
					#endif
					noiseUV1 *= _Noise1Scale;
				#endif

				mainUV += noiseUV0 + noiseUV1;
				
				#if defined(TEXTURE1_ON) || defined(BUMP_ON)
					uv1Bump = i.uv1 + noiseUV0.xyxy + noiseUV1.xyxy;
				#endif

				float3 n = GetNormal(uv1Bump.zw);

				#if defined(FLUID_FOG)
					fixed4 tex = tex2D(_FogFluidMask, i.fluidAndRampUv.xy + noiseUV0 + noiseUV1);
				#else
					fixed4 tex = tex2D(_MainTex, mainUV);
				#endif

				#if defined(TEXTURE1_ON)
					fixed4 tex1 = tex2D(_MainTex1, uv1Bump.xy);

					if (_Tex1MixMode <= 1)
					{
						tex = lerp(tex, tex1, _MainTex1Weight);
					}
					else
					{
						tex.rgb = lerp(tex, tex1, _MainTex1Weight);
						tex.a *= tex1.a;
					}
				#endif

				#if defined(COLOR_ALPHA_RAMP)
					float2 colorRampUv = float2(tex.a * _ColorAlphaRamp_ST.x + _ColorAlphaRamp_ST.z, .5);
					colorRampUv.x += i.fluidAndRampUv.z + _RampScrollSpeed * (_Time.y + _TimeEditor);
					float3 ramp = tex2D(_ColorAlphaRamp, colorRampUv);
					tex.rgb = lerp(ramp, ramp * tex.rgb, _RampAlbedoWeight);
				#endif

				#if defined(SOFT_PARTICLES)
					if (_SubstractSoftness > 0)
					{
						tex.a = saturate(tex.a - (1 - softFactor));
					}
				#endif

				if (_SubstractVertexAlpha > 0)
				{
					tex.a = saturate(tex.a - (1 - i.color.a));
				}
				else
				{
					tex.a *= i.color.a;
				}

				float alpha = tex.a;
				alpha = saturate(alpha * _AlphaScale);
				#if defined(OPACITY_FALLOFF)
					alpha *= i.lightDir.w;
				#endif
				tex.a = alpha;

				fixed4 finalColor = fixed4(tex.rgb * i.color.rgb, tex.a);

				#if defined(ALPHAPREMULTIPLY_ON)
					finalColor.rgb *= finalColor.a;
				#endif

				if (_Cutout > 0)
				{
					clip(finalColor.a - _Cutout);
				}

				#if defined(PARTICLES_LIGHTING_ON)
					float NdotLFront = max(0.0, dot(n, i.lightDir) * _LightingWrapFactor + (1 - _LightingWrapFactor));
					float NdotLBack = max(0.0, dot(-n, i.lightDir) * _LightingWrapFactor + (1 - _LightingWrapFactor));
					float3 diffuseColor = NdotLFront * _LightColor0.rgb + NdotLBack * _LightColor0.rgb * (1 - finalColor.a * _LightingBackfaceFactor);

					#if defined(VERTEXLIGHT_ON)
						const float3 _HL2Basis0 = float3(0.8164, 0, 0.57735);
						const float3 _HL2Basis1 = float3(-0.4082, 0.7071, 0.57735);
						const float3 _HL2Basis2 = float3(-0.4082, -0.7071, 0.57735);
						float3 w = saturate(float3(dot(n, _HL2Basis0), dot(n, _HL2Basis1), dot(n, _HL2Basis2)));
						diffuseColor += (i.basisCol0 * w.x + i.basisCol1 * w.y + i.basisCol2 * w.z) * _LightingVertexInfluence;
					#endif

					diffuseColor += i.lightProbe.rgb;
					finalColor.rgb *= diffuseColor;
				#endif

				float4 preFogColor = finalColor;
				
				UNITY_APPLY_FOG_COLOR(i.fogCoord, finalColor, unity_FogColor * finalColor.a);
				finalColor = lerp(preFogColor, finalColor, _FogInfluence);

				APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor, i.uv.xy)

				finalColor.rgb = clamp(finalColor.rgb, float3(0, 0, 0), _HDRColorClamp.xxx);
				
				#if defined(DISTORTION_ON)
					i.screenPos.xy /= i.screenPos.w;
					/*#if UNITY_UV_STARTS_AT_TOP
						i.screenPos.y = 1 - i.screenPos.y;
					#endif*/
					float2 distort = n.xy * _Distortion;
					fixed4 refraction = tex2D(_RefractionTex, i.screenPos.xy + distort.xy + _DistortionOffset.xy);
					finalColor.rgb = lerp(finalColor.rgb, refraction.rgb * finalColor.a, _DistortionColorFactor);
				#endif

				#if defined(SOFT_PARTICLES)
					// TODO: здесь нужно домножать ТОЛЬКО альфа-канал, но т.к. уже много контента сделано, то оставим на след. проект
					finalColor *= softFactor;
				#endif

				#if defined(ALPHABLENDMULTIPLY_ON)
					finalColor = lerp(fixed4(1, 1, 1, 1), finalColor, finalColor.a);
				#endif

				#if defined(ALPHABLEND_ON) || defined(ALPHAPREMULTIPLY_ON)
					finalColor.a = finalColor.a;
				#else
					finalColor.a = 1;
				#endif

				return finalColor;
			}
			ENDCG
		}
	}

	CustomEditor "ParticlesShaderGUI"
}
