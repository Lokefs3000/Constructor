#ifndef FP_GLOBALS_HLSL
#define FP_GLOBALS_HLSL

struct cbGlobalMatricies
{
    float4x4 ViewProjection;
    float4x4 View;
    float4x4 Projection;
};

struct cbCameraData
{
    float3 Position;
    float3 Direction;
};

[global]
ConstantBuffer<cbGlobalMatricies> cbFP_GlobalMatricies;

[global]
ConstantBuffer<cbCameraData> cbFP_CameraData;

#undef __CURR_BINDGROUP
#endif