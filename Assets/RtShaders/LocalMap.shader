Shader "Owlcat/Game/LocalMap"
{
	Properties
	{
		_BackColor("Color", Color) = (1,1,1,1)
		
		// ObsoleteProperties needs to alphatested baked GI
		[HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "grey" {}
		[HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
     
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
 
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_local_fragment _ UNITY_UI_CLIP_RECT

			#include "Packages/com.owlcat.visual/Runtime/RenderPipeline/ShaderLibrary/Core.hlsl"
			#include "Packages/com.owlcat.visual/Runtime/RenderPipeline/ShaderLibrary/Input.hlsl"

			struct appdata
			{
				float3 positionOS		: POSITION;
				float2 uv				: TEXCOORD0;
			};

			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
			};

			TEXTURE2D(_LocalMapColorTex);
			TEXTURE2D(_LocalMapDepthTex);
			float4 _LocalMapColorTex_ST;
			float4 _BackColor;
			float4x4 _InverseViewProj;
			float4 LocalMapFowScaleOffset;
			float4 _ClipRect;

			v2f vert (appdata v)
			{
				v2f o;
				o.positionCS = TransformObjectToHClip(v.positionOS);
				o.positionWS = v.positionOS;
				o.uv = TRANSFORM_TEX(v.uv, _LocalMapColorTex);

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 finalColor = SAMPLE_TEXTURE2D(_LocalMapColorTex, s_point_clamp_sampler, i.uv);

				float2 fowUV = i.uv * LocalMapFowScaleOffset.xy + LocalMapFowScaleOffset.zw;

				float4 fogOfWar = SAMPLE_TEXTURE2D(_FogOfWarMask, s_linear_clamp_sampler, fowUV);
				//return fogOfWar;
				float alpha = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);

				fogOfWar.g *= (1 - _BackColor.a);
				float mask = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
				finalColor.rgb = LinearToSRGB(lerp(_BackColor.rgb, finalColor.rgb, mask));
				finalColor.a *= alpha;

				#ifdef UNITY_UI_CLIP_RECT
				float2 inside = step(_ClipRect.xy, i.positionWS.xy) * step(i.positionWS.xy, _ClipRect.zw);
				finalColor.a *= inside.x * inside.y;
				#endif

				//if (all(finalColor.xz >= _LocalMap_WorldRect.xy && finalColor.xz <= _LocalMap_WorldRect.zw))
				//{
				//	return float4(0, 1, 0, 1);
				//}
				//else
				//{
				//	return float4(0, 0, 1, 1);
				//}

				//float deviceDepth = SAMPLE_TEXTURE2D(_LocalMapDepthTex, s_point_clamp_sampler, i.uv).r;

				//float4 pos = float4(i.uv.xy * 2 - 1, deviceDepth, 1);
				//pos = mul(_InverseViewProj, pos);
				//pos /= pos.w;
				////return float4(pos.xyz, 1);
				//float2 fowUV = saturate(TRANSFORM_TEX(pos.xz, _FogOfWarMask));
				////return float4(fowUV, 0, 1);

				//float4 fogOfWar = SAMPLE_TEXTURE2D(_FogOfWarMask, s_linear_clamp_sampler, fowUV);
				////return fogOfWar;
				//float alpha = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
				//// на карте может быть такая ситуация, когда видны пиксели с максимальной глубиной
				//// но при восстановлении позиции у этих пикселей будет правильные XZ-координаты, которые мы не хотим отображать
				//// поэтому обрезаем их по глубине
				//float linearDepth = LinearEyeDepth(deviceDepth);
				//float linearDepth01 = (linearDepth - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);
				//alpha = (linearDepth01 < .1 || linearDepth01 > .9) ? 0 : alpha;
				//fogOfWar.g *= (1 - _BackColor.a);
				//float mask = max(fogOfWar.r, fogOfWar.g) * (1 - fogOfWar.b);
				//finalColor.rgb = LinearToSRGB(lerp(_BackColor.rgb, finalColor.rgb, mask));
				//finalColor.a = alpha;
				return finalColor;
			}
			ENDHLSL
		}
	}
}
