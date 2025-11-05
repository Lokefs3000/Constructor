struct sbRenderFlag
{
    float4x4 Model;
    uint DataId;
};

[global]
StructuredBuffer<sbRenderFlag> sbFP_RenderFlagBuffer;