#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct LinesMetadata
{
    uint16_t HasStroke;

    float16_t4 Color;
    uint16_t ZIndex;

    float16_t LineWidth;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    LinesMetadata metadata = baMetadata.Load<LinesMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.01, 1.0),
        float2(input.UV.x, metadata.LineWidth),
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    float lineWidth = input.UV.y;
    float dist = lineWidth - abs(input.UV.x - lineWidth * 0.5) * 2.0;

    return float4(input.Color.rgb, input.Color.a * (half)saturate(dist));
}