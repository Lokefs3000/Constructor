#include "Engine/Shaders/Implicit.hlsl"
#include "../../Include/WorldBuffers.hlsl"

struct VsInput
{
    float3 Position : Position;
    float4 Color : Color;
};

struct PsInput
{
    float4 Position : SV_Position;
    float4 Color : Color;
};

struct TransformationData
{
    float4x4 Model;
};

[constants]
ConstantBuffer<TransformationData> cbTransformation;

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output = {
        mul(mul(cbWorldMatricies.ViewProjection, cbTransformation.Model), float4(input.Position, 1.0)),
        input.Color
    };

    return output;
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    return input.Color;
}