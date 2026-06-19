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

[property]
Sampler2D(float4, txImage);

[pixel]
float4 PixelMain(DefaultPsInput input) : SV_Target
{
    return input.Color * txImage.Sample(GetSampler(txImage), input.UV);
}