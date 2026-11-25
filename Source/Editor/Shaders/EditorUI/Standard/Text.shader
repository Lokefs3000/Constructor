#include "../Common.hlsl"

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    DefaultPsInput output = {
        mul(cbGlobalData.Model, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.UV2,
        input.Tint,

        input.Position,

        input.DataOffset
    };

    return output;
}

struct TextShaderData
{
    SharedData Shared;
    float2 MaxExtents;
}

Texture2D<float4> txFontAtlas;

float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

[pixel]
PsOutput PixelMain(DefaultPsInput input) : SV_Target
{
    TextShaderData shaderData = baDataBuffer.Load<TextShaderData>(input.DataOffset);

    if (any(input.Position.xy > shaderData.MaxExtents))
        discard;

    float4 msd = txFontAtlas.Sample(ssDefaultLinear, input.UV);
    float sd = median(msd.r, msd.g, msd.b);
    float screenPxDistance = max(input.UV2.x, 1.0) * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    PsOutput output = { float4(input.Color.rgb, input.Color.a * opacity)/*, input.Position.z*/ };
    return output;
}