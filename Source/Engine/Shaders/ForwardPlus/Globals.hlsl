#ifndef FP_GLOBALS_HLSL
#define FP_GLOBALS_HLSL

struct cbGlobalMatricies
{
    float4x4 ViewProjection;
};

[global]
ConstantBuffer<cbGlobalMatricies> cbFP_GlobalMatricies;

#undef __CURR_BINDGROUP
#endif