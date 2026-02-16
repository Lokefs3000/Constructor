#include "Shared.hlsl2"
#include "SDF.hlsl"

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    DefaultPsInput output =
    {
        mul(cbGlobals.Model, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.Color,

        0
    };
	
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    return input.Color;
}