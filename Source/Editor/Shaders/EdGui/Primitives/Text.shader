#include "../Shared.hlsl2"
#include "../SDF.hlsl"

#define TEXT_SUPERSAMPLING true

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
        float4(mul(transpose(cbGlobals.Model), float3(input.Position, 1.0)), metadata.ZIndex * 0.00001, 1.0),
        input.UV,
        metadata.Color,

        input.MetadataOffset | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

Texture2D<float4> txFontAtlas;

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

float SampleFont(float2 uv)
{
    float4 msd = txFontAtlas.Sample(ssDefaultLinear, uv);
    float sd = Median(msd.r, msd.g, msd.b);
    float screenPxDistance = ScreenPxRange(2.0, uv) * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    return opacity;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
#if TEXT_SUPERSAMPLING == true
    // https://www.reddit.com/r/gamedev/comments/2879jd/comment/ci8h5qc/?utm_source=share&utm_medium=web3x&utm_name=web3xcss&utm_term=1&utm_content=share_button

    float center = SampleFont(input.UV);

    float dscale = 0.354;
    float friends = 0.5;

    float2 duv = dscale * (ddx(input.UV) + ddy(input.UV));
    float4 box = float4(input.UV - duv, input.UV + duv);

    float c = SampleFont(box.xy) + SampleFont(box.zw) + SampleFont(box.xw) + SampleFont(box.zy);
    float sum = 4.0;

    float opacity = (center + friends * c) / (1.0 + sum + friends);
    opacity += opacity;
#else
    float opacity = SampleFont(input.UV);
#endif

    return float4(input.Color.rgb, input.Color.w * opacity);
}