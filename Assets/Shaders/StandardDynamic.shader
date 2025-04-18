Shader "PF/StandardDynamic"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TintColor ("Color", Color) = (1,1,1,1)
		_AlphaScale("Alpha Scale", float) = 1
		_Cutout("Cutout", Range(0, 1)) = .5
		[ToggleOff] _AlphaToMask("Alpha To Coverage", float) = 0
		[NoScaleOffset]
		[Normal]
		_BumpMap("Bump Map", 2D) = "bump" {}
		[NoScaleOffset]
		_Parameters("Mask (R=Roughness, G=Emission, B=Metallic)", 2D) = "white" {}
		_Roughness("Roughness", Range(0, 1)) = 1
		_Metallic("Metallic", Range(0, 1)) = 0
		_Emission("Emission", Range(0, 10)) = 1
		_DissolveTex("Dissolve Texture (R Channel)", 2D) = "white" {}
		_Dissolve("Dissolve", Range(0, 1)) = 0
		_DissolveWidth("Dissolve Width", Range(0,1)) = .1
		[ToggleOff]
		_DissolveCutout("Dissolve Cutout", float) = 1
		[ToggleOff]
		_DissolveEmission("Dissolve Emission", float) = 1
		_DissolveColor("Dissolve Color", Color) = (1,1,1,1)
		_DissolveColorScale("Dissolve Color Scale", float) = 1
		_PetrificationTex("Petrification Texture (R Channel)", 2D) = "white" {}
		_Petrification("Petrification", Range(0, 1)) = 0
		_PetrificationColorScale("Petrification Color Scale", float) = 1
		_PetrificationColorClamp("Petrification Color Clamp", float) = 1
		_PetrificationAlphaScale("Petrification Alpha Scale", float) = 1
		_PetrificationColor("Petrification Color", Color) = (1,1,1,1)
		[ToggleOff] _RimLighting ("Rim Lighting Enabled", float) = 0
		_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimPower("Rim Power", float) = 4
		[ToggleOff] _RimShadeNdotL("Rim Shade NdotL", float) = 0
		_NdotLPower("NdotL Power", Range(0,2)) = 1
		_FogInfluence("Fog Influence", Range(0, 1)) = 0.7

		[HideInInspector]
		_QueueOffset("__queueOffset", float) = 0
		[HideInInspector]
		_Type("__type", float) = 0.0
		[HideInInspector]
		_SrcBlend ("__src", Float) = 1.0
		[HideInInspector]
		_DstBlend ("__dst", Float) = 0.0
		[HideInInspector] 
		_ZWrite ("__zw", Float) = 1.0
		[HideInInspector]
		_StencilRef("__stencilRef", float) = 0
		[HideInInspector]
		_StencilCompOp("__stencilCompOp", float) = 0
		[HideInInspector]
		_StencilPassOp("__stencilPassOp", float) = 2 //2==Replace
		[HideInInspector]
		_StencilZFailOp("__stencilZFailOp", float) = 0
	}

	CGINCLUDE
	
	ENDCG
	SubShader
	{
		Tags { "RenderType"="Opaque" "Reflection"="Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode"="ForwardBase" }
			Blend[_SrcBlend][_DstBlend]
			ZWrite [_ZWrite]
			AlphaToMask [_AlphaToMask]
			Stencil
			{
				Ref [_StencilRef]
				Comp [_StencilCompOp]
				Pass [_StencilPassOp]
				ZFail [_StencilZFailOp]
			}

			CGPROGRAM
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma multi_compile_fwdbase_fullshadows
			#pragma skip_variants VERTEXLIGHT_ON
			#pragma multi_compile_fog
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
            #pragma multi_compile_instancing
			#pragma shader_feature __ BUMP_ON
			#pragma shader_feature __ SPECULAR_ON
			#pragma shader_feature __ METALLNESS_ON
			#pragma shader_feature __ EMISSION_ON
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma shader_feature __ USE_LIGHT_PROBE_PROXY_VOLUME
			#pragma multi_compile __ DISSOLVE_ON
			#pragma multi_compile __ PETRIFICATION_ON

			#pragma target 4.0
			#pragma vertex vert_base
			#pragma fragment frag_base
			#define _GLOSSYREFLECTIONS_OFF

			#include "Includes/StandardConfig.cginc"
			ENDCG
		}

		Pass
		{
			Name "ForwardAdd"
			Tags{ "LightMode" = "ForwardAdd" }
			Blend[_SrcBlend] One
			Fog{ Color(0,0,0,0) }
			ZWrite Off
			AlphaToMask[_AlphaToMask]
			ZTest LEqual

			CGPROGRAM
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma skip_variants VERTEXLIGHT_ON
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma skip_variants DIRECTIONAL_COOKIE POINT_COOKIE
			#pragma skip_variants FOG_EXP FOG_EXP2
            #pragma multi_compile_instancing
			#pragma shader_feature __ BUMP_ON
			#pragma shader_feature __ SPECULAR_ON
			#pragma shader_feature __ METALLNESS_ON
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma multi_compile __ DISSOLVE_ON
			#pragma multi_compile __ PETRIFICATION_ON
			#pragma target 4.0
			#pragma vertex vert_add
			#pragma fragment frag_add

			#include "Includes/StandardConfig.cginc"
			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On
			ZTest LEqual
			//AlphaToMask [_AlphaToMask]
			
			CGPROGRAM
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma skip_variants VERTEXLIGHT_ON
			#pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
			#pragma shader_feature __ CUTOUT_ON ALPHABLEND_ON ALPHAPREMULTIPLY_ON
			#pragma multi_compile __ DISSOLVE_ON
			#pragma target 4.0
			#pragma vertex vert_shadow_caster
			#pragma fragment frag_shadow_caster
			#include "Includes/StandardConfig.cginc"
			ENDCG
		}

		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma only_renderers d3d11 metal vulkan glcore ps4
			#pragma vertex vert_meta
			#pragma fragment frag_meta
			#pragma shader_feature __ EMISSION_ON

			#include "Includes/PFCore.cginc"
			ENDCG
		}
	}

	CustomEditor "PFStandardDynamicShaderGUI"
}
