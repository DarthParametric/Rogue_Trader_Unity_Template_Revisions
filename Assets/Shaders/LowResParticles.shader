// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/LowResParticles"
{
	SubShader
	{
		// downsample
		Pass
		{
			Cull Off ZTest Always

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
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			struct fragOutput
			{
				float4 color : SV_Target;
				float depth : SV_Depth;
			};

			sampler2D_float _CameraDepthTexture;
			float4 _CameraDepthTexture_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				float2 offset = _CameraDepthTexture_TexelSize.xy;
				o.uv0 = v.uv + float2(offset.x, offset.y);
				o.uv1 = v.uv + float2(-offset.x, offset.y);
				o.uv2 = v.uv + float2(offset.x, -offset.y);
				o.uv3 = v.uv + float2(-offset.x, -offset.y);
				return o;
			}

			fragOutput frag (v2f i)
			{
				fragOutput o = (fragOutput)0;

				float d0 = tex2D(_CameraDepthTexture, i.uv0);
				float d1 = tex2D(_CameraDepthTexture, i.uv1);
				float d2 = tex2D(_CameraDepthTexture, i.uv2);
				float d3 = tex2D(_CameraDepthTexture, i.uv3);

				//o.depth = min(d0, min(d1, min(d2, d3)));
				o.depth = max(d0, max(d1, max(d2, d3)));
				o.color = o.depth;

				return o;
			}
			ENDCG
		}

		// NVidiaSDK11 upsampling
		Pass
		{
			Blend One SrcAlpha
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
				float2 uv00 : TEXCOORD0;
				float2 uv10 : TEXCOORD1;
				float2 uv01 : TEXCOORD2;
				float2 uv11 : TEXCOORD3;
				float2 uv : TEXCOORD4;
				float4 vertex : SV_POSITION;
			};

			sampler2D _LowResColorBilinear;
			sampler2D _LowResColorPoint;
			sampler2D _LowResDepth;
			float4 _LowResDepth_TexelSize;
			sampler2D_float _CameraDepthTexture;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv00 = v.uv + .5 * _LowResDepth_TexelSize.xy;
				o.uv10 = v.uv + float2(_LowResDepth_TexelSize.x, 0);
				o.uv01 = v.uv + float2(0, _LowResDepth_TexelSize.y);
				o.uv11 = v.uv + _LowResDepth_TexelSize.xy;
				o.uv = v.uv;
				return o;
			}

			float FetchLowResDepth(float2 uv)
			{
				return Linear01Depth(tex2D(_LowResDepth, uv).r);
			}

			float FetchFullResDepth(float2 uv)
			{
				return Linear01Depth(tex2D(_CameraDepthTexture, uv).r);
			}

			void UpdateNearestSample(	inout float MinDist,
							inout float2 NearestUV,
							float Z,
							float2 UV,
							float ZFull
							)
			{
				float Dist = abs(Z - ZFull);
				if (Dist < MinDist)
				{
					MinDist = Dist;
					NearestUV = UV;
				}
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float z00 = FetchLowResDepth(i.uv00);
				float z10 = FetchLowResDepth(i.uv10);
				float z01 = FetchLowResDepth(i.uv01);
				float z11 = FetchLowResDepth(i.uv11);

				float zfull = FetchFullResDepth(i.uv);

				float MinDist = 1.e8f;
				float2 NearestUV = i.uv00;

				UpdateNearestSample(MinDist, NearestUV, z00, i.uv00, zfull);
				UpdateNearestSample(MinDist, NearestUV, z10, i.uv10, zfull);
				UpdateNearestSample(MinDist, NearestUV, z01, i.uv01, zfull);
				UpdateNearestSample(MinDist, NearestUV, z11, i.uv11, zfull);

				float g_DepthThreshold = 0.0002f;
				// UNITY_BRANCH
				if (abs(z00 - zfull) < g_DepthThreshold &&
					abs(z10 - zfull) < g_DepthThreshold &&
					abs(z01 - zfull) < g_DepthThreshold &&
					abs(z11 - zfull) < g_DepthThreshold)
				{
					return tex2D(_LowResColorBilinear, i.uv);
				}
				else
				{
					return tex2D(_LowResColorPoint, NearestUV);
				}
			}
			ENDCG
		}
	}
}
