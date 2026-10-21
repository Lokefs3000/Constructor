#define INCLUDE_RENDERFLAG_CONSTANTS

#include "Include/WorldBuffers.hlsl"

struct VsInput
{
    float3 Position : Position;
    float3 Normal : Normal;
    float4 Tangent : Tangent;
    float2 UV : Texcoord;
};

struct PsInput
{
    float4 Position : SV_Position;
}

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float4x4 model = sbRenderFlags[cbRenderConstants.FlagOffset + instanceId].Model;
    float3 fragPos = mul(model, float4(input.Position, 1.0)).xyz;

    PsInput output = {
        mul(cbWorldMatricies.ViewProjection, float4(fragPos, 1.0)),
    };

    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return float4(0.0, 0.0, 0.0, 1.0);
}