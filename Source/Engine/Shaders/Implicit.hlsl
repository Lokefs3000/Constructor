#ifndef IMPLICIT_HLSL
#define IMPLICIT_HLSL

#define Sampler1D(generic, name) \
    [sampled] \
    Texture1D<generic> name; \
    SamplerState name##_Sampler

#define Sampler2D(generic, name) \
    [sampled] \
    Texture2D<generic> name; \
    SamplerState name##_Sampler

#define Sampler3D(generic, name) \
    [sampled] \
    Texture3D<generic> name; \
    SamplerState name##_Sampler

#define GetSampler(name) name##_Sampler

#endif