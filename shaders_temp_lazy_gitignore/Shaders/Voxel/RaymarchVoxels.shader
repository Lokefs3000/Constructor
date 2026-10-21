#include "../Include/VoxelManipulation.hlsl"

static const float2 s_ScreenVertices[3] =
{
    float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : Texcoord;
}

struct ProjectionData
{
    float4x4 InvViewMatrix;
    float4x4 InvProjectionMatrix;
    float3 CameraOrigin;
}

ConstantBuffer<ProjectionData> cbProjectionData;

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexID)
{
    float2 pos = s_ScreenVertices[vertexId];
    float2 uv = pos * 0.5 + 0.5;

    PsInput output = {
        float4(pos, 0.0, 1.0),
        float2(uv.x, 1.0 - uv.y)
    };

    return output;
}

bool IsIntersecting(float3 origin, float3 invDir, float3 boxMin, float3 boxMax, out float minDist, out float maxDist)
{
    float t1 = (boxMin.x - origin.x) * invDir.x;
    float t2 = (boxMax.x - origin.x) * invDir.x;

    float tmin = min(t1, t2);
    float tmax = max(t1, t2);

    for (int i = 0; i < 3; ++i)
    {
        t1 = (boxMin[i] - origin[i]) * invDir[i];
        t2 = (boxMax[i] - origin[i]) * invDir[i];

        tmin = max(tmin, min(min(t1, t2), tmax));
        tmax = min(tmax, max(max(t1, t2), tmin));
    }

    minDist = tmin < 0.0f ? tmax : tmin;
    maxDist = tmin < 0.0f ? tmin : tmax;

    return tmax > max(tmin, 0.0);
}

struct Ray
{
    float3 Origin;
    float3 Direction;
    float3 InvDirection;
};

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float3 halfVoxelSize = cbVoxelInfo.VoxelSize / 2;

    float3 voxelSpaceMin = cbVoxelInfo.VoxelOrigin - halfVoxelSize;
    float3 voxelSpaceMax = cbVoxelInfo.VoxelOrigin + halfVoxelSize;

    float2 relative = input.UV * 2.0 - 1.0;

    float3 dir = float3(relative.x, -relative.y, -1.0);
    dir = mul(cbProjectionData.InvProjectionMatrix, float4(dir, 1.0)).xyz;
    dir = mul(cbProjectionData.InvViewMatrix, float4(dir, 1.0)).xyz;
    dir = normalize(dir - cbProjectionData.CameraOrigin);

    Ray ray = {
        cbProjectionData.CameraOrigin - dir,
        dir,
        1.0 / dir
    };

    float minDist, maxDist;
    bool doesHitVoxelSpace = IsIntersecting(ray.Origin, ray.InvDirection, voxelSpaceMin, voxelSpaceMax, minDist, maxDist);
    if (doesHitVoxelSpace)
    {
        if (all(ray.Origin >= voxelSpaceMin) && all(ray.Origin <= voxelSpaceMax))
        {
            maxDist = minDist;
            minDist = 0.0;
        }

        float3 enterPosition = ray.Origin + ray.Direction * minDist - cbVoxelInfo.VoxelOrigin;
        float3 exitPosition = ray.Origin + ray.Direction * maxDist - cbVoxelInfo.VoxelOrigin;

        enterPosition = clamp(enterPosition + halfVoxelSize, 0.0, 47.0);
        exitPosition = clamp(exitPosition + halfVoxelSize, 0.0, 47.0);

        int stepCount = min((int)ceil(distance(enterPosition, exitPosition)), 48) * 16;
        float invStepCountValue = 1.0 / (float)stepCount;

        for (int i = 0; i < stepCount; ++i)
        {
            float3 pos = lerp(enterPosition, exitPosition, i * invStepCountValue);

            bool isWithinSpace;
            int3 voxelPos = (int3)round(pos);

            if (!isWithinSpace)
            {
                continue;
            }

            uint voxel = txVoxelBuffer.Load(voxelPos);
            if (voxel != 0)
            {
                return float4(1.0 - i * invStepCountValue, i * invStepCountValue, 0.0, 1.0);
            }
        }
    }

    discard;
    return 0.0;
}