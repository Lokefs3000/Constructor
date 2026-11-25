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

struct LinesShaderData
{
    SharedData Shared;
    float16_t Thickness;
}

[pixel]
PsOutput PixelMain(DefaultPsInput input) : SV_Target
{
    LinesShaderData shaderData = baDataBuffer.Load<LinesShaderData>(input.DataOffset);
    SharedData sharedData = shaderData.Shared;

    if (sharedData.StrokeWidth > 0)
    {
        float2 forward = normalize(input.UV2 - input.UV) * sharedData.StrokeWidth;

        float outer = sdOrientedBox(input.FragPos, input.UV, input.UV2, shaderData.Thickness);
        float inner = sdOrientedBox(input.FragPos, input.UV + forward, input.UV2 - forward, shaderData.Thickness - float(sharedData.StrokeWidth) * 2);

        float diff = opSubtraction(inner, outer);

        float4 color = lerp(input.Color, sharedData.StrokeColor, clamp(SmoothSDF(diff), 0.0, 1.0));
        PsOutput output = { float4(color.rgb, color.a * SmoothSDF(outer))/*, input.Position.z*/ };
        return output;
    }

    float sdf = sdOrientedBox(input.FragPos, input.UV, input.UV2, shaderData.Thickness);
    PsOutput output = { float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf))/*, input.Position.z*/ };
    return output;
}