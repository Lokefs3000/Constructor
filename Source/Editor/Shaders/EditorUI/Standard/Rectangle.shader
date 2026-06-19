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

struct RectangleShaderData
{
    SharedData Shared;
    uint16_t2 BoxSize;
    float16_t4 CornerRadii;
}

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    RectangleShaderData shaderData = baDataBuffer.Load<RectangleShaderData>(0);
    return float4(baDataBuffer.Load(9) == 0, 0, 0, 1);

    if (shaderData.CornerRadii.x <= 0.0f)
        return input.Color;

    float sdf = sdRoundedBox(input.FragPos, shaderData.BoxSize, shaderData.CornerRadii);
    return float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf));
}