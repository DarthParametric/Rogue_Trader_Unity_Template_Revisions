Shader "PF/GlobalMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TintColor ("Color", Color) = (1,1,1,1)
		[NoScaleOffset]
		_HiddenAreaTex("Hidden Area Tex", 2D) = "black" {}
		_BorderMask("Border Mask", 2D) = "white" {}
		_BorderColor("Border Color", Color) = (1,1,1,1)
		_BorderScale("Border Scale", float) = 1
		[NoScaleOffset]
		[Normal]
		_BumpMap("Bump Map", 2D) = "bump" {}
		[NoScaleOffset]
		_Parameters("Mask (R=Roughness, G=Emission, B=Metallic)", 2D) = "white" {}
		_Roughness("Roughness", Range(0, 1)) = 1
		_Metallic("Metallic", Range(0, 1)) = 0
		_Emission("Emission", Range(0, 2)) = 1
		/*[ToggleOff] _RimGlobalLighting ("Rim Global Lighting Enabled", float) = 0
		_RimGlobalPower("Rim Global Power", float) = 4
		[ToggleOff] _RimGlobalShadeNdotL ("Rim Global Shade NdotL", float) = 0*/
		[ToggleOff] _RimLighting ("Rim Lighting Enabled", float) = 0
		_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimPower("Rim Power", float) = 4
		[ToggleOff] _RimShadeNdotL("Rim Shade NdotL", float) = 0
		_NdotLPower("NdotL Power", Range(0,2)) = 1
		_FogInfluence("Fog Influence", Range(0, 1)) = 1

		[HideInInspector]
		_QueueOffset("__queueOffset", float) = 0
		[HideInInspector]
		_Type("__type", float) = 0.0
		[HideInInspector]
		_SrcBlend ("__src", Float) = 1.0
		[HideInInspector]
		_DstBlend ("__dst", Float) = 0.0
		[HideInInspector] 
		_ZWrite ("__zw", Float) = 1.0
		[HideInInspector]
		_StencilRef("__stencilRef", float) = 0
		[HideInInspector]
		_StencilCompOp("__stencilCompOp", float) = 0
		[HideInInspector]
		_StencilPassOp("__stencilPassOp", float) = 2 //2==Replace
		[HideInInspector]
		_StencilZFailOp("__stencilZFailOp", float) = 0
	}

	CGINCLUDE
		#define FOG_OF_WAR_ON
		sampler2D _HiddenAreaTex;
		sampler2D _BorderMask;
		float4 _BorderMask_ST;
		float4 _BorderColor;
		float _BorderScale;

		float3 ApplyFogOfWar(sampler2D fogOfWarMask, float3 albedo, float4 fogOfWarCoords, float2 uv)
		{
			float4 hiddenTex = tex2D(_HiddenAreaTex, uv);
			float4 borderMask = tex2D(_BorderMask, fogOfWarCoords.zw);
			float4 fogOfWar = tex2D(fogOfWarMask, fogOfWarCoords.xy);
			float mask = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
			float border = (1 - mask) * mask * borderMask.a * _BorderScale;
			mask = saturate(mask - border);
			float4 hiddenColor = lerp(hiddenTex, _BorderColor, mask);
			albedo.rgb = mask > .9 ? albedo.rgb : hiddenColor.rgb;
			return albedo;
		}
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "Reflection"="Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode"="ForwardBase" }
			Blend[_SrcBlend][_DstBlend]
			Stencil
			{
				Ref [_StencilRef]
				Comp [_StencilCompOp]
				Pass [_StencilPassOp]
				ZFail [_StencilZFailOp]
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_fwdbase_fullshadows
			#pragma skip_variants VERTEXLIGHT_ON
			#pragma multi_compile_fog
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
			#pragma shader_feature __ BUMP_ON
			#pragma shader_feature __ SPECULAR_ON
			#pragma shader_feature __ METALLNESS_ON
			#pragma shader_feature __ EMISSION_ON
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON

			#pragma target 3.5
			#define _GLOSSYREFLECTIONS_OFF

			#include "Includes/PFCore.cginc"

			FragmentInputData vert(VertexInputData v)
			{
				FragmentInputData o = VertexCommon(v);

				o.fogOfWarCoords.zw = TRANSFORM_TEX(v.uv, _BorderMask);

				return o;
			}

			half4 frag(FragmentInputData i) : SV_Target
			{
				float3 decalsEmission = 0;
				float4 albedo = CalculateAlbedo(i, i.pos.xy, decalsEmission);

				albedo.rgb = ApplyFogOfWar(_FogOfWarMask, albedo.rgb, i.fogOfWarCoords, i.uv);
				
				half4 parametersMask = ParametersMask(i.uv);

				albedo.a = saturate(albedo.a * _AlphaScale);

				#if defined(EMISSION_ON)
					half emission = parametersMask.g * _Emission * albedo.rgb;
				#endif

				#if defined(CUTOUT_ON)
					clip(albedo.a - _Cutout);
				#endif

				FragmentCommonData s = FragmentSetup(i, albedo, parametersMask);
				UnityLight mainLight = MainLight();

				UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

				half ndotl = saturate(dot(s.normalWorld, mainLight.dir));
				UnityGI gi = FragmentGI(s, 1, i.ambientOrLightmapUV, atten, mainLight);
				half3 attenuatedLightColor = gi.light.color * pow(ndotl, _NdotLPower);

				half grazingTerm = saturate(s.smoothness + (1 - s.oneMinusReflectivity));
				half ndotv = DotClamped(s.normalWorld, -s.eyeVec);
				half fresnelTerm = Pow4(1 - ndotv);
				half3 r = reflect(s.eyeVec, s.normalWorld);
				half rdotl = DotClamped(r, mainLight.dir);
				half4 finalColor = albedo;
				finalColor.rgb = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, grazingTerm, fresnelTerm);
				finalColor.rgb += BRDF3DirectSimplePF(s.diffColor, s.specColor, s.smoothness, rdotl) * attenuatedLightColor;
				
				if (_GlobalClusterLightingEnabled > 0)
				{
					finalColor.rgb += ClusteredLighting(i, s.posWorld, s.normalWorld, r, s.diffColor, s.specColor, s.smoothness);
				}

				#if defined(EMISSION_ON)
					finalColor.rgb += emission;
				#endif

				finalColor.rgb += decalsEmission;

				// rim
				finalColor = RimLighting(finalColor, ndotv, ndotl);

				// apply fog
				float4 preFogColor = finalColor;
				UNITY_APPLY_FOG(i.fogCoord, finalColor);
				finalColor = lerp(preFogColor, finalColor, _FogInfluence);

				//APPLY_FOG_OF_WAR(i.fogOfWarCoords.xy, finalColor)

				return float4(finalColor.rgb, s.alpha);
			}
			ENDCG
		}

		Pass
		{
			Name "ForwardAdd"
			Tags{ "LightMode" = "ForwardAdd" }
			Blend[_SrcBlend] One
			Fog{ Color(0,0,0,0) }
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
			#pragma shader_feature __ BUMP_ON
			#pragma shader_feature __ SPECULAR_ON
			#pragma shader_feature __ METALLNESS_ON
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma target 3.5

			#include "Includes/PFCore.cginc"

			FragmentInputData vert(VertexInputData v)
			{
				FragmentInputData o = VertexCommon(v);

				o.fogOfWarCoords.zw = TRANSFORM_TEX(v.uv, _BorderMask);

				return o;
			}

			float4 frag(FragmentInputData i) : SV_Target
			{
				float3 decalsEmission = 0;
				float4 albedo = CalculateAlbedo(i, i.pos.xy, decalsEmission);

				//float4 hiddenTex = tex2D(_HiddenAreaTex, i.uv);
				//float4 borderMask = tex2D(_BorderMask, i.fogOfWarCoords.zw);
				//float4 fogOfWar = tex2D(_FogOfWarMask, i.fogOfWarCoords.xy);
				////fogOfWar.g *= _FogOfWarColor.a;
				//float mask = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
				//float border = borderMask.a - mask;
				////mask = mask < 1 ? mask + borderMask.a : mask;
				//mask *= 1 - border;
				////finalColor.rgb = lerp(hiddenTex.rgb, finalColor.rgb, mask);
				//float4 hiddenColor = lerp(hiddenTex, _BorderColor, mask);
				//albedo.rgb = mask > .9 ? albedo.rgb : hiddenColor.rgb;
				////return albedo;

				albedo.rgb = ApplyFogOfWar(_FogOfWarMask, albedo.rgb, i.fogOfWarCoords, i.uv);

				half4 parametersMask = ParametersMask(i.uv);

				albedo.a = saturate(albedo.a * _AlphaScale);

				#if defined(EMISSION_ON)
					half emission = parametersMask.g * _Emission * albedo.rgb;
				#endif

				#if defined(CUTOUT_ON)
					clip(albedo.a - _Cutout);
				#endif

				FragmentCommonData s = FragmentSetup(i, albedo, parametersMask);
				half3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - s.posWorld.xyz, _WorldSpaceLightPos0.w));
				UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

				half ndotl = saturate(dot(s.normalWorld, lightDir));
				half3 r = reflect(s.eyeVec, s.normalWorld);
				half rdotl = DotClamped(r, lightDir);
				
				half3 attenuatedLightColor = _LightColor0.rgb * atten * pow(ndotl, _NdotLPower);

				half4 finalColor = albedo;
				finalColor.rgb = BRDF3DirectSimplePF(s.diffColor, s.specColor, s.smoothness, rdotl) * attenuatedLightColor;

				// apply fog
				float4 preFogColor = finalColor;
				UNITY_APPLY_FOG_COLOR(i.fogCoord, finalColor, fixed4(0, 0, 0, 0));
				finalColor = lerp(preFogColor, finalColor, _FogInfluence);

				//APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor)

				return finalColor;
			}
			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_shadowcaster
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma target 4.0

			#include "Includes/PFCore.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				#if defined(VERTEX_ANIMATION_ON)
					float2 uv3 : TEXCOORD3;
					float4 color : COLOR0;
				#endif
			};

			struct v2f
			{
				V2F_SHADOW_CASTER;
				#if defined(CUTOUT_ON)
					float2 uv : TEXCOORD1;
					#if defined(DISSOLVE_ON)
						float2 uv2 : TEXCOORD2;
					#endif
				#endif
				FOG_OF_WAR_COORDS(3)
			};

			v2f vert(appdata v)
			{
				#if defined(VERTEX_ANIMATION_ON)
					v.vertex = AnimateVertex(v.vertex, v.normal, v.color, v.uv3, _Wind);
				#endif

				v2f o = (v2f)0;
				#if defined(CUTOUT_ON)
					o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
					#if defined(DISSOLVE_ON)
						o.uv2 = TRANSFORM_TEX(v.texcoord, _DissolveTex);
					#endif
				#endif
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)

				#if defined(FOG_OF_WAR_DISSOLVE_ON)
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					TRANSFER_FOG_OF_WAR(o, worldPos.xz, o.pos);
				#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				#if defined(CUTOUT_ON)
					float4 albedo = tex2D(_MainTex, i.uv);
					#if defined(DISSOLVE_ON)
						float4 dissolveMask = tex2D(_DissolveTex, i.uv2);
						float3 dissolveEmission = 0;
						albedo = Dissolve(albedo, dissolveMask.r, dissolveEmission);
					#endif
					clip(albedo.a - _Cutout);
				#endif

				#if defined(FOG_OF_WAR_DISSOLVE_ON)
					float4 finalColor = float4(0,0,0,1);
					APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor)
				#endif

				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}
	}

	CustomEditor "GlobalMapShaderGUI"
}
