Shader "Hidden/ClustersDebug"
{
	CGINCLUDE
	#include "UnityCG.cginc"
    #pragma target 4.0

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };

    SamplerState ltc_linear_clamp_sampler;

	// Generates a triangle in homogeneous clip space, s.t.
	// v0 = (-1, -1, 1), v1 = (3, -1, 1), v2 = (-1, 3, 1).
	float2 GetFullScreenTriangleTexCoord(uint vertexID)
	{
		#if UNITY_UV_STARTS_AT_TOP
			return float2((vertexID << 1) & 2, 1.0 - (vertexID & 2));
		#else
			return float2((vertexID << 1) & 2, vertexID & 2);
		#endif
	}

	float4 GetFullScreenTriangleVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
	{
		float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
		return float4(uv * 2.0 - 1.0, z, 1.0);
	}

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);

        return output;
    }
    ENDCG

    SubShader
    {
        Pass
        {
            Name "CLUSTERS"
            ZTest Always
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.0
            #include "Includes/PFCore.cginc"

            float _Debug_ClustersSlice;

            float4 Frag(Varyings i) : SV_Target
            {
                uint lightMask = asuint(_ClustersRT.Load(int4(i.texcoord * _Clusters.xy, _Debug_ClustersSlice, 0)).x);
                return lightMask > 0;
            }


            ENDCG
        }

        Pass
        {
            Name "CLUSTERS HEATMAP"
            ZTest Always
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.0
            #include "Includes/PFCore.cginc"

            float4 _Debug_ClustersHeatmap[32];

            float4 Frag(Varyings IN) : SV_Target
            {
                float result = 0;
                float2 tc = IN.texcoord;
                #if UNITY_UV_STARTS_AT_TOP
                    if (_ProjectionParams.x < 1)
                        tc.y = 1 - tc.y;
                #endif
                for (int i = 0; i < _Clusters.z; i++)
                {
                    int4 clustersUv = int4(tc * _Clusters.xy, i, 0);
                    uint lightMask = asuint(_ClustersRT.Load(clustersUv));
                    int lightsCount = countbits(lightMask);
                    result = max(result, lightsCount);
                }

                float4 lutColor = _Debug_ClustersHeatmap[result];
                return result > 0 ? float4(lutColor.rgb, .5) : 0;
            }

            ENDCG
        }
    }
    Fallback Off
}
