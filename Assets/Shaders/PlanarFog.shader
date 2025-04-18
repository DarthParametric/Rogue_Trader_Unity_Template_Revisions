Shader "PF/PlanarFog"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}

	CGINCLUDE
	#include "Includes/PFCore.cginc"
	ENDCG
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Transparent+1" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }
			Blend One OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				FOG_OF_WAR_COORDS(5)
				UNITY_FOG_COORDS(6)
			};

			sampler2D _FogFluidMask;
			float4 _FogFluidMask_ST;
			sampler2D _CameraDepthTexture;
			float _Density;

			FragmentInputData vert (VertexInputData v)
			{
				FragmentInputData o = VertexCommon(v);

				half3 posWorld = half3(o.tangentToWorldAndWorldPos[0].w, o.tangentToWorldAndWorldPos[1].w, o.tangentToWorldAndWorldPos[2].w);
				half3 normalWorld = o.tangentToWorldAndWorldPos[2].xyz;

				o.uv.zw = TRANSFORM_TEX(posWorld.xz, _FogFluidMask);

				#if UNITY_SHOULD_SAMPLE_SH
					o.ambient.rgb = ShadeSHPerVertex (normalWorld, o.ambient.rgb);
				#endif

				#ifdef VERTEX_LIGHTING_ON
					float4 screenPos = ComputeScreenPos(o.pos);
					o.ambient.rgb += LightIndexLighting(screenPos.xy / screenPos.w, posWorld, normalWorld);
				#endif

				return o;
			}
			
			half4 frag (FragmentInputData i) : SV_Target
			{
				return tex2D(_FogFluidMask, i.uv.zw);

				//// refraction and depth texture coords
				//float waterDepth = i.screenPos.w;
				//i.screenPos.xy /= i.screenPos.w;
				//i.screenPos.zw = i.screenPos.xy;
				//#if UNITY_UV_STARTS_AT_TOP
				//	i.screenPos.w = 1 - i.screenPos.w;
				//#endif

				//float2 mainUV = i.uv;

				//#if defined(NOISE_ON)
				//	float2 noiseUV = tex2D(_NoiseMap, i.uv1).rg * 2.0 - 1.0;
				//	noiseUV *= _NoiseScale;
				//	mainUV += noiseUV;
				//#endif

				//fixed4 tex = tex2D(_MainTex, mainUV);
				//
				//// depth and density
				//float sceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.screenPos.xy);
				//sceneDepth = LinearEyeDepth(sceneDepth);
				//float linearDepth = sceneDepth - waterDepth;
				//float expDepth = saturate(1.0 - exp(-_Density * tex.r * linearDepth));

				//float4 finalColor = _LightColor0 * _BaseColor;

				//UNITY_APPLY_FOG(i.fogCoord, finalColor);
				//APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor);

				//return finalColor * expDepth;
			}
			ENDCG
		}

		//Pass
		//{
		//	Name "ForwarAdd"
		//	Tags{ "LightMode" = "ForwardAdd" }
		//	Blend One One
		//	Fog{ Color(0,0,0,0) }
		//	ZWrite Off
		//	ZTest LEqual

		//	CGPROGRAM
		//	#pragma vertex vert
		//	#pragma fragment frag
		//	#pragma multi_compile_fwdadd
		//	#pragma multi_compile_fog
		//	#pragma multi_compile __ FLOW_ON
		//	#include "UnityCG.cginc"
		//	#include "AutoLight.cginc"
		//	#include "Lighting.cginc"

		//	struct appdata
		//	{
		//		float4 vertex : POSITION;
		//		float2 uv : TEXCOORD0;
		//		float3 normal : NORMAL;
		//		float4 tangent : TANGENT;
		//	};

		//	struct v2f
		//	{
		//		float4 pos : SV_POSITION;
		//		float2 uv : TEXCOORD0;
		//		float2 uv1 : TEXCOORD1;
		//		float3 lightDir : TEXCOORD2;
		//		float3 viewDir : TEXCOORD3;
		//		FOG_OF_WAR_COORDS(4)
		//		UNITY_FOG_COORDS(5)
		//		float3 posLight : TEXCOORD6;
		//		float4 posWorld : TEXCOORD7;
		//		float4 screenPos : TEXCOORD8;
		//	};

		//	sampler2D _SecondLayer;
		//	float4 _SecondLayer_ST;
		//	sampler2D _CameraDepthTexture;

		//	float _Density;
		//	float4 _BaseColor;

		//	v2f vert (appdata v)
		//	{
		//		VERTEX_COMMON_TANGENT
		//		UNITY_TRANSFER_FOG(o, o.pos);

		//		o.uv1 = TRANSFORM_TEX(v.uv, _SecondLayer);
		//		o.screenPos = ComputeScreenPos(o.pos);

		//		o.posWorld = mul(unity_ObjectToWorld, v.vertex);
		//		#ifdef POINT
		//			o.posLight = mul(unity_WorldToLight, float4(o.posWorld.xyz, 1)).xyz;
		//		#endif
		//		
		//		TRANSFER_FOG_OF_WAR(o, o.posWorld.xz, o.pos);

		//		return o;
		//	}
		//	
		//	fixed4 frag (v2f i) : SV_Target
		//	{
		//		float attenuation = 1.0f;
		//		#ifdef POINT
		//			float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;

		//			float distance = i.posLight.z;
		//			// use z coordinate in light space as signed distance
		//			attenuation = tex2D(_LightTexture0, dot(i.posLight, i.posLight)).UNITY_ATTEN_CHANNEL;
		//		#endif

		//		// refraction and depth texture coords
		//		float waterDepth = i.screenPos.w;
		//		i.screenPos.xy /= i.screenPos.w;
		//		i.screenPos.zw = i.screenPos.xy;
		//		#if UNITY_UV_STARTS_AT_TOP
		//				i.screenPos.w = 1 - i.screenPos.w;
		//		#endif

		//		fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_SecondLayer, i.uv1);
		//		
		//		// depth and density
		//		float sceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.screenPos.xy);
		//		sceneDepth = LinearEyeDepth(sceneDepth);
		//		float linearDepth = sceneDepth - waterDepth;
		//		float expDepth = saturate(1.0 - exp(-_Density * tex.r * linearDepth));

		//		float4 finalColor = _LightColor0 * attenuation * expDepth * _BaseColor;

		//		// apply fog
		//		UNITY_APPLY_FOG_COLOR(i.fogCoord, finalColor, fixed4(0, 0, 0, 0));
		//		APPLY_FOG_OF_WAR(i.fogOfWarCoords, finalColor);

		//		return finalColor;
		//	}
		//	
		//	ENDCG
		//}
	}

	CustomEditor "PlanarFogShaderGUI"
}
