#define INCLUDE_RENDERFLAG_CONSTANTS
#define REDIRECT_PROPERTY_SOURCE

#include "Engine/Shaders/Implicit.hlsl"

#include "Include/WorldBuffers.hlsl"
#include "Include/MathOperations.hlsl"

#include "Lighting/LightDataTypes.hlsl"
#include "Lighting/LightComputation.hlsl"

struct VsInput
{
    float3 Position : Position;
    float3 Normal : Normal;
    float4 Tangent : Tangent;
    float2 UV : Texcoord;
};

struct PsInput
{
    float4 Position : SV_Position;
    float3 FragPos : Position;
    
    float3 Normal : Normal;
    float3x3 TBN : Normal1;

    float2 UV : Texcoord;
}

[vertex]
PsInput VertexMain(VsInput input, uint instanceId : SV_InstanceID)
{
    float4x4 model = sbRenderFlags[cbRenderConstants.FlagOffset + instanceId].Model;
    float3 fragPos = mul(model, float4(input.Position, 1.0)).xyz;
    
    float3 n = normalize(mul(AdjugateModelToNormal(model), input.Normal));
    float3 t = input.Tangent.xyz;
    float3 b = normalize(cross(t, n)) * sign(input.Tangent.w);
    float3x3 tbn = float3x3(t, b, n);

    PsInput output = {
        mul(cbWorldMatricies.ViewProjection, float4(fragPos, 1.0)),
        fragPos,

        n,
        tbn,

        float2(input.UV.x, 1.0 - input.UV.y),
    };

    return output;
}

[property("Diffuse")]
Sampler2D(float4, txDiffuse);

[property("Normal", Default = "TexNormal")]
Sampler2D(float4, txNormal);

[property("Mask", Default = "TexMask")]
Sampler2D(float4, txMask);

[property("UV Scale", Default = "NumOne")]
static float2 ppUVScale;

void ComputeAllLightsInCluster(uint clusterIndex, LightComputeVectors vectors, MaterialProperties material, inout float3 diffuse)
{
    LightGrid grid = sbLightGrid[clusterIndex];
    for (uint i = 0; i < grid.Count; ++i)
    {
        uint lightIndex = baLightIndexList.Load((grid.Offset + i) * 4);
        if ((lightIndex & LightType_Point) > 0)
        {
            PointLightData pointLight = sbPointLights[lightIndex & ~LightType_Point];
            EvaluatePointLight(pointLight, vectors, material, diffuse);
        }
    }
}

float3 ReconstructNormal(float4 normal)
{
    float r = normal.a;
    float g = normal.g;
    float b = 0.5 + sqrt(1.0 - pow(2.0 * r - 1.0, 2.0) - pow(2.0 * g - 1.0, 2.0)) * 0.5;

    return normalize(float3(r, g, b) * 2.0 - 1.0);
}

// https://cprimozic.net/notes/posts/a-small-change-to-improve-triplanar-mapping/

float4 TriplanarSample(Texture2D<float4> texture, SamplerState sampler, float3 pos, float3 weights)
{
    float4 sample = 0.0;
    if (weights.x > 0.01)
        sample += texture.Sample(sampler, pos.yz * ppUVScale) * weights.x;
    if (weights.y > 0.01)
        sample += texture.Sample(sampler, pos.zx * ppUVScale) * weights.y;
    if (weights.z > 0.01)
        sample += texture.Sample(sampler, pos.xy * ppUVScale) * weights.z;

    return sample;
}

// https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a

// Reoriented Normal Mapping for Unity3d
// http://discourse.selfshadow.com/t/blending-in-detail/21/18

float3 rnmBlendUnpacked(float3 n1, float3 n2)
{
    n1 += float3( 0,  0, 1);
    n2 *= float3(-1, -1, 1);
    return n1*dot(n1, n2)/n1.z - n2;
}

float3 TriplanarNormalSample(Texture2D<float4> texture, SamplerState sampler, float3 worldNormal, float3 pos, float3 weights)
{
    float3 absNormal = abs(worldNormal);
    float3 axisSign = sign(worldNormal);

    float3 normal = worldNormal;
    if (weights.x > 0.01)
    {
        float3 axisNormal = ReconstructNormal(texture.Sample(sampler, pos.yz * ppUVScale));
        axisNormal = rnmBlendUnpacked(float3(worldNormal.yz, absNormal.x), axisNormal);
        axisNormal.z *= axisSign.x;

        normal += axisNormal.zyx * weights.x;
    }

    if (weights.y > 0.01)
    {
        float3 axisNormal = ReconstructNormal(texture.Sample(sampler, pos.zx * ppUVScale));
        axisNormal = rnmBlendUnpacked(float3(worldNormal.zx, absNormal.y), axisNormal);
        axisNormal.z *= axisSign.y;
        
        normal += axisNormal.xzy * weights.y;
    }

    if (weights.z > 0.01)
    {
        float3 axisNormal = ReconstructNormal(texture.Sample(sampler, pos.xy * ppUVScale));
        axisNormal = rnmBlendUnpacked(float3(worldNormal.xy, absNormal.z), axisNormal);
        axisNormal.z *= axisSign.z;
        
        normal += axisNormal * weights.z;
    }

    return normalize(normal);
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float3 worldNormal = normalize(input.Normal);

    float3 triplanarWeights = abs(worldNormal);
    triplanarWeights = pow(triplanarWeights, 8.0);
    triplanarWeights /= (triplanarWeights.x + triplanarWeights.y + triplanarWeights.z);

    float4 mask = TriplanarSample(txMask, GetSampler(txMask), input.FragPos, triplanarWeights);

    MaterialProperties material;
    material.Diffuse = TriplanarSample(txDiffuse, GetSampler(txDiffuse), input.FragPos, triplanarWeights).rgb;
    material.Specular = 1.0;
    material.Roughness = mask.r;
    material.Metallic = 0.0;

    LightComputeVectors vectors;
    vectors.FragPos = input.FragPos;
    vectors.ViewDir = normalize(cbCameraData.CameraPos.xyz - input.FragPos);
    vectors.F0 = EVALUATE_F0(material.Diffuse, material.Metallic);

    vectors.Normal = TriplanarNormalSample(txNormal, GetSampler(txNormal), worldNormal, input.FragPos, triplanarWeights);
    // vectors.Normal = normalize(mul(vectors.Normal, input.TBN));

    float3 diffuse = material.Diffuse * 0.2;
    uint clusterIndex = GetClusterIndexFor(input.Position.xy, LinearizeDepth(input.Position.z));
    ComputeAllLightsInCluster(clusterIndex, vectors, material, diffuse);
    
    return float4(diffuse, 1.0);
}