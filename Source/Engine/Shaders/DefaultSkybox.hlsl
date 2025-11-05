#pragma path "Standard/Unlit"

static const float3 s_Vertices[8] = {
    float3(-1, -1, 1),
    float3(-1,  1, 1),
    float3(-1, -1, -1),
    float3(-1,  1, -1),
    float3(1,  -1, 1),
    float3(1,   1, 1),
    float3(1,  -1, -1),
    float3(1,   1, -1),
};

static const uint s_Indices[36] = {
    2, 1, 0,
    6, 3, 2,
    4, 7, 6,
    0, 5, 4,
    0, 6, 2,
    5, 3, 7,
    2, 3, 1,
    6, 7, 3,
    4, 5, 7,
    0, 1, 5,
    0, 4, 6,
    5, 1, 3,
};

struct PsInput
{
    float4 SVPosition : SV_Position;
	float3 Position : POSITION;
};

struct cbModel
{
    float4x4 Model;
};

[constants]
ConstantBuffer<cbModel> cbModelData : register(b0);

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexID)
{
    float3 pos = s_Vertices[s_Indices[vertexId]];

    PsInput output =
    {
        mul(cbModelData.Model, float4(pos, 1.0)),
		pos
    };
    
    return output;
}

SamplerState ssSampler : defaultLinear;
TextureCube txSkybox : register(t0);

struct PsOutput
{
    float4 Target : SV_Target;
    float Depth : SV_Depth;
};

[pixel]
PsOutput PixelMain(PsInput input)
{
    PsOutput output = {
        float4(txSkybox.Sample(ssSampler, input.Position).rgb, 1.0),
        1.0
    };

    return output;
}