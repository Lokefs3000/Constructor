#include "Buffers.hlsl"
#include "Converters.hlsl"
#include "Lighting.hlsl"

#pragma path "Standard/Lit"

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

    float3x3 TBN : FOG;
};

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float3 fragPos = ConvertObjectToModel(input.Position, instanceId);
    
    float4x4 model = GetModelMatrix(instanceId);
    float3 t = normalize(mul(model, float4(input.Tangent, 0.0)).xyz);
    float3 b = normalize(mul(model, float4(input.Bitangent, 0.0)).xyz);
    float3 n = normalize(mul(model, float4(input.Normal, 0.0)).xyz);

    PsInput output =
    {
        ConvertModelToWorld(fragPos, instanceId),
        fragPos,
        input.Position,
		NormalObjectToWorld(input.Normal, instanceId),
		input.UV,

        transpose(float3x3(t, b, n))
    };
    
    return output;
}

SamplerState ssSampler : defaultLinear;

[property(Name = "Albedo")]
Texture2D<float4> txAlbedo : register(t0);

[property(Name = "Specular")]
Texture2D<float4> txSpecular : register(t1);

[property(Name = "Normal", Default = "Normal")]
Texture2D<float4> txNormal : register(t2);

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float4 spec = txSpecular.Sample(ssSampler, input.UV);

    MatProps props =
    {
        txAlbedo.Sample(ssSampler, input.UV).rgb,
        spec.rgb,
        spec.a * 8.0
    };

    float4 normalXY = txNormal.Sample(ssSampler, input.UV);
    float2 dxAG = float2(normalXY.a, normalXY.g);

    float3 sampledNormal = float3(dxAG * 2.0 - 1.0, sqrt(1 - (dxAG.r * dxAG.r) - (dxAG.g * dxAG.g)));
    sampledNormal = mul(input.TBN, sampledNormal);

    float3 lighting = ComputeAllLights(props, input.FragPos, input.VertexPos, sampledNormal);
    return float4(lighting, 1.0);
}