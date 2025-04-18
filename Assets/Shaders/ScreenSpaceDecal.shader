Shader "PF/Decals/ScreenSpaceDecal"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_UV0Speed("UV0 Speed", Vector) = (0,0,0,0)
		_MainTex1("Texture", 2D) = "white" {}
		_UV1Speed("UV1 Speed", Vector) = (0,0,0,0)
		_MainTex1Weight("Main Tex 1 Weight", Range(0,1)) = 1.0
		[Enum(Lerp RGBA,1,Lerp RGB Multiply A,2)] _Tex1MixMode("Mix Mode", float) = 1.0
		_Noise0Tex("Noise 0", 2D) = "black" {}
		_Noise1Tex("Noise 1", 2D) = "black" {}
		_Noise0Scale("Noise 0 Scale", float) = 0.0
		_Noise0Speed("Noise 0 Speed", Vector) = (0,0,0,0)
		_Noise1Scale("Noise 1 Scale", float) = 1.0
		_Noise1Speed("Noise 1 Speed", Vector) = (0,0,0,0)
		_EmissionTex("Emission mask (A-channel)", 2D) = "white" {}
		_EmissionUVSpeed("Emission UV Speed", Vector) = (0,0,0,0)
		_Emission("Emission Intensity", float) = 1
		_EmissionColor("Emission Color", Color) = (1,1,1,1)
		_EmissionColorFactor("Emission Color Factor", Range(0, 1)) = 1
		_Color ("Color", Color) = (1,1,1,1)
		_HdrColorScale ("HDR Color Scale", float) = 1
		//_HdrColorClamp ("HDR Clamp", float) = 100
		_AlphaScale ("Alpha Scale", float) = 1
		_Cutout("Cutout", Range(0, 1)) = .5
		_ColorAlphaRamp("Color Alpha Ramp", 2D) = "white" {}
		_RampScrollSpeed("Ramp Scroll Speed", float) = 0
		_RampAlbedoWeight("Ramp -> Albedo x Ramp Lerp", Range(0, 1)) = 0
		[ToggleOff] _WorldUvXz ("Snap UV to World XZ", float) = 0
		//[ToggleOff] _FogOfWarMaterialFlag("Fog Of War Affected", float) = 1

		[Enum(MaxOpacityTop,0,MaxOpacityMiddle,1,MaxOpacityBottom,2)] _GradientMode("Gradient Mode", float) = 0
		[ToggleOff] _ExpGradient("Exp Gradient", float) = 0

		_SlopeFadeStart("Slope Fade Start", Range(0, 1)) = 1
		_SlopeFadePower("Slope Fade Power", float) = 1

		[HideInInspector]
		_FullScreenDecal("Is Full Screen", float) = 0

		[HideInInspector]
		_Type("__type", float) = 0.0
		[HideInInspector]
		_SrcBlend ("__src", Float) = 1.0
		[HideInInspector]
		_DstBlend ("__dst", Float) = 0.0

		[HideInInspector]
		_ZTest ("__ztest", float) = 0
		[HideInInspector]
		_CullMode ("__cullmode", float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Blend[_SrcBlend][_DstBlend]
			ZTest [_ZTest] //GEqual
			ZWrite Off
			Cull [_CullMode] //Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma shader_feature __ EMISSION_ON
			#pragma shader_feature __ SLOPE_FADE_ON
			#pragma shader_feature __ GRADIENT_FADE_ON
			#pragma shader_feature __ TEXTURE1_ON
			#pragma shader_feature __ NOISE0_ON
			#pragma shader_feature __ NOISE1_ON
			#pragma shader_feature __ NOISE_UV_CORRECTION
			#pragma shader_feature __ COLOR_ALPHA_RAMP

			#include "Includes/PFCore.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD2;
				float3 projectionDir : TEXCOORD3;
			};
			
			float2 _UV0Speed;

			sampler2D _MainTex1;
			float4 _MainTex1_ST;
			float2 _UV1Speed;
			float _Tex1MixMode;
			float _MainTex1Weight;

			sampler2D _Noise0Tex;
			float4 _Noise0Tex_ST;
			float _Noise0Scale;
			float2 _Noise0Speed;

			sampler2D _Noise1Tex;
			float4 _Noise1Tex_ST;
			float _Noise1Scale;
			float2 _Noise1Speed;

			sampler2D_float _CameraDepthTexture;
			sampler2D _CameraNormalsTex;
			sampler2D _EmissionTex;
			float4 _EmissionUVSpeed;
			sampler2D _ColorAlphaRamp;
			float4 _ColorAlphaRamp_ST;
			float _RampScrollSpeed;
			float4 _EmissionTex_ST;
			float4 _CameraDepthTexture_TexelSize;
			float _SlopeFadeStart;
			float _SlopeFadePower;
			float _EmissionColorFactor;
			float _GradientMode;
			float _ExpGradient;
			float _HdrColorScale;
			//float _HdrColorClamp;
			float _RampAlbedoWeight;
			float _SubstractAlphaFlag;
			float _WorldUvXz;
			float _FullScreenDecal;
			
			v2f vert (appdata v)
			{
				v2f o = (v2f)0;
				o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
				
				if (_FullScreenDecal > 0)
				{
					o.ray = CreateRay(o.pos.xyz / o.pos.w);
					o.uv = v.uv;
				}
				else
				{
					o.ray = UnityObjectToViewPos(float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);
					o.uv = v.vertex.xyz + 0.5;
				}
				
				o.projectionDir = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, -1, 0)));

				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				float3 wpos = 0;
				float3 opos = 0;
				float2 screenUv = i.uv;

				// read depth and reconstruct world position
				if (_FullScreenDecal > 0)
				{
					float z = DECODE_EYEDEPTH(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
					wpos = ReconstructPosition(i.ray, _WorldSpaceCameraPos.xyz, z);
					opos = wpos;
				}
				else
				{
					i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
					screenUv = i.pos.xy / _ScreenParams.xy;
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUv);
					depth = Linear01Depth(depth);
					float4 vpos = float4(i.ray * depth, 1);
					wpos = mul(unity_CameraToWorld, vpos).xyz;
					opos = mul(unity_WorldToObject, float4(wpos,1)).xyz;
				}

				if (_FullScreenDecal < 1)
				{
					clip(float3(0.5, 0.5, 0.5) - abs(opos.xyz));
				}
				
				float2 uv1 = opos.xz + 0.5;
				float2 uv0 = uv1;
				if (_WorldUvXz > 0 || _FullScreenDecal > 0)
				{
					uv0 = wpos.xz;
				}

				float2 noiseUV0 = 0;
				float2 noiseUV1 = 0;

				#if (defined(NOISE_UV_CORRECTION) && (defined(NOISE0_ON) || defined(NOISE1_ON)))
					float2 uvCorrection = uv0 * 2.0 - 1.0;
					float noiseFade = saturate(1 - length(uvCorrection));
				#endif

				#if defined(NOISE0_ON)
					noiseUV0 = TRANSFORM_TEX(uv0, _Noise0Tex) + _Noise0Speed.xy * _Time.yy;
					noiseUV0 = tex2Dlod(_Noise0Tex, float4(noiseUV0, 0, 0)).rg * 2.0 - 1.0;
					#if defined(NOISE_UV_CORRECTION)
						_Noise0Scale *= noiseFade;
					#endif
					noiseUV0 *= _Noise0Scale;
				#endif

				#if defined(NOISE1_ON)
					noiseUV1 = TRANSFORM_TEX(uv0, _Noise1Tex) + _Noise1Speed.xy * _Time.yy;
					noiseUV1 = tex2Dlod(_Noise1Tex, float4(noiseUV1, 0, 0)).rg * 2.0 - 1.0;
					#if defined(NOISE_UV_CORRECTION)
						_Noise1Scale *= noiseFade;
					#endif
					noiseUV1 *= _Noise1Scale;
				#endif

				float2 albedoUV = TRANSFORM_TEX(uv0, _MainTex) + _UV0Speed.xy * _Time.yy + noiseUV0 + noiseUV1;
				float4 albedo = tex2Dlod(_MainTex, float4(albedoUV, 0, 0));

				#if defined(TEXTURE1_ON)
					float2 albedoUV1 = TRANSFORM_TEX(uv1, _MainTex1) + _UV1Speed.xy * _Time.yy + noiseUV0 + noiseUV1;
					float4 tex1 = tex2D(_MainTex1, albedoUV1);

					if (_Tex1MixMode <= 1)
					{
						albedo = lerp(albedo, tex1, _MainTex1Weight);
					}
					else
					{
						albedo.rgb = lerp(albedo.rgb, tex1.rgb, _MainTex1Weight);
						albedo.a *= tex1.a;
					}
				#endif

				#if defined(COLOR_ALPHA_RAMP)
					float2 rampUv = float2(albedo.a * _ColorAlphaRamp_ST.x + _ColorAlphaRamp_ST.z, .5);
					rampUv.x += _RampScrollSpeed * _Time.y;
					float3 ramp = tex2D(_ColorAlphaRamp, rampUv);
					albedo.rgb = lerp(ramp, ramp * albedo.rgb, _RampAlbedoWeight);
				#endif

				albedo.rgb *= _Color.rgb * _HdrColorScale;
				albedo.rgb = saturate(albedo.rgb);

				float alphaModifier = 1;

				#if defined(SLOPE_FADE_ON)
					float3 normal = normalize(tex2D(_CameraNormalsTex, screenUv).rgb * 2 - 1);
					float slopeFactor = dot(normal.xyz, i.projectionDir) + (1 - _SlopeFadeStart);

					slopeFactor = saturate(1 - slopeFactor);
					slopeFactor = pow(slopeFactor, _SlopeFadePower);

					alphaModifier *= slopeFactor;
				#endif

				#if defined(GRADIENT_FADE_ON)
					float gradientFactor = abs(opos.y) * 2;
					gradientFactor += _GradientMode == 0 && opos.y > 0 ? 1 : 0;
					gradientFactor += _GradientMode == 2 && opos.y < 0 ? 1 : 0;
					gradientFactor = saturate(gradientFactor);
					if (_ExpGradient > 0)
					{
						gradientFactor = 1 - saturate(pow(gradientFactor, 3));
					}
					else
					{
						gradientFactor = 1 - gradientFactor;
					}

					alphaModifier *= gradientFactor;
				#endif

				if (_SubstractAlphaFlag > 0)
				{
					albedo.a = saturate(albedo.a - (1 - _Color.a));
				}
				else
				{
					albedo.a *= _Color.a;
				}

				albedo.a = saturate(albedo.a * _AlphaScale) * alphaModifier;

				if (_Cutout > 0)
				{
					clip(albedo.a - _Cutout);
				}

				#if defined(ALPHAPREMULTIPLY_ON)
					albedo.rgb *= albedo.a;
				#endif

				float emissionGradient = 0;
				#if defined(EMISSION_ON)
					float2 emissionUV = TRANSFORM_TEX(uv0, _EmissionTex) + _EmissionUVSpeed.xy * _Time.yy;
					float4 parameters = tex2Dlod(_EmissionTex, float4(emissionUV, 0, 0));
					emissionGradient = _Emission * parameters.r * _EmissionColor.a * albedo.a;
					float emissionFactor = emissionGradient;
					float3 emissionColor = lerp(albedo.rgb, _EmissionColor.rgb, parameters.a * _EmissionColorFactor);
					//albedo.rgb += emissionColor * emissionFactor;
					albedo.rgb = emissionColor.rgb * max(0, 1 - emissionFactor) + emissionColor * emissionFactor;
					//albedo.rgb = lerp(albedo.rgb, emissionColor, emissionFactor);
					//albedo.a = emissionFactor > 0 ? -emissionFactor : albedo.a;
				#endif

				// pack two 8-bit gradient into one 16-bit float
				//albedo.a = floor(emissionGradient * 255) + albedo.a;
				return albedo;
				
				//float4 finalColor = albedo;
				/*#if !defined(ALPHAPREMULTIPLY_ON) && !defined(ALPHABLEND_ON)
					finalColor.a = 1;
				#endif*/
				/*#if defined(EMISSION_ON)
					finalColor.a = -finalColor.a;
				#endif*/

				//finalColor.rgb = clamp(finalColor.rgb, float3(0, 0, 0), _HdrColorClamp.xxx);

				/*float4 fogOfWarUV = 0;
				fogOfWarUV.xy = TRANSFORM_TEX(wpos.xz, _FogOfWarMask);

				APPLY_FOG_OF_WAR(fogOfWarUV, finalColor);*/

				//return finalColor;
			}
			ENDCG
		}
	}

	CustomEditor "ScreenSpaceDecalGUI"
}
