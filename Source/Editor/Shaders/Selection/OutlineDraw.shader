#include "Engine/Shaders/Implicit.hlsl"

struct VsInput
{
    float3 Position : POSITION;
	float3 Normal	: NORMAL;
	float3 Tangent  : TANGENT;
	float3 Bitangent: TANGENT2;
	float2 UV 		: TEXCOORD;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
};

struct ObjectMatrix
{
    float4x4 MVP;
};

struct sbMatrixIndex
{
    uint Index;
};

[constants]
ConstantBuffer<sbMatrixIndex> sbMatrixIndex;
StructuredBuffer<ObjectMatrix> sbMatricies;

[vertex]
PsInput VertexMain(VsInput input)
{
    float4x4 mvp = sbMatricies[sbMatrixIndex.Index].MVP;

    PsInput output =
    {
        mul(mvp, float4(input.Position, 1.0)),
    };
    
    return output;
}

[pixel]
float4 PixelMain() : SV_Target
{
    return 0.0;
}