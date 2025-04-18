Shader "PF/Clusters"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "INSTANCED GEOMETRY"

            ZTest Off
            Cull Off
			ZClip Off

			CGPROGRAM
			#pragma only_renderers d3d11 vulkan ps4 metal
			#pragma vertex Vert
            #pragma geometry Geom
			#pragma fragment Frag
            #pragma multi_compile_instancing
			#pragma target 4.0
			
			#define INSTANCED_GEOMETRY

			#include "Includes/PFCore.cginc"
			#include "Includes/ClusteredUtils.cginc"
			#include "UnityCG.cginc"
			ENDCG
		}

		Pass
		{
			Name "GEOMETRY"

            ZTest Off
            Cull Off
			ZClip Off

			CGPROGRAM
			#pragma only_renderers d3d11 vulkan ps4 metal
			#pragma vertex Vert
            #pragma geometry Geom
			#pragma fragment Frag
			#pragma target 4.0
			
			#define GEOMETRY

			#include "Includes/PFCore.cginc"
			#include "Includes/ClusteredUtils.cginc"
			#include "UnityCG.cginc"
			ENDCG
		}

		Pass
		{
			Name "SIMPLE"


            ZTest Off
            Cull Off
			ZClip Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Includes/PFCore.cginc"
			#include "Includes/ClusteredUtils.cginc"
			#include "UnityCG.cginc"
			ENDCG
		}
	}

	SubShader
	{
		Pass
		{
			Name "SIMPLE"


            ZTest Off
            Cull Off
			ZClip Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Includes/PFCore.cginc"
			#include "Includes/ClusteredUtils.cginc"
			#include "UnityCG.cginc"
			ENDCG
		}
	}
}
