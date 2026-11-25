#include "Engine/Shaders/Implicit.hlsl"
#include "Engine/Shaders/ForwardPlus/Globals.hlsl"

struct VsInput
{
    float3 Position : POSITION;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
};

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbFP_GlobalMatricies.ViewProjection, float4(input.Position, 1.0)),
    };
    
    return output;
}

[pixel]
float4 PixelMain() : SV_Target
{
    return 0.0;
}