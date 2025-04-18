// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/MipMapsDebug"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
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
				float2 uv : TEXCOORD0;
				float2 mipuv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _DebugMipMapTex;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.mipuv = o.uv * float2(_MainTex_TexelSize.zw) / 8;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				half4 mip = tex2D(_DebugMipMapTex, i.mipuv);
				half4 res;
				res.rgb = lerp(col.rgb, mip.rgb, mip.a);
				res.a = col.a;
				return res;
			}
			ENDCG
		}
	}

	SubShader
	{
		Tags { "RenderType"="TransparentCutout" }
		LOD 100

		Pass
		{
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
				float2 uv : TEXCOORD0;
				float2 mipuv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _DebugMipMapTex;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.mipuv = o.uv * float2(_MainTex_TexelSize.zw) / 8;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				half4 mip = tex2D(_DebugMipMapTex, i.mipuv);
				half4 res;
				res.rgb = lerp(col.rgb, mip.rgb, mip.a);
				res.a = col.a;
				return res;
			}
			ENDCG
		}
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" }
		LOD 100

		Pass
		{
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
				float2 uv : TEXCOORD0;
				float2 mipuv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _DebugMipMapTex;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.mipuv = o.uv * float2(_MainTex_TexelSize.zw) / 8;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				half4 mip = tex2D(_DebugMipMapTex, i.mipuv);
				half4 res;
				res.rgb = lerp(col.rgb, mip.rgb, mip.a);
				res.a = col.a;
				return res;
			}
			ENDCG
		}
	}
}
