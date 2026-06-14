#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct RectMetadata
{
    uint16_t HasStroke;
    
    float16_t4 Color;
    uint16_t ZIndex;

    uint16_t2 BoxSize;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    RectMetadata metadata = baMetadata.Load<RectMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), 0.0, 1.0),
        input.UV * 2.0 - metadata.BoxSize,
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    if (input.HasStroke())
    {
        RectMetadata metadata = baMetadata.Load<RectMetadata>(input.GetMetadataOffset());
        StrokeMetadata stroke = baMetadata.Load<StrokeMetadata>(input.GetMetadataOffset() + sizeof(RectMetadata));
        
        float dist = sdBox(input.UV, metadata.BoxSize);
        if (dist + (stroke.Width + stroke.Width) >= 0.0)
            return stroke.Color;
    }

    return input.Color;
}