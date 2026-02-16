#include "Shared.hlsl2"
#include "SDF.hlsl"

struct StrokeMetadata
{
    float4 UVTransform;

    float2 BoxSize;
    float2 InnerBoxSize;
    float4 Rounding;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    input.MetadataOffset += cbGlobals.MetadataOffset;

    StrokeMetadata metadata = baMetadata.Load<StrokeMetadata>(input.MetadataOffset);

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
    StrokeMetadata metadata = baMetadata.Load<StrokeMetadata>(input.MetadataOffset);

    if (input.Color.a < 0.0)
    {
        input.Color = txGradients.Sample(ssDefaultLinear, input.Color.xy);
    }

    float d1 = sdRoundedBox(input.UV, metadata.BoxSize, metadata.Rounding);
    float d2 = sdRoundedBox(input.UV, metadata.InnerBoxSize, metadata.Rounding);

    return float4(input.Color.rgb, input.Color.a * -sign(opSubtraction(d2, d1)));
}