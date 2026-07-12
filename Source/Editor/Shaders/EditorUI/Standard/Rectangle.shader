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

struct RectangleShaderData
{
    SharedData Shared;
    uint16_t2 BoxSize;
    uint16_t __pad0;
    float16_t4 CornerRadii;
}

[pixel]
PsOutput PixelMain(DefaultPsInput input) : SV_Target
{
    RectangleShaderData shaderData = baDataBuffer.Load<RectangleShaderData>(input.DataOffset);
    SharedData sharedData = shaderData.Shared;
  
    float2 fragPos = input.FragPos;

    if (sign(shaderData.CornerRadii.x) < 0)
    {
        if (sharedData.StrokeWidth > 0)
        {
            float outer = sdBox(fragPos, shaderData.BoxSize * 0.5);
            float inner = sdBox(fragPos, shaderData.BoxSize * 0.5 - sharedData.StrokeWidth);

            float diff = opSubtraction(inner, outer);

            PsOutput output = { lerp(input.Color, sharedData.StrokeColor, clamp(-sign(diff), 0.0, 1.0))/*, input.Position.z*/ };
            return output;
        }

        PsOutput output = { input.Color/*, input.Position.z*/ };
        return output;
    }

    if (sharedData.StrokeWidth > 0)
    {
        float outer = sdRoundedBox(fragPos, shaderData.BoxSize * 0.5, shaderData.CornerRadii);
        float inner = sdRoundedBox(fragPos, shaderData.BoxSize * 0.5 - sharedData.StrokeWidth, shaderData.CornerRadii);

        float diff = opSubtraction(inner, outer);

        float4 color = lerp(input.Color, sharedData.StrokeColor, clamp(SmoothSDF(diff), 0.0, 1.0));
        PsOutput output = { float4(color.rgb, color.a * SmoothSDF(outer))/*, input.Position.z*/ };
        return output;
    }

    float sdf = sdRoundedBox(fragPos, shaderData.BoxSize * 0.5, shaderData.CornerRadii);
    PsOutput output = { float4(input.Color.rgb, input.Color.a * SmoothSDF(sdf))/*, input.Position.z*/ };
    return output;
}