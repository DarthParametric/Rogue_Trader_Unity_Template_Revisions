﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/2D/Dissolve"
{
	Properties
	{
		[PerRendererData] _MainTex("Main texture", 2D) = "white" {}
		_DissolveTex("Dissolution texture", 2D) = "gray" {}
		_Threshold("Threshold", Range(0., 1.01)) = 0.

		// required for UI.Mask
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "true" "RenderType" = "Transparent" }
		ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Cull Off
		// required for UI.Mask
		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Pass
		{

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma target 3.0
		#include "UnityCG.cginc"

		struct appdata_t
		{
			float4 vertex   : POSITION;
			float4 color    : COLOR;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f
		{
			float2 texcoord  : TEXCOORD0;
			float4 vertex   : SV_POSITION;
			float4 color    : COLOR;
		};

		sampler2D _MainTex;
		sampler2D _DissolveTex;
		float _Threshold;

		v2f vert(appdata_t IN)
		{
			v2f OUT;
			OUT.vertex = UnityObjectToClipPos(IN.vertex);
			OUT.texcoord = IN.texcoord;
			OUT.color = IN.color;

			return OUT;
		}

		float4 frag(v2f i) : COLOR
		{
			float2 uv = i.texcoord;

			float4 c = tex2D(_MainTex, uv)*i.color;
			float val = tex2D(_DissolveTex, uv).r;

			c.a *= step(_Threshold, val);
			return float4(LinearToGammaSpace(c.rgb), c.a);
		}

		ENDCG
		}
	}
	Fallback "Sprites/Default"
}