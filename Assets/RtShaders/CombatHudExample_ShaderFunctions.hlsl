namespace CombatHudExample
{
    float2 GetSplineTiledUv(
       float2 uv,
       float segmentLength,
       float offset)
    {
        float2 result;
        result.x = uv.x;
        result.y = (uv.y - offset) / segmentLength;
        return result;
    }
                
    float2 GetSplineClampedUv(
        float2 uv,
        float segmentLength,
        float intervalLength,
        float offset)
    {
        const float wrap = (segmentLength + intervalLength) / segmentLength;

        float2 result;
        result.x = uv.x;
        result.y = (uv.y - offset) / segmentLength;
        result.y = fmod(fmod(result.y, wrap) + wrap, wrap);
        return result;
    }

    float2 GetSplineTiledSeamlessUv(
        float2 uv,
        float splineLength,
        float segmentLength,
        float offset)
    {
        return GetSplineTiledUv(
            uv,
            splineLength / ceil(splineLength / segmentLength),
            offset);
    }

    float2 GetSplineClampedSeamlessUv(
        float2 uv,
        float splineLength,
        float segmentLength,
        float intervalLength,
        float offset)
    {
        const float fullSegmentsPerSpline = ceil(splineLength / (segmentLength + intervalLength));
        const float fullSegmentLength = splineLength / fullSegmentsPerSpline;
        const float mutatedSegmentLength = min(segmentLength, fullSegmentLength);
        const float mutatedIntervalLength = fullSegmentLength - segmentLength;
    
        return GetSplineClampedUv(
            uv,
            mutatedSegmentLength,
            mutatedIntervalLength,        
            offset);
    }

    float2 GetCellUv(float2 uv, float tiling)
    {
        return uv * tiling;
    }

    float2 GetAreaUv(float2 uv, float2 areaSize, float imageSize, bool clampImageSize)
    {
        const float mutatedImageSize = lerp(
            imageSize,
            min(min(areaSize.x, areaSize.y), imageSize),
            clampImageSize);
            
        const float2 scale = areaSize / float2(mutatedImageSize, mutatedImageSize);
        const float2 offset = float2(0.5, 0.5);
        return (uv / areaSize - offset) * scale + offset;
    }
}

void CombatHudExample_GetSplineTextureUv_float(
    float2 uv,
    float splineLength,
    float segmentLength,
    float intervalLength,
    float offset,
    out float2 tiledUv,
    out float2 tiledSeamlessUv,
    out float2 clampedUv,
    out float2 clampedSeamlessUv)
{
    tiledUv = CombatHudExample::GetSplineTiledUv(uv, segmentLength, offset);
    tiledSeamlessUv = CombatHudExample::GetSplineTiledSeamlessUv(uv, splineLength, segmentLength, offset);
    clampedUv = CombatHudExample::GetSplineClampedUv(uv, segmentLength, intervalLength, offset);
    clampedSeamlessUv = CombatHudExample::GetSplineClampedSeamlessUv(uv, splineLength, segmentLength, intervalLength, offset);
}

void CombatHudExample_GetFillTextureUv_float(
    float2 uv,
    float2 areaSize,
    float tiling,
    float imageSize,
    bool clampImageSize,
    out float2 cellUv,
    out float2 areaUv)
{
    cellUv = CombatHudExample::GetCellUv(uv, tiling);
    areaUv = CombatHudExample::GetAreaUv(uv, areaSize, imageSize, clampImageSize);
}
