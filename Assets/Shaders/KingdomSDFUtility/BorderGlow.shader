Shader "PF/KingdomUtil/BorderGlow"
{
	Properties
	{
		_SdfTex("Borders Sdf", 2D) = "white" {}

		_From("GlowFrom", Vector) = (1,1,1,1)
		_To("GlowTo", Vector) = (0.8,0.8,0.8,0.8)
		_FromA("AlphaFrom", Color) = (1,1,1,1)
		_ToA("AlphaTo", Color) = (0,0,0,0)

		_ColorR("ColorR", Color) = (1,1,1,1)
		_ColorG("ColorG", Color) = (1,1,1,1)
		_ColorB("ColorB", Color) = (1,1,1,1)
		_ColorA("ColorA", Color) = (1,1,1,1)

	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent+999" }
			LOD 100
			Pass
			{
				Blend One OneMinusSrcAlpha 
				Zwrite Off

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

				sampler2D _SdfTex;
				float4 _SdfTex_ST;
				float4 _From;
				float4 _FromA;
				float4 _To;
				float4 _ToA;

				float4 _ColorR;
				float4 _ColorG;
				float4 _ColorB;
				float4 _ColorA;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _SdfTex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					// sample the texture
					fixed4 bd = tex2D(_SdfTex, i.uv);

					float4 s = smoothstep(_To, _From, bd); // _to is higher than _from
					
					//return s.r;
					s = lerp(_FromA, _ToA, 1-s); // lerp alpha values
					
					s *= step(_From, bd); // cut everything that's outside completely

					fixed4 res = 
					_ColorR*s.r +
					_ColorG*s.g +
					_ColorB*s.b +
					_ColorA*s.a;

					return saturate(res);
				}
				ENDCG
			}
		}
}
