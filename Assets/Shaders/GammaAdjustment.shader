﻿Shader "Hidden/GammaAdjustment"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float _Gamma;

			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);

				// Gamma correction
				//col = 1.055F * pow(col, _Gamma) - .055F;
				col = pow(col, _Gamma);

				// Brightess additive
				//col = (_Gamma < 1) ? (col * _Gamma) : (col + (_Gamma - 1));

				// Brightness multiplicative
				//col *= _Gamma;

				return col;
			}
			ENDCG
		}
	}
}
