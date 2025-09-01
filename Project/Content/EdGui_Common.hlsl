#ifndef EDGUI_COMMON_HLSL
#define EDGUI_COMMON_HLSL

#define EDGUI_BINDGROUP "EdGuiConst"
#pragma bindgroup "EdGuiConst"

struct cbCanvasData
{
    float4x4 Projection;
};

[bindgroup(Group = EDGUI_BINDGROUP)]
ConstantBuffer<cbCanvasData> cbCanvas : register(b0);

#endif