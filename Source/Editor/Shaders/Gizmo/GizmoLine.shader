struct VsInput
{
    float3 Position : POSITION;
    float4 Color    : COLOR;
};

struct PsInput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
};

struct CbGlobalData
{
    float4x4 Model;
};

ConstantBuffer<CbGlobalData> cbGlobals;

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbGlobals.Model, float4(input.Position, 1.0)),
        input.Color
    };
	
    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return input.Color;
}