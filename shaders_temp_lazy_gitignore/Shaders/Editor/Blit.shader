#include "Engine/Shaders/Implicit.hlsl"

static const float2 s_ScreenVertices[3] =
{
    float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : Texcoord;
};

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexID)
{
    float2 pos = s_ScreenVertices[vertexId];

    PsInput output = {
        float4(pos, 0.0, 1.0),
        float2(pos.x, pos.y) * 0.5 + 0.5
    };

    output.UV.y = 1.0 - output.UV.y;
    return output;
}

[property]
Sampler2D(float4, txTexture);

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return txTexture.Sample(GetSampler(txTexture), input.UV);
}