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

Texture2D<float4> txImage;

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    if (input.Color.a < 0.0)
    {
        input.Color = txGradients.Sample(ssDefaultLinear, input.Color.xy);
    }

    return txImage.Sample(ssDefaultLinear, input.UV) * input.Color;
}