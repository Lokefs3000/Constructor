#pragma path "Hidden/Editor/DearImGui"

struct VsInput
{
    float2 Position : POSITION;
    float2 UV : TEXCOORD;
    float4 Color : COLOR;
};

struct PsInput
{
    float4 Position : SV_Position;
    float4 Color : COLOR;
    float2 UV : TEXCOORD;
};

struct cbVertexData
{
    float4x4 Projection;
};

ConstantBuffer<cbVertexData> cbVertex : register(b0);

[vertex]
[ialayout(Name = "COLOR", Format = "Byte4")]
PsInput VertexMain(VsInput input)
{
    ConstantBuffer<cbVertexData> cb = cbVertex;

    PsInput output =
    {
        mul(cb.Projection, float4(input.Position, 0.0, 1.0)),
        input.Color,
        input.UV
    };

    return output;
}

Texture2D<float4> txTexture : register(t0);
SamplerState ssSampler : defaultLinear;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	Texture2D<float4> tx = txTexture;
    return input.Color * tx.Sample(ssSampler, input.UV);
}