using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI
{
    public abstract class Texture : Resource
    {
        public abstract ref readonly TextureDescription Description { get; }
        public abstract string Name { set; }
    }

    public struct TextureDescription
    {
        public uint Width;
        public uint Height;
        public uint Depth;

        public uint MipLevels;

        public TextureDimension Dimension;
        public TextureFormat Format;
        public MemoryUsage Memory;
        public TextureUsage Usage;
        public CPUAccessFlags CpuAccessFlags;
    }

    public enum TextureDimension : byte
    {
        Texture1D,
        Texture2D,
        Texture3D,
        TextureCube,
    }

    public enum TextureFormat : byte
    {
        Undefined = 0,

        RGBA32t,
        RGBA32sf,
        RGBA32ui,
        RGBA32si,

        RGB32t,
        RGB32sf,
        RGB32ui,
        RGB32si,

        RGBA16t,
        RGBA16sf,
        RGBA16ui,
        RGBA16si,

        RG32t,
        RG32sf,
        RG32ui,
        RG32si,

        R32G8X24t,

        R32sfX8X24t,
        X32tG8X24ui,

        RGB10A2t,
        RGB10A2un,
        RGB10A2ui,

        RGBA8t,
        RGBA8un,
        RGBA8un_sRGB,
        RGBA8ui,
        RGBA8sn,
        RGBA8si,

        RG16t,
        RG16sf,
        RG16un,
        RG16ui,
        RG16sn,
        RG16si,

        R32t,
        R32sf,
        R32ui,
        R32si,

        R24G8t,

        R24unX8t,
        X24tG8ui,

        RG8t,
        RG8un,
        RG8ui,
        RG8sn,
        RG8si,

        R16t,
        R16sf,
        R16un,
        R16ui,
        R16sn,
        R16si,

        R8t,
        R8un,
        R8ui,
        R8sn,
        R8si,

        A8un,

        R1un,

        RG8BG8un,
        GR8GB8un,

        BC1t,
        BC1un,
        BC1un_sRGB,

        BC2t,
        BC2un,
        BC2un_sRGB,

        BC3t,
        BC3un,
        BC3un_sRGB,

        BC4t,
        BC4un,
        BC4sn,

        BC5t,
        BC5un,
        BC5sn,

        B5G6R5un,

        BGR5A1un,
        
        BGRA8un,
        BGRX8un,
        BGRA8t,
        BGRA8un_sRGB,
        BGRX8t,
        BGRX8un_sRGB,

        BC6Ht,
        BC6Huf16,
        BC6Hsf16,

        BC7t,
        BC7un,
        BC7un_sRGB
    }

    [Flags]
    public enum TextureUsage : byte
    {
        None = 0,
        ShaderResource = 1 << 0,
    }
}
