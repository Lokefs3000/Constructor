#ifndef LIGHTCOMPUTATION
#define LIGHTCOMPUTATION

#include "../Include/WorldBuffers.hlsl"
#include "../BRDF/CookTorrance.hlsl"

#include "LightDataTypes.hlsl"

struct LightComputeVectors
{
    float3 FragPos;
    float3 Normal;
    float3 ViewDir;
    float3 F0;
};

struct MaterialProperties
{
    float3 Diffuse;
    float3 Specular;
    float Roughness;
    float Metallic;
};

#define EVALUATE_F0(baseColor, metallic) lerp(float3(0.04, 0.04, 0.04), baseColor, metallic)

// https://lisyarus.github.io/blog/posts/point-light-attenuation.html
#ifndef SQUARE_HELPER
#define SQUARE_HELPER
float Square(float x)
{
    return x * x;
}
#endif

float AttenuateNoCusp(float dist, float radius, float maxIntensity, float falloff)
{
    float s = dist / radius;

    if (s >= 1.0)
    {
        return 0.0;
    }

    float s2 = Square(s);
    return maxIntensity * Square(1.0 - s2) / (1.0 + falloff * s2);
}

float AttenuateCusp(float dist, float radius, float maxIntensity, float falloff)
{
    float s = dist / radius;

    if (s >= 1.0)
    {
        return 0.0;
    }

    float s2 = Square(s);
    return maxIntensity * Square(1.0 - s2) / (1.0 + falloff * s);
}

float3 GetSHIrradiance(float3 n, float3 sh[9])
{
    const float a0 = 3.141593;
    const float a1 = 2.094395;
    const float a2 = 0.785398;

    float3 irradiance = 
        sh[0] * 0.282095 * a0 +

        sh[1] * 0.488603 * n.y * a1 +
        sh[2] * 0.488603 * n.z * a1 +
        sh[3] * 0.488603 * n.x * a1 +

        sh[4] * 1.092548 * (n.x * n.y) * a2 +
        sh[5] * 1.092548 * (n.y * n.z) * a2 +
        sh[6] * 0.315392 * (3.0 * n.z * n.z - 1.0) +
        sh[7] * 1.092548 * (n.x * n.z) * a2 +
        sh[8] * 0.546274 * (n.x * n.x - n.y * n.y) * a2;

    return max(irradiance, 0.0);
}

// The magic number used is 1.0 / 256.0 = 0.00390625

void EvaluateDirectionalLight(DirectionalLightData directionalLight, LightComputeVectors vectors, MaterialProperties material, inout float3 diffuse)
{
    float3 lightDir = normalize(-directionalLight.Direction.xyz);
    float3 radiance = directionalLight.Diffuse.rgb * directionalLight.Direction.a;

    float3 H = normalize(vectors.ViewDir + lightDir);
    
    float NdotL = max(dot(vectors.Normal, lightDir), 0.00390625);
    float NdotV = max(dot(vectors.Normal, vectors.ViewDir), 0.00390625);
    float NdotH = max(dot(vectors.Normal, H), 0.0);
    float HdotV = max(dot(H, vectors.ViewDir), 0.0);
    
    float D = DistributionGGX(NdotH, material.Roughness);
    float G = GeometrySmith(NdotV, NdotL, material.Roughness);
    float3 F = FresnelShlick(HdotV, vectors.F0);
    
    float3 numerator = D * G * F;
    float denominator = 4.0 * NdotV * NdotL;
    float3 specular = numerator / denominator;
    
    float3 kS = F;
    float3 kD = 1.0 - kS;
    kD *= 1.0 - material.Metallic;
    
    diffuse += (kD * material.Diffuse / PI + specular) * radiance * NdotL;
}

void EvaluatePointLight(PointLightData pointLight, LightComputeVectors vectors, MaterialProperties material, inout float3 diffuse)
{
    float3 lightDir = pointLight.Position - vectors.FragPos;

    float distance = length(pointLight.Position - vectors.FragPos);
    if (WaveActiveAnyTrue(distance <= pointLight.Radius))
    {
        float attenuation = AttenuateCusp(distance, pointLight.Radius, pointLight.Diffuse.a, pointLight.Specular.a);

        lightDir = normalize(lightDir);
        float3 radiance = pointLight.Diffuse.rgb * attenuation;
        
        float3 H = normalize(vectors.ViewDir + lightDir);
        
        float NdotL = max(dot(vectors.Normal, lightDir), 0.00390625);
        float NdotV = max(dot(vectors.Normal, vectors.ViewDir), 0.00390625);
        float NdotH = max(dot(vectors.Normal, H), 0.0);
        float HdotV = max(dot(H, vectors.ViewDir), 0.0);
        
        float D = DistributionGGX(NdotH, material.Roughness);
        float G = GeometrySmith(NdotV, NdotL, material.Roughness);
        float3 F = FresnelShlick(HdotV, vectors.F0);
        
        float3 numerator = D * G * F;
        float denominator = 4.0 * NdotV * NdotL;
        float3 specular = numerator / denominator;
        
        float3 kS = F;
        float3 kD = 1.0 - kS;
        kD *= 1.0 - material.Metallic;
        
        diffuse += (kD * material.Diffuse / PI + specular) * radiance * NdotL;
    }
}

#endif