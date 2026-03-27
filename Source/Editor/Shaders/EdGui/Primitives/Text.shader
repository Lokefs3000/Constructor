#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct TextMetadata
{
    uint16_t HasStroke;
    
    float16_t4 Color;
    uint16_t ZIndex;
};

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    TextMetadata metadata = baMetadata.Load<TextMetadata>(input.MetadataOffset);
    DefaultPsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.01, 1.0),
        input.UV,
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

Texture2D<float2> txFontAtlas;

float Median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

float ScreenPxRange(float param, float2 uv)
{
    uint width;
    uint height;
    txFontAtlas.GetDimensions(width, height);
    
    float2 unitRange = param / float2(width, height);
    float2 screenTexSize = 1.0 / fwidth(uv);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    float2 msd = txFontAtlas.Sample(ssDefaultLinear, input.UV);
    float sd = msd.r;
    float screenPxDistance = ScreenPxRange(2.0, input.UV) * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    //return float4(msd.rgb, 1.0);
    return float4(input.Color.rgb, input.Color.w * opacity);
}