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
    
    float4x4 model = GetRenderFlag(instanceId).Model;
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

[property("Diffuse")]
Sampler2D(float4, txDiffuse);

[property("Normal", "TexNormal")]
Sampler2D(float4, txNormal);

[property("Mask", "TexMask")]
Sampler2D(float4, txMask);

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float4 color = txDiffuse.Sample(GetSampler(txDiffuse), input.UV);
	return color;
}