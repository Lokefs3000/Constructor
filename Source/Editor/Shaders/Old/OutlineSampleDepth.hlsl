#pragma path "Hidden/Editor/OutlineSampleDepth"

static const float s_OutlineWidth = 1.0;

static const float2 s_Vertices[3] =
{
    float2(-1.0, -1.0),
	float2(3.0, -1.0),
	float2(-1.0, 3.0)
};

struct PsInput
{
    float4 SVPosition : SV_Position;
    float2 UV : TEXCOORD;
};

[vertex]
PsInput VertexMain(uint vertexId : SV_VertexId)
{
    PsInput output =
    {
        float4(s_Vertices[vertexId], 0.0, 1.0),
		float2(0.5, -0.5) * s_Vertices[vertexId] + 0.5
    };
    
    return output;
}

struct PsConstants
{
    float Near;
    float Far;
};

SamplerState ssSampler : defaultLinear
{
    Filter = "Linear",
    AddressModeU = "ClampToBorder",
    AddressModeV = "ClampToBorder",
    Border = "OpaqueWhiteUInt"
};

[constants]
ConstantBuffer<PsConstants> cbConstants : register(b0);

Texture2D<uint2> txStencil : register(t0);

float SampleDepth(float2 uv)
{
    //float depthSample = txDepth.Sample(ssSampler, uv);

    float ndc = 0.0 * 2.0 - 1.0;
    return (2.0 * cbConstants.Near * cbConstants.Far) / (cbConstants.Far + cbConstants.Near - ndc * (cbConstants.Far - cbConstants.Near));
}

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    uint width, height;
    txStencil.GetDimensions(width, height);

    uint2 screen = uint2(input.SVPosition.xy);

    uint right = txStencil.Load(uint3(screen + uint2(1, 0), 0)).y;
    uint left = txStencil.Load(uint3(screen - uint2(1, 0), 0)).y;
    uint top = txStencil.Load(uint3(screen + uint2(0, 1), 0)).y;
    uint bottom = txStencil.Load(uint3(screen - uint2(0, 1), 0)).y;
    
    uint minimum = min(min(right, left), min(top, bottom));
    uint maximum = max(max(right, left), max(top, bottom));

    uint count = (right > 0 ? 1 : 0) + (left > 0 ? 1 : 0) + (top > 0 ? 1 : 0) + (bottom > 0 ? 1 : 0);
    if (minimum == maximum)
        discard;

    return float4(1.0, 0.5, 0.0, count / 4.0);
}