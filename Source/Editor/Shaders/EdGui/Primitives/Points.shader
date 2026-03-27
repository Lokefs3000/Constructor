#include "../Shared.hlsl2"
#include "../SDF.hlsl"

struct PointsMetadata
{
    uint16_t HasStroke;

    float16_t4 Color;
    uint16_t ZIndex;
};

struct PsInput
{
    float4 Position : SV_Position;
    float16_t4 Color : Color;

    nointerpolation uint MetadataOffset : Texcoord1;
};

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexId, uint instanceId : SV_InstanceId)
{
    PointsMetadata metadata = baMetadata.Load<PointsMetadata>(0);
    PsInput output =
    {
        float4(mul(transpose(cbGlobals.Model), float3(s_RectTLVertices[vertexId] + baMetadata.Load<float2>(0 + sizeof(PointsMetadata) + instanceId * 8), 1.0)), metadata.ZIndex * 0.01, 1.0),
        metadata.Color,

        0 | (uint(metadata.HasStroke) << 31)
    };
    
    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return input.Color;
}