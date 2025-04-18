Shader "PF/KingdomUtil/AddSdfBorder"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Accumulator("Texture", 2D) = "white" {}
		_ChannelMask("Mask", Color) = (1,1,1,1)
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100
			Pass
			{
				BlendOp Max

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

				sampler2D _MainTex;
				sampler2D _Accumulator;
				float4 _MainTex_ST;
				fixed4 _ChannelMask;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					fixed col = tex2D(_MainTex, i.uv).a;

					// get existing pixel
					fixed4 ex = tex2D(_Accumulator, i.uv);
					// find the component we're  drawing now
					fixed excol = dot(ex, _ChannelMask);
					// select value that is closer to 0.5
					if(abs(excol-0.5)<abs(col-0.5))
					{
						col=excol;
					}

					// col is the new component, the other components come from existing pixel
					return col*_ChannelMask + ex*(1-_ChannelMask);
				}
				ENDCG
			}
		}
}
