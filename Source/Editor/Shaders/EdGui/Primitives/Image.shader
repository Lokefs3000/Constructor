#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct ImageMetadata
{
    uint16_t HasStroke;
    
    float16_t4 Color;
    uint16_t ZIndex;

    uint16_t2 BoxSize;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    ImageMetadata metadata = baMetadata.Load<ImageMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.00001, 1.0),
        input.UV,
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[property]
Sampler2D(float4, txImage);

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    if (input.HasStroke())
    {
        ImageMetadata metadata = baMetadata.Load<ImageMetadata>(input.GetMetadataOffset());
        StrokeMetadata stroke = baMetadata.Load<StrokeMetadata>(input.GetMetadataOffset() + sizeof(ImageMetadata));
        
        float dist = sdBox(input.UV * (metadata.BoxSize + metadata.BoxSize) - metadata.BoxSize, metadata.BoxSize);
        if (dist + (stroke.Width + stroke.Width) >= 0.0)
            return stroke.Color;
            
        float2 div = float(stroke.Width) / float2(metadata.BoxSize);
        input.UV *= div * 2.0 + 1.0;
        input.UV -= div;
    }

    return txImage.Sample(GetSampler(txImage), float2(input.UV.x, input.UV.y)) * input.Color;
}