#ifndef FP_OBJECTS_HLSL
#define FP_OBJECTS_HLSL

#pragma property_source "baFP_RawDataBuffer.Load<$GENERIC$>(GetRenderFlag($SV_INSTANCEID$).DataId)"
#pragma gen_properties_in_header false

#include "Globals.hlsl"

struct sbRenderFlag
{
    float4x4 Model;
    uint DataId;
};

struct cbDynamicData
{
    uint InstanceOffset;
};

[global]
StructuredBuffer<sbRenderFlag> sbFP_RenderFlagBuffer;

[global]
ByteAddressBuffer baFP_RawDataBuffer;

[global]
ConstantBuffer<cbDynamicData> cbFP_DynamicData;

sbRenderFlag GetRenderFlag(uint index)
{
    return sbFP_RenderFlagBuffer[index];
}

uint GetInstanceOffset()
{
    return cbFP_DynamicData.InstanceOffset;
}

float3 ConvertObjectToModel(float3 objectPosition, uint index)
{
    return mul(sbFP_RenderFlagBuffer[index].Model, float4(objectPosition, 1.0)).xyz;
}

float4 ConvertModelToWorld(float3 modelPosition, uint index)
{
    return mul(cbFP_GlobalMatricies.ViewProjection, float4(modelPosition, 1.0));
}

float4 ConvertObjectToWorld(float3 objectPosition, uint index)
{
    return mul(mul(cbFP_GlobalMatricies.ViewProjection, sbFP_RenderFlagBuffer[index].Model), float4(objectPosition, 1.0));
}

float3 NormalObjectToWorld(float3 normalVector, uint index)
{
    return mul((float3x3)sbFP_RenderFlagBuffer[index].Model, normalVector);
}

#undef __CURR_BINDGROUP
#endif