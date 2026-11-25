#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct CircleMetadata
{
    uint16_t HasStroke;
    
    float16_t4 Color;
    uint16_t ZIndex;

    float16_t Radius;

    uint16_t __pad;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    CircleMetadata metadata = baMetadata.Load<CircleMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.00001, 1.0),
        input.UV * 2.0 - float(metadata.Radius),
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    CircleMetadata metadata = baMetadata.Load<CircleMetadata>(input.GetMetadataOffset());

    float dist = sdCircle(input.UV, metadata.Radius);
    if (input.HasStroke())
    {
        StrokeMetadata stroke = baMetadata.Load<StrokeMetadata>(input.GetMetadataOffset() + sizeof(CircleMetadata));

        float innerDist = sdCircle(input.UV, metadata.Radius - stroke.Width);
        float subtraction = opSubtraction(innerDist, dist);

        float4 color = lerp(stroke.Color, input.Color, subtraction);
        return float4(color.rgb, color.a * SmoothSDF(dist));
    }

    return float4(input.Color.rgb, float(input.Color.a) * SmoothSDF(dist));
}