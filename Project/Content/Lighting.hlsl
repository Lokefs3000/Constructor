#ifndef LIGHTING_HLSL
#define LIGHTING_HLSL

#include "Buffers.hlsl"
#include "TextureUtil.hlsl"

#define __CURR_BINDGROUP "__Internal_Lighting"
#pragma bindgroup "__Internal_Lighting"

#pragma variant toggle "__IntLighting_DirLightTg" "Directional light"
#pragma variant toggle "__IntLighting_AddLightsTg" "Additional lights"

#define LightType_Disabled 0
#define LightType_Spot 1
#define LightType_Point 2

#define ShadowIndex_Invalid -1

struct MatProps
{
    float3 Diffuse;
    float3 Specular;
    
    float Shininess;
};

struct cbDirectionalLight
{
    float3 Direction;
	
    float3 Ambient;
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

float CalculatePointShadowIntensity(sbRawLight lightData, float3 normal, float3 lightDir, float3 lightPos, float3 pos)
{
    if (lightData.ShadowIndex == ShadowIndex_Invalid)
    {
        return 0.0;
    }
    
    float normalBias = saturate(1.0 - dot(normal, lightDir)) * 0.05 + 0.00005;
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
    
    float normalBias = saturate(1.0 - dot(normal, lightDir)) * 0.05 + 0.05;
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

void ComputeDirectionalLight(MatProps mat, float3 viewDir, float3 normal, out float3 ambient, out float3 diffuse, out float3 specular)
{
    float diff = max(dot(normal, cbDirectional.Direction), 0.0);
    float3 reflectDir = reflect(-cbDirectional.Diffuse, normal);
    float3 halfwayDir = normalize(cbDirectional.Direction + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), mat.Shininess);
    
    ambient = cbDirectional.Ambient;
    diffuse = cbDirectional.Diffuse * diff;
    specular = cbDirectional.Specular * spec;
}

void ComputePointLight(sbRawLight light, MatProps mat, float3 fragPos, float3 viewDir, float3 normal, out float3 diffuse, out float3 specular)
{
    float3 lightDir = normalize(light.Position - fragPos);

    float shadowIntensity = CalculatePointShadowIntensity(light, normal, lightDir, light.Position, fragPos);
    if (shadowIntensity > 0.5)
    {
        diffuse = 0.0;
        specular = 0.0;
        return;
    }

    float diff = max(dot(normal, lightDir), 0.0);
    float3 reflectDir = reflect(-lightDir, normal);
    float3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), mat.Shininess);
    
    float distance = length(light.Position - fragPos);
    float attenutation = 1.0 / (distance * distance);
    
    diffuse = light.Diffuse * (diff * attenutation);
    specular = light.Specular * (spec * attenutation);
}

void ComputeSpotLight(sbRawLight light, MatProps mat, float3 fragPos, float3 vertexPos, float3 viewDir, float3 normal, out float3 diffuse, out float3 specular)
{
    float3 lightDir = normalize(light.Position - fragPos);

    float shadowIntensity = CalculateSpotShadowIntensity(light, normal, lightDir, fragPos);
    if (shadowIntensity > 0.5)
    {
        diffuse = 0.0;
        specular = 0.0;
        return;
    }

    float diff = max(dot(normal, lightDir), 0.0);
    float3 reflectDir = reflect(-lightDir, normal);
    float3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), mat.Shininess);
    
    float theta = dot(lightDir, light.Direction);
    float epsilon = light.SpotOuterCone - light.SpotInnerCone;
    float intensity = clamp((theta - light.SpotOuterCone) / epsilon, 0.0, 1.0);
    
    if (intensity == 0.0)
    {
        diffuse = 0.0;
        specular = 0.0;
        return;
    }

    float distance = length(light.Position - fragPos);
    float attenutation = 1.0 / (distance * distance);

    attenutation *= intensity;

    /*if (CalculateShadowIntensity(light.ShadowIndex, normal, lightDir, fragPos) > 0.5)
    {
        diffuse = 0.0;
        specular = 0.0;
        return;
    }
    else
    {
        diffuse = 1.0;
        specular = 1.0;
        return;
    }*/

    diffuse = light.Diffuse * (diff * attenutation);
    specular = light.Specular * (spec * attenutation);
}

float3 ComputeLightColor(MatProps mat, float3 ambient, float3 diffuse, float3 specular)
{
    return mat.Diffuse * ambient + mat.Diffuse * diffuse + mat.Specular * specular;
}

float3 ComputeAllLights(MatProps mat, float3 fragPos, float3 vertexPos, float3 normal)
{
    float3 ambient, diffuse, specular;
    float3 color = mat.Diffuse * 0.05;

    float3 viewDir = normalize( cbWorld.ViewPos - fragPos);
    
#ifndef __IntLighting_DirLightTg_False
    if (cbWorld.HasDirLight)
    {
        ComputeDirectionalLight(mat, viewDir, normal, ambient, diffuse, specular);
        color = ComputeLightColor(mat, ambient, diffuse, specular);
    }
#endif
    
#ifndef __IntLighting_AddLightsTg_False
    for (uint i = 0; i < cbWorld.RawLightCount; i++)
    {
        sbRawLight rawLight = sbLightBuffer[i];

        if (rawLight.Type == LightType_Point)
        {
            ComputePointLight(rawLight, mat, fragPos, viewDir, normal, diffuse, specular);
            color += ComputeLightColor(mat, 0.0, diffuse, specular);
        }
        else if (rawLight.Type == LightType_Spot)
        {
            ComputeSpotLight(rawLight, mat, fragPos, vertexPos, viewDir, normal, diffuse, specular);
            color += ComputeLightColor(mat, 0.0, diffuse, specular);
        }
    }
#endif

    return color;
}

#undef __CURR_BINDGROUP

#endif