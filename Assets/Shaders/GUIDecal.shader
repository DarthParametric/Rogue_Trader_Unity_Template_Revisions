// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PF/Decals/GUIDecal"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Cutout("Cutout", Range(0, 1)) = .5
		[ToggleOff] _FogOfWarMaterialFlag("Fog Of War Affected", float) = 1
		[Enum(MaxOpacityTop,0,MaxOpacityMiddle,1,MaxOpacityBottom,2)] _GradientMode("Gradient Mode", float) = 1
		[ToggleOff] _ExpGradient("Exp Gradient", float) = 0
		_SlopeFadeStart("Slope Fade Start", Range(0, 1)) = 1
		_SlopeFadePower("Slope Fade Power", float) = 1

		[HideInInspector]
		_Type("__type", float) = 0.0
		[HideInInspector]
		_SrcBlend ("__src", Float) = 1.0
		[HideInInspector]
		_DstBlend ("__dst", Float) = 0.0
		[HideInInspector]
		_StencilRef("__stencilRef", float) = 0

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Blend[_SrcBlend][_DstBlend]
			ZTest GEqual
			ZWrite Off
			Cull Front
			Stencil
			{
				Ref 4 // ignore characters
				//ReadMask [_StencilRef]
				Comp NotEqual
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature __ FOG_OF_WAR_DISSOLVE_ON
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma shader_feature __ CIRCLE_ON
			#define FOG_OF_WAR_ON

			#include "Includes/PFCore.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 screenUV : TEXCOORD1;
				float3 ray : TEXCOORD2;
				float radius : TEXCOORD3;
				float3 projectionDir : TEXCOORD4;
			};
			
			sampler2D_float _CameraDepthTexture;
			float4 _CameraDepthTexture_TexelSize;
			sampler2D _CameraNormalsTex;
			float _SlopeFadeStart;
			float _SlopeFadePower;
			float _GradientMode;
			float _ExpGradient;
			
			v2f vert (appdata v)
			{
				v2f o = (v2f)0;
				o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
				o.uv = v.vertex.xyz + 0.5;
				o.screenUV = ComputeScreenPos(o.pos);
				o.ray = UnityObjectToViewPos(float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);

				float4 p0 = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
				float4 p1 = mul(unity_ObjectToWorld, float4(1, 0, 0, 1));
				o.radius = length(p1.xyz - p0.xyz);

				o.projectionDir = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, -1, 0)));

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
				float2 uv = i.screenUV.xy / i.screenUV.w;
				// read depth and reconstruct world position
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				depth = Linear01Depth (depth);
				float4 vpos = float4(i.ray * depth,1);
				float3 wpos = mul (unity_CameraToWorld, vpos).xyz;
				float3 opos = mul (unity_WorldToObject, float4(wpos,1)).xyz;

				clip(float3(0.5, 0.5, 0.5) - abs(opos.xyz));

				i.uv = opos.xz + 0.5;

				#if defined(CIRCLE_ON)
					float2 circleUV = i.uv * 2 - 1;
					float angle = atan2(circleUV.y, circleUV.x);
					angle = (angle / (UNITY_PI * 2)) + .5;
					// длина окружности
					float segmentCount = i.radius * UNITY_PI * 2;
					float uvX = angle * (int)_MainTex_ST.x + _MainTex_ST.z;
					uvX *= (int)segmentCount;

					float uvY = saturate((length(circleUV) * i.radius - i.radius + _MainTex_ST.y) / _MainTex_ST.y);

					//return uvY;
					
					circleUV = float2(uvX, uvY);

					float4 albedo = tex2Dlod(_MainTex, float4(circleUV, 0, 0)) * _Color;
				#else
					float4 albedo = tex2Dlod(_MainTex, float4(i.uv * _MainTex_ST.xy + _MainTex_ST.zw, 0, 0)) * _Color;
				#endif

				#if defined(CUTOUT_ON)
					clip(albedo.a - _Cutout);
				#endif

				#if defined(ALPHAPREMULTIPLY_ON)
					albedo.rgb *= albedo.a;
				#endif
				
				float4 finalColor = albedo;
				
				// slope fade
				float alphaModifier = 1;
				float3 normal = normalize(tex2D(_CameraNormalsTex, uv).rgb * 2 - 1);
				float slopeFactor = dot(normal.xyz, i.projectionDir) + (1 - _SlopeFadeStart);
				slopeFactor = saturate(1 - slopeFactor);
				slopeFactor = pow(slopeFactor, _SlopeFadePower);
				alphaModifier *= slopeFactor;

				// height gradient fade
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

				finalColor.a *= alphaModifier;

				float4 fogOfWarUV = 0;
				fogOfWarUV.xy = TRANSFORM_TEX(wpos.xz, _FogOfWarMask);
				APPLY_FOG_OF_WAR(fogOfWarUV, finalColor, i.uv.xy);

				return finalColor;
			}
			ENDCG
		}
	}

	CustomEditor "GUIDecalShaderGUI"
}
