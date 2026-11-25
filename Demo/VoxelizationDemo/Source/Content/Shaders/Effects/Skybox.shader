#include "Engine/Shaders/Implicit.hlsl"
#include "../Include/WorldBuffers.hlsl"
#include "../Lighting/LightDataTypes.hlsl"

struct VsInput
{
    float3 Position : Position;
    float3 Normal : Normal;
};

struct PsInput
{
    float4 Position : SV_Position;
    float3 Normal : Texcoord;
};

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbWorldMatricies.ViewProjection, float4(input.Position * cbCameraData.ZFar + cbCameraData.CameraPos.xyz, 1.0)),
        input.Normal
    };

    return output;
}

[property, sampled]
TextureCube<float4> txSky;
SamplerState txSky_Sampler;

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return txSky.Sample(txSky_Sampler, normalize(input.Normal)) * float4(cbDirectionalLight.Diffuse.rgb, 1.0);
}