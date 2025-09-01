#pragma path "Hidden/ShadowBlit"

static const float2 s_Vertices[3] = {
	float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD;
    float2 Clip : TEXCOORD2;
};

struct cbBlitData
{
    float2 Offset;
    float2 Scale;

    float NearClip;
    float FarClip;
};

[constants]
ConstantBuffer<cbBlitData> cbBlit : register(b0);

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexId)
{
    PsInput output =
    {
        float4(s_Vertices[vertexId] * cbBlit.Scale + cbBlit.Offset, 0.0, 1.0),
		0.5 * s_Vertices[vertexId] + 0.5,
        float2(cbBlit.NearClip, cbBlit.FarClip)
    };
	
	output.UV = float2(output.UV.x, 1.0 - output.UV.y);

    return output;
}

Texture2D<float> txTexture : register(t0);
SamplerState ssSampler : defaultLinear;

float LinearizeDepth(float depth, float near, float far)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float linearDepth = LinearizeDepth(txTexture.Sample(ssSampler, input.UV), input.Clip.x, input.Clip.y) / input.Clip.y;
    return float4(linearDepth.rrr, 1.0);
}