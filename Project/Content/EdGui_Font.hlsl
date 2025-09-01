#include "EdGui_Common.hlsl"

#pragma path "Hidden/Editor/EdGui_Font"

struct VsInput
{
    float2 Position : POSITION;
    float2 UV : TEXCOORD;
    float4 Color : COLOR;
    float Metadata : FOG;
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD;
    float4 Color : COLOR;
    float Metadata : FOG;
};

[vertex]
PsInput VertexMain(VsInput input)
{
    ConstantBuffer<cbCanvasData> cb = cbCanvas;

    PsInput output =
    {
        mul(cb.Projection, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.Color,
        input.Metadata
    };

    return output;
}

Texture2D<float4> txAssignedTexture : register(t0);
SamplerState ssFontSampler : defaultLinear;

float FontMedian(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float4 msd = txAssignedTexture.Sample(ssFontSampler, input.UV);
    float sd = FontMedian(msd.r, msd.g, msd.b);
    float screenPxDistance = input.Metadata * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    return float4(input.Color.rgb, input.Color.a * opacity);
}