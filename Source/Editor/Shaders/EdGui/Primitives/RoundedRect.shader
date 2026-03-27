#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct RoundedRectMetadata
{
    uint16_t HasStroke;
    
    float16_t4 Color;
    uint16_t ZIndex;
    
    float16_t Radius;
    uint16_t2 BoxSize;

    uint16_t __pad;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    RoundedRectMetadata metadata = baMetadata.Load<RoundedRectMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.01, 1.0),
        input.UV * 2.0 - metadata.BoxSize,
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    RoundedRectMetadata metadata = baMetadata.Load<RoundedRectMetadata>(input.GetMetadataOffset());

    float dist = sdRoundedBox(input.UV, metadata.BoxSize, metadata.Radius);
    if (input.HasStroke())
    {
        StrokeMetadata stroke = baMetadata.Load<StrokeMetadata>(input.GetMetadataOffset() + sizeof(RoundedRectMetadata));

        if (dist + (stroke.Width + stroke.Width) >= 0.0)
            return float4(stroke.Color.rgb, float(stroke.Color.a) * SmoothSDF(dist));
    }

    return float4(input.Color.rgb, float(input.Color.a) * SmoothSDF(dist));
}