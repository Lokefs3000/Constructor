#pragma path "Hidden/Editor/SceneGrid"

static const float2 s_Vertices[6] = {
	float2(1.0, 1.0),
    float2(-1.0, -1.0),
    float2(-1.0, 1.0),

    float2(1.0, 1.0),
    float2(1.0, -1.0),
    float2(-1.0, -1.0)
};

struct PsInput
{
    float4 Position : SV_Position;
    nointerpolation float3 Color : COLOR;
};

struct cbWorld
{
    float4x4 Projection;
    float4x4 View;
};

struct sbBillboard
{
    float3 Position;
    float Size;
    float3 Color;
};

ConstantBuffer<cbWorld> cbVertex : register(b0);

StructuredBuffer<sbBillboard> sbBillboards : register(t0);

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    sbBillboard billboard = sbBillboards[instanceId];

    float2 v = s_Vertices[vertexId] * billboard.Size;
    float3 camSpace = mul(cbVertex.View, float4(billboard.Position, 1.0)).xyz;

    PsInput output =
    {
        mul(cbVertex.Projection, float4(camSpace + float3(v.xy, 0.0), 1.0)),
        billboard.Color
    };

    return output;
}


[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return float4(input.Color, 1.0);
}