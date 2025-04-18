// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/OccludedObjectHighlight"
{
	Properties
	{
		[HideInInspector]
		_StencilRef("__stencilRef", float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			ZWrite Off
			ZTest Greater
			Stencil
			{
				Ref [_StencilRef]
				ReadMask [_StencilRef]
				Comp Equal
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Includes/PFCore.cginc"

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

			fixed4 _OccludedHighlightColor;
			float _CutoutFlag;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				if (_CutoutFlag > 0)
				{
					clip(tex2D(_MainTex, i.uv).a - _Cutout);
				}

				return _OccludedHighlightColor;
			}
			ENDCG
		}
	}
}
