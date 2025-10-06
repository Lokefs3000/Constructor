#include "Buffers.hlsl"
#include "Converters.hlsl"
#include "Lighting.hlsl"

#pragma path "Standard/Unlit"

struct VsInput
{
    float3 Position : POSITION;
	float2 UV 		: TEXCOORD;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
	float2 UV 		  : TEXCOORD;
};

[vertex]
[ialayout(Name = "TEXCOORD", Offset = 48)]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float3 fragPos = ConvertObjectToModel(input.Position, instanceId);

    PsInput output =
    {
        ConvertModelToWorld(fragPos, instanceId),
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
    return float4(txAlbedo.Sample(ssSampler, input.UV).rgb, 1.0);
}