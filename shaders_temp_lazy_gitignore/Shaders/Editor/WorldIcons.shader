#include "Engine/Shaders/Implicit.hlsl"
#include "../Include/WorldBuffers.hlsl"

static const float2 s_QuadVertices[6] =
{
    float2(-1.0, 1.0),
    float2(1.0, -1.0),
    float2(-1.0, -1.0),

    float2(-1.0, 1.0),
    float2(1.0, 1.0),
    float2(1.0, -1.0)
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV : Texcoord;
    float4 Color : Color;
};

struct WorldIconData
{
    float4 Position;
    float4 UVMinMax;
    float4 Color;
};

struct RenderOptions
{
    float2 AspectScale;
};

[constants]
ConstantBuffer<RenderOptions> cbOptions;
StructuredBuffer<WorldIconData> sbIcons;

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    WorldIconData iconData = sbIcons[instanceId];
    
    float2 pos = s_QuadVertices[vertexId];
    float4 center = mul(cbWorldMatricies.ViewProjection, float4(iconData.Position.xyz, 1.0));

    float dist = distance(cbCameraData.CameraPos, iconData.Position);

    PsInput output = {
        float4(center.xy + pos * cbOptions.AspectScale * (center.z + (center.z < 3.0 ? (3.0 - center.z) : 0.0)), center.z, center.w),
        float2(pos.x < 0.0 ? iconData.UVMinMax.x : iconData.UVMinMax.z, pos.y < 0.0 ? iconData.UVMinMax.w : iconData.UVMinMax.y),
        iconData.Color
    };

    return output;
}

Texture2D<float4> txIconAtlas;
static SamplerState ssSampler : defaultPoint;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float4 color = txIconAtlas.Sample(ssSampler, input.UV) * input.Color;

    clip(color.a - 0.5);
    return color;
}