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

struct CircleShaderData
{
    SharedData Shared;
    float16_t Radius;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    CircleShaderData shaderData = baDataBuffer.Load<CircleShaderData>(input.DataOffset);

    float sdf = sdCircle(input.FragPos, shaderData.Radius);
    return float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf));
}