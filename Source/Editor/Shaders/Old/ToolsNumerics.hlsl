#pragma path "Hidden/Editor/ToolsTriangle"

struct VsInput
{
    float3 Position : POSITION;
    float2 UV : TEXCOORD;
    float4 Color : COLOR;
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD;
    float4 Color : COLOR;
};

struct cbVertexData
{
    float4x4 Projection;
};

[constants]
ConstantBuffer<cbVertexData> cbVertex : register(b0);

[vertex]
[ialayout(Name = "COLOR", Format = "Byte4")]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbVertex.Projection, float4(input.Position, 1.0)),
        input.UV,
        input.Color,
    };

    return output;
}

SamplerState ssDefaultPoint : defaultPoint;
Texture2D<float> txNumbers : register(t0);

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return float4(input.Color.b, input.Color.g, input.Color.r, txNumbers.Sample(ssDefaultPoint, input.UV) * input.Color.a);
}