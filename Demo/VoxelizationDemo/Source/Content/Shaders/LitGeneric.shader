// #pragma variant_toggle("Light_HasDirLight")

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
    
    float3x3 adj = AdjugateModelToNormal(model);

    float3 n = normalize(mul(adj, input.Normal));
    float3 t = normalize(input.Tangent.xyz);
    float3 b = normalize(cross(t, n) * sign(input.Tangent.w));
    float3x3 tbn = float3x3(t, b, n);

    PsInput output = {
        mul(cbWorldMatricies.ViewProjection, float4(fragPos, 1.0)),
        fragPos,

        input.Normal,
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

[property("Diffuse Tint", Default = "NumOne")]
static float4 ppDiffuseTint;

[property("Roughness Scale", Default = "NumOne")]
static float ppRoughnessMultiplier;

[property("Metallic Scale", Default = "NumZero")]
static float ppMetallicMultiplier;

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

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float4 mask = txMask.Sample(GetSampler(txMask), input.UV);
	float4 diffuseAlpha = txDiffuse.Sample(GetSampler(txDiffuse), input.UV) * ppDiffuseTint;

    MaterialProperties material;
    material.Diffuse = diffuseAlpha.rgb;
    material.Specular = 1.0;
    material.Roughness = mask.r * ppRoughnessMultiplier;
    material.Metallic = ppMetallicMultiplier;

#ifdef CLIP_ALPHA
	clip(diffuseAlpha.a - 0.5);
#endif

    LightComputeVectors vectors;
    vectors.FragPos = input.FragPos;
    vectors.ViewDir = normalize(cbCameraData.CameraPos.xyz - input.FragPos);
    vectors.F0 = EVALUATE_F0(material.Diffuse, material.Metallic);

    vectors.Normal = ReconstructNormal(txNormal.Sample(GetSampler(txNormal), input.UV));
    vectors.Normal = normalize(mul(vectors.Normal, input.TBN));

    float3 sh[9];
    sh[0] = float3(3.6494102, 3.7915745, 3.6046827);
    sh[1] = float3(3.892, 3.873, 3.418);
    sh[2] = float3(-0.338, -0.455, -0.570);
    sh[3] = float3(-3.323, -2.962, -2.120);
    sh[4] = float3(-5.245, -4.680, -3.345);
    sh[5] = float3(-0.613, -0.828, -0.952);
    sh[6] = float3(-2.782, -2.310, -1.313);
    sh[7] = float3(0.0, 0.0, 0.0);
    sh[8] = float3(-0.748, -1.113, -1.568);

    float3 diffuse = GetSHIrradiance(vectors.Normal, sh) * 0.03;
    if (cbDirectionalLight.IsEnabled > 0)
    {
        EvaluateDirectionalLight(cbDirectionalLight, vectors, material, diffuse);
    }

    uint clusterIndex = GetClusterIndexFor(input.Position.xy, LinearizeDepth(input.Position.z));
    ComputeAllLightsInCluster(clusterIndex, vectors, material, diffuse);
    
    return float4(diffuse, 1.0);
}