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

struct ImageShaderData
{
    SharedData Shared;
}

[property]
Sampler2D(float4, txImage);

[pixel]
PsOutput PixelMain(DefaultPsInput input) : SV_Target
{
    PsOutput output = { input.Color * txImage.SampleLevel(GetSampler(txImage), input.UV, -1.0f)/*, input.Position.z*/ };
    return output;
}