#pragma path "Hidden/Editor/Gizmos/GZSphereShader"

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
    float2 UV : TEXCOORD;
    nointerpolation float4 Color : COLOR;
    nointerpolation uint IsWire : FOG;
};

struct cbGlobalData
{
    float4x4 View;
    float4x4 Projection;
};

struct sbSphereData
{
    float3 Position;
    float Radius;
    float4 Color;
    uint IsWire;
};

ConstantBuffer<cbGlobalData> cbGlobal : register(b0);
StructuredBuffer<sbSphereData> sbSpheres : register(t0);

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    sbSphereData sphere = sbSpheres[instanceId];

    float2 v = s_Vertices[vertexId] * sphere.Radius;
    float3 camSpace = mul(cbGlobal.View, float4(sphere.Position, 1.0)).xyz;

    PsInput output =
    {
        mul(cbGlobal.Projection, float4(camSpace + float3(v.xy, 0.0), 1.0)),
        s_Vertices[vertexId],
        sphere.Color,
        sphere.IsWire
    };

    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float a = distance(input.UV, float2(0.0, 0.0));
    if (input.IsWire == 1)
    {
        float gradient = sqrt(dot(input.UV, input.UV));
        float width = length(float2(ddx(gradient), ddy(gradient)));
        float centeredGradient = abs(gradient - 1.0);

        a = smoothstep(-0.5, 1.0, 0.5 - centeredGradient / width);
    }
    else
    {
        a = 1.0 - floor(a);
    }

    return float4(input.Color.rgb, input.Color.a * a);
}