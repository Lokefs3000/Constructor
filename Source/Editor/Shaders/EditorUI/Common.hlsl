#include "Engine/Shaders/Implicit.hlsl"
#include "Editor/Shaders/EditorUI/SDF.hlsl"

struct VsInput
{
    float2 Position : POSITION;
    float2 UV       : TEXCOORD;
    float2 UV2      : TEXCOORD1;
    float4 Tint     : COLOR;

    nointerpolation uint Depth : TEXCOORD2;
    nointerpolation uint DataOffset : TEXCOORD3;
};

struct DefaultPsInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD;
    float2 UV2      : TEXCOORD1;
    float16_t4 Color: COLOR;

    float2 FragPos  : POSITION;

    nointerpolation uint DataOffset : TEXCOORD2;
};

struct GlobalData
{
    float3x2 Model;
};

struct SharedData
{
    uint16_t StrokeWidth;
    float16_t4 StrokeColor;
};

ConstantBuffer<GlobalData> cbGlobalData;
ByteAddressBuffer baDataBuffer;

Texture2D<float4> txGradientAtlas;

static SamplerState ssDefaultLinear : defaultLinear;
static SamplerState ssDefaultPoint : defaultPoint;