Shader "Owlcat/Decals/SurfaceCombatHUD"
{
    Properties
    {
        [Enum(Local, 0, World, 1, Radial, 2)] _UvMapping("UV Mapping", float) = 0
        _MoveAreaLineThickness("Move area line thickness", Float) = 0.1
        _DecalScale("Decal scale", Range(0, 1)) = 0.9
        _GlobalGridLineColor("Global grid line color", Color) = (0.0, 1.0, 0.0)
        _GlobalGridLineThickness("Global grid line thickness", Float) = 0.05
        _gridCellSize("Grid cell size", Float) = 1.35
        _decalSmoothness("Decal smoothness", Float) = 0.9
        _gridLineSmoothness("Grid line smoothness", Float) = 1.7
        
        [PropertyGroup(_SLOPE_FADE, slope)]_SlopeFade("Slope Fade", float) = 0
        _DecalSlopeFadeStart("Slope Fade Start", Range(0, 1)) = 1
		_DecalSlopeFadePower("Slope Fade Power", float) = 1
		_DecalSlopeHardEdgeNormalFactor("Hard Edge Normal Factor", Range(0, 1)) = 0
		
		[PropertyGroup(_HEIGHT_CUTOFF, heightcutoff)]_HeightCutoff("Height cutoff", float) = 0
        _HeightCutoffDelta("Height cutoff", Float) = 0.2
        
        _NopassableTerrainFilteringLength("Move area line thickness", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="OwlcatPipeline" }
        LOD 100

        Pass
        {
            Name "DECAL GUI"
            Tags { "LightMode" = "DecalGUI"}

            // back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			Blend 0 SrcAlpha OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha

			ColorMask RGBA 0
			ColorMask RGBA 1
			ColorMask RG 2

			Stencil
			{
				Ref 1 // see StencilRef
				ReadMask 1
				Comp equal
			}

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal
            #pragma vertex DecalVertex
            #pragma fragment DecalFragment_Polygon

            //--------------------------------------
            // Material keywords
            #pragma shader_feature_local _SLOPE_FADE
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _MASKSMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _EMISSIONMAP
            #pragma shader_feature_local _GRADIENT_FADE
			#pragma shader_feature_local TEXTURE1_ON
			#pragma shader_feature_local RADIAL_ALPHA
			#pragma shader_feature_local NOISE0_ON
			#pragma shader_feature_local NOISE1_ON
			#pragma shader_feature_local NOISE_UV_CORRECTION
			#pragma shader_feature_local COLOR_ALPHA_RAMP
			#pragma shader_feature_local _HEIGHT_CUTOFF

			// -------------------------------------
            // Unity defined keywords
			#pragma shader_feature_local _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // -------------------------------------
            // Owlcat defined keywords
			#pragma multi_compile _ DEBUG_DISPLAY

            #define PROJECTOR
			#define SUPPORT_FOG_OF_WAR			

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"             

			//#ifdef DEBUG_DISPLAY
   //             #include "Packages/com.owlcat.visual/Runtime/RenderPipeline/Debugging/DebugInput.hlsl"
   //         #endif
            
            #include "Packages/com.owlcat.visual/Shaders/Decals/DecalPass.hlsl"

            uniform float _MoveAreaLineThickness;
            uniform float _DecalScale;
            
            uniform float _NopassableTerrainFilteringLength;
            
            #define _NO_PASSABLE_TERRAIN 19999
            #define DECAL_BYTE_DATA_MASK 0x00FF0000
            #define COLOR_BYTE_DATA_MASK 0x0000FF00
            #define BORDER_COLOR_INDEX_MASK 0x000000F0
            #define COMBAT_GRID_BUFFER_SIZE 4096
            #define OUT_OF_GRID_DATA 0 
            
            uniform float _gridCellSize;
            uniform int _gridCenterX;
            uniform int _gridCenterZ;
            
            TEXTURE2D_ARRAY(_DecalsArray);
            SAMPLER(sampler_DecalsArray);

            uniform float _HeightCutoffDelta;
            TEXTURE2D(_GridHeightTex);
            uniform float4 _GridHeightTexSize;
            uniform float3 _GridHeightCenter;
            uniform float2 _GridHeightSize;
                        
            uniform float4 _DecalColors[32];
            uniform float4 _LineColors[16];
            
            uniform float4 _GlobalGridLineColor;
            uniform float _GlobalGridLineThickness;
            
            uniform float _decalSmoothness;
            uniform float _gridLineSmoothness;
            
            CBUFFER_START(_CombatGridBuffer)
                float4 _CombatGridBufferData[COMBAT_GRID_BUFFER_SIZE];
            CBUFFER_END
            
            uint GetGridData(uint2 index){
                uint4 data = asuint(_CombatGridBufferData[(index.x * 128 + index.y) / 4]);
                uint c = (index.x * 128 + index.y) % 4;
                return Select4(data, c);
            }
            
            uint GetGridData(float2 cell){
                int2 index = floor(cell / _gridCellSize);
                int2 gridCenter = int2(_gridCenterX, _gridCenterZ);
                index = index - gridCenter + int2(64,64);
                
                if (index.x < 0 || index.y < 0 || index.x > 127 || index.y > 127){
                    return OUT_OF_GRID_DATA;
                }
                
                uint2 uintIndex = index;
                return GetGridData(uintIndex);
            }
            
            bool GetBit(uint b, int bitNumber){
                bool bit = (b & (((uint) 1) << bitNumber)) != 0;
                return bit;
            }
            
            uint GetTextureArrayIndex(uint b){
                return ((b & DECAL_BYTE_DATA_MASK) >> 16);                
            }
            
            uint GetColorArrayIndex(uint b){
                return ((b & COLOR_BYTE_DATA_MASK) >> 8);                
            }

            uint GetBorderColorArrayIndex(uint b)
            {
                return ((b & BORDER_COLOR_INDEX_MASK) >> 4);
            }
            
            float2 customfwidth(float4 gradients){
                return abs(gradients.xy) + abs(gradients.zw);
            }

            float4 DecalFragment_Polygon(Varyings input) : SV_Target0
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DecalInput decalInput = GetDecalInput(input.positionCS.xy);
                
                // Fog of war early exit.
                #ifdef SUPPORT_FOG_OF_WAR
                    float fowFactor = GetFogOfWarFactor(decalInput.positionWS);
                    if (fowFactor <= 0)
                    {
                        return float4(_FogOfWarColor.rgb, 0);
                    }
                #endif
                
                // slope case
                float alphaModifier = 1;
                #if defined(_SLOPE_FADE)
                    float3 forwardWS = normalize(GetObjectToWorldMatrix()._12_22_32);
                    float slopeFactor = dot(lerp(decalInput.normalWS, decalInput.hardEdgeNormalWS, _DecalSlopeHardEdgeNormalFactor), forwardWS) + (1 - _DecalSlopeFadeStart);
                    slopeFactor = max(0, slopeFactor);
                    slopeFactor = pow(slopeFactor, _DecalSlopeFadePower);
                    alphaModifier *= saturate(slopeFactor);
                #endif
                
                // core
                float2 pos = decalInput.positionWS.xz;
                uint data = GetGridData(pos);
                uint textureIndex = GetTextureArrayIndex(data);
                uint colorIndex = GetColorArrayIndex(data);
                uint borderColorIndex = GetBorderColorArrayIndex(data);
                
                float2 normalizedUV = (decalInput.uv + 10000 * float2(_gridCellSize, _gridCellSize)) % _gridCellSize;
                normalizedUV = normalizedUV / _gridCellSize;
                
                // basic grid & height cutoff
                float2 gridHeightLeftDownCorner = _GridHeightCenter.xz - (_GridHeightSize / 2);
                float2 gridHeightUV = (pos - gridHeightLeftDownCorner) / _GridHeightSize;
                float height = (SAMPLE_TEXTURE2D(_GridHeightTex, s_point_clamp_sampler, gridHeightUV)).r;
                
                if (height >= _NO_PASSABLE_TERRAIN){
                    #if defined(PLATFORM_SUPPORT_GATHER)
                        float4 heightTexSpecialUV = float4(gridHeightUV - _GridHeightTexSize.zw, gridHeightUV) + (0.5*_GridHeightTexSize.zw).xyxy;
                        const float4 d0 = GATHER_TEXTURE2D(_GridHeightTex, s_point_clamp_sampler, heightTexSpecialUV.xy);
                        const float4 d1 = GATHER_TEXTURE2D(_GridHeightTex, s_point_clamp_sampler, heightTexSpecialUV.zw);
                        float d = d1.w;
                        float dx0 = d0.x;// Depth.Load(texCoord, int2(-1,  0));
                        float dx1 = d1.z;// Depth.Load(texCoord, int2(+1,  0));
                        float dy0 = d0.z;// Depth.Load(texCoord, int2( 0, -1));
                        float dy1 = d1.x;// Depth.Load(texCoord, int2( 0, +1));
                    #else
                        float2 pixelSpace = gridHeightUV * _GridHeightTexSize;
                        float d = LOAD_TEXTURE2D(_GridHeightTex, pixelSpace.xy).r;
                        float dx0 = LOAD_TEXTURE2D(_GridHeightTex, pixelSpace.xy + int2(-1, 0)).r;
                        float dx1 = LOAD_TEXTURE2D(_GridHeightTex, pixelSpace.xy + int2(+1, 0)).r;
                        float dy0 = LOAD_TEXTURE2D(_GridHeightTex, pixelSpace.xy + int2(0, -1)).r;
                        float dy1 = LOAD_TEXTURE2D(_GridHeightTex, pixelSpace.xy + int2(0, +1)).r;
                    #endif
                    
                    bool leftPixel = dx0 >= _NO_PASSABLE_TERRAIN;
                    bool rightPixel = dx1 >= _NO_PASSABLE_TERRAIN;
                    bool downPixel = dy0 >= _NO_PASSABLE_TERRAIN;
                    bool upPixel = dy1 >= _NO_PASSABLE_TERRAIN;
                    
                    float4 distances = smoothstep(1, 0, clamp(float4(normalizedUV, 1.0 - normalizedUV) / _NopassableTerrainFilteringLength, 0, 1));
                    distances *= 1 - float4(leftPixel, downPixel, rightPixel, upPixel);
                    alphaModifier *= max(max(distances.x, distances.y), max(distances.z, distances.w));
                } else {
                    #if defined(_HEIGHT_CUTOFF)
                        alphaModifier *= smoothstep(1, 0, clamp(abs(height - decalInput.positionWS.y) - _HeightCutoffDelta, 0, 1));
                    #endif
                }

                float4 globalGridColor = float4(0,0,0,0);
                float4 decalColor = float4(0,0,0,0);
                float4 lineColor = float4(0,0,0,0);
                
                // Global grid
                // we always draw global grid now, and disable renderer at no combat time.
                float2 alignedNormalizedUVGlobalGrid = saturate((1 - 2 * abs(normalizedUV - 0.5)) - (_GlobalGridLineThickness));
                float2 smoothedUVsGlobalGrid = 1 - saturate(alignedNormalizedUVGlobalGrid / customfwidth(decalInput.textureGradients.xyzw * _gridLineSmoothness)); 
                globalGridColor = _GlobalGridLineColor;
                globalGridColor.a *= max(smoothedUVsGlobalGrid.x, smoothedUVsGlobalGrid.y);
                
                // Decal rendering
                float decalCutSize = (1.0 - _DecalScale) / 2.0;
                if (normalizedUV.x > decalCutSize && normalizedUV.y > decalCutSize && normalizedUV.x < (1 - decalCutSize) && normalizedUV.y < (1 - decalCutSize)){
                    float2 normalizedScaledUV = (normalizedUV - float2(decalCutSize, decalCutSize)) * (1.0 / _DecalScale);
                    decalColor = SAMPLE_TEXTURE2D_ARRAY_GRAD(_DecalsArray, sampler_DecalsArray, normalizedScaledUV, textureIndex, decalInput.textureGradients.xy * _decalSmoothness, decalInput.textureGradients.zw * _decalSmoothness) * _DecalColors[colorIndex];
                }
                
                // Border area lines rendering
                // we always draw border area lines, and disable it via passing 0 to our uint constant buffer.                
                float2 alignedNormalizedUVforRightUp = saturate((1 - 2 * (normalizedUV - 0.5)) - _MoveAreaLineThickness);
                float2 alignedNormalizedUVforLeftDown = saturate((1 - 2 * (0.5 - normalizedUV)) - _MoveAreaLineThickness);
                float2 smoothedUVsForRightUp = 1 - saturate(alignedNormalizedUVforRightUp / customfwidth(decalInput.textureGradients.xyzw * _gridLineSmoothness));                
                float2 smoothedUVsForLeftDown = 1 - saturate(alignedNormalizedUVforLeftDown / customfwidth(decalInput.textureGradients.xyzw * _gridLineSmoothness));
                bool right = GetBit(data, 0);
                bool up = GetBit(data, 1);
                bool left = GetBit(data, 2);
                bool down = GetBit(data, 3);
                smoothedUVsForRightUp *= float2(right, up);
                smoothedUVsForLeftDown *= float2(left, down);
                lineColor = _LineColors[borderColorIndex];
                lineColor.a *= max(smoothedUVsForRightUp.x , max(smoothedUVsForRightUp.y , max(smoothedUVsForLeftDown.x , smoothedUVsForLeftDown.y)));
                
                float4 color = float4(0,0,0,0);
                if (globalGridColor.a > 0){
                    color = globalGridColor;
                }
                if (decalColor.a > 0){
                    color = decalColor;
                }
                if (lineColor.a > 0){
                    color = lineColor;
                }
                color.a *= alphaModifier;
                
                // Support fog of war + regular fog
                #ifdef SUPPORT_FOG_OF_WAR
                    ApplyFogOfWarFactor(fowFactor, color.rgb);
                #endif
                
                FinalColorOutput(color);
                
                color.rgb = MixFog(color.rgb, ComputeFogFactor(LinearEyeDepth(decalInput.deviceDepth)));

                return color;
            }
            ENDHLSL
        }
    }
}