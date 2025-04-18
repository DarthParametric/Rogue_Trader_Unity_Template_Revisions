Shader "Hidden/LocalMapBaker"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScaleOffset("_ScaleOffset", Vector) = (1, 1, 0, 0)
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
            float4 _ScaleOffset;
			float4 _LocalMapBorderParams;

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _ScaleOffset.xy + _ScaleOffset.zw;
                float col = tex2D(_MainTex, uv).r;

                if (i.vertex.x < _LocalMapBorderParams.x || i.vertex.y < _LocalMapBorderParams.y || i.vertex.x > _LocalMapBorderParams.z || i.vertex.y > _LocalMapBorderParams.w) 
                { 
                    col = 0;
                }

				return col;
            }
            ENDCG
        }

        Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Lighting Off
			Fog { Mode Off }
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DIAGONAL_DIRECTIONS STRAIGHT_DIRECTIONS ALL_DIRECTIONS
			
			#include "UnityCG.cginc"
			
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float4 _MainTex_TexelSize;
			
			uniform float _HighlightingBlurOffset;
			uniform half _Intensity;

			struct vs_input
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct ps_input
			{
				float4 pos : SV_POSITION;

				#if defined(ALL_DIRECTIONS)
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				#else
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				#endif
			};
			
			ps_input vert(vs_input v)
			{
				ps_input o;
				o.pos = UnityObjectToClipPos(v.vertex);

				float2 uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
				float2 offs = _HighlightingBlurOffset * _MainTex_TexelSize.xy;

				#if defined(ALL_DIRECTIONS)

				// Diagonal
				o.uv0.x = uv.x - offs.x;
				o.uv0.y = uv.y - offs.y;
				
				o.uv0.z = uv.x + offs.x;
				o.uv0.w = uv.y - offs.y;
				
				o.uv1.x = uv.x + offs.x;
				o.uv1.y = uv.y + offs.y;
				
				o.uv1.z = uv.x - offs.x;
				o.uv1.w = uv.y + offs.y;

				// Straight
				o.uv2.x = uv.x - offs.x;
				o.uv2.y = uv.y;
				
				o.uv2.z = uv.x + offs.x;
				o.uv2.w = uv.y;
				
				o.uv3.x = uv.x;
				o.uv3.y = uv.y - offs.y;
				
				o.uv3.z = uv.x;
				o.uv3.w = uv.y + offs.y;

				#elif defined(STRAIGHT_DIRECTIONS)

				// Straight
				o.uv0.x = uv.x - offs.x;
				o.uv0.y = uv.y;
				
				o.uv0.z = uv.x + offs.x;
				o.uv0.w = uv.y;
				
				o.uv1.x = uv.x;
				o.uv1.y = uv.y - offs.y;
				
				o.uv1.z = uv.x;
				o.uv1.w = uv.y + offs.y;

				#else 

				// Diagonal
				o.uv0.x = uv.x - offs.x;
				o.uv0.y = uv.y - offs.y;
				
				o.uv0.z = uv.x + offs.x;
				o.uv0.w = uv.y - offs.y;
				
				o.uv1.x = uv.x + offs.x;
				o.uv1.y = uv.y + offs.y;
				
				o.uv1.z = uv.x - offs.x;
				o.uv1.w = uv.y + offs.y;

				#endif

				return o;
			}
			
			float4 frag(ps_input i) : SV_Target
			{
				float color1 = tex2D(_MainTex, i.uv0.xy).r;
				float color2;

				// For straight or diagonal directions
				color2 = tex2D(_MainTex, i.uv0.zw).r;
				color1 = max(color1, color2);

				color2 = tex2D(_MainTex, i.uv1.xy).r;
				color1 = max(color1, color2);

				color2 = tex2D(_MainTex, i.uv1.zw).r;
				color1 = max(color1, color2);

				// For all directions
				#if defined(ALL_DIRECTIONS)
					color2 = tex2D(_MainTex, i.uv2.xy).r;
					color1 = max(color1, color2);

					color2 = tex2D(_MainTex, i.uv2.zw).r;
					color1 = max(color1, color2);

					color2 = tex2D(_MainTex, i.uv3.xy).r;
					color1 = max(color1, color2);

					color2 = tex2D(_MainTex, i.uv3.zw).r;
					color1 = max(color1, color2);
				#endif
				
				return color1;
			}
			ENDCG
		}

        Pass
        {
			ZTest Always
			Cull Off
			ZWrite Off
			Lighting Off

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
			sampler2D _LocalMapMask;
			float4 _LocalMapBorderColor;
			float4 _LocalMapMainColor;

            half4 frag (v2f i) : SV_Target
            {
                half border = tex2D(_MainTex, i.uv).r;
				half mask = tex2D(_LocalMapMask, i.uv).r;
				//return border;
				border = saturate(border - mask);
				

				return float4(_LocalMapBorderColor.rgb * (border > 0), border * _LocalMapBorderColor.a) + mask * _LocalMapMainColor;
            }
            ENDCG
        }
    }
}
