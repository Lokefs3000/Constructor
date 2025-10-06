#include "Buffers.hlsl"

#ifndef CONVERTERS_HLSL
#define CONVERTERS_HLSL

float3 ConvertObjectToModel(float3 objectPosition, uint instanceId)
{
    return mul(sbFlagBuffer[cbObject.MatrixId + instanceId].Model, float4(objectPosition, 1.0)).xyz;
}

float4 ConvertModelToWorld(float3 objectPosition, uint instanceId)
{
    return mul(cbWorld.VP, float4(objectPosition, 1.0));
}

float4 ConvertObjectToWorld(float3 objectPosition, uint instanceId)
{
    return mul(mul(cbWorld.VP, sbFlagBuffer[cbObject.MatrixId + instanceId].Model), float4(objectPosition, 1.0));
}

float3 NormalObjectToWorld(float3 normalVector, uint instanceId)
{
    return mul((float3x3)sbFlagBuffer[cbObject.MatrixId + instanceId].Model, normalVector);
}

float4x4 GetModelMatrix(uint instanceId)
{
    return sbFlagBuffer[cbObject.MatrixId + instanceId].Model;
}

#endif