struct VsInput
{
    float3 Position : POSITION;
	float2 UV		: TEXCOORD;
    float4 Color    : COLOR;
};

struct PsInput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
	float2 UV		: TEXCOORD;
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
        input.Color,
		input.UV
    };
	
    return output;
}

Texture2D<float4> txFontAtlas;
static SamplerState ssPoint : defaultPoint;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	return float4(txFontAtlas.Sample(ssPoint, input.UV).aaa, 1.0) * input.Color;
}