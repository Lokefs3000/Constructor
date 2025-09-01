#pragma path "Hidden/Editor/IconBillboard"

static const float2 s_Vertices[6] = {
	float2(1.0, 1.0),
	float2(-1.0, -1.0),
	float2(-1.0, 1.0),
	float2(1.0, 1.0),
	float2(1.0, -1.0),
	float2(-1.0, -1.0),
};

struct PsInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD;
    float Distance  : FOG;
};

struct cbFrameData
{
    float4x4 VP;
    float3 Camera;
};

struct sbBillboardData
{
    float4x4 Model;
    float2 UVMin;
    float2 UVMax;
};

ConstantBuffer<cbFrameData> cbFrame;
StructuredBuffer<sbBillboardData> sbBillboards;

[vertex]
PsInput VertexMain(uint vertex : SV_VertexID, uint instance : SV_InstanceID)
{
    sbBillboardData data = sbBillboards[instance];

    float2 position = s_Vertices[vertex];

    PsInput output =
    {
        mul(mul(cbFrame.VP, data.Model), float4(position.xy * 0.5, 0.0, 1.0)),
        data.UVMin + (position * 0.5 + 0.5) * (data.UVMax - data.UVMin),
        distance(cbFrame.Camera, float3(data.Model[0][3], data.Model[1][3], data.Model[2][3]))
    };

    return output;
}

SamplerState ssSampler : defaultLinear;
Texture2D<float4> txAlbedo : register(t0);

[pixel]
float4 PixelMain(PsInput input) : SV_Target
{
    float a = smoothstep(0.0, 1.0, txAlbedo.Sample(ssSampler, input.UV).r);
    if (a < 0.92)
        discard;
    return float4(1.0, 1.0, 1.0, lerp(0.0, a, clamp((input.Distance - 1.0) * 2.0, 0.0, 1.0)));
}