#ifndef FP_OBJECTS_HLSL
#define FP_OBJECTS_HLSL

#include "Globals.hlsl"

struct sbRenderFlag
{
    float4x4 Model;
    uint DataId;
};

[global]
StructuredBuffer<sbRenderFlag> sbFP_RenderFlagBuffer;

sbRenderFlag GetRenderFlag(uint index)
{
    return sbFP_RenderFlagBuffer[index];
}

float3 ConvertObjectToModel(float3 objectPosition, uint index)
{
    return mul(sbFP_RenderFlagBuffer[index].Model, float4(objectPosition, 1.0)).xyz;
}

float4 ConvertModelToWorld(float3 modelPosition, uint index)
{
    return mul(sbFP_GlobalMatricies.ViewProjection, float4(modelPosition, 1.0));
}

float4 ConvertObjectToWorld(float3 objectPosition, uint index)
{
    return mul(mul(sbFP_GlobalMatricies.ViewProjection, sbFP_RenderFlagBuffer[index].Model), float4(objectPosition, 1.0));
}

float3 NormalObjectToWorld(float3 normalVector, uint index)
{
    return mul((float3x3)sbFP_RenderFlagBuffer[index].Model, normalVector);
}

#undef __CURR_BINDGROUP
#endif