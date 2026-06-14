#include "Engine/Shaders/Implicit.hlsl"
#include "Globals.hlsl"
#include "Objects.hlsl"

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
    float3 Normal : NORMAL;
};

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float3 fragPos = ConvertObjectToModel(input.Position, instanceId);
    
    PsInput output =
    {
        ConvertModelToWorld(fragPos, instanceId),
        NormalObjectToWorld(input.Normal, instanceId)
    };
    
    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float2 position = floor(input.SVPosition.xy / 32.0);
    float checker = floor(position.x) + floor(position.y);
    checker = (frac(checker * 0.5) + 0.5) * 0.2 + 0.8;

    checker *= (dot(input.Normal, float3(0.0, 0.0, -1.0)) * 0.5 + 0.5);

    return float4(0.0, checker, checker, 1.0);
}