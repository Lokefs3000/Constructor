#define INCLUDE_RENDERFLAG_CONSTANTS

#include "../Include/WorldBuffers.hlsl"

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
    float3 Vertex : Position;
    float3 Normal : Normal;
}

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float4x4 model = sbRenderFlags[cbRenderConstants.FlagOffset + instanceId].Model;
    
    float3 fragPos = mul(model, float4(input.Position, 1.0)).xyz;
    float3 normal = mul((float3x3)model, input.Normal);

    PsInput output = {
        mul(cbWorldMatricies.ViewProjection, float4(fragPos, 1.0)),
        fragPos,
        normalize(normal)
    };

    return output;
}

struct PsOutput
{
    float4 Position : SV_Target0;
    float4 Normal : SV_Target1;
};

[pixel]
PsOutput PixelMain(PsInput input)
{
    PsOutput output = {
        float4(input.Vertex, 1.0),
        float4(normalize(input.Normal), 1.0)
    };

    return output;
}