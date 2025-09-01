using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI
{
    public static class FormatStatistics
    {
        public static FormatInfo Query(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Undefined: return new FormatInfo();
                case TextureFormat.RGBA32t:
                case TextureFormat.RGBA32sf:
                case TextureFormat.RGBA32ui:
                case TextureFormat.RGBA32si: return new FormatInfo(16, 4);
                case TextureFormat.RGB32t:
                case TextureFormat.RGB32sf:
                case TextureFormat.RGB32ui:
                case TextureFormat.RGB32si: return new FormatInfo(12, 3);
                case TextureFormat.RGBA16t:
                case TextureFormat.RGBA16sf:
                case TextureFormat.RGBA16ui:
                case TextureFormat.RGBA16si: return new FormatInfo(8, 4);
                case TextureFormat.RG32t:
                case TextureFormat.RG32sf:
                case TextureFormat.RG32ui:
                case TextureFormat.RG32si: return new FormatInfo(8, 2);
                case TextureFormat.R32G8X24t: return new FormatInfo(8, 2);
                case TextureFormat.R32sfX8X24t: return new FormatInfo(8, 1);
                case TextureFormat.X32tG8X24ui: return new FormatInfo(8, 2);
                case TextureFormat.RGB10A2t:
                case TextureFormat.RGB10A2un:
                case TextureFormat.RGB10A2ui: return new FormatInfo(4, 4);
                case TextureFormat.RGBA8t:
                case TextureFormat.RGBA8un:
                case TextureFormat.RGBA8un_sRGB:
                case TextureFormat.RGBA8ui:
                case TextureFormat.RGBA8sn:
                case TextureFormat.RGBA8si: return new FormatInfo(4, 4);
                case TextureFormat.RG16t:
                case TextureFormat.RG16sf:
                case TextureFormat.RG16un:
                case TextureFormat.RG16ui:
                case TextureFormat.RG16sn:
                case TextureFormat.RG16si: return new FormatInfo(4, 2);
                case TextureFormat.R32t:
                case TextureFormat.R32sf:
                case TextureFormat.R32ui:
                case TextureFormat.R32si: return new FormatInfo(4, 1);
                case TextureFormat.R24G8t: return new FormatInfo(4, 2);
                case TextureFormat.R24unX8t: return new FormatInfo(4, 1);
                case TextureFormat.X24tG8ui: return new FormatInfo(4, 1);
                case TextureFormat.RG8t:
                case TextureFormat.RG8un:
                case TextureFormat.RG8ui:
                case TextureFormat.RG8sn:
                case TextureFormat.RG8si: return new FormatInfo(2, 2);
                case TextureFormat.R16t:
                case TextureFormat.R16sf:
                case TextureFormat.R16un:
                case TextureFormat.R16ui:
                case TextureFormat.R16sn:
                case TextureFormat.R16si: return new FormatInfo(2, 1);
                case TextureFormat.R8t:
                case TextureFormat.R8un:
                case TextureFormat.R8ui:
                case TextureFormat.R8sn:
                case TextureFormat.R8si:
                case TextureFormat.A8un:
                case TextureFormat.R1un: return new FormatInfo(1, 1);
                case TextureFormat.RG8BG8un: return new FormatInfo(4, 4);
                case TextureFormat.GR8GB8un: return new FormatInfo(4, 4);
                case TextureFormat.BC1t:
                case TextureFormat.BC1un:
                case TextureFormat.BC1un_sRGB: return new FormatInfo(8, 3, 4);
                case TextureFormat.BC2t:
                case TextureFormat.BC2un:
                case TextureFormat.BC2un_sRGB: return new FormatInfo(16, 4, 4);
                case TextureFormat.BC3t:
                case TextureFormat.BC3un:
                case TextureFormat.BC3un_sRGB: return new FormatInfo(16, 4, 4);
                case TextureFormat.BC4t:
                case TextureFormat.BC4un:
                case TextureFormat.BC4sn: return new FormatInfo(8, 1, 4);
                case TextureFormat.BC5t:
                case TextureFormat.BC5un:
                case TextureFormat.BC5sn: return new FormatInfo(16, 2, 4);
                case TextureFormat.B5G6R5un: return new FormatInfo(2, 3);
                case TextureFormat.BGR5A1un: return new FormatInfo(2, 4);
                case TextureFormat.BGRA8un: return new FormatInfo(4, 4);
                case TextureFormat.BGRX8un: return new FormatInfo(4, 3);
                case TextureFormat.BGRA8t:
                case TextureFormat.BGRA8un_sRGB: return new FormatInfo(4, 4);
                case TextureFormat.BGRX8t:
                case TextureFormat.BGRX8un_sRGB: return new FormatInfo(4, 3);
                case TextureFormat.BC6Ht:
                case TextureFormat.BC6Huf16:
                case TextureFormat.BC6Hsf16: return new FormatInfo(16, 3, 4);
                case TextureFormat.BC7t:
                case TextureFormat.BC7un:
                case TextureFormat.BC7un_sRGB: return new FormatInfo(16, 4, 4);
                default: return new FormatInfo();
            }
        }
    }

    public readonly record struct FormatInfo
    {
        public readonly int BytesPerPixel;
        public readonly int BlockWidth;
        public readonly int ChannelCount;

        public readonly bool IsBlockCompressed;

        public FormatInfo(int bpp, int count, int blockWidth = 0)
        {
            BytesPerPixel = bpp;
            BlockWidth = blockWidth;
            ChannelCount = count;

            IsBlockCompressed = blockWidth > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long CalculatePitch(long width)
        {
            return IsBlockCompressed ?
                (long)(((width + ((long)BlockWidth - 1)) / (long)BlockWidth) * (long)BytesPerPixel) :
                width * (long)BytesPerPixel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long CalculateSize(long width, long height, long depth = 1)
        {
            return IsBlockCompressed ?
                ((width + 3) / 4) * ((height + 3) / 4) * ((depth + 3) / 4) * BytesPerPixel :
                width * height * depth * (long)BytesPerPixel;
        }
    }
}
