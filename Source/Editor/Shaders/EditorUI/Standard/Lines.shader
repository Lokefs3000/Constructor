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

struct PointsShaderData
{
    SharedData Shared;
    float16_t Thickness;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    PointsShaderData shaderData = baDataBuffer.Load<PointsShaderData>(input.DataOffset);

    return input.Color;
}