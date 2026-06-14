struct VsInput
{
    float2 Position : POSITION;
    float4 Color    : COLOR;
};

struct PsInput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
};

struct CbGlobalData
{
    float2 Projection_R0;
    float2 Projection_R1;
    float2 Projection_R2;
};

ConstantBuffer<CbGlobalData> cbGlobals;

[vertex]
PsInput VertexMain(VsInput input)
{
    float3x2 proj = float3x2(cbGlobals.Projection_R0, cbGlobals.Projection_R1, cbGlobals.Projection_R2);

    PsInput output =
    {
        float4(mul(transpose(proj), float3(input.Position, 1.0)), 0.0, 1.0),
        input.Color
    };
	
    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return input.Color;
}