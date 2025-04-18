Shader "Hidden/Fluid2D"
{
	CGINCLUDE
	#include "UnityCG.cginc"

	CBUFFER_START(FluidBuffer)
		float2 _FadeVelocity;
		float2 _FadeDye;
		float _Dt;
		float2 _Speed;
		float _FluidGlobalTime;
	CBUFFER_END
	ENDCG

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		// Advect Velocity
		Pass
		{
			Name "Advect Velocity"

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

			sampler2D _Velocity;
			sampler2D _Target;
			float4 _Target_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 u = tex2D(_Velocity, i.uv.xy);
				float2 offsetScale = _Target_TexelSize.xy * _Dt;
				float2 tracedPos0 = i.uv.xy - (u.xy * offsetScale);
				float2 tracedPos1 = i.uv.xy - (u.zw * offsetScale);
				float2 result0 = tex2D(_Target, tracedPos0.xy).xy;
				float2 result1 = tex2D(_Target, tracedPos1.xy).zw;
				return float4(result0, result1);
			}
			ENDCG
		}

		// Apply Forces
		Pass
		{
			Name "Apply Forces"

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

			sampler2D _Velocity;
			sampler2D _ForceTex;
			float4 _ForceTex_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 v = tex2D(_Velocity, i.uv.xy);
				float4 f = tex2D(_ForceTex, i.uv.xy);

				/*v.xy *= _FadeVelocity.x;
				v.zw *= _FadeVelocity.y;*/

				v += f;

				return v;
			}
			ENDCG
		}

		// Advect Dye
		Pass
		{
			Name "Advect Dye"

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

			sampler2D _Velocity;
			sampler2D _Target;
			float4 _Target_TexelSize;
			float4 _VelocityMask;
			float2 _FadeMask;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 velocitySample = tex2D(_Velocity, i.uv.xy) * _VelocityMask;
				float2 u = velocitySample.xy + velocitySample.zw;
				float2 offsetScale = _Target_TexelSize.xy * _Dt;
				float2 tracedPos = i.uv.xy - (u * offsetScale);
				float fade = dot(_FadeDye, _FadeMask);
				float4 dye = tex2D(_Target, tracedPos.xy);
				dye.a = saturate(dye.a - fade);
				return dye;
			}
			ENDCG
		}

		// Velocity divergence
		Pass
		{
			Name "Velocity Divergence"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ VELOCITY_BOUNDARY
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float halfRDX : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Velocity;
			sampler2D _Obstacles;
			float4 _Velocity_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.halfRDX = _Velocity_TexelSize.x * .5;
				return o;
			}

			float4 sampleVelocity(sampler2D velocity, float2 coord)
			{
				float2 cellOffset = float2 (0.0, 0.0);
				float2 multiplier = float2 (1.0, 1.0);

				//free-slip boundary: the average flow across the boundary is restricted to 0
				//avg(uA.xy, uB.xy) dot (boundary normal).xy = 0
				//walls
				#ifdef VELOCITY_BOUNDARY
					if (coord.x < 0.0) {
						cellOffset.x = 1.0;
						multiplier.x = -1.0;
					}
					else if (coord.x > 1.0) {
						cellOffset.x = -1.0;
						multiplier.x = -1.0;
					}
					if (coord.y < 0.0) {
						cellOffset.y = 1.0;
						multiplier.y = -1.0;
					}
					else if (coord.y > 1.0) {
						cellOffset.y = -1.0;
						multiplier.y = -1.0;
					}
				#endif

				return multiplier.xyxy * tex2D(velocity, coord + cellOffset * _Velocity_TexelSize.xy);
			}

			float4 frag (v2f i) : SV_Target
			{
				/*float4 L = tex2D(_Velocity, i.uv.xy - float2 (_Velocity_TexelSize.x, 0));
				float4 R = tex2D(_Velocity, i.uv.xy + float2 (_Velocity_TexelSize.x, 0));
				float4 B = tex2D(_Velocity, i.uv.xy - float2 (0, _Velocity_TexelSize.y));
				float4 T = tex2D(_Velocity, i.uv.xy + float2 (0, _Velocity_TexelSize.y));*/

				float4 L = sampleVelocity(_Velocity, i.uv.xy - float2 (_Velocity_TexelSize.x, 0));
				float4 R = sampleVelocity(_Velocity, i.uv.xy + float2 (_Velocity_TexelSize.x, 0));
				float4 B = sampleVelocity(_Velocity, i.uv.xy - float2 (0, _Velocity_TexelSize.y));
				float4 T = sampleVelocity(_Velocity, i.uv.xy + float2 (0, _Velocity_TexelSize.y));

				float4 oL = tex2D(_Obstacles, i.uv.xy - float2 (_Velocity_TexelSize.x, 0));
				float4 oR = tex2D(_Obstacles, i.uv.xy + float2 (_Velocity_TexelSize.x, 0));
				float4 oB = tex2D(_Obstacles, i.uv.xy - float2 (0, _Velocity_TexelSize.y));
				float4 oT = tex2D(_Obstacles, i.uv.xy + float2 (0, _Velocity_TexelSize.y));

				//R.x *= oR.r > 0 ? 1 : -1;
				//L.x *= oL.r > 0 ? 1 : -1;
				//B.y *= oB.r > 0 ? 1 : -1;
				//T.y *= oT.r > 0 ? 1 : -1;
				//
				//R.z *= oR.g > 0 ? 1 : -1;
				//L.z *= oL.g > 0 ? 1 : -1;
				//B.w *= oB.g > 0 ? 1 : -1;
				//T.w *= oT.g > 0 ? 1 : -1;

				R.xy *= oR.r > 0;
				B.xy *= oB.r > 0;
				T.xy *= oT.r > 0;
				L.xy *= oL.r > 0;
				
				R.zw *= oR.g > 0;
				B.zw *= oB.g > 0;
				T.zw *= oT.g > 0;
				L.zw *= oL.g > 0;

				float halfRDX = _Velocity_TexelSize.x * .5;
				float4 result = float4(halfRDX * ((R.x - L.x) + (T.y - B.y)), halfRDX * ((R.z - L.z) + (T.w - B.w)), 0, 1);
				return result;
			}
			ENDCG
		}

		// Solve Pressure
		Pass
		{
			Name "Solve Pressure"

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

			sampler2D _Pressure;
			sampler2D _Obstacles;
			float4 _Pressure_TexelSize;
			sampler2D _Divergence;
			float _Alpha;		// alpha = -(dx)^2, where dx = grid cell size

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 C = tex2D(_Pressure, i.uv.xy).rg;
				float2 L = tex2D(_Pressure, i.uv.xy - float2 (_Pressure_TexelSize.x, 0)).rg;
				float2 R = tex2D(_Pressure, i.uv.xy + float2 (_Pressure_TexelSize.x, 0)).rg;
				float2 B = tex2D(_Pressure, i.uv.xy - float2 (0, _Pressure_TexelSize.y)).rg;
				float2 T = tex2D(_Pressure, i.uv.xy + float2 (0, _Pressure_TexelSize.y)).rg;

				float4 oL = tex2D(_Obstacles, i.uv.xy - float2 (_Pressure_TexelSize.x, 0));
				float4 oR = tex2D(_Obstacles, i.uv.xy + float2 (_Pressure_TexelSize.x, 0));
				float4 oB = tex2D(_Obstacles, i.uv.xy - float2 (0, _Pressure_TexelSize.y));
				float4 oT = tex2D(_Obstacles, i.uv.xy + float2 (0, _Pressure_TexelSize.y));

				float2 bC = tex2D(_Divergence, i.uv.xy).rg;

				L.x = oL.r > 0 ? L.x : C.x;
				R.x = oR.r > 0 ? R.x : C.x;
				B.x = oB.r > 0 ? B.x : C.x;
				T.x = oT.r > 0 ? T.x : C.x;

				L.y = oL.g > 0 ? L.y : C.y;
				R.y = oR.g > 0 ? R.y : C.y;
				B.y = oB.g > 0 ? B.y : C.y;
				T.y = oT.g > 0 ? T.y : C.y;

				float4 finalColor = float4((L + R + B + T + _Alpha * bC) * 0.25, 0, 1);//rBeta = .25

				return finalColor;
			}
			ENDCG
		}

		// Substract Pressure Gradient
		Pass
		{
			Name "Substract Pressure Gradient"

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
				float halfRDX : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Velocity;
			sampler2D _Pressure;
			sampler2D _Obstacles;
			float4 _Pressure_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.halfRDX = _Pressure_TexelSize.x * .5;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 C = tex2D(_Pressure, i.uv.xy).rg;
				float2 L = tex2D(_Pressure, i.uv.xy - float2(_Pressure_TexelSize.x, 0)).rg;
				float2 R = tex2D(_Pressure, i.uv.xy + float2(_Pressure_TexelSize.x, 0)).rg;
				float2 B = tex2D(_Pressure, i.uv.xy - float2(0, _Pressure_TexelSize.y)).rg;
				float2 T = tex2D(_Pressure, i.uv.xy + float2(0, _Pressure_TexelSize.y)).rg;

				float4 oL = tex2D(_Obstacles, i.uv.xy - float2 (_Pressure_TexelSize.x, 0));
				float4 oR = tex2D(_Obstacles, i.uv.xy + float2 (_Pressure_TexelSize.x, 0));
				float4 oB = tex2D(_Obstacles, i.uv.xy - float2 (0, _Pressure_TexelSize.y));
				float4 oT = tex2D(_Obstacles, i.uv.xy + float2 (0, _Pressure_TexelSize.y));

				L.x = oL.r > 0 ? L.x : C.x;
				R.x = oR.r > 0 ? R.x : C.x;
				B.x = oB.r > 0 ? B.x : C.x;
				T.x = oT.r > 0 ? T.x : C.x;

				L.y = oL.g > 0 ? L.y : C.y;
				R.y = oR.g > 0 ? R.y : C.y;
				B.y = oB.g > 0 ? B.y : C.y;
				T.y = oT.g > 0 ? T.y : C.y;

				float4 v = tex2D(_Velocity, i.uv.xy);
				float halfRDX = _Pressure_TexelSize.x * .5;
				float4 finalColor = float4(v.xy - halfRDX * float2(R.x - L.x, T.x - B.x), v.zw - halfRDX * float2(R.y - L.y, T.y - B.y));

				return finalColor;
			}
			ENDCG
		}

		// Vorticity
		Pass
		{
			Name "Vorticity"

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

			sampler2D _Velocity;
			float4 _Velocity_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 L = tex2D(_Velocity, i.uv.xy - float2(_Velocity_TexelSize.x, 0)).yw;
				float2 R = tex2D(_Velocity, i.uv.xy + float2(_Velocity_TexelSize.x, 0)).yw;
				float2 B = tex2D(_Velocity, i.uv.xy - float2(0, _Velocity_TexelSize.y)).xz;
				float2 T = tex2D(_Velocity, i.uv.xy + float2(0, _Velocity_TexelSize.y)).xz;

				//float scale = .5 / gridScale;
				float scale = .125;
				return float4(scale * (R - L - T + B), 0.0, 1.0);
			}
			ENDCG
		}

		// Vorticity Confinement
		Pass
		{
			Name "Vorticity Confinement"

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

			sampler2D _Velocity;
			sampler2D _Vorticity;
			float4 _Vorticity_TexelSize;
			float2 _Curl;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 L = tex2D(_Vorticity, i.uv.xy - float2(_Vorticity_TexelSize.x, 0)).xy;
				float2 R = tex2D(_Vorticity, i.uv.xy + float2(_Vorticity_TexelSize.x, 0)).xy;
				float2 B = tex2D(_Vorticity, i.uv.xy - float2(0, _Vorticity_TexelSize.y)).xy;
				float2 T = tex2D(_Vorticity, i.uv.xy + float2(0, _Vorticity_TexelSize.y)).xy;
				float2 C = tex2D(_Vorticity, i.uv).xy;

				float scale = .125;

				float2 force0 = scale * float2(abs(T.x) - abs(B.x), abs(R.x) - abs(L.x));
				float lengthSquared = max(2.4414e-4, dot(force0, force0));
				force0 *= rsqrt(lengthSquared) * _Curl.x * C.x;
				force0.y *= -1;

				float2 force1 = scale * float2(abs(T.y) - abs(B.y), abs(R.y) - abs(L.y));
				lengthSquared = max(2.4414e-4, dot(force1, force1));
				force1 *= rsqrt(lengthSquared) * _Curl.y * C.y;
				force1.y *= -1;

				float4 velc = tex2D(_Velocity, i.uv.xy);
				float2 vel0 = velc.xy + (_Dt * force0);
				float2 vel1 = velc.zw + (_Dt * force1);
				return float4(vel0, vel1);
			}
			ENDCG
		}

		// Draw blood particles to velocity buffer
		Pass
		{
			Name "Draw Blood Particles To Velocity Buffer"
			ColorMask RG

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"

				struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 flowMapUv : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			sampler2D _FlowMask;
			float4 _FlowMask_ST;
			sampler2D _FluidBloodTex;
			float2 _InvertFlow;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.flowMapUv = TRANSFORM_TEX(worldPos.xz, _FlowMask);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 blood = tex2D(_FluidBloodTex, i.uv);
				float4 flowTex = tex2D(_FlowMask, i.flowMapUv);
				float2 flow = flowTex.xy * 2 - 1;
				flow *= _InvertFlow;
				return float4(flow.xy * _Speed.x, 0, 0);
			}
			ENDCG
		}

		// Draw blood particles to color buffer
		Pass
		{
			Name "Draw Blood Particles To Color Buffer"
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
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
				float4 color : COLOR0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _FluidBloodTex;
			
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
				float4 blood = tex2D(_FluidBloodTex, i.uv);
				return blood * i.color;
			}
			ENDCG
		}

		// Update Dye Blood
		Pass
		{
			Name "Update Dye Blood"

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

			sampler2D _Dye;
			sampler2D _UpdateDyeTex;
			float2 _FadeMask;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 d = tex2D(_Dye, i.uv.xy);
				float4 u = tex2D(_UpdateDyeTex, i.uv.xy);

				float fade = dot(_FadeDye, _FadeMask);
				//d *= fade;
				d.a = saturate(d.a - fade);

				// alpha blending
				//float4 result = d + u;
				float4 result = lerp(d, u, u.a);
				return result;
			}
			ENDCG
		}

		// Draw fog particles to velocity buffer
		Pass
		{
			Name "Draw Fog Particles To Velocity Buffer"

			ZTest Off
			ZWrite Off
			ColorMask BA

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 uvLifetime : TEXCOORD0; // xy - UV, z - AgePercent, w - Inverse Start Lifetime (1 / Lifetime)
				float4 centerRotation : TEXCOORD1; // xyz - Center, w - Rotation Z
				float4 parameters : TEXCOORD2; // x - radial weight, y - force factor
				float4 color : COLOR0;
			};

			struct v2f
			{
				float4 uvLifetime : TEXCOORD0;
				float2 radialDir : TEXCOORD1;
				float4 sideDir : TEXCOORD2;
				float4 parameters : TEXCOORD3;
				float2 velocityUv : TEXCOORD4;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Velocity;
			float4 _Velocity_ST;
			
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

				o.vertex = mul(UNITY_MATRIX_VP, worldPos);
				o.uvLifetime = v.uvLifetime;
				o.parameters = v.parameters;

				o.velocityUv = TRANSFORM_TEX(worldPos.xz, _Velocity);

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 prevVel = tex2D(_Velocity, i.velocityUv);
				float2 sideDir = i.uvLifetime.y > .5 ? i.sideDir.zw : i.sideDir.xy;
				sideDir *= 1 - abs(.5 - i.uvLifetime.y) * 2;

				float2 radialDir = (i.uvLifetime.xy * 2 - 1);
				float radialAtten = 1 - saturate(length(radialDir));
				//clip(1 - radialAtten);

				radialDir = i.radialDir;

				float radialWeight = i.parameters.x;

				float2 direction = lerp(sideDir, radialDir, radialWeight);
				return prevVel + direction.xyxy * radialAtten * i.parameters.y;
			}
			ENDCG
		}

		// Draw fog particles to color buffer
		Pass
		{
			Name "Draw Fog Particles To Color Buffer"

			ZTest Off
			ZWrite Off
			//Blend OneMinusSrcAlpha SrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 uvLifetime : TEXCOORD0; // xy - UV, z - AgePercent, w - Inverse Start Lifetime (1 / Lifetime)
				float4 centerRotation : TEXCOORD1; // xyz - Center, w - Rotation Z
				float4 parameters : TEXCOORD2; // x - radial weight, y - force factor
				float4 color : COLOR0;
			};

			struct v2f
			{
				float4 uvLifetime : TEXCOORD0;
				float2 radialDir : TEXCOORD1;
				float4 sideDir : TEXCOORD2;
				float2 dyeUv : TEXCOORD3;
				float4 parameters : TEXCOORD4;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Dye;
			float4 _Dye_ST;
			
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

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvLifetime = v.uvLifetime;
				o.dyeUv = TRANSFORM_TEX(worldPos.xz, _Dye);
				o.parameters = v.parameters;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 prevDye = tex2D(_Dye, i.dyeUv);
				float2 sideDir = i.uvLifetime.y > .5 ? i.sideDir.zw : i.sideDir.xy;
				sideDir *= 1 - abs(.5 - i.uvLifetime.y) * 2;
				float sideAtten = 1 - saturate(length(sideDir));
				float radialAtten = saturate(length(i.uvLifetime.xy * 2 - 1));
				float radialWeight = i.parameters.x;
				float atten = lerp(sideAtten, radialAtten, radialWeight);
				float4 result = prevDye * atten;
				//result = lerp(prevDye, result, i.parameters.z);
				return result;
			}
			ENDCG
		}

		// Draw ambient fog to velocity buffer
		Pass
		{
			Name "Draw Ambient Fog To Velocity Buffer"

			ZTest Off
			ZWrite Off
			ColorMask BA

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
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Velocity;
			float _FogWindScale;
			sampler2D _FogWindTex;
			float4 _FogWindTex_ST;

			v2f vert (appdata v)
			{
				v2f o = (v2f)0;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = v.uv;
				o.uv.zw = _FogWindTex_ST.xy * (v.uv.xy + _FogWindTex_ST.zw * _FluidGlobalTime.xx);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 velocity = tex2D(_Velocity, i.uv.xy).ba;
				float2 wind = tex2D(_FogWindTex, i.uv.zw).rg * 2 - 1;
				float2 result = velocity + wind * _FogWindScale;
				return float4(0, 0, result);
			}
			ENDCG
		}

		// Draw ambient fog to color buffer
		Pass
		{
			Name "Draw Ambient Fog To Color Buffer"

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Dye;
			sampler2D _FogTex0;
			float4 _FogTex0_ST;
			sampler2D _FogTex1;
			float4 _FogTex1_ST;
			float _FogIntensityScale;
			float2 _FadeMask;

			v2f vert (appdata v)
			{
				v2f o = (v2f)0;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv1.xy = _FogTex0_ST.xy * (v.uv.xy + _FogTex0_ST.zw * _FluidGlobalTime.xx);
				o.uv1.zw = _FogTex1_ST.xy * (v.uv.xy + _FogTex1_ST.zw * _FluidGlobalTime.xx);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 prevDye = tex2D(_Dye, i.uv);
				float4 fog0 = tex2D(_FogTex0, i.uv1.xy);
				float4 fog1 = tex2D(_FogTex1, i.uv1.zw);
				float4 fog = fog0 + fog1;
				return lerp(prevDye, fog, _FogIntensityScale);
			}
			ENDCG
		}
		
		// Init Ambient Fog Color
		Pass
		{
			Name "INIT AMBIENT FOG COLOR"

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
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _FogTex0;
			float4 _FogTex0_ST;
			sampler2D _FogTex1;
			float4 _FogTex1_ST;
			float _FogIntensityScale;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = _FogTex0_ST.xy * (v.uv.xy + _FogTex0_ST.zw * _FluidGlobalTime.xx);
				o.uv.zw = _FogTex1_ST.xy * (v.uv.xy + _FogTex1_ST.zw * _FluidGlobalTime.xx);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float fog0 = tex2D(_FogTex0, i.uv.xy).a;
				float fog1 = tex2D(_FogTex1, i.uv.zw).a;
				float result = fog0 * fog1 * _FogIntensityScale;
				return result;
			}
			ENDCG
		}

		// Draw water flow map
		Pass
		{
			Name "FLOW MASK"
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
				return float4(flowMap.rg, 1, 1);
				float obstacleMask = length(flowMap.rg * 2 - 1) > .1 ? 1 : 0;
				return float4(flowMap.rg, obstacleMask, 1);
			}
			ENDCG
		}
	}
}
