namespace Primary.RHI2
{
    public struct RHIFormatInfo
    {
        public byte BytesPerPixel;
        public byte ChannelCount;
        public byte BlockWidth;

        public bool IsBlockCompressed;

        public RHIFormatInfo(int bppOrBlockWidth, int channelCount)
        {
            BytesPerPixel = (byte)bppOrBlockWidth;
            ChannelCount = (byte)channelCount;
            BlockWidth = 0;

            IsBlockCompressed = false;
        }

        public RHIFormatInfo(int bppOrBlockWidth, int channelCount, int blockWidth)
        {
            BytesPerPixel = (byte)bppOrBlockWidth;
            ChannelCount = (byte)channelCount;
            BlockWidth = (byte)blockWidth;

            IsBlockCompressed = true;
        }

        public long CalculatePitch(long width)
        {
            return IsBlockCompressed ?
                (long)(((width + ((long)BlockWidth - 1)) / (long)BlockWidth) * (long)BytesPerPixel) :
                width * (long)BytesPerPixel;
        }

        public long CalculateSize(long width, long height, long depth = 1)
        {
            return IsBlockCompressed ?
                ((width + 3) / 4) * ((height + 3) / 4) * ((depth + 3) / 4) * BytesPerPixel :
                width * height * depth * (long)BytesPerPixel;
        }

        public static RHIFormatInfo Query(RHIFormat format) => format switch
        {
            RHIFormat.RGBA32_Typeless => new RHIFormatInfo(16, 4),
            RHIFormat.RGBA32_Float => new RHIFormatInfo(16, 4),
            RHIFormat.RGBA32_UInt => new RHIFormatInfo(16, 4),
            RHIFormat.RGBA32_SInt => new RHIFormatInfo(16, 4),
            RHIFormat.RGB32_Typeless => new RHIFormatInfo(12, 3),
            RHIFormat.RGB32_Float => new RHIFormatInfo(12, 3),
            RHIFormat.RGB32_UInt => new RHIFormatInfo(12, 3),
            RHIFormat.RGB32_SInt => new RHIFormatInfo(12, 3),
            RHIFormat.RG32_Typeless => new RHIFormatInfo(8, 2),
            RHIFormat.RG32_Float => new RHIFormatInfo(8, 2),
            RHIFormat.RG32_UInt => new RHIFormatInfo(8, 2),
            RHIFormat.RG32_SInt => new RHIFormatInfo(8, 2),
            RHIFormat.R32_Typeless => new RHIFormatInfo(4, 1),
            RHIFormat.R32_Float => new RHIFormatInfo(4, 1),
            RHIFormat.R32_UInt => new RHIFormatInfo(4, 1),
            RHIFormat.R32_SInt => new RHIFormatInfo(4, 1),
            RHIFormat.RGBA16_Typeless => new RHIFormatInfo(8, 4),
            RHIFormat.RGBA16_Float => new RHIFormatInfo(8, 4),
            RHIFormat.RGBA16_UNorm => new RHIFormatInfo(8, 4),
            RHIFormat.RGBA16_UInt => new RHIFormatInfo(8, 4),
            RHIFormat.RGBA16_SNorm => new RHIFormatInfo(8, 4),
            RHIFormat.RGBA16_SInt => new RHIFormatInfo(8, 4),
            RHIFormat.RG16_Typeless => new RHIFormatInfo(4, 2),
            RHIFormat.RG16_Float => new RHIFormatInfo(4, 2),
            RHIFormat.RG16_UNorm => new RHIFormatInfo(4, 2),
            RHIFormat.RG16_UInt => new RHIFormatInfo(4, 2),
            RHIFormat.RG16_SNorm => new RHIFormatInfo(4, 2),
            RHIFormat.RG16_SInt => new RHIFormatInfo(4, 2),
            RHIFormat.R16_Typeless => new RHIFormatInfo(2, 1),
            RHIFormat.R16_Float => new RHIFormatInfo(2, 1),
            RHIFormat.R16_UInt => new RHIFormatInfo(2, 1),
            RHIFormat.R16_SNorm => new RHIFormatInfo(2, 1),
            RHIFormat.R16_SInt => new RHIFormatInfo(2, 1),
            RHIFormat.RGBA8_Typeless => new RHIFormatInfo(4, 4),
            RHIFormat.RGBA8_UNorm => new RHIFormatInfo(4, 4),
            RHIFormat.RGBA8_UNorm_sRGB => new RHIFormatInfo(4, 4),
            RHIFormat.RGBA8_UInt => new RHIFormatInfo(4, 4),
            RHIFormat.RGBA8_SNorm => new RHIFormatInfo(4, 4),
            RHIFormat.RGBA8_SInt => new RHIFormatInfo(4, 4),
            RHIFormat.RG8_Typeless => new RHIFormatInfo(2, 2),
            RHIFormat.RG8_UNorm => new RHIFormatInfo(2, 2),
            RHIFormat.RG8_UInt => new RHIFormatInfo(2, 2),
            RHIFormat.RG8_SNorm => new RHIFormatInfo(2, 2),
            RHIFormat.RG8_SInt => new RHIFormatInfo(2, 2),
            RHIFormat.R8_Typeless => new RHIFormatInfo(1, 1),
            RHIFormat.R8_UNorm => new RHIFormatInfo(1, 1),
            RHIFormat.R8_UInt => new RHIFormatInfo(1, 1),
            RHIFormat.R8_SNorm => new RHIFormatInfo(1, 1),
            RHIFormat.R8_SInt => new RHIFormatInfo(1, 1),
            RHIFormat.RGB10A2_Typeless => new RHIFormatInfo(4, 4),
            RHIFormat.RGB10A2_UNorm => new RHIFormatInfo(4, 4),
            RHIFormat.RGB10A2_UInt => new RHIFormatInfo(4, 4),
            RHIFormat.RG11B10_Float => new RHIFormatInfo(4, 3),
            RHIFormat.D32_Float => new RHIFormatInfo(4, 1),
            RHIFormat.D16_UNorm => new RHIFormatInfo(2, 1),
            RHIFormat.R32G8X24_Typeless => new RHIFormatInfo(8, 2),
            RHIFormat.D32_Float_S8X24_UInt => new RHIFormatInfo(8, 2),
            RHIFormat.R32_Float_X8X24_Typeless => new RHIFormatInfo(8, 1),
            RHIFormat.X32_Typeless_G8X24_UInt => new RHIFormatInfo(8, 1),
            RHIFormat.R24G8_Typeless => new RHIFormatInfo(4, 2),
            RHIFormat.D24_UNorm_S8_UInt => new RHIFormatInfo(4, 2),
            RHIFormat.R24_UNorm_X8_Typeless => new RHIFormatInfo(4, 1),
            RHIFormat.X24_Typeless_G8_UInt => new RHIFormatInfo(4, 1),
            RHIFormat.BC1_Typeless => new RHIFormatInfo(8, 3, 4),
            RHIFormat.BC1_UNorm => new RHIFormatInfo(8, 3, 4),
            RHIFormat.BC1_UNorm_sRGB => new RHIFormatInfo(8, 3, 4),
            RHIFormat.BC2_Typeless => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC2_UNorm => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC2_UNorm_sRGB => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC3_Typeless => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC3_UNorm => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC3_UNorm_sRGB => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC4_Typeless => new RHIFormatInfo(8, 1, 4),
            RHIFormat.BC4_UNorm => new RHIFormatInfo(8, 1, 4),
            RHIFormat.BC4_SNorm => new RHIFormatInfo(8, 1, 4),
            RHIFormat.BC5_Typeless => new RHIFormatInfo(16, 2, 4),
            RHIFormat.BC5_UNorm => new RHIFormatInfo(16, 2, 4),
            RHIFormat.BC5_SNorm => new RHIFormatInfo(16, 2, 4),
            RHIFormat.BC6H_Typeless => new RHIFormatInfo(16, 3, 4),
            RHIFormat.BC6H_UFloat16 => new RHIFormatInfo(16, 3, 4),
            RHIFormat.BC6H_SFloat16 => new RHIFormatInfo(16, 3, 4),
            RHIFormat.BC7_Typeless => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC7_UNorm => new RHIFormatInfo(16, 4, 4),
            RHIFormat.BC7_UNorm_sRGB => new RHIFormatInfo(16, 4, 4),
            _ => default,
        };
    }
}
