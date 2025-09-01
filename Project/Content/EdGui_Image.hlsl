#include "EdGui_Common.hlsl"

#pragma path "Hidden/Editor/EdGui_Image"

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
};

[vertex]
PsInput VertexMain(VsInput input)
{
    ConstantBuffer<cbCanvasData> cb = cbCanvas;

    PsInput output =
    {
        mul(cb.Projection, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.Color
    };

    return output;
}

Texture2D<float4> txAssignedTexture : register(t0);
SamplerState ssFontSampler : defaultLinear;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return txAssignedTexture.Sample(ssFontSampler, input.UV) * input.Color;
}