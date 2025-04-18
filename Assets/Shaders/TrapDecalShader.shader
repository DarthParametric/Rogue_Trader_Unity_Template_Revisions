Shader "PF/Decals/TrapDecalShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_PrimaryColor("Primary Color", Color) = (1,1,1,1)
		_SecondaryColor("Secondary Color", Color) = (1,1,1,1)
		_FogInfluence("Fog Influence", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="AlphaTest" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile __ TRAP_ICON
			#define FOG_OF_WAR_ON
			
			#include "Includes/PFCore.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				FOG_OF_WAR_COORDS(0)
				UNITY_FOG_COORDS(1)
				#ifdef TRAP_ICON
					float2 uv : TEXCOORD2;
				#endif
				float4 pos : SV_POSITION;
			};

			fixed4 _PrimaryColor;
			fixed4 _SecondaryColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				TRANSFER_FOG_OF_WAR(o, worldPos.xz);
				UNITY_TRANSFER_FOG(o, o.pos);
				#ifdef TRAP_ICON
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				#endif
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				#ifdef TRAP_ICON
					fixed4 result = tex2D(_MainTex, i.uv) * _PrimaryColor;
				#else
					float2 ssCoords = i.pos.xy / _ScreenParams.xy;

					fixed4 result = lerp(_PrimaryColor, _SecondaryColor, ssCoords.y);
				#endif
				fixed4 preFogResult = result;
				UNITY_APPLY_FOG(i.fogCoord, result);
				result.rgb = lerp(preFogResult.rgb, result.rgb, _FogInfluence);

				_FogOfWarMaterialFlag = 1;
				APPLY_FOG_OF_WAR(i.fogOfWarCoords, result, 0)

				return result;
			}
			ENDCG
		}
	}

	CustomEditor "TrapDecalShaderGUI"
}
