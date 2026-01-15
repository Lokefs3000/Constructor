static const float2 s_Vertices[3] = {
	float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct PsInput
{
    float4 Screen : SV_Position;
    float2 UV : TEXCOORD;
};

struct CbConstants
{
    float Roughness;
    uint FaceIndex;
};

PsInput VertexMain(uint vertexId : SV_VertexId)
{
    PsInput output =
    {
        float4(s_Vertices[vertexId], 0.0, 1.0),
        s_Vertices[vertexId] * 0.5 + 0.5
    };
	
    return output;
}

[constants]
ConstantBuffer<CbConstants> cbConstants : register(b0);

SamplerState ssSampler : defaultLinear;
Texture2DArray<float4> txEnviorment : register(t0);

float4 PixelMain(PsInput input) : SV_Target
{
    return txEnviorment.Sample(ssSampler, float3(input.UV, cbConstants.FaceIndex));
}