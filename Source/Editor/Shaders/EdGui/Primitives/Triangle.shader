#include "../Shared.hlsl2"
#include "../SDF.hlsl"

#define EPSILON 0.00000001

struct TriangleMetadata
{
    float2 A;
    float2 B;
    float2 C;

    float Rounding;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    input.MetadataOffset += cbGlobals.MetadataOffset;

    DefaultPsInput output =
    {
        mul(cbGlobals.Model, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.Color,

        input.MetadataOffset
    };
	
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    TriangleMetadata metadata = baMetadata.Load<TriangleMetadata>(input.MetadataOffset);

    [branch]
    if (input.Color.a < 0.0)
    {
        input.Color = txGradients.Sample(ssDefaultLinear, input.Color.xy);
    }

    [branch]
    if (metadata.Rounding < 0.00000001)
    {
        return input.Color;
    }

    float d1 = opRound(sdTriangle(input.UV, metadata.A, metadata.B, metadata.C), metadata.Rounding);
    return float4(input.Color.rgb, input.Color.a * -sign(d1));
}