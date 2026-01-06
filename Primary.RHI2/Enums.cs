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

    public enum RHIFormat : byte
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

        RGBA8_Typeless,
        RGBA8_UNorm,
        RGBA8_UNorm_sRGB,
        RGBA8_UInt,
        RGBA8_SNorm,
        RGBA8_SInt,

        RG8_Typeless,
        RG8_UNorm,
        RG8_UInt,
        RG8_SNorm,
        RG8_SInt,

        R8_Typeless,
        R8_UNorm,
        R8_UInt,
        R8_SNorm,
        R8_SInt,

        RGB10A2_Typeless,
        RGB10A2_UNorm,
        RGB10A2_UInt,
        RG11B10_Float,

        D32_Float,
        D16_UNorm,

        R32G8X24_Typeless,

        D32_Float_S8X24_UInt,
        R32_Float_X8X24_Typeless,
        X32_Typeless_G8X24_UInt,

        R24G8_Typeless,
        D24_UNorm_S8_UInt,
        R24_UNorm_X8_Typeless,
        X24_Typeless_G8_UInt,

        BC1_Typeless,
        BC1_UNorm,
        BC1_UNorm_sRGB,

        BC2_Typeless,
        BC2_UNorm,
        BC2_UNorm_sRGB,

        BC3_Typeless,
        BC3_UNorm,
        BC3_UNorm_sRGB,

        BC4_Typeless,
        BC4_UNorm,
        BC4_SNorm,

        BC5_Typeless,
        BC5_UNorm,
        BC5_SNorm,

        BC6H_Typeless,
        BC6H_UFloat16,
        BC6H_SFloat16,

        BC7_Typeless,
        BC7_UNorm,
        BC7_UNorm_sRGB,
    }

    public enum RHIDimension : byte
    {
        Texture1D = 0,
        Texture2D,
        Texture3D,
        TextureCube
    }

    public enum RHISwizzleChannel : byte
    {
        Red = 0,
        Green,
        Blue,
        Alpha,
        One,
        Zero
    }

    public enum RHIDeviceAPI : byte
    {
        None = 0,

        Direct3D12
    }

    public enum RHIResourceType : byte
    {
        Buffer = 0,
        Texture,
        Sampler
    }

    public enum RHIFilterType : byte
    {
        Point = 0,
        Linear
    }

    public enum RHIReductionType : byte
    {
        Standard = 0,
        Comparison,
        Minimum,
        Maximum
    }

    public enum RHITextureAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        Clamp,
        Border,
        MirrorOnce
    }

    public enum RHIComparisonFunction : byte
    {
        None = 0,
        Never,
        Less,
        Equal,
        LessEqual,
        Greater,
        NotEqual,
        GreaterEqual,
        Always
    }

    public enum RHIFillMode : byte
    {
        Solid = 0,
        Wireframe
    }

    public enum RHICullMode : byte
    {
        None = 0,
        Back,
        Front
    }

    public enum RHIDepthWriteMask : byte
    {
        None = 0,
        All
    }
    
    public enum RHIPrimitiveTopologyType : byte
    {
        Triangle = 0,
        Line,
        Point,
        Patch
    }

    public enum RHIStencilOperation : byte
    {
        Keep = 0,
        Zero,
        Replace,
        IncrSaturation,
        DecrSatuaration,
        Invert,
        Increment,
        Decrement
    }

    public enum RHIBlend : byte
    {
        Zero = 0,
        One,
        SrcColor,
        InvSrcColor,
        SrcAlpha,
        InvSrcAlpha,
        DestAlpha,
        InvDestAlpha,
        DestColor,
        InvDestColor,
        SrcAlphaSaturate,
        BlendFactor,
        InvBlendFactor,
        Src1Color,
        InvSrc1Color,
        Src1Alpha,
        InvSrc1Alpha,
        AlphaFactor,
        InvAlphaFactor
    }

    public enum RHIBlendOperation : byte
    {
        Add = 0,
        Subtract,
        ReverseSubtract,
        Minimum,
        Maximum,
    }

    public enum RHISamplerBorder : byte
    {
        TransparentBlack = 0,
        OpaqueBlack,
        OpaqueWhite,
        OpaqueBlackUInt,
        OpaqueWhiteUInt
    }

    public enum RHIElementSemantic : byte
    {
        Position = 0,
        Texcoord,
        Color,
        Normal,
        Tangent,
        //Bitangnet,
        BlendIndices = Tangent + 2,
        BlendWeight,
        PositionT,
        PSize,
        Fog,
        TessFactor,
    }

    public enum RHIElementFormat : byte
    {
        Single1,
        Single2,
        Single3,
        Single4,

        Byte4,
    }

    public enum RHIInputClass : byte
    {
        PerVertex,
        PerInstance
    }
}
