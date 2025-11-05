#ifndef LIGHTING_HLSL
#define LIGHTING_HLSL

#include "Buffers.hlsl"
#include "TextureUtil.hlsl"

#define __CURR_BINDGROUP "__Internal_Lighting"
#pragma bindgroup "__Internal_Lighting"

#pragma variant toggle "__IntLighting_DirLightTg" "Directional light"
#pragma variant toggle "__IntLighting_AddLightsTg" "Additional lights"

#pragma variant enum "__IntLighting_Workflow" "Specular" "Metallic"

#define LightType_Disabled 0
#define LightType_Spot 1
#define LightType_Point 2

#define ShadowIndex_Invalid -1

struct MatProps
{
    float3 Albedo;
    float Roughness;
    float Metallic;
    float AO;
};

struct SharedLightData
{
    float3 Normal;
    float3 ViewDir;
    float3 FragPos;
    float3 F0;
    float ARoughness;
};

struct cbDirectionalLight
{
    float3 Direction;
	
    float3 Diffuse;
    float3 Specular;
};

struct sbRawLight
{
    uint Type;

    uint LightId;
    uint ShadowIndex;

    float3 Position;
    float3 Direction;

    float SpotInnerCone;
    float SpotOuterCone;

    float3 Diffuse;
    float3 Specular;
};

struct sbShadowData
{
    float2 UVMinimum;
    float2 UVSize;

    float4x4 LightProjection;
};

struct sbShadowCubemap
{
    uint Indices[6];
};

static const float4x4 s_BiasMatrix = float4x4(
            0.5, 0.0, 0.0, 0.0,
            0.0, 0.5, 0.0, 0.0,
            0.0, 0.0, 0.5, 0.0,
            0.5, 0.5, 0.5, 1.0);

[bindgroup(Group = __CURR_BINDGROUP)]
ConstantBuffer<cbDirectionalLight> cbDirectional : register(b7);
[bindgroup(Group = __CURR_BINDGROUP)]
StructuredBuffer<sbRawLight> sbLightBuffer : register(t8);

[bindgroup(Group = __CURR_BINDGROUP)]
StructuredBuffer<sbShadowData> sbShadowBuffer : register(t9);
[bindgroup(Group = __CURR_BINDGROUP)]
StructuredBuffer<sbShadowCubemap> sbShadowCubemaps : register(t10);
[bindgroup(Group = __CURR_BINDGROUP)]
Texture2D<float> txShadowAtlas : register(t7);

SamplerState ssShadowSampler : defaultPoint
{
    AddressModeU = "ClampToBorder",
    AddressModeV = "ClampToBorder",
    AddressModeW = "ClampToBorder"
};

#define PI 3.14159265359

float DistributionGGX(float3 N, float3 H, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float3 ImportanceSampleGGX(float2 Xi, float roughness, float N)
{
    
}

float CalculatePointShadowIntensity(sbRawLight lightData, float3 normal, float3 lightDir, float3 lightPos, float3 pos)
{
    if (lightData.ShadowIndex == ShadowIndex_Invalid)
    {
        return 0.0;
    }
    
    float normalBias = saturate(1.0 - dot(normal, lightDir)) * 0.00005;
    pos += normal * normalBias;

    float3 fragToLight = pos - lightPos;

    float faceIndex;
    float2 uvCoords = SampleCubeCoordinates(fragToLight, faceIndex);

    sbShadowData shadowData = sbShadowBuffer[sbShadowCubemaps[lightData.ShadowIndex].Indices[faceIndex]];

    uvCoords = shadowData.UVMinimum + float2(uvCoords.x, 1.0 - uvCoords.y) * shadowData.UVSize;
    float2 larger = shadowData.UVMinimum + shadowData.UVSize;
    if (uvCoords.x < shadowData.UVMinimum.x || uvCoords.y < shadowData.UVMinimum.y || uvCoords.x > larger.x || uvCoords.y > larger.y)
    {
        return 0.0;
    }

    float closestDepth = txShadowAtlas.Sample(ssShadowSampler, uvCoords) * 20.0;
    float currentDepth = length(fragToLight);

    float shadow = currentDepth > closestDepth ? 1.0 : 0.0;
    return 0.0;
}

float CalculateSpotShadowIntensity(sbRawLight lightData, float3 normal, float3 lightDir, float3 pos)
{
    if (lightData.ShadowIndex == ShadowIndex_Invalid)
    {
        return 0.0;
    }

    sbShadowData shadowData = sbShadowBuffer[lightData.ShadowIndex];
    
    float normalBias = saturate(1.0 - dot(normal, lightDir)) * 0.005 + 0.05;
    pos += normal * normalBias;

    float4 fragPosLightSpace = mul(shadowData.LightProjection, float4(pos, 1.0));
    float3 projCoords = (fragPosLightSpace.xyz / fragPosLightSpace.w);

    float2 uvCoords = shadowData.UVMinimum + (float2(projCoords.x, -projCoords.y) * 0.5 + 0.5) * shadowData.UVSize;

    float2 larger = shadowData.UVMinimum + shadowData.UVSize;
    if (uvCoords.x < shadowData.UVMinimum.x || uvCoords.y < shadowData.UVMinimum.y || uvCoords.x > larger.x || uvCoords.y > larger.y)
    {
        return 0.0;
    }

    float closestDepth = txShadowAtlas.Sample(ssShadowSampler, uvCoords);
    float currentDepth = projCoords.z;

    float shadow = currentDepth > closestDepth ? 1.0 : 0.0;
    return shadow;
}

void ComputeDirectionalLight(MatProps mat, SharedLightData sld, inout float3 Lo)
{
    float3 L = cbDirectional.Direction;
    float3 H = normalize(sld.ViewDir + L);

    float3 Bd = cbDirectional.Diffuse / PI * max(dot(sld.Normal, L), 0.0);

    float NDF = DistributionGGX(sld.Normal, H, sld.ARoughness);
    float G = GeometrySmith(sld.Normal, sld.ViewDir, L, sld.ARoughness);
    float3 F = fresnelSchlick(clamp(dot(H, sld.ViewDir), 0.0, 1.0), sld.F0);
    
    float3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(sld.Normal, sld.ViewDir), 0.0) * max(dot(sld.Normal, L), 0.0) + 0.0001;

    Lo += numerator / denominator;
    return;

    //float3 radiance = cbDirectional.Diffuse;
    
    // Cook-Torrance BRDF
    //float NDF = DistributionGGX(sld.Normal, H, mat.Roughness);
    //float G = GeometrySmith(sld.Normal, sld.ViewDir, L, mat.Roughness);
    //float3 F = fresnelSchlick(clamp(dot(H, sld.ViewDir), 0.0, 1.0), sld.F0);
    //
    //float3 numerator = NDF * G * F;
    //float denominator = 4.0 * max(dot(sld.Normal, sld.ViewDir), 0.0) * max(dot(sld.Normal, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
    //float3 specular = numerator / denominator * cbDirectional.Specular.rrr;
    //
    //float3 Ks = F;
    //float3 kD = 1.0 - Ks;
    //kD *= 1.0 - mat.Metallic;
    //
    //float3 NdotL = max(dot(sld.Normal, L), 0.0);
    //Lo += (kD * mat.Albedo / PI + specular) * radiance * NdotL;
}

void ComputePointLight(sbRawLight light, MatProps mat, float3 fragPos, float3 viewDir, float3 normal, out float3 diffuse, out float3 specular)
{
    //float3 lightDir = normalize(light.Position - fragPos);
    //
    //float shadowIntensity = CalculatePointShadowIntensity(light, normal, lightDir, light.Position, fragPos);
    //if (shadowIntensity > 0.5)
    //{
    //    diffuse = 0.0;
    //    specular = 0.0;
    //    return;
    //}
    //
    //float diff = max(dot(normal, lightDir), 0.0);
    //float3 reflectDir = reflect(-lightDir, normal);
    //float3 halfwayDir = normalize(lightDir + viewDir);
    //float spec = pow(max(dot(normal, halfwayDir), 0.0), mat.Shininess);
    //
    //float distance = length(light.Position - fragPos);
    //float attenutation = 1.0 / (1.0 + distance * distance);
    //
    //diffuse = light.Diffuse * (diff * attenutation);
    //specular = light.Specular * (spec * attenutation);
}

void ComputeSpotLight(sbRawLight light, MatProps mat, float3 fragPos, float3 vertexPos, float3 viewDir, float3 normal, out float3 diffuse, out float3 specular)
{
    //float3 lightDir = normalize(light.Position - fragPos);
    //
    //float shadowIntensity = CalculateSpotShadowIntensity(light, normal, lightDir, fragPos);
    //if (shadowIntensity > 0.5)
    //{
    //    diffuse = 0.0;
    //    specular = 0.0;
    //    return;
    //}
    //
    //float diff = max(dot(normal, lightDir), 0.0);
    //float3 reflectDir = reflect(-lightDir, normal);
    //float3 halfwayDir = normalize(lightDir + viewDir);
    //float spec = pow(max(dot(normal, halfwayDir), 0.0), mat.Shininess);
    //
    //float theta = dot(lightDir, light.Direction);
    //float epsilon = light.SpotOuterCone - light.SpotInnerCone;
    //float intensity = clamp((theta - light.SpotOuterCone) / epsilon, 0.0, 1.0);
    //
    //if (intensity == 0.0)
    //{
    //    diffuse = 0.0;
    //    specular = 0.0;
    //    return;
    //}
    //
    //float distance = length(light.Position - fragPos);
    //float attenutation = 1.0 / (1.0 + distance * distance);
    //
    //attenutation *= intensity;
    //
    ///*if (CalculateShadowIntensity(light.ShadowIndex, normal, lightDir, fragPos) > 0.5)
    //{
    //    diffuse = 0.0;
    //    specular = 0.0;
    //    return;
    //}
    //else
    //{
    //    diffuse = 1.0;
    //    specular = 1.0;
    //    return;
    //}*/
    //
    //diffuse = light.Diffuse * (diff * attenutation);
    //specular = light.Specular * (spec * attenutation);
}

float3 ComputeAllLights(MatProps mat, float3 fragPos, float3 vertexPos, float3 normal)
{
    float3 viewDir = normalize(cbWorld.ViewPos - fragPos);
    
    float3 F0 = 0.04;
    F0 = lerp(F0, mat.Albedo, mat.Metallic);

    float3 Lo = mat.Albedo * 0.25;

    SharedLightData sld =
    {
        normal,
        viewDir,
        fragPos,
        F0,
        mat.Roughness * mat.Roughness
    };
    
#ifndef __IntLighting_DirLightTg_False
    if (cbWorld.HasDirLight)
    {
        ComputeDirectionalLight(mat, sld, Lo);
    }
#endif
    
#ifndef __IntLighting_AddLightsTg_False
    //for (uint i = 0; i < cbWorld.RawLightCount; i++)
    //{
    //    sbRawLight rawLight = sbLightBuffer[i];
    //
    //    if (rawLight.Type == LightType_Point)
    //    {
    //        ComputePointLight(rawLight, mat, fragPos, viewDir, normal, diffuse, specular);
    //        color += ComputeLightColor(mat, diffuse, specular);
    //    }
    //    else if (rawLight.Type == LightType_Spot)
    //    {
    //        ComputeSpotLight(rawLight, mat, fragPos, vertexPos, viewDir, normal, diffuse, specular);
    //        color += ComputeLightColor(mat, diffuse, specular);
    //    }
    //}
#endif

    return Lo;
}

#undef __CURR_BINDGROUP

#endif