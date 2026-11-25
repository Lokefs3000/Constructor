#include "../Common.hlsl"

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    DefaultPsInput output = {
        mul(cbGlobalData.Model, float4(input.Position, 0.0, 1.0)),
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

float2 GetInwardVector(float2 a, float2 b, float2 center)
{
    float2 vector = center - lerp(a, b, 0.5);
    float angle = atan2(vector.y, vector.x);

    return float2(sin(angle), cos(angle));
}

[pixel]
PsOutput PixelMain(DefaultPsInput input) : SV_Target
{
    TriangleShaderData shaderData = baDataBuffer.Load<TriangleShaderData>(input.DataOffset);
    SharedData sharedData = shaderData.Shared;

    float sdf = sdTriangle(input.FragPos, shaderData.A, shaderData.B, shaderData.C);
    PsOutput output = { float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf))/*, input.Position.z*/ };
    return output;
}