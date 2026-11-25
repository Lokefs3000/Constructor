#include "../Lighting/LightDataTypes.hlsl"

static const float2 s_ScreenVertices[3] =
{
    float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct ConstantsData
{
    int2 OutputSize;
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : Texcoord;
};

[constants]
ConstantBuffer<ConstantsData> cbConstants;

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

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    uint2 pixelIndex = (uint2)(input.Position.xy / (float2(cbConstants.OutputSize) / float2(16.0, 9.0)));

    pixelIndex.y = 8 - pixelIndex.y;

    uint gridDepthPitch = 16 * 9;
    uint offset = pixelIndex.x + pixelIndex.y * 16;

    uint accum = 0;
    for (int i = 0; i < 24; ++i)
    {
        accum += sbLightGrid[offset + gridDepthPitch * i].Count;
    }

    float t = saturate(accum / 16.0);
    return t < 0.5 ? lerp(float4(0.0, 0.0, 1.0, 1.0), float4(0.0, 1.0, 0.0, 1.0), t * 2.0) : lerp(float4(0.0, 1.0, 0.0, 1.0), float4(1.0, 0.0, 0.0, 1.0), (t - 0.5) * 2.0);
}