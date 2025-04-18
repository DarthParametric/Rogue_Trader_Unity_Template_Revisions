Shader "Hidden/ScreenSpaceFogOfWar"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "Includes/PFCore.cginc"

			#pragma multi_compile _ MSAA_FOW

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

			sampler2D_float _CameraDepthTexture;
			float _ScreenSpaceFowMaxDistance;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.cameraRay = CreateRay(o.pos.xyz / o.pos.w);
				return o;
			}

			float GetDepth(float2 uv)
			{
				#if ORTHOGRAPHIC_PROJECTION_ON
					float z = _ProjectionParams.y + SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv) * (_ProjectionParams.z - _ProjectionParams.y);
				#else
					float z = DECODE_EYEDEPTH(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
				#endif

				return z;
			}

			float GetFowMask(float depth, float3 cameraRay)
			{
				float3 worldPos = ReconstructPosition(cameraRay, _WorldSpaceCameraPos.xyz, depth);

				float2 fowUv = TRANSFORM_TEX(worldPos.xz, _FogOfWarMask);
				float4 fogOfWar = tex2Dlod(_FogOfWarMask, float4(fowUv, 0, 0));

				fogOfWar.g *= _FogOfWarColor.a;
				float mask = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
				mask = saturate(mask);

				mask = (depth > _ScreenSpaceFowMaxDistance * _ProjectionParams.z) ? 1 : mask;

				return mask;
			}

			float4 frag (v2f IN) : SV_Target
			{
				if (_FogOfWarGlobalFlag)
				{
					#ifdef MSAA_FOW
						float2 texelOffset = _ScreenParams.zw - 1;
						float depth0 = GetDepth(IN.uv);
						float depth1 = GetDepth(IN.uv + float2(texelOffset.x, 0));
						float depth2 = GetDepth(IN.uv + float2(0, texelOffset.y));
						float depth3 = GetDepth(IN.uv + float2(texelOffset.x, texelOffset.y));

						float mask0 = GetFowMask(depth0, IN.cameraRay);
						float mask1 = GetFowMask(depth1, IN.cameraRay);
						float mask2 = GetFowMask(depth2, IN.cameraRay);
						float mask3 = GetFowMask(depth3, IN.cameraRay);

						float mask = min(min(mask0, mask1), min(mask2, mask3));

						float4 result = 0;
						result.rgb = _FogOfWarColor.rgb * mask;
						result.a = 1 - mask;

						return result;
					#else
						float z = GetDepth(IN.uv);
						float mask = GetFowMask(z, IN.cameraRay);
						//finalColor.rgb = lerp(_FogOfWarColor.rgb, finalColor.rgb, mask);

						float4 result = 0;
						result.rgb = _FogOfWarColor.rgb * mask;
						result.a = 1 - mask;

						return result;
					#endif
				}
				else
				{
					return 0;
				}
			}
			ENDCG
		}
	}
}
