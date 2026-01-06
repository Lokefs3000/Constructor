using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public enum RHIResourceUsage : byte
    {
        None = 0,

        ShaderResource = 1 << 0,
        ConstantBuffer = 1 << 1,
        UnorderedAccess = 1 << 2,
        VertexInput = 1 << 3,
        IndexInput = 1 << 4,
        RenderTarget = 1 << 5,
        DepthStencil = 1 << 6,
    }

    public enum RHIBufferMode : byte
    {
        Default = 0,
        Raw,
    }

    public enum RHIFormat : uint
    {
        Unknown = 0,

        RGBA32_Typeless,
        RGBA32_Float,
        RGBA32_UInt,
        RGBA32_SInt,

        RGB32_Typeless,
        RGB32_Float,
        RGB32_UInt,
        RGB32_SInt,

        RG32_Typeless,
        RG32_Float,
        RG32_UInt,
        RG32_SInt,

        R32_Typeless,
        R32_Float,
        R32_UInt,
        R32_SInt,

        RGBA16_Typeless,
        RGBA16_Float,
        RGBA16_UNorm,
        RGBA16_UInt,
        RGBA16_SNorm,
        RGBA16_SInt,

        RG16_Typeless,
        RG16_Float,
        RG16_UNorm,
        RG16_UInt,
        RG16_SNorm,
        RG16_SInt,

        R16_Typeless,
        R16_Float,
        R16_UInt,
        R16_SNorm,
        R16_SInt,

        D32_Float,
        D16_Float
    }
}
