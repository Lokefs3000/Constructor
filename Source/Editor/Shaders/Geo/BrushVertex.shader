#include "Engine/Shaders/Implicit.hlsl"
#include "Engine/Shaders/ForwardPlus/Globals.hlsl"

static const float2 s_Vertices[6] = {
    float2(-1.0, 1.0),
    float2(1.0, -1.0),
    float2(-1.0, -1.0),

    float2(-1.0, 1.0),
    float2(1.0, -1.0),
    float2(1.0, 1.0),
};

struct PsInput
{
    float4 SVPosition   : SV_Position;
    float2 UV           : Texcoord;
    uint IsActive       : Texcoord1;
};

struct VertexWorldInstanceData
{
    float3 Position;
    uint IsActive;
};

StructuredBuffer<VertexWorldInstanceData> sbWorldData;

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexId, uint instanceId : SV_InstanceId)
{
    VertexWorldInstanceData instanceData = sbWorldData[instanceId];

    float4 v = mul(cbFP_GlobalMatricies.View, float4(instanceData.Position, 1.0));
    v.xy += s_Vertices[vertexId] * 0.075;

    PsInput output =
    {
        mul(cbFP_GlobalMatricies.Projection, v),
        s_Vertices[vertexId],
        instanceData.IsActive
    };
    
    return output;
}

Texture2D<float> txDepth;
SamplerState ssLinear : defaultLinear;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    uint width, height;
    txDepth.GetDimensions(width, height);

    float2 screen = input.SVPosition.xy / float2(width, height);

    float depth = input.SVPosition.z;
    float writtenDepth = txDepth.Sample(ssLinear, screen);

    float stripe = frac((input.SVPosition.x + input.SVPosition.y) * 0.05) > 0.5;
    float4 color = input.IsActive ? float4(0.85, 0.85, 0.0, 1.0) : float4(0.5, 0.5, 0.5, 0.5);

    return float4(color.rgb, color.a * (depth >= writtenDepth ? (stripe * 0.5 + 0.5) : 1.0));
}