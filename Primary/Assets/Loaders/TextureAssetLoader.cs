using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering;
using Primary.Utility;
using Serilog;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Assets.Loaders
{
    internal static unsafe class TextureAssetLoader
    {
        internal static IInternalAssetData FactoryCreateNull()
        {
            return new TextureAssetData();
        }

        internal static IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not TextureAssetData textureData)
                throw new ArgumentException(nameof(assetData));

            return new TextureAsset(textureData);
        }

        internal static void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not TextureAsset texture)
                throw new ArgumentException(nameof(asset));
            if (assetData is not TextureAssetData textureData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                /*
                Format support (from "TextureProcessor.cs" within "Editor.Processors")
                    BC7     - Yes
                    BC6s    - Yes
                    BC6u    - Yes
                    ASTC    - No
                    BC5u    - Yes
                    BC4u    - Yes
                    BC3     - Yes
                    BC3n    - Unknown?
                    BC2     - Yes
                    BC1a    - Unknown?
                    BC1     - Yes
                    R8a     - Needs testing
                    R8l     - Needs testing
                    BGR8    - Yes
                    BGRA8   - Yes
                    BGRX8   - Yes
                    RGB8    - Yes
                    RGBA8   - Yes
                    R16     - Yes
                    RG16    - Yes
                    RGBA16  - Yes
                    R32     - Yes
                    RG32    - Yes
                    RGBA32  - Yes

                Feature support:
                    DDPF_ALPHAPIXELS - Yes
                    DDPF_ALPHA - Yes
                    DDPF_FOURCC - Yes
                    DDPF_RGB - Ignored
                    DDPF_YUV - No
                    DDPF_LUMINANCE - No
                */

                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
                ExceptionUtility.Assert(stream != null);

                BinaryReader br = new BinaryReader(stream!, Encoding.UTF8, true);

                ExceptionUtility.Assert(br.ReadUInt32() == DDSMagicNumber);

                DDS_HEADER ddsHeader = br.Read<DDS_HEADER>();
                bool hasDxtHeader = ddsHeader.ddspf.fourCC == DXT10HeaderCC;
                DDS_HEADER_DXT10 ddsDxt10 = hasDxtHeader ? br.Read<DDS_HEADER_DXT10>() : default;

                IntermediateTextureFormat textureFormat = IntermediateTextureFormat.Undefined;
                RHI.TextureDimension dimension = RHI.TextureDimension.Texture2D;

                if (hasDxtHeader)
                {
                    ExceptionUtility.Assert(ddsDxt10.resourceDimension == 3/*DDS_DIMENSION_TEXTURE2D*/, "Only Texture2Ds are supported yet!");
                    ExceptionUtility.Assert(ddsDxt10.arraySize == 1, "Array textures are not supported yet!");

                    if (ddsDxt10.resourceDimension == 3 && FlagUtility.HasFlag(ddsDxt10.miscFlag, 0x4u/*DDS_RESOURCE_MISC_TEXTURECUBE*/))
                        dimension = RHI.TextureDimension.TextureCube;
                    if (ddsDxt10.miscFlags2 != 0)
                        Log.Warning("[t:{path}]: DDS DXT10 header {paramName} is not zero! This is ignored by the importer but could cause issues in others.", sourcePath, nameof(ddsDxt10.miscFlags2));

                    switch (ddsDxt10.dxgiFormat)
                    {
                        case Vortice.DXGI.Format.BC7_UNorm: textureFormat = IntermediateTextureFormat.BC7; break;
                        case Vortice.DXGI.Format.BC6H_Sf16: textureFormat = IntermediateTextureFormat.BC6s; break;
                        case Vortice.DXGI.Format.BC6H_Uf16: textureFormat = IntermediateTextureFormat.BC6u; break;
                        case Vortice.DXGI.Format.BC5_UNorm: textureFormat = IntermediateTextureFormat.BC5u; break;
                        case Vortice.DXGI.Format.BC4_UNorm: textureFormat = IntermediateTextureFormat.BC4u; break;
                        case Vortice.DXGI.Format.BC3_UNorm: textureFormat = IntermediateTextureFormat.BC3; break;
                        case Vortice.DXGI.Format.BC3_Typeless: textureFormat = IntermediateTextureFormat.BC3n; break;
                        case Vortice.DXGI.Format.BC2_UNorm: textureFormat = IntermediateTextureFormat.BC2; break;
                        case Vortice.DXGI.Format.BC1_Typeless: textureFormat = IntermediateTextureFormat.BC1a; break;
                        case Vortice.DXGI.Format.BC1_UNorm: textureFormat = IntermediateTextureFormat.BC1; break;
                    }
                }

                if (textureFormat == IntermediateTextureFormat.Undefined)
                {
                    if (!FlagUtility.HasFlag(ddsHeader.ddspf.flags, 0x4u/*DDPF_FOURCC*/) || !PixelFormatConversionTable.TryGetValue(ddsHeader.ddspf.fourCC, out textureFormat))
                    {
                        bool hasAlpha = FlagUtility.HasFlag(ddsHeader.ddspf.flags, 0x1u/*DDPF_ALPHAPIXELS*/ | 0x2u/*DDPF_ALPHA*/);
                        uint bitMaskCount = MakeBitmaskCount(
                            ddsHeader.ddspf.RBitMask, ddsHeader.ddspf.GBitMask, ddsHeader.ddspf.BBitMask,
                            hasAlpha ? ddsHeader.ddspf.ABitMask : 0xffffffff);

                        if (!TextureFormatMaskDictionary.TryGetValue((bitMaskCount, ddsHeader.ddspf.RGBBitCount), out textureFormat))
                        {
                            throw new Exception();
                        }
                    }
                }

                RHI.FormatInfo fi = default;
                switch (textureFormat)
                {
                    case IntermediateTextureFormat.BC7: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC7un); break;
                    case IntermediateTextureFormat.BC6s: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC6Hsf16); break;
                    case IntermediateTextureFormat.BC6u: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC6Huf16); break;
                    case IntermediateTextureFormat.BC5u: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC5un); break;
                    case IntermediateTextureFormat.BC4u: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC4un); break;
                    case IntermediateTextureFormat.BC3: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC3un); break;
                    case IntermediateTextureFormat.BC3n: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC3un); break;
                    case IntermediateTextureFormat.BC2: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC2un); break;
                    case IntermediateTextureFormat.BC1a: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC1un); break;
                    case IntermediateTextureFormat.BC1: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BC1un); break;
                    case IntermediateTextureFormat.R8a: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.R8un); break;
                    case IntermediateTextureFormat.R8l: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.R8un); break;
                    case IntermediateTextureFormat.BGR8: fi = new RHI.FormatInfo(3, 3); break;
                    case IntermediateTextureFormat.BGRA8: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BGRA8un); break;
                    case IntermediateTextureFormat.BGRX8: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.BGRX8un); break;
                    case IntermediateTextureFormat.RGB8: fi = new RHI.FormatInfo(3, 3); break;
                    case IntermediateTextureFormat.RGBA8: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.RGBA8un); break;
                    case IntermediateTextureFormat.R16: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.R16sf); break;
                    case IntermediateTextureFormat.RG16: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.RG16sf); break;
                    case IntermediateTextureFormat.RGBA16: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.RGBA16sf); break;
                    case IntermediateTextureFormat.R32: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.R32sf); break;
                    case IntermediateTextureFormat.RG32: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.RG32sf); break;
                    case IntermediateTextureFormat.RGBA32: fi = RHI.FormatStatistics.Query(RHI.TextureFormat.RGBA32sf); break;
                }

                //bool hasMips = FlagUtility.HasFlag(ddsHeader.flags, 0x20000u/*DDSD_MIPMAPCOUNT*/);
                if (ddsHeader.depth > 1 && ddsHeader.mipMapCount > 0)
                    throw new Exception();

                RHI.Texture? rhiTexture = null;

                //TODO: use an ArrayPool instead to avoid memalloc
                nint[] mipLevels = new nint[ddsHeader.mipMapCount];
                try
                {
                    for (int i = 0; i < ddsHeader.mipMapCount; i++)
                    {
                        uint width = ddsHeader.width >> i;
                        uint height = ddsHeader.height >> i;
                        uint depth = Math.Max(1, ddsHeader.depth);

                        //ulong dataPitch = (ulong)fi.CalculatePitch(width);
                        //ulong dataSize = (dataPitch * height) * depth;
                        ulong dataSize = (ulong)fi.CalculateSize(width, height, depth);

                        ExceptionUtility.Assert(dataSize <= uint.MaxValue);

                        nint mipData = (nint)NativeMemory.Alloc((nuint)dataSize);
                        stream!.ReadExactly(new Span<byte>(mipData.ToPointer(), (int)dataSize));

                        if (textureFormat == IntermediateTextureFormat.BGR8 || textureFormat == IntermediateTextureFormat.RGB8)
                        {
                            ExceptionUtility.Assert(depth > 1, "can depth actually have a 24bit format?");
                            nint newMipData = AddAlphaChannelToPixelData(mipData, width, height, (uint)fi.BytesPerPixel);

                            NativeMemory.Free(mipData.ToPointer());
                            mipData = newMipData;
                        }

                        mipLevels[i] = mipData;
                    }

                    RHI.TextureFormat rhiFormat = textureFormat switch
                    {
                        IntermediateTextureFormat.BC7 => RHI.TextureFormat.BC7un,
                        IntermediateTextureFormat.BC6s => RHI.TextureFormat.BC6Hsf16,
                        IntermediateTextureFormat.BC6u => RHI.TextureFormat.BC6Huf16,
                        IntermediateTextureFormat.BC5u => RHI.TextureFormat.BC5un,
                        IntermediateTextureFormat.BC4u => RHI.TextureFormat.BC4un,
                        IntermediateTextureFormat.BC3 => RHI.TextureFormat.BC3un,
                        IntermediateTextureFormat.BC3n => RHI.TextureFormat.BC3t,
                        IntermediateTextureFormat.BC2 => RHI.TextureFormat.BC2un,
                        IntermediateTextureFormat.BC1a => RHI.TextureFormat.BC1t,
                        IntermediateTextureFormat.BC1 => RHI.TextureFormat.BC1un,
                        IntermediateTextureFormat.R8a => RHI.TextureFormat.R8un,
                        IntermediateTextureFormat.R8l => RHI.TextureFormat.R8un,
                        IntermediateTextureFormat.BGR8 => RHI.TextureFormat.BGRA8un,
                        IntermediateTextureFormat.BGRA8 => RHI.TextureFormat.BGRA8un,
                        IntermediateTextureFormat.BGRX8 => RHI.TextureFormat.BGRX8un,
                        IntermediateTextureFormat.RGB8 => RHI.TextureFormat.RGBA8un,
                        IntermediateTextureFormat.RGBA8 => RHI.TextureFormat.RGBA8un,
                        IntermediateTextureFormat.R16 => RHI.TextureFormat.R16sf,
                        IntermediateTextureFormat.RG16 => RHI.TextureFormat.RG16sf,
                        IntermediateTextureFormat.RGBA16 => RHI.TextureFormat.RGBA16sf,
                        IntermediateTextureFormat.R32 => RHI.TextureFormat.R32sf,
                        IntermediateTextureFormat.RG32 => RHI.TextureFormat.RG32sf,
                        IntermediateTextureFormat.RGBA32 => RHI.TextureFormat.RGBA32sf,
                        _ => RHI.TextureFormat.Undefined
                    };

                    ExceptionUtility.Assert(rhiFormat != RHI.TextureFormat.Undefined);

                    rhiTexture = RenderingManager.Device.CreateTexture(new RHI.TextureDescription
                    {
                        Width = ddsHeader.width,
                        Height = Math.Max(ddsHeader.height, 1),
                        Depth = Math.Max(ddsHeader.depth, 1),

                        MipLevels = ddsHeader.mipMapCount,

                        Dimension = RHI.TextureDimension.Texture2D,
                        Format = rhiFormat,
                        Memory = RHI.MemoryUsage.Immutable,
                        Usage = RHI.TextureUsage.ShaderResource,
                        CpuAccessFlags = RHI.CPUAccessFlags.None
                    }, mipLevels.AsSpan());
                }
                finally
                {
                    for (int i = 0; i < mipLevels.Length; i++)
                    {
                        if (mipLevels[i] != nint.Zero)
                        {
                            NativeMemory.Free(mipLevels[i].ToPointer());
                        }
                    }
                }

                textureData.UpdateAssetData(texture, rhiTexture);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                textureData.UpdateAssetFailed(texture);
                Log.Error(ex, "Failed to load texture: {name}", sourcePath);
            }
#endif
        }

        private static nint AddAlphaChannelToPixelData(nint pixels, uint width, uint height, uint bpp)
        {
            uint totalLocalSize = width * height;

            byte* oldPixels = (byte*)pixels;
            byte* newPixels = (byte*)NativeMemory.Alloc((nuint)(totalLocalSize * (ulong)bpp));

            try
            {
                for (uint i = 0; i < totalLocalSize; i++)
                {
                    ulong originalPixelLoc = i * 3ul;
                    ulong newPixelLoc = i * 4ul;

                    newPixels[newPixelLoc] = oldPixels[originalPixelLoc];
                    newPixels[newPixelLoc + 1] = oldPixels[originalPixelLoc + 1];
                    newPixels[newPixelLoc + 2] = oldPixels[originalPixelLoc + 2];
                    newPixels[newPixelLoc + 3] = 255;
                }
            }
            finally
            {
                NativeMemory.Free(newPixels);
            }

            return (nint)newPixels;
        }

        private const uint DDSMagicNumber = 0x20534444u;
        private static readonly uint DXT10HeaderCC = MakeFourCC('D', 'X', '1', '0');

        #region Utility and data types
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MakeFourCC(uint r, uint g, uint b, uint a)
        {
            return ((uint)r) | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
        }

        private static uint MakeBitmaskCount(uint rMask, uint gMask, uint bMask, uint aMask)
        {
            if (rMask == 0) rMask = uint.MaxValue;
            if (gMask == 0) gMask = uint.MaxValue;
            if (bMask == 0) bMask = uint.MaxValue;
            if (aMask == 0) aMask = uint.MaxValue;

            return (uint)(BitOperations.LeadingZeroCount(rMask) / 2 << 24) |
                            (uint)(BitOperations.LeadingZeroCount(gMask) / 2 << 16) |
                            (uint)(BitOperations.LeadingZeroCount(bMask) / 2 << 8) |
                            (uint)BitOperations.LeadingZeroCount(aMask) / 2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct DDS_HEADER
        {
            public uint size;
            public uint flags;
            public uint height;
            public uint width;
            public uint pitchOrLinearSize;
            public uint depth;
            public uint mipMapCount;
            public fixed uint reserved1[11];
            public DDS_PIXELFORMAT ddspf;
            public uint caps;
            public uint caps2;
            public uint caps3;
            public uint caps4;
            public uint reserved2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct DDS_HEADER_DXT10
        {
            public Vortice.DXGI.Format dxgiFormat;
            public uint resourceDimension;
            public uint miscFlag;
            public uint arraySize;
            public uint miscFlags2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct DDS_PIXELFORMAT
        {
            public uint size;
            public uint flags;
            public uint fourCC;
            public uint RGBBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;
        }

        private static FrozenDictionary<uint, IntermediateTextureFormat> PixelFormatConversionTable = new Dictionary<uint, IntermediateTextureFormat>()
        {
            { 111, IntermediateTextureFormat.R16 },
            { 112, IntermediateTextureFormat.RG16 },
            { 113, IntermediateTextureFormat.RGBA16 },
            { 114, IntermediateTextureFormat.R32 },
            { 115, IntermediateTextureFormat.RG32 },
            { 116, IntermediateTextureFormat.RGBA32 },
            { MakeFourCC('D', 'X', 'T', '1'), IntermediateTextureFormat.BC1 },
            { MakeFourCC('D', 'X', 'T', '3'), IntermediateTextureFormat.BC2 },
            { MakeFourCC('D', 'X', 'T', '5'), IntermediateTextureFormat.BC3 },
            { MakeFourCC('B', 'C', '4', 'U'), IntermediateTextureFormat.BC4u },
            //{ MakeFourCC('B', 'C', '4', 'S'), IntermediateTextureFormat.BC4s },
            { MakeFourCC('B', 'C', '5', 'U'), IntermediateTextureFormat.BC5u },
            //{ MakeFourCC('B', 'C', '5', 'S'), IntermediateTextureFormat.BC5s },
        }.ToFrozenDictionary();

        private static FrozenDictionary<(uint Mask, uint Count), IntermediateTextureFormat> TextureFormatMaskDictionary = new Dictionary<(uint Mask, uint Count), IntermediateTextureFormat>()
        {
            { (MakeBitmaskCount(0x0000ff00u, 0x00ff0000u, 0xff000000u, 0xffffffffu), 24), IntermediateTextureFormat.BGR8 }, //BGR8
            { (MakeBitmaskCount(0x0000ff00u, 0x00ff0000u, 0xff000000u, 0x000000ffu), 32), IntermediateTextureFormat.BGRA8 }, //BGRA8
            { (MakeBitmaskCount(0x0000ff00u, 0x00ff0000u, 0xff000000u, 0xffffffffu), 32), IntermediateTextureFormat.BGRX8 }, //BGRX8
            { (MakeBitmaskCount(0xff000000u, 0x00ff0000u, 0x0000ff00u, 0xffffffffu), 24), IntermediateTextureFormat.RGB8 }, //RGB8
            { (MakeBitmaskCount(0x00ff0000u, 0x0000ff00u, 0x000000ffu, 0xffffffffu), 24), IntermediateTextureFormat.RGB8 }, //RGB8 (alt)
            { (MakeBitmaskCount(0xff000000u, 0x00ff0000u, 0x0000ff00u, 0x000000ffu), 32), IntermediateTextureFormat.RGBA8 }, //RGBA8
            { (MakeBitmaskCount(0x00ff0000u, 0x0000ff00u, 0x000000ffu, 0xff000000u), 32), IntermediateTextureFormat.RGBA8 }, //RGB8 (alt)
            { (MakeBitmaskCount(0xff000000u, 0xffffffffu, 0xffffffffu, 0xffffffffu), 16), IntermediateTextureFormat.R16 }, //R16
            { (MakeBitmaskCount(0xff000000u, 0x00ff0000u, 0xffffffffu, 0xffffffffu), 32), IntermediateTextureFormat.RG16 }, //RG16
            { (MakeBitmaskCount(0xff000000u, 0x00ff0000u, 0x0000ff00u, 0x000000ffu), 64), IntermediateTextureFormat.RGBA16 }, //RGBA16
            { (MakeBitmaskCount(0xff000000u, 0xffffffffu, 0xffffffffu, 0xffffffffu), 32), IntermediateTextureFormat.R32 }, //R32
            { (MakeBitmaskCount(0xff000000u, 0x00ff0000u, 0xffffffffu, 0xffffffffu), 64), IntermediateTextureFormat.RG32 }, //RG32
            { (MakeBitmaskCount(0xff000000u, 0x00ff0000u, 0x0000ff00u, 0x000000ffu), 128), IntermediateTextureFormat.RGBA32 }, //RGBA32
            { (MakeBitmaskCount(0x000000ffu, 0xffffffffu, 0xffffffffu, 0xffffffffu), 8), IntermediateTextureFormat.R8a }, //R8a
        }.ToFrozenDictionary();

        private enum IntermediateTextureFormat : byte
        {
            Undefined = 0,

            BC7,
            BC6s,
            BC6u,
            BC5u,
            BC4u,
            BC3,
            BC3n,
            BC2,
            BC1a,
            BC1,
            R8a,
            R8l,
            BGR8,
            BGRA8,
            BGRX8,
            RGB8,
            RGBA8,
            R16,
            RG16,
            RGBA16,
            R32,
            RG32,
            RGBA32
        }
        #endregion
    }
}
