#include "Buffers.hlsl"
#include "Converters.hlsl"

#pragma path "Hidden/Missing"

struct VsInput
{
    float3 Position : POSITION;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
};

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float3 fragPos = ConvertObjectToModel(input.Position, instanceId);
    
    PsInput output =
    {
        ConvertModelToWorld(fragPos, instanceId),
    };
    
    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float2 position = floor(input.SVPosition.xy / 32.0);
    float checker = floor(position.x) + floor(position.y);
    checker = frac(checker * 0.5) + 0.5;

    return float4(0.0, checker, checker, 1.0);
}