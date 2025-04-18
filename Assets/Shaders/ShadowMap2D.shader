Shader "Hidden/ShadowMap2D"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Blend One One
			ColorMask A
			BlendOp RevSub
			ZWrite Off
			ZTest Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 segmentA_Index : POSITION;
				float2 segmentB : TEXCOORD0;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 penumbra : TEXCOORD0;
				float clipValue : TEXCOORD1;
			};

			uniform float4 _Vertices[4];
			uniform float _Radius;
			uniform float2 _LightPosition;

			// port from https://github.com/discosultan/penumbra
			float2x2 PenumbraMatrix(float2 basisX, float2 basisY)
			{
				float2x2 m = transpose(float2x2(basisX, basisY));
				return float2x2(m._m11, -m._m01, -m._m10, m._m00) / determinant(m);
			}
			
			v2f vert (appdata v)
			{
				v2f o;

				// Segments are in CCW order.
				// Stencil.x=0: dealing with segment vertex A; X=1: dealing with segment vertex B.	
				// Stencil.y=0: not projecting the vertex; y=1: projecting the vertex.

				float2 stencil = _Vertices[v.segmentA_Index.z];

				float2 toSegmentA = v.segmentA_Index.xy - _LightPosition;
				float2 toSegmentB = v.segmentB.xy - _LightPosition;

				// Find radius offsets 90deg left and right from light source relative to vertex.
				float2 toLightOffsetA = float2(-_Radius, _Radius) * normalize(toSegmentA).yx;
				float2 toLightOffsetB = float2(_Radius, -_Radius) * normalize(toSegmentB).yx;
				float2 lightOffsetA = _LightPosition + toLightOffsetA; // 90 CCW.
				float2 lightOffsetB = _LightPosition + toLightOffsetB; // 90 CW.

				// From each edge, project a quad. We have 4 vertices per edge.	
				float2 position = lerp(v.segmentA_Index.xy, v.segmentB, stencil.x);
				float2 projectionOffset = lerp(lightOffsetA, lightOffsetB, stencil.x);

				// Setting projected.w to 0 will cause the position to be projected (scaled) infinitely far away in the 
				// perspective division phase. Instead of dividing by 0, the pipeline seems to divide by a very small positive number instead.
				float4 projected = float4(position - projectionOffset * stencil.y, 0.0, 1.0 - stencil.y);

				// Transform to clip space.
				float4 clipPosition = mul(UNITY_MATRIX_VP, projected);

				float2 penumbraA = mul(PenumbraMatrix(toLightOffsetA, toSegmentA), projected.xy - (v.segmentA_Index.xy) * projected.w);
				float2 penumbraB = mul(PenumbraMatrix(toLightOffsetB, toSegmentB), projected.xy - (v.segmentB) * projected.w);

				// Find the edge normal. A to B CW 90 deg.
				// ClipValue < 0 means the projection is pointing towards light => no shadow should be generated.
				float2 clipNormal = (v.segmentB - v.segmentA_Index).yx * float2(1.0, -1.0);
				float clipValue = dot(clipNormal, projected.xy - projected.w * position);

				o.position = mul(UNITY_MATRIX_VP, projected.xzyw);
				o.penumbra = float4(penumbraA, penumbraB);
				o.clipValue = clipValue;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// If clipvalue < 0, don't shadow. We are clipping shadows cast from edges which normals are pointing
				// towards the light.
				clip(i.clipValue);

				float2 p = clamp(i.penumbra.xz / i.penumbra.yw, -1.0, 1.0);
				float2 value = lerp(p*(3.0 - p*p)*0.25 + 0.5, 1.0, step(i.penumbra.yw, 0.0));	// Step penumbra.yw < 0: 1; otherwise 0.	
				float occlusion = value.x + value.y - 1.0;
				return float4(occlusion, occlusion, occlusion, occlusion);
			}
			ENDCG
		}
	}
}
