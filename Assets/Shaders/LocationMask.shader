// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/LocationMask"
{
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 100

		// Pass 0
		// Copy static mask B-channel
		Pass
		{
			ZTest Off
			ZWrite Off
			BlendOp Max
			
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

			sampler2D _StaticMask;
			sampler2D _FogOfWarShadowMap;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 staticMask = tex2D(_StaticMask, i.uv);
				float4 shadow = tex2D(_FogOfWarShadowMap, i.uv);
				return float4(shadow.r, shadow.g, staticMask.b, 1);
			}

			ENDCG
		}

		// Pass 1
		// Clears color
		Pass
		{
			ZTest Off
			ZWrite Off
			ColorMask [_ColorMask]
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			float4 _ClearColor;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return _ClearColor;
			}

			ENDCG
		}

		// Pass 2
		// Draw character quads to Fog Of War mask
		Pass
		{
			ZTest Off
			ZWrite Off
			Blend DstAlpha One
			BlendOp Add
			ColorMask RG

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
				//float2 uv1:TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			float4x4 VIEW_PROJ;
			float _FogOfWarRadius;
			float _FogOfWarBorderWidth;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(VIEW_PROJ, worldPos);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 center = .5;
				float vignette = distance(center, i.uv) * 2; 
				float falloff = _FogOfWarBorderWidth / _FogOfWarRadius;
				float invFalloff = 1 - falloff;
				float falloffNormalized = saturate((vignette - invFalloff) / falloff);
				vignette = (sin((falloffNormalized - 1.5)*UNITY_PI) + 1) / 2;
				return vignette;
			}
			ENDCG
		}

		// Pass 3
		// Draw character quads to Foliage Interaction mask
		Pass
		{
			ZTest Off
			ZWrite Off
			Blend One One
			//Blend SrcAlpha OneMinusSrcAlpha
			BlendOp Add
			ColorMask RG

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 uvLifetime : TEXCOORD0; // xy - UV, z - AgePercent, w - Inverse Start Lifetime (1 / Lifetime)
				float4 centerRotation : TEXCOORD1; // xyz - Center, w - Rotation Z
				float4 parameters : TEXCOORD2; // x - radial weight, y - rotation, z - inOutBalance, w - frequency scale
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 radialDir : TEXCOORD1;
				float4 sideDir : TEXCOORD2;
				float4 parameters : TEXCOORD3;
				float4 vertex : SV_POSITION;
				float influence : COLOR0;
			};

			float4x4 VIEW_PROJ;

			float4 _CurveOffsetX;
			float4 _CurveOffsetY;
			float4 _CurveAmplitude;
			float4 _CurveFrequency;
			float4 _CurveScrollSpeed;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 toVertexDir = normalize(worldPos.xyz - v.centerRotation.xyz);

				// 2D direction from center point to vertex point
				o.radialDir.xy = normalize(toVertexDir.xz);

				// 2D direction from center LINE to left side and right side
				float2 toRight = float2(0, -1);
				float2 toLeft = float2(0, 1);
				float s, c;
				//float rotationY = v.parameters.y;
				float rotationY = v.centerRotation.w;
				sincos(rotationY, s, c);
				float2x2 rotation = float2x2(c, -s, s, c);
				o.sideDir.xy = normalize(mul(toRight, rotation));
				o.sideDir.zw = normalize(mul(toLeft, rotation));

				o.vertex = mul(VIEW_PROJ, worldPos);
				o.uv = v.uvLifetime.xy;

				float2 lifetime = float2(v.uvLifetime.z, v.uvLifetime.w);

				// scale curve
				float4 time = float4(0, v.color.r, v.color.b, 1);
				float4 value = float4(0, v.color.g, v.color.a, 0);
				int index = 0;
				if (lifetime.x > time.y && lifetime.x < time.z)
				{
					index = 1;
				}

				if (lifetime.x > time.z)
				{
					index = 2;
				}

				float scale = lerp(value[index], value[index + 1], saturate((lifetime.x - time[index]) / (time[index + 1] - time[index])));

				// waves animation
				float phaseOffset = v.parameters.y;
				float inOutBalance = v.parameters.z;
				float frequencyScale = v.parameters.w;
				float absoluteLifetime = 1.0 / lifetime.y * lifetime.x;
				float4 frequency = sin(_CurveFrequency * frequencyScale.xxxx * absoluteLifetime.xxxx + _Time.yyyy * _CurveScrollSpeed + _CurveOffsetX + phaseOffset) * .5f;
				float4 amplitude = _CurveAmplitude + _CurveOffsetY;

				o.influence = dot(frequency, amplitude) * scale + inOutBalance * dot(amplitude, .25) * scale;
				o.parameters = v.parameters;

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float2 sideDir = i.uv.y > .5 ? i.sideDir.zw : i.sideDir.xy;
				sideDir *= 1 - abs(.5 - i.uv.y) * 2;

				float radialAtten = saturate(1 - length(float2(.5, .5) - i.uv) * 2) * 4;
				float2 radialDir = i.radialDir * radialAtten;

				float radialWeight = i.parameters.x;

				float2 direction = lerp(sideDir, radialDir, radialWeight);

				return direction.xyxy * i.influence;
			}
			ENDCG
		}

		// Pass 4
		// Draw Fog Of War Shadows
		// port from https://github.com/discosultan/penumbra
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
				float3 segmentB_Offset : NORMAL0;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 penumbra : TEXCOORD0;
				float2 clipValue : TEXCOORD1;
			};

			float4x4 _MVP;
			uniform float4 _Vertices[4];
			uniform float _Radius;
			uniform float2 _LightPosition;
			uniform float _Falloff;

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

				_Vertices[0] = float4(0, 0, 0, 1);
				_Vertices[1] = float4(1, 0, 0, 1);
				_Vertices[2] = float4(0, 1, 0, 1);
				_Vertices[3] = float4(1, 1, 0, 1);

				float2 stencil = _Vertices[v.segmentA_Index.z];

				float2 toSegmentA = v.segmentA_Index.xy - _LightPosition;
				float2 toSegmentB = v.segmentB_Offset.xy - _LightPosition;
				float offset = v.segmentB_Offset.z;

				float2 normToSegmentA = normalize(toSegmentA);
				float2 normToSegmentB = normalize(toSegmentB);

				float2 offsetA = normToSegmentA * offset;
				float2 offsetB = normToSegmentB * offset;

				float2 segmentA = v.segmentA_Index.xy + offsetA;
				float2 segmentB = v.segmentB_Offset.xy + offsetB;

				// Find radius offsets 90deg left and right from light source relative to vertex.
				float2 toLightOffsetA = float2(-_Radius, _Radius) * normToSegmentA.yx;
				float2 toLightOffsetB = float2(_Radius, -_Radius) * normToSegmentB.yx;
				float2 lightOffsetA = _LightPosition + toLightOffsetA + offsetA; // 90 CCW.
				float2 lightOffsetB = _LightPosition + toLightOffsetB + offsetB; // 90 CW.

				// From each edge, project a quad. We have 4 vertices per edge.
				//float2 position = lerp(v.segmentA_Index.xy + normToSegmentA * offset, v.segmentB_Offset.xy + normToSegmentB * offset, stencil.x);
				float2 position = lerp(segmentA.xy, segmentB.xy, stencil.x);
				float2 projectionOffset = lerp(lightOffsetA, lightOffsetB, stencil.x);

				// Setting projected.w to 0 will cause the position to be projected (scaled) infinitely far away in the 
				// perspective division phase. Instead of dividing by 0, the pipeline seems to divide by a very small positive number instead.
				float4 projected = float4(position - projectionOffset * stencil.y, 0.0, 1.0 - stencil.y);

				float2 penumbraA = mul(PenumbraMatrix(toLightOffsetA, toSegmentA), projected.xy - (segmentA.xy) * projected.w);
				float2 penumbraB = mul(PenumbraMatrix(toLightOffsetB, toSegmentB), projected.xy - (segmentB.xy) * projected.w);

				// Find the edge normal. A to B CW 90 deg.
				// ClipValue < 0 means the projection is pointing towards light => no shadow should be generated.
				float2 clipNormal = (segmentB.xy - segmentA.xy).yx * float2(1.0, -1.0);
				float clipValue = dot(clipNormal, projected.xy - projected.w * position);

				// flip Y and Z axis for top-down Fog Of War
				o.position = mul(_MVP, projected.xzyw);
				o.penumbra = float4(penumbraA, penumbraB);
				float4 lightPos = mul(_MVP, float4(_LightPosition, 0, 1.0));
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

		// Pass 5
		// Final blend
		Pass
		{
			ZTest Off
			ZWrite Off
			BlendOp Max
			
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

			sampler2D _FogOfWarShadowMap;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 shadow = tex2D(_FogOfWarShadowMap, i.uv);
				return float4(shadow.r, shadow.g, 0, 1);
			}

			ENDCG
		}

		// Pass 6
		// Draw custom revealer quads to Fog Of War mask
		Pass
		{
			ZTest Off
			ZWrite Off
			Blend DstAlpha One
			BlendOp Add
			ColorMask RG

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
				//float2 uv1:TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			float4x4 VIEW_PROJ;
			sampler2D _FogOfWarCustomRevealerMask;

			v2f vert(appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(VIEW_PROJ, worldPos);
				o.uv = v.uv;
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				float4 shadow = tex2D(_FogOfWarCustomRevealerMask, i.uv);
				return float4(shadow.r, shadow.r, 0, 1);
			}
			ENDCG
		}

		// Pass 7
		// Draw character quads for Water Interaction BUMP mask
		Pass
		{
			ZTest Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				// нужно подключить в Renderer Use Custom Vertex Streams и добавить CenterAndVertexID
				//float4 centerAndVertexID : TEXCOORD1;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				//float2 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			float4x4 VIEW_PROJ;
			sampler2D _WaterParticlesBump;
			float _WaterBumpTreshold;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(VIEW_PROJ, worldPos);
				o.uv = v.uv;
				// конвертируем ID в uv1, потому что uv0 может быть изменен текстурной анимацией системы частиц
				/*o.uv1.x = (v.centerAndVertexID.w == 0 || v.centerAndVertexID.w == 3) ? 0 : 1;
				o.uv1.y = v.centerAndVertexID.w > 1 ? 1 : 0;*/
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				/*return saturate(1 - distance(float2(1,1), i.uv1 * 2));*/
				float4 tex = tex2D(_WaterParticlesBump, i.uv);
				float3 normal = tex.rgb * 2 - 1;
				//float3 normal = UnpackNormal(tex);
				normal = lerp(float3(0, 0, 1), normal, i.color.a);
				float alpha = tex.a;
				normal = normal * .5 + .5;
				return float4(normal, alpha);
			}
			ENDCG
		}

		// Pass 8
		// Draw character quads for Water Interaction FOAM mask
		Pass
		{
			ZTest Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			BlendOp Max
			ColorMask A

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				// нужно подключить в Renderer Use Custom Vertex Streams и добавить CenterAndVertexID
				float4 centerAndVertexID : TEXCOORD1;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			float4x4 VIEW_PROJ;
			float _InteractionFoamStrength;
			float _InteractionFoamPower;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(VIEW_PROJ, worldPos);
				// конвертируем ID в uv1, потому что uv0 может быть изменен текстурной анимацией системы частиц
				o.uv1.x = (v.centerAndVertexID.w == 0 || v.centerAndVertexID.w == 3) ? 0 : 1;
				o.uv1.y = v.centerAndVertexID.w > 1 ? 1 : 0;
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float result = saturate(1 - distance(float2(1,1), i.uv1 * 2)) * i.color.a;
				result = pow(result, _InteractionFoamPower) * _InteractionFoamStrength;
				return result;
			}
			ENDCG
		}

		// Pass 9
		// Draw character quads for Water Interaction FLUID mask
		Pass
		{
			ZTest Off
			ZWrite Off
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			float4x4 VIEW_PROJ;
			sampler2D _MainTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(VIEW_PROJ, worldPos);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv) * i.color;
			}
			ENDCG
		}

		// Pass 10
		// Fluid advect
		Pass
		{
			ZTest Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			sampler2D _SrcTex;
			float2 _SrcTex_TexelSize;
			sampler2D _VelocityTex;
			float _DeltaT;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float2 u = tex2D(_VelocityTex, i.uv).xy;
				float2 flowDir = u * float2(-2.0, 2.0) - float2(-1.0, 1.0);
				float2 coord = i.uv - flowDir * _DeltaT	* _SrcTex_TexelSize;
				float4 result = tex2D(_SrcTex, coord);
				return result;
			}
			ENDCG
		}

		// Pass 11
		// Fluid fade
		Pass
		{
			ZTest Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			sampler2D _SrcTex;
			float2 _SrcTex_TexelSize;
			float _FluidFade;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float3 duv = _SrcTex_TexelSize.xyx * float3(0.5, 0.5, -0.5);

				// Sample source colors.
				float4 c0 = tex2D(_SrcTex, i.uv - duv.xy);
				float4 c1 = tex2D(_SrcTex, i.uv - duv.zy);
				float4 c2 = tex2D(_SrcTex, i.uv + duv.zy);
				float4 c3 = tex2D(_SrcTex, i.uv + duv.xy);
				float4 result = tex2D(_SrcTex, i.uv);
				//return max(result, max(c0, max(c1, max(c2, c3)))) - _FluidFade;
				return (c0 + c1 + c2 + c3) * .25 - _FluidFade;
			}
			ENDCG
		}

		// Pass 12
		// Draw water flow map
		Pass
		{
			ZTest Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			float4x4 VIEW_PROJ;
			sampler2D _WaterFlowMap;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(VIEW_PROJ, worldPos);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 flowMap = tex2D(_WaterFlowMap, i.uv);
				return float4(flowMap.rg, 0, 1);
			}
			ENDCG
		}

		// Pass 13
		// Init composite static mask
		Pass
		{
			Name "INIT_COMPOSITE_STATIC_MASK"
			ZTest Off
			ZWrite Off
			BlendOp Min
			
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

			sampler2D _StaticMaskPart;
			float4 _StaticMaskPart_ScaleOffset;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv * _StaticMaskPart_ScaleOffset.xy + _StaticMaskPart_ScaleOffset.zw;
				//o.uv.y = 1 - o.uv.y;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				if (i.uv.x < 0 || i.uv.y < 0 || i.uv.x > 1 || i.uv.y > 1)
				{
					return 1;
				}

				float4 staticMask = tex2D(_StaticMaskPart, i.uv);
				return float4(staticMask.rgb, 1);
			}
			ENDCG
		}
	}
}
