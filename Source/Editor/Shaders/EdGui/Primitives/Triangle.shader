#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct TriangleMetadata
{
    uint16_t HasStroke;
    
    float16_t4 Color;
    uint16_t ZIndex;

    float16_t2 A;
    float16_t2 B;
    float16_t2 C;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    TriangleMetadata metadata = baMetadata.Load<TriangleMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.01, 1.0),
        input.UV,
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    TriangleMetadata metadata = baMetadata.Load<TriangleMetadata>(input.GetMetadataOffset());

    float dist = sdTriangle(input.UV, metadata.A, metadata.B, metadata.C);
    if (input.HasStroke())
    {
        StrokeMetadata stroke = baMetadata.Load<StrokeMetadata>(input.GetMetadataOffset() + sizeof(TriangleMetadata));

        if (dist + stroke.Width >= 0.0)
            return float4(stroke.Color.rgb, float(stroke.Color.a) * SmoothSDF(dist));
    }

    return float4(input.Color.rgb, float(input.Color.a) * SmoothSDF(dist));
}