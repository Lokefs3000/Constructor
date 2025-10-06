#ifndef BUFFERS_HLSL
#define BUFFERS_HLSL

#define __CURR_BINDGROUP "__Internal_Buffers"
#pragma bindgroup "__Internal_Buffers"

struct cbWorldData
{
    float4x4 VP;
    
    float3 ViewPos;

    uint HasDirLight;
    uint RawLightCount;
};

struct cbObjectData
{
	uint MatrixId;
};

struct sbFlagData
{
	float4x4 Model;
	uint MaterialId;
};

[bindgroup(Group = __CURR_BINDGROUP)]
ConstantBuffer<cbWorldData> cbWorld : register(b5);
[bindgroup(Group = __CURR_BINDGROUP)]
ConstantBuffer<cbObjectData> cbObject : register(b6);

[bindgroup(Group = __CURR_BINDGROUP)]
StructuredBuffer<sbFlagData> sbFlagBuffer : register(t7);

#undef __CURR_BINDGROUP

#endif