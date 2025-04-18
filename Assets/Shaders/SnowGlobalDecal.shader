Shader "PF/Decals/SnowGlobalDecal"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Tint Color", Color) = (1,1,1,1)
		_ScatterPower ("Scatter Power", float) = 0
		_ScatterThreshold("Scatter Threshold", Range(0, 1)) = 1
		_SlopePower("Slope Power", Range(0, 10)) = 1
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Includes/PFCore.cginc"

			sampler2D _CameraNormalsTex;
			sampler2D_float _CameraDepthTexture;
			float _SlopePower;
			float _ScatterPower;
			float _ScatterThreshold;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 cameraRay : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.cameraRay = CreateRay(o.pos.xyz / o.pos.w);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				#if ORTHOGRAPHIC_PROJECTION_ON
					float z = _ProjectionParams.y + SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv) * (_ProjectionParams.z - _ProjectionParams.y);
				#else
					float z = DECODE_EYEDEPTH(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
				#endif

				float3 worldPos = ReconstructPosition(i.cameraRay, _WorldSpaceCameraPos.xyz, z);
				float2 mainUv = TRANSFORM_TEX(worldPos.xz, _MainTex);
				float3 normal = normalize(tex2D(_CameraNormalsTex, i.uv).rgb * 2 - 1).rgb;
				float slope = saturate(dot(normal, float3(0, 1, 0)) * _SlopePower);
				
				float4 diff = tex2D(_MainTex, mainUv * 0.02);
				float altG = tex2D(_MainTex, mainUv * 0.5).g;
				float altNoise = diff.r * altG - 0.7;
				float alpha = (altNoise - _ScatterThreshold) * _ScatterPower * slope;
				alpha = pow(alpha, _ScatterPower);

				return float4(diff.rgb * _Color.rgb, alpha);
			}
			ENDCG
		}
	}

	//CustomEditor "SnowGlobalDecalGUI"
}
