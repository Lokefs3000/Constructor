#include "../Shared.hlsl2"

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    DefaultPsInput output =
    {
        mul(cbGlobals.Model, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.Color,

        input.MetadataOffset
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

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    [branch]
    if (input.Color.a < 0.0)
    {
        input.Color = txGradients.Sample(ssDefaultLinear, input.Color.xy);
    }
    
    return float4(input.Color.rgb, smoothstep(0.0, 1.0, input.Color.a * txFontAtlas.Sample(ssDefaultLinear, input.UV).a));

    const float DistanceRange = 2.0;

    float4 msdf = txFontAtlas.Sample(ssDefaultLinear, input.UV);
    float sd = Median(msdf.r, msdf.g, msdf.b);
    float screenPxDistance = ScreenPxRange(DistanceRange, input.UV) * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    return float4(input.Color.rgb, input.Color.a * opacity);
}