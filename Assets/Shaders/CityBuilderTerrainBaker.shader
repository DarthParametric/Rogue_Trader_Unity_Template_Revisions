Shader "Hidden/CityBuilderTerrainBaker"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

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

			float2 _Uv[4];
			float _RemoveDecal;

			v2f vert (appdata v)
			{
				v2f o;

				float4 pos = UnityObjectToClipPos(v.vertex);

				float index = 0;
				if (v.uv.x < .5 && v.uv.y > .5)
				{
					index = 0;
				}
				else if (v.uv.x > .5 && v.uv.y > .5)
				{
					index = 1;
				}
				else if (v.uv.x > .5 && v.uv.y < .5)
				{
					index = 2;
				}
				else if (v.uv.x < .5 && v.uv.y < .5)
				{
					index = 3;
				}

				float2 uv = _Uv[index];

				o.uv = v.uv;
				if (_RemoveDecal > 0)
				{
					o.uv = uv;
				}

				uv.y = 1 - uv.y;
				uv = uv * 2 - 1;
				o.vertex = float4(uv, pos.zw);
				
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
