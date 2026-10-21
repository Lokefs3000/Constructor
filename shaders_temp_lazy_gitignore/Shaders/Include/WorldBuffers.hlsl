#ifndef WORLDBUFFERS_HLSL
#define WORLDBUFFERS_HLSL

struct RenderDataConstants
{
    uint FlagOffset;
    uint MaterialOffset;
};

struct WorldMatrixData
{
    float4x4 View;
    float4x4 Projection;
    float4x4 ViewProjection;
};

struct RenderFlag
{
    float4x4 Model;
    uint DataId;
};

struct CameraData
{
    float4 CameraPos;
    float4 CameraDir;

    float ZNear;
    float ZFar;
};

#ifdef INCLUDE_RENDERFLAG_CONSTANTS
[constants]
ConstantBuffer<RenderDataConstants> cbRenderConstants;
#endif

#ifdef REDIRECT_PROPERTY_SOURCE
#pragma property_source "baMaterialData.Load<$GENERIC$>(cbRenderConstants.MaterialOffset + $BYTEOFFSET$)"
#pragma gen_properties_in_header false
#endif

[global]
ConstantBuffer<WorldMatrixData> cbWorldMatricies;
[global]
ConstantBuffer<CameraData> cbCameraData;

[global]
StructuredBuffer<RenderFlag> sbRenderFlags;
[global]
ByteAddressBuffer baMaterialData;

#endif