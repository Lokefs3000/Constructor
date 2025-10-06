#pragma path "Hidden/Blit"

static const float2 s_Vertices[3] = {
	float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD;
};

struct cbBlitData
{
    float2 Offset;
    float2 Scale;
};

[constants]
ConstantBuffer<cbBlitData> cbBlit : register(b0);

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexId)
{
    PsInput output =
    {
        float4(s_Vertices[vertexId] * cbBlit.Scale + cbBlit.Offset, 0.0, 1.0),
		0.5 * s_Vertices[vertexId] + 0.5
    };
	
	output.UV = float2(output.UV.x, 1.0 - output.UV.y);

    return output;
}

Texture2D<float4> txTexture : register(t0);
SamplerState ssSampler : defaultLinear;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
	Texture2D<float4> tx = txTexture;
    return tx.Sample(ssSampler, input.UV);
}