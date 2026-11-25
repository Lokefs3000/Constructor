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
    float3 Normal : Normal;
}

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float4x4 model = sbRenderFlags[cbRenderConstants.FlagOffset + instanceId].Model;
    float3 fragPos = mul(model, float4(input.Position, 1.0)).xyz;

    PsInput output = {
        mul(cbWorldMatricies.ViewProjection, float4(fragPos, 1.0)),
        mul((float3x3)model, input.Normal)
    };

    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float color = dot(normalize(input.Normal), normalize(float3(1,-1,0))) * 0.2 + 0.8;
    return float4(0.0, color, color, 1.0);
}