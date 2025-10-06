#pragma path "Hidden/Editor/ToolsTriangle"

struct VsInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct PsInput
{
    float4 Position : SV_Position;
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
        input.Color,
    };

    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return float4(input.Color.b, input.Color.g, input.Color.r, input.Color.a);
}