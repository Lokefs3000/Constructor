#include "Buffers.hlsl"
#include "Converters.hlsl"

#pragma path "Hidden/ShadowPass"

struct cbShadowData
{
    float4x4 LightProjection;
    float4 LightPos_FarPlane;
};

struct VsInput
{
    float3 Position : POSITION;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
    float3 FragPos : POSITION2;

    nointerpolation float3 LightPos : POSITION;
    nointerpolation float FarPlane : FOG;
};

ConstantBuffer<cbShadowData> cbShadow;

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float3 fragPos = ConvertObjectToModel(input.Position, instanceId);
    
    PsInput output =
    {
        mul(cbShadow.LightProjection, float4(fragPos, 1.0)),
        fragPos,
        cbShadow.LightPos_FarPlane.xyz,
        cbShadow.LightPos_FarPlane.w
    };
    
    return output;
}

[pixel]
float PixelMain(PsInput input) : SV_Depth
{
    if (input.FarPlane > 0.0)
    {
        return length(input.FragPos - input.LightPos) / input.FarPlane;
    }

    return input.SVPosition.z;
}