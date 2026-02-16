#include "../Shared.hlsl2"
#include "../SDF.hlsl"

#define EPSILON 0.00000001

struct RectMetadata
{
    float4 UVTransform;

    float2 BoxSize;
    float4 Rounding;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    input.MetadataOffset += cbGlobals.MetadataOffset;

    RectMetadata metadata = baMetadata.Load<RectMetadata>(input.MetadataOffset);

    DefaultPsInput output =
    {
        mul(cbGlobals.Model, float4(input.Position, 0.0, 1.0)),
        input.UV * metadata.UVTransform.xy + metadata.UVTransform.zw,
        input.Color,

        input.MetadataOffset
    };
	
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    RectMetadata metadata = baMetadata.Load<RectMetadata>(input.MetadataOffset);

    [branch]
    if (input.Color.a < 0.0)
    {
        input.Color = txGradients.Sample(ssDefaultLinear, input.Color.xy);
    }

    [branch]
    if (!any(metadata.Rounding - EPSILON))
    {
        return input.Color;
    }

    float d1 = sdRoundedBox(input.UV, metadata.BoxSize, metadata.Rounding);
    return float4(input.Color.rgb, input.Color.a * -sign(d1));
}