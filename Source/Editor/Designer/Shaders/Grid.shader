#include "Editor/Shaders/EdGui/Shared.hlsl2"

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD;
};

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), 0.0, 1.0),
        input.UV,
    };
	
	return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return float4(input.UV, 1.0, 1.0);
}