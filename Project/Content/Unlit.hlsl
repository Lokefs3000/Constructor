#include "Buffers.hlsl"
#include "Converters.hlsl"
#include "Lighting.hlsl"

#pragma path "Standard/Unlit"

struct VsInput
{
    float3 Position : POSITION;
	float3 Normal	: NORMAL;
	float3 Tangent  : TANGENT;
	float3 Bitangent: BITANGENT;
	float2 UV 		: TEXCOORD;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
    float3 FragPos    : POSITION;
    float3 VertexPos  : POSITION2;
	float3 Normal	  : NORMAL;
	float2 UV 		  : TEXCOORD;
};

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float3 fragPos = ConvertObjectToModel(input.Position, instanceId);
    
    PsInput output =
    {
        ConvertModelToWorld(fragPos, instanceId),
        fragPos,
        input.Position,
		NormalObjectToWorld(input.Normal, instanceId),
		input.UV
    };
    
    return output;
}

SamplerState ssSampler : defaultLinear;

[property(Name = "Albedo")]
Texture2D<float4> txAlbedo : register(t0);

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    MatProps props =
    {
        txAlbedo.Sample(ssSampler, input.UV).rgb,
        float3(0.75, 0.75, 0.75),
        12.0
    };

    float3 normal = normalize(input.Normal);
    
    float3 lighting = ComputeAllLights(props, input.FragPos, input.VertexPos, normal);
    return float4(lighting, 1.0);
}