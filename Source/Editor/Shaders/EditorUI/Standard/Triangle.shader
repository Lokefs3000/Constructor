#include "../Common.hlsl"

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    DefaultPsInput output = {
        float4(mul(transpose(cbGlobalData.Model), float3(input.Position, 1.0)), input.Depth * 0.0001, 1.0),
        input.UV,
        input.UV2,
        input.Tint,

        input.Position,

        input.DataOffset
    };

    return output;
}

struct TriangleShaderData
{
    SharedData Shared;
    float2 A;
    float2 B;
    float2 C;
    float16_t CornerRadius;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    TriangleShaderData shaderData = baDataBuffer.Load<TriangleShaderData>(input.DataOffset);

    if (shaderData.CornerRadius < 0.0f)
        return input.Color;

    float sdf = opRound(sdTriangle(input.FragPos, shaderData.A, shaderData.B, shaderData.C), shaderData.CornerRadius);
    return float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf));
}