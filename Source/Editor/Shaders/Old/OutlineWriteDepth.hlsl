#pragma path "Hidden/Editor/OutlineWriteDepth"

struct VsInput
{
    float3 Position : POSITION;
};

struct PsInput
{
    float4 SVPosition : SV_Position;
};

struct CbObjectBuffer
{
    float4x4 MVP;
};

ConstantBuffer<CbObjectBuffer> cbObject : register(b0);

[vertex]
PsInput VertexMain(VsInput input)
{
    PsInput output =
    {
        mul(cbObject.MVP, float4(input.Position, 1.0))
    };
    
    return output;
}

struct CbBiasData
{
    float Bias;
};

[constants]
ConstantBuffer<CbBiasData> cbBias : register(b0);

[pixel]
float PixelMain(PsInput input) : SV_Depth
{
    return input.SVPosition.z - cbBias.Bias;
}