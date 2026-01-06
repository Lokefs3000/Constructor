using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Resources
{
    public struct FrameGraphTextureDesc
    {
        public int Width;
        public int Height;
        public int Depth;

        public FGTextureDimension Dimension;
        public FGTextureFormat Format;
        public FGTextureUsage Usage;

        public FrameGraphTextureSwizzle Swizzle;

        public FrameGraphTextureDesc()
        {
            Width = 0;
            Height = 0;
            Depth = 1;

            Dimension = FGTextureDimension._2D;
            Format = FGTextureFormat.Undefined;
            Usage = FGTextureUsage.Undefined;

            Swizzle = FrameGraphTextureSwizzle.RGBA;
        }

        public FrameGraphTextureDesc(FrameGraphTextureDesc @base)
        {
            Width = @base.Width;
            Height = @base.Height;
            Depth = @base.Depth;

            Dimension = @base.Dimension;
            Format = @base.Format;
            Usage = @base.Usage;

            Swizzle = @base.Swizzle;
        }
    }

    public struct FrameGraphTextureSwizzle
    {
        public ushort Encoded;

        public FrameGraphTextureSwizzle()
        {
            this = RGBA;
        }

        public FrameGraphTextureSwizzle(FGTextureSwizzleChannel r, FGTextureSwizzleChannel g, FGTextureSwizzleChannel b, FGTextureSwizzleChannel a)
        {
            Encoded = (ushort)((((int)r) << 6) | (((int)g) << 4) | (((int)b) << 2) | ((int)a));
        }

        public FGTextureSwizzleChannel R { get => (FGTextureSwizzleChannel)((Encoded >> 9) & 0x7); set => Encoded = (ushort)((Encoded & ~(0x7 << 9)) | ((int)value << 9)); }
        public FGTextureSwizzleChannel G { get => (FGTextureSwizzleChannel)((Encoded >> 6) & 0x7); set => Encoded = (ushort)((Encoded & ~(0x7 << 6)) | ((int)value << 6)); }
        public FGTextureSwizzleChannel B { get => (FGTextureSwizzleChannel)((Encoded >> 3) & 0x7); set => Encoded = (ushort)((Encoded & ~(0x7 << 3)) | ((int)value << 3)); }
        public FGTextureSwizzleChannel A { get => (FGTextureSwizzleChannel)(Encoded & 0x7); set => Encoded = (ushort)((Encoded & ~0x7) | (int)value); }

        public static readonly FrameGraphTextureSwizzle RGBA = new FrameGraphTextureSwizzle(FGTextureSwizzleChannel.Red, FGTextureSwizzleChannel.Green, FGTextureSwizzleChannel.Blue, FGTextureSwizzleChannel.Alpha);
    }

    public enum FGTextureDimension : byte
    {
        _1D = 0,
        _2D,
        _3D
    }

    public enum FGTextureUsage : byte
    {
        Undefined = 0,

        GenericShader = 1 << 0,
        PixelShader = 1 << 1,
        RenderTarget = 1 << 2,
        DepthStencil = 1 << 3,

        ShaderResource = 1 << 5,

        Global = 1 << 4
    }

    public enum FGTextureFormat : byte
    {
        Undefined = 0,

        RGBA32_Typeless,
        RGBA32_Float,
        RGBA32_UInt,
        RGBA32_SInt,

        RGB32_Typeless,
        RGB32_Float,
        RGB32_UInt,
        RGB32_SInt,

        RGBA16_Typeless,
        RGBA16_Float,
        RGBA16_UInt,
        RGBA16_SInt,

        RG32_Typeless,
        RG32_Float,
        RG32_UInt,
        RG32_SInt,

        R32G8X24_Typeless,
        R32_Float_X8X24_Typeless,
        X32_Typeless_G8X24_UInt,

        RGB10A2_Typeless,
        RGB10A2_UNorm,
        RGB10A2_UInt,

        RG11B10_Float,

        RGBA8_Typeless,
        RGBA8_UNorm,
        RGBA8_UNorm_sRGB,
        RGBA8_UInt,
        RGBA8_SNorm,
        RGBA8_SInt,

        RG16_Typeless,
        RG16_Float,
        RG16_UNorm,
        RG16_UInt,
        RG16_SNorm,
        RG16_SInt,

        R32_Typeless,
        R32_Float,
        R32_UInt,
        R32_SInt,

        R24G8_Typeless,
        R24_Typeless_X8_UInt,
        R24_UNorm_X8_Typeless,
        X24_Typeless_G8_UInt,

        R8_Typeless,
        R8_UNorm,
        R8_UInt,
        R8_SNorm,
        R8_SInt,
        A8_UNorm,

        R16_Typeless,
        R16_Float,
        R16_UNorm,
        R16_UInt,
        R16_SNorm,
        R16_SInt,

        RG8_UNorm,
        RG8_UInt,
        RG8_SNorm,
        RG8_SInt,

        BC1_Typeless,
        BC1_UNorm,
        BC1_Unorm_sRGB,

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

        D32_Float_S8X24_UInt,
        D32_Float,
        D24_UNorm_S8_UInt,
        D16_UNorm,
    }

    public enum FGTextureSwizzleChannel : byte
    {
        Red = 0,
        Green,
        Blue,
        Alpha,
        One,
        Zero
    }
}
