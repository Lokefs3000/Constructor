struct VsInput
{
    float2 Position : POSITION;
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
    float2 Projection_R0;
    float2 Projection_R1;
    float2 Projection_R2;
};

[constants]
ConstantBuffer<cbVertexData> cbVertex : register(b0);

[vertex]
[ialayout(Name = "Color", Format = "Byte4")]
PsInput VertexMain(VsInput input)
{
    float3x2 projection = float3x2(cbVertex.Projection_R0, cbVertex.Projection_R1, cbVertex.Projection_R2);

    PsInput output =
    {
        float4(mul(transpose(projection), float3(input.Position, 1.0)), 0.0, 1.0),
        input.UV,
		input.Color,
    };

    return output;
}

Texture2D<float4> txTexture;
static SamplerState ssDefaultLinear : defaultLinear;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return txTexture.Sample(ssDefaultLinear, input.UV) * input.Color;
}