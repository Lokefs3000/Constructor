#pragma path "Hidden/Editor/ToolsLine"

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

struct cbGlobalData
{
    float4x4 VP;
};

[constants]
ConstantBuffer<cbGlobalData> cbGlobal : register(b0);

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbGlobal.VP, float4(input.Position, 1.0)),
        input.Color,
    };

    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return input.Color;
}