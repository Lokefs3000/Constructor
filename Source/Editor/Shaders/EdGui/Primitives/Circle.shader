#include "../Shared.hlsl2"
#include "../SDF.hlsl"

#define EPSILON 0.00000001

struct CircleMetadata
{
    float InfillRadius;
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
    CircleMetadata metadata = baMetadata.Load<CircleMetadata>(input.MetadataOffset);

    [branch]
    if (input.Color.a < 0.0)
    {
        input.Color = txGradients.Sample(ssDefaultLinear, input.Color.xy);
    }

    [branch]
    if (metadata.InfillRadius > EPSILON)
    {
        return float4(input.Color.rgb, input.Color.a * -sign(sdCircle(input.UV, 1.0)));
    }

    float d1 = opSubtraction(sdCircle(input.UV, 1.0), sdCircle(input.UV, metadata.InfillRadius));
    return float4(input.Color.rgb, input.Color.a * -sign(d1));
}