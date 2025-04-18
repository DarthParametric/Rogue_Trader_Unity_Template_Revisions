Shader "PF/KingdomUtil/BorderClaimAnimation"
{
	Properties
	{
		_SdfTex("Region Sdf", 2D) = "white" {}

		_Distance("Distance", Range(0,1)) = 0
		_Width("Width", float) = 0.02

		_ColorBorder("ColorBorder", Color) = (1,1,1,1)
		_ColorInside("ColorInside", Color) = (1,1,1,1)
		_ColorOutside("ColorOutside", Color) = (1,1,1,1)

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
				float _Distance;
				float _Width;

				float4 _ColorBorder;
				float4 _ColorInside;
				float4 _ColorOutside;

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
					fixed red = bd.r;
					fixed green = bd.g;

					fixed regionCut = 0;
					fixed border = 0;

					fixed inside = 0;
					fixed outside = 0;

					_Distance = 0.5 + _Distance / 2;

					{
						// border moving from red into green region

						// cut out everything that is outside of green region
						regionCut = smoothstep(0.5 - _Width*0.6, 0.5 - _Width*0.4, green);
						fixed regionCut2 = 1 - step(0.5 - _Width*0.6, red);

						//return _ColorBorder*regionCut2;

						// cut out insides of expansion
						fixed ex = (1 - smoothstep((1 - _Distance) - _Width*0.1, (1 - _Distance) + _Width*0.1, red));

						// cut out everything that is outside of expanded red region
						fixed outsideCut = smoothstep((1 - _Distance) - _Width*0.6, (1 - _Distance) - _Width*0.4, red);

						// cut out insides of green region for border
						fixed greenBorder = 1 - smoothstep(0.5 - _Width*0.1, 0.5 + _Width*0.1, green);

						border = saturate((ex + greenBorder*regionCut2)*outsideCut);

						outside = saturate(ex - border);
						inside = saturate(outsideCut - border);

					}


					//return _ColorInside;// *inside;
					_ColorBorder *= _ColorBorder.a;
					_ColorInside *= _ColorInside.a;
					_ColorOutside *= _ColorOutside.a;
					return (_ColorBorder*border + _ColorInside*inside + _ColorOutside*outside)*regionCut;
				}
				ENDCG
			}
		}
}
