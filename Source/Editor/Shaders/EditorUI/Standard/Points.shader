#include "../Common.hlsl"

[vertex]
DefaultPsInput VertexMain(VsInput input)
{
    DefaultPsInput output = {
        mul(cbGlobalData.Model, float4(input.Position, 0.0, 1.0)),
        input.UV,
        input.UV2,
        input.Tint,

        input.Position - input.UV2,

        input.DataOffset
    };

    return output;
}

struct PointsShaderData
{
    SharedData Shared;
}

[pixel]
PsOutput PixelMain(DefaultPsInput input) : SV_Target
{
    PointsShaderData shaderData = baDataBuffer.Load<PointsShaderData>(input.DataOffset);
    SharedData sharedData = shaderData.Shared;

    float radius = input.UV.x;

    if (sharedData.StrokeWidth > 0)
    {
        float outer = sdCircle(input.FragPos, radius);
        float inner = sdCircle(input.FragPos, radius - float(sharedData.StrokeWidth));

        float diff = opSubtraction(inner, outer);

        float4 color = lerp(input.Color, sharedData.StrokeColor, clamp(SmoothSDF(diff), 0.0, 1.0));
        PsOutput output = { float4(color.rgb, color.a * SmoothSDF(outer))/*, input.Position.z*/ };
        return output;
    }

    float sdf = sdCircle(input.FragPos, radius);
    PsOutput output = { float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf))/*, input.Position.z*/ };
    return output;
}