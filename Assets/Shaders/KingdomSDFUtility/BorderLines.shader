Shader "PF/KingdomUtil/BorderLines"
{
	Properties
	{
		_SdfTex("Region Sdf", 2D) = "white" {}

		_Width("Width", float) = 0.02

		_Color1("Color1", Color) = (1,1,1,1)
		_Color2("Color2", Color) = (1,1,1,1)

	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100
			Pass
			{
				Blend One OneMinusSrcAlpha 

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
				float _Width;

				float4 _Color1;
				float4 _Color2;

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
					fixed left = bd.r;
					fixed right = bd.g;

					{
						left = smoothstep(0.5-_Width*0.6, 0.5-_Width*0.4, left);
						right = smoothstep(0.5 -_Width*0.6, 0.5 -_Width*0.4, right)
							*(1-smoothstep(0.5 -_Width*0.1, 0.5 +_Width*0.1, right));
					}

					fixed4 res = _Color1*_Color1.a*left*right;

					left = bd.b;
					right = bd.a;

					{
						left = smoothstep(0.5 - _Width*0.6, 0.5 - _Width*0.4, left);
						right = smoothstep(0.5 - _Width*0.6, 0.5 - _Width*0.4, right)
							*(1 - smoothstep(0.5 - _Width*0.1, 0.5 + _Width*0.1, right));
					}

					res += _Color2*_Color2.a*left*right;
					return res;
				}
				ENDCG
			}
		}
}
