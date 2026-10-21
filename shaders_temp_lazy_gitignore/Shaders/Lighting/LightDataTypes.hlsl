#ifndef LIGHTDATATYPES_HLSL
#define LIGHTDATATYPES_HLSL

#define LightType_Point (1 << 29)
#define LightType_Spot (1 << 30)

struct PointLightData
{
    float3 Position;
    float Radius;

    float4 Diffuse;
    float4 Specular;
};

struct SpotLightData
{
    float4 Position;
    float4 Direction;

    float16_t InnerCone;
    float16_t OuterCone;

    float4 Diffuse;
    float4 Specular;
};

struct DirectionalLightData
{
    float4 Direction;
    float4 Diffuse;
    uint IsEnabled;
};

struct LightGrid
{
    uint Offset;
    uint Count;
};

struct ScreenToViewLight
{
    float4x4 InverseProjection;
    uint16_t4 TileSizes;
    uint16_t2 ScreenDimensions;
    float Scale;
    float Bias;
};

[global]
StructuredBuffer<PointLightData> sbPointLights;
[global]
ConstantBuffer<DirectionalLightData> cbDirectionalLight;

[global]
ConstantBuffer<ScreenToViewLight> cbScreenToViewLight;

#ifndef EXCLUDE_LIGHT_GRID_DATA
[global]
ByteAddressBuffer baLightIndexList;
[global]
StructuredBuffer<LightGrid> sbLightGrid;
#endif

uint GetClusterIndexFor(float2 screenPos, float linearDepth)
{
    uint clusterZ = (uint)max(log2(linearDepth) * cbScreenToViewLight.Scale + cbScreenToViewLight.Bias, 0.0);
    uint3 tiles = uint3(uint2(screenPos / cbScreenToViewLight.TileSizes.w), clusterZ);
    
    return tiles.x + cbScreenToViewLight.TileSizes.x * (8 - tiles.y) + (cbScreenToViewLight.TileSizes.x * cbScreenToViewLight.TileSizes.y) * tiles.z;
}

#endif