Shader "PF/Unlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_FogInfluence("Fog Influence", Range(0, 1)) = 0
		_Cutout("Cutout", Range(0, 1)) = 0


		[HideInInspector]
		_StencilRef("__stencilRef", float) = 0
		[HideInInspector]
		_StencilCompOp("__stencilCompOp", float) = 0
		[HideInInspector]
		_StencilPassOp("__stencilPassOp", float) = 2 //2==Replace
		[HideInInspector]
		_StencilZFailOp("__stencilZFailOp", float) = 0
	}
	SubShader
	{
		Stencil
		{
			Ref [_StencilRef]
			Comp [_StencilCompOp]
			Pass [_StencilPassOp]
			ZFail [_StencilZFailOp]
		}

		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma skip_variants FOG_EXP FOG_EXP2
			
			#include "Includes/PFCore.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;

				if (_Cutout > 0)
				{
					clip(col.a - _Cutout);
				}

				// apply fog
				float4 preFogColor = col;
				UNITY_APPLY_FOG(i.fogCoord, col);
				col = lerp(preFogColor, col, _FogInfluence);

				return col;
			}
			ENDCG
		}
	}

	CustomEditor "UnlitShaderGUI"
}
