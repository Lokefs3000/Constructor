#include "../Include/WorldBuffers.hlsl"
#include "../Include/VoxelManipulation.hlsl"

struct VsInput
{
    float3 Position : Position;
};

struct PsInput
{
    float4 Position : SV_Position;
    float3 FragPos : Position;
}

struct ProjectViewData
{
    float4x4 ViewProjection;
}

ConstantBuffer<ProjectViewData> cbProjectedView;

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float4x4 model = sbRenderFlags[cbRenderConstants.FlagOffset + instanceId].Model;
    float3 fragPos = mul(model, float4(input.Position, 1.0)).xyz;

    PsInput output = {
        mul(cbProjectedView.ViewProjection, float4(fragPos, 1.0)),
        fragPos
    };

    return output;
}

[pixel]
float PixelMain(PsInput input) : SV_Target
{
    bool isWithinSpace;
    uint3 voxelPos = GetVoxelPositionFromWorld(input.FragPos, isWithinSpace);

    if (isWithinSpace)
    {
        txVoxelBuffer[voxelPos] = 1;
    }

    return isWithinSpace ? 1 : 0;
}