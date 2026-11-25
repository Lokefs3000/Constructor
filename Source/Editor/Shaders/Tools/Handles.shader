#include "Engine/Shaders/Implicit.hlsl"
#include "Engine/Shaders/ForwardPlus/Globals.hlsl"

struct VsInput
{
    float3 Position : POSITION;
    float3 Color    : COLOR;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
    float3 Color      : COLOR;
};

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbFP_GlobalMatricies.ViewProjection, float4(input.Position, 1.0)),
        input.Color
    };
    
    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return float4(input.Color, 1.0);
}