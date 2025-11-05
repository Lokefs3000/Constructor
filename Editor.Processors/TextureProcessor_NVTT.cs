using Editor.Interop.NVTT;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.Mathematics;
using Primary.Utility;
using Serilog;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Processors
{
    public class TextureProcessor : IAssetProcessor
    {
        public static ILogger? Logger
        {
            get => s_logger;
            set
            {
                unsafe
                {
                    s_logger = value;

                    //if (value != null)
                    //    NVTT.SetMessageCallback(&MessageReporter, null);
                    //else
                    //    NVTT.SetMessageCallback(null, null);
                }
            }
        }
        private static ILogger? s_logger = null;

        private ILogger? _logger;

        public bool Execute(object args_in) => throw new NotImplementedException();
        public unsafe bool Execute(object args_in, TextureCompositeArgs? compositeArgs, TextureCubemapArgs? cubemapArgs)
        {
            TextureProcessorArgs args = (TextureProcessorArgs)args_in;

            if (args.Sources.Length == 0)
            {
                args.Logger?.Error("[NVTT]: No sources specified!");
                return false;
            }

            NvttSurface*[]? surfaces = null;
            NvttCompressionOptions* compressionOptions = null;
            NvttOutputOptions* outputOptions = null;
            NvttContext* context = null;

            try
            {
                unsafe
                {
                    //NVTT.SetMessageCallback(&MessageReporter, null);
                }

                NvttBoolean hasAlpha = NvttBoolean.False;
                byte alphaBits = 8;


                if (compositeArgs.HasValue)
                {
                    TextureCompositeArgs composite = compositeArgs.Value;

                    NvttSurface*[] tempSurfaces = new NvttSurface*[Math.Min(args.Sources.Length, 4)];
                    TextureSwizzleChannel[] channels = new TextureSwizzleChannel[4];

                    Array.Clear(tempSurfaces);
                    Array.Fill(channels, TextureSwizzleChannel.Zero);

                    int minTextureSizeX = int.MaxValue;
                    int minTextureSizeY = int.MaxValue;

                    try
                    {
                        for (int i = 0, j = 0; i < 4; i++)
                        {
                            if (FlagUtility.HasFlag(composite.Channels, (TextureCompositeChannel)(1 << i)))
                            {
                                if (j >= args.Sources.Length)
                                {
                                    args.Logger?.Error("[NVTT]: Not enough sources returned for composite: {c}", (TextureCompositeChannel)(1 << i));
                                    return false;
                                }

                                string absoluteFilepath = args.Sources[j].AbsoluteFilepath;

                                Span<byte> imageBytes;
                                using (FileStream stream = NullableUtility.AlwaysThrowIfNull(FileUtility.TryWaitOpen(absoluteFilepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                                {
                                    //TODO: use ArrayPool!!
                                    imageBytes = new Span<byte>(new byte[stream.Length]);
                                    stream.ReadExactly(imageBytes);
                                }

                                NvttSurface* tempSurf = NVTT.nvttCreateSurface();
                                if (NVTT.nvttSurfaceLoadFromMemory(tempSurf, Unsafe.AsPointer(ref imageBytes[0]), (ulong)imageBytes.Length, &hasAlpha, NvttBoolean.False, null) != NvttBoolean.True)
                                {
                                    args.Logger?.Error("[NVTT]: Failed to load composite channel file: {af} (channel: {c})", absoluteFilepath, (TextureCompositeChannel)(1 << i));
                                    return false;
                                }

                                minTextureSizeX = Math.Min(minTextureSizeX, NVTT.nvttSurfaceWidth(tempSurf));
                                minTextureSizeY = Math.Min(minTextureSizeY, NVTT.nvttSurfaceHeight(tempSurf));

                                tempSurfaces[j] = tempSurf;
                                channels[i] = (TextureSwizzleChannel)j;

                                j++;
                            }
                        }

                        surfaces = [NVTT.nvttCreateSurface()];
                        NvttSurface* surface = surfaces[0];

                        if (NVTT.nvttSurfaceSetImage(surface, minTextureSizeX, minTextureSizeY, 1, null) == NvttBoolean.False)
                        {
                            args.Logger?.Error("[NVTT]: Failed to create composite texture");
                            NVTT.nvttDestroySurface(surface);
                            return false;
                        }

                        if (tempSurfaces.Length > 3)
                        {
                            hasAlpha = NvttBoolean.True;
                            args.AlphaSource = TextureAlphaSource.Source;
                            NVTT.nvttSetSurfaceAlphaMode(surface, NvttAlphaMode.Transparency);

                            if (args.ImageFormat == TextureImageFormat.Undefined)
                                args.ImageFormat = TextureImageFormat.BC3; //TODO: 'BC3' (DXT5) might not have a good enough alpha channel for this
                        }
                        else
                        {
                            hasAlpha = NvttBoolean.False;
                            args.AlphaSource = TextureAlphaSource.None;
                            NVTT.nvttSetSurfaceAlphaMode(surface, NvttAlphaMode.None);
                        }

                        for (int i = 0; i < tempSurfaces.Length; i++)
                        {
                            Checking.Assert(tempSurfaces[i] != null);

                            int sourceChannel = args.Sources[i].Channel switch
                            {
                                TextureSwizzleChannel.R => 0,
                                TextureSwizzleChannel.G => 1,
                                TextureSwizzleChannel.B => 2,
                                TextureSwizzleChannel.A => 3,
                                _ => throw new NotImplementedException()
                            };

                            switch (i)
                            {
                                case 0: args.TextureSwizzle.R = RemapSwizzleChannel(args.TextureSwizzle.R); break;
                                case 1: args.TextureSwizzle.G = RemapSwizzleChannel(args.TextureSwizzle.G); break;
                                case 2: args.TextureSwizzle.B = RemapSwizzleChannel(args.TextureSwizzle.B); break;
                                case 3: args.TextureSwizzle.A = RemapSwizzleChannel(args.TextureSwizzle.A); break;
                            }

                            NvttSurface* sourceSurface = tempSurfaces[i];
                            if (minTextureSizeX == NVTT.nvttSurfaceWidth(sourceSurface) && minTextureSizeY == NVTT.nvttSurfaceHeight(sourceSurface))
                            {
                                float* srcChannelData = NVTT.nvttSurfaceChannel(sourceSurface, sourceChannel);
                                float* dstChannelData = NVTT.nvttSurfaceChannel(surface, i);

                                NativeMemory.Copy(srcChannelData, dstChannelData, (nuint)(minTextureSizeX * minTextureSizeY * sizeof(float)));
                            }
                            else
                            {
                                float* srcChannelData = NVTT.nvttSurfaceChannel(sourceSurface, sourceChannel);
                                float* dstChannelData = NVTT.nvttSurfaceChannel(surface, i);

                                int srcRowPitch = NVTT.nvttSurfaceWidth(sourceSurface);
                                int dstRowPitch = minTextureSizeX;

                                for (int row = 0; row < minTextureSizeY; row++)
                                {
                                    float* src = srcChannelData + srcRowPitch * row;
                                    float* dst = dstChannelData + dstRowPitch * row;

                                    NativeMemory.Copy(src, dst, (nuint)(dstRowPitch * sizeof(float)));
                                }
                            }
                        }

                        TextureSwizzleChannel RemapSwizzleChannel(TextureSwizzleChannel channel)
                        {
                            if ((int)channel >= channels.Length)
                                return channel;

                            switch (channel)
                            {
                                case TextureSwizzleChannel.R: return channels[0];
                                case TextureSwizzleChannel.G: return channels[1];
                                case TextureSwizzleChannel.B: return channels[2];
                                case TextureSwizzleChannel.A: return channels[3];
                                default: return channel;
                            }
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < tempSurfaces.Length; i++)
                        {
                            if (tempSurfaces[i] != null)
                                NVTT.nvttDestroySurface(tempSurfaces[i]);
                        }
                    }
                }
                else if (cubemapArgs.HasValue)
                {
                    if (args.Sources.Length != 6)
                    {
                        args.Logger?.Warning("[NVTT]: Incorrect number of sources specified for cubemap.");
                        return false;
                    }

                    TextureCubemapArgs cubemap = cubemapArgs.Value;

                    surfaces = new NvttSurface*[6];
                    Array.Clear(surfaces);

                    int generalSizeX = 0;
                    int generalSizeY = 0;

                    for (int i = 0; i < args.Sources.Length; i++)
                    {
                        Span<byte> imageBytes;
                        using (FileStream stream = NullableUtility.AlwaysThrowIfNull(FileUtility.TryWaitOpen(args.Sources[i].AbsoluteFilepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                        {
                            //TODO: use ArrayPool!!
                            imageBytes = new Span<byte>(new byte[stream.Length]);
                            stream.ReadExactly(imageBytes);
                        }

                        surfaces[i] = NVTT.nvttCreateSurface();
                        if (NVTT.nvttSurfaceLoadFromMemory(surfaces[i], Unsafe.AsPointer(ref imageBytes[0]), (ulong)imageBytes.Length, &hasAlpha, NvttBoolean.False, null) != NvttBoolean.True)
                        {
                            //report error
                            args.Logger?.Error("[NVTT]: Failed to load surface from image bytes: {af}", args.Sources[i].AbsoluteFilepath);
                            return false;
                        }

                        if (i == 0)
                        {
                            generalSizeX = NVTT.nvttSurfaceWidth(surfaces[i]);
                            generalSizeY = NVTT.nvttSurfaceHeight(surfaces[i]);

                            if (generalSizeX != generalSizeY)
                            {
                                args.Logger?.Error("[NVTT]: Cubemap texture must be square: {af}", args.Sources[i].AbsoluteFilepath);
                                return false;
                            }

                            if (!int.IsPow2(generalSizeX))
                            {
                                args.Logger?.Error("[NVTT]: Cubemap texture must be a power of 2: {af}", args.Sources[i].AbsoluteFilepath);
                                return false;
                            }
                        }
                        else
                        {
                            int sizeX = NVTT.nvttSurfaceWidth(surfaces[i]);
                            int sizeY = NVTT.nvttSurfaceHeight(surfaces[i]);

                            if (sizeX != generalSizeX || sizeY != generalSizeY)
                            {
                                args.Logger?.Error("[NVTT]: Cubemap texture has an inconsistent size: {af}", args.Sources[i].AbsoluteFilepath);
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    if (args.Sources.Length > 1)
                    {
                        args.Logger?.Warning("[NVTT]: More then one source specified on non composite texture. Excess will go unused.");
                    }

                    string absoluteFilepath = args.Sources[0].AbsoluteFilepath;

                    Span<byte> imageBytes;
                    using (FileStream stream = NullableUtility.AlwaysThrowIfNull(FileUtility.TryWaitOpen(absoluteFilepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        //TODO: use ArrayPool!!
                        imageBytes = new Span<byte>(new byte[stream.Length]);
                        stream.ReadExactly(imageBytes);
                    }

                    surfaces = [NVTT.nvttCreateSurface()];
                    if (NVTT.nvttSurfaceLoadFromMemory(surfaces[0], Unsafe.AsPointer(ref imageBytes[0]), (ulong)imageBytes.Length, &hasAlpha, NvttBoolean.False, null) != NvttBoolean.True)
                    {
                        //report error
                        args.Logger?.Error("[NVTT]: Failed to load surface from image bytes: {af}", absoluteFilepath);
                        return false;
                    }
                }

                compressionOptions = NVTT.nvttCreateCompressionOptions();
                NVTT.nvttSetCompressionOptionsQuality(compressionOptions, NvttQuality.Normal);

                outputOptions = NVTT.nvttCreateOutputOptions();
                NVTT.nvttErrorHandlerDelegate @delegate = ErrorReporter;
                NVTT.nvttSetOutputOptionsErrorHandler(outputOptions, ErrorReporter);

                TextureImageFormat imageFormat = args.ImageFormat;
                if (imageFormat == TextureImageFormat.Undefined)
                {
                    switch (args.ImageType)
                    {
                        case TextureImageType.Color:
                        case TextureImageType.Cubemap:
                            {
                                switch (args.AlphaSource)
                                {
                                    case TextureAlphaSource.None:
                                    case TextureAlphaSource.Opaque: imageFormat = TextureImageFormat.BC1; break;
                                    case TextureAlphaSource.Source:
                                    case TextureAlphaSource.Red: imageFormat = TextureImageFormat.BC3; break;
                                }
                                break;
                            }
                        case TextureImageType.Grayscale:
                            {
                                switch (args.AlphaSource)
                                {
                                    case TextureAlphaSource.None:
                                    case TextureAlphaSource.Opaque: imageFormat = TextureImageFormat.BC4u; break;
                                    case TextureAlphaSource.Source:
                                    case TextureAlphaSource.Red: imageFormat = TextureImageFormat.BC3; break;
                                }
                                break;
                            }
                        case TextureImageType.Normal: imageFormat = TextureImageFormat.BC3n; break;
                        case TextureImageType.Specular:
                            {
                                TextureProcessorSpecularArgs specularArgs = args.ImageMetadata.SpecularArgs;
                                switch (specularArgs.Source)
                                {
                                    case TextureSpecularSource.Colored: imageFormat = TextureImageFormat.BC3; break;
                                    case TextureSpecularSource.Grayscale: imageFormat = TextureImageFormat.BC4u; break;
                                    case TextureSpecularSource.Roughness: imageFormat = TextureImageFormat.BC4u; break;
                                }

                                break;
                            }
                    }
                }

                switch (imageFormat)
                {
                    case TextureImageFormat.BC7: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC7); break;
                    case TextureImageFormat.BC6s: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC6S); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); break;
                    case TextureImageFormat.BC6u: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC6U); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedFloat); break;
                    //case TextureImageFormat.ASTC: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.ASTC_LDR_4x4); break;
                    case TextureImageFormat.BC5u: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC5); break;
                    case TextureImageFormat.BC4u: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC4); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); break;
                    case TextureImageFormat.BC3: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC3); break;
                    case TextureImageFormat.BC3n: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC3n); break;
                    //case ImageFormat.BC3n_agbr: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.(BC3_RGBM); break;
                    case TextureImageFormat.BC2:
                        NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC2);
                        if (args.CutoutDither)
                        {
                            byte bits = 4;
                            if (alphaBits != bits)
                                bits = alphaBits;
                            NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, bits);
                            NVTT.nvttSetCompressionOptionsQuantization(compressionOptions, NvttBoolean.False, NvttBoolean.True, NvttBoolean.False, 0);
                        }
                        break;
                    case TextureImageFormat.BC1a: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC1a); NVTT.nvttSetCompressionOptionsQuantization(compressionOptions, NvttBoolean.False, args.CutoutDither ? NvttBoolean.True : NvttBoolean.False, NvttBoolean.False, 0); break;
                    case TextureImageFormat.BC1: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC1); break;
                    case TextureImageFormat.R8a: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 8, 0xff, 0, 0, 0); break;
                    case TextureImageFormat.R8l: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 8, 0xff, 0, 0, 0); break;
                    case TextureImageFormat.BGR8: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGB); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, 0); break;
                    case TextureImageFormat.BGRA8: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, 8); break;
                    case TextureImageFormat.BGRX8: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, 8); break;
                    case TextureImageFormat.RGB8: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGB); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, 0); break;
                    case TextureImageFormat.RGBA8: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedNorm); /*NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, 8);*/ break;
                    //case TextureImageFormat.R16f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 16, 0, 0, 0); break;
                    //case TextureImageFormat.RG16f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 16, 16, 0, 0); break;
                    //case TextureImageFormat.RGBA16f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 16, 16, 16, 16); break;
                    //case TextureImageFormat.R32f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 32, 0, 0, 0); break;
                    //case TextureImageFormat.RG32f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 32, 32, 0, 0); break;
                    //case TextureImageFormat.RGBA32f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 32, 32, 32, 32); break;
                    default: break;
                }

                NVTT.nvttSetOutputOptionsContainer(outputOptions, args.ImageFormat <= TextureImageFormat.BC6u ? NvttContainer.DDS10 : NvttContainer.DDS);

                if (args.CutoutDither && hasAlpha == NvttBoolean.True && args.ImageFormat != TextureImageFormat.BC2 && args.ImageFormat != TextureImageFormat.BC1a)
                {
                    //NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 8, 8, 8, 8, alphaBits);
                    NVTT.nvttSetCompressionOptionsQuantization(compressionOptions, NvttBoolean.False, NvttBoolean.True, NvttBoolean.False, args.CutoutThreshold);
                }

                Checking.Assert(surfaces != null);

                int mip0Width = NVTT.nvttSurfaceWidth(surfaces![0]);
                int mip0Height = NVTT.nvttSurfaceHeight(surfaces![0]);
                int mip0Depth = NVTT.nvttSurfaceDepth(surfaces![0]);

                int mipmapCount = args.GenerateMipmaps ? (int)Math.Log2(Math.Max(mip0Width, mip0Height)) + 1 : 1;
                int arraySize = cubemapArgs.HasValue ? 6 : 1;

                TextureHeader header = new TextureHeader
                {
                    FileHeader = TextureHeader.Header,
                    FileVersion = TextureHeader.Version,

                    Width = (ushort)mip0Width,
                    Height = (ushort)mip0Height,
                    Depth = (ushort)mip0Depth,

                    Format = (TextureFormat)(imageFormat - 1),
                    Flags = TextureFlags.None,

                    MipLevels = (ushort)mipmapCount,
                    ArraySize = (ushort)arraySize,

                    Swizzle = new TextureSwizzle(args.TextureSwizzle.Code),
                };

                if (args.ImageType == TextureImageType.Cubemap)
                    header.Flags |= TextureFlags.Cubemap;

                using OutputHandler outputHandler = new OutputHandler(args.AbsoluteOutputPath, header);

                NVTT.nvttBeginImageDelegate beginImageDelegate = outputHandler.nvttBeginImage;
                NVTT.nvttWriteDataWriteData writeDataWriteData = outputHandler.nvttWriteData;
                NVTT.nvttEndImageDelegate endImageDelegate = outputHandler.nvttEndImage;

                NVTT.nvttSetOutputOptionsOutputHandler(outputOptions, beginImageDelegate, writeDataWriteData, endImageDelegate);

                context = NVTT.nvttCreateContext();
                for (int i = 0; i < surfaces.Length; i++)
                {
                    CompressSingle(ref args, context, surfaces[i], outputOptions, compressionOptions, hasAlpha, alphaBits, imageFormat);
                }

                GC.KeepAlive(beginImageDelegate);
                GC.KeepAlive(writeDataWriteData);
                GC.KeepAlive(endImageDelegate);
                return true;
            }
            finally
            {
                if (surfaces != null)
                {
                    for (int i = 0; i < surfaces.Length; i++)
                    {
                        if (surfaces[i] != null)
                            NVTT.nvttDestroySurface(surfaces[i]);
                    }
                }

                if (outputOptions != null)
                    NVTT.nvttDestroyOutputOptions(outputOptions);
                if (compressionOptions != null)
                    NVTT.nvttDestroyCompressionOptions(compressionOptions);

                if (context != null)
                    NVTT.nvttDestroyContext(context);
            }
        }

        private unsafe bool CompressSingle(ref TextureProcessorArgs args, NvttContext* context, NvttSurface* surface, NvttOutputOptions* outputOptions, NvttCompressionOptions* compressionOptions, NvttBoolean hasAlpha, byte alphaBits, TextureImageFormat imageFormat)
        {
            NvttBatchList* batchList = null;
            List<nint> surfaces = new List<nint>();

            try
            {
                int mip0Width = NVTT.nvttSurfaceWidth(surface);
                int mip0Height = NVTT.nvttSurfaceHeight(surface);
                int mip0Depth = NVTT.nvttSurfaceDepth(surface);

                if (hasAlpha == NvttBoolean.True)
                {
                    switch (args.AlphaSource)
                    {
                        case TextureAlphaSource.None:
                        case TextureAlphaSource.Opaque:
                            {
                                NVTT.nvttSetSurfaceAlphaMode(surface, NvttAlphaMode.None);
                                hasAlpha = NvttBoolean.False;
                                break;
                            }
                        case TextureAlphaSource.Red:
                            {
                                NVTT.nvttSetSurfaceAlphaMode(surface, NvttAlphaMode.Transparency);

                                float* red = NVTT.nvttSurfaceChannel(surface, 0);
                                float* alpha = NVTT.nvttSurfaceChannel(surface, 3);

                                NativeMemory.Copy(red, alpha, (nuint)(mip0Width * mip0Height * mip0Depth * sizeof(float)));
                                break;
                            }
                    }
                }
                else
                {
                    if (args.AlphaSource == TextureAlphaSource.Red)
                    {
                        NVTT.nvttSetSurfaceAlphaMode(surface, NvttAlphaMode.Transparency);

                        float* red = NVTT.nvttSurfaceChannel(surface, 0);
                        float* alpha = NVTT.nvttSurfaceChannel(surface, 3);

                        NativeMemory.Copy(red, alpha, (nuint)(mip0Width * mip0Height * mip0Depth * sizeof(float)));
                    }
                }

                if (!args.FlipVertical)
                {
                    NVTT.nvttSurfaceFlipY(surface, null);
                }

                if (args.GammaCorrect)
                    NVTT.nvttSurfaceToSrgbUnclamped(surface, null);

                NvttAlphaMode alphaMode = NVTT.nvttSurfaceAlphaMode(surface);

                alphaMode = (args.PremultipliedAlpha && args.AlphaSource > TextureAlphaSource.Opaque) ? NvttAlphaMode.Premultiplied : alphaMode;
                if (args.ImageFormat == TextureImageFormat.BC6u || args.ImageFormat == TextureImageFormat.BC6s)
                    alphaMode = NvttAlphaMode.None;

                NVTT.nvttSetSurfaceAlphaMode(surface, alphaMode);

                if (imageFormat == TextureImageFormat.R8a && alphaMode != NvttAlphaMode.None)
                {
                    switch (args.AlphaSource)
                    {
                        case TextureAlphaSource.Source:
                            {
                                float* red = NVTT.nvttSurfaceChannel(surface, 0);
                                float* alpha = NVTT.nvttSurfaceChannel(surface, 3);

                                NativeMemory.Copy(alpha, red, (nuint)(mip0Width * mip0Height * mip0Depth * sizeof(float)));
                                break;
                            }
                    }
                }

                NvttMipmapFilter filter = NvttMipmapFilter.Box;
                switch (args.MipmapFilter)
                {
                    case TextureMipmapFilter.Box: filter = NvttMipmapFilter.Box; break;
                    case TextureMipmapFilter.Kaiser: filter = NvttMipmapFilter.Kaiser; break;
                    case TextureMipmapFilter.Triangle: filter = NvttMipmapFilter.Triangle; break;
                    case TextureMipmapFilter.Mitchell: filter = NvttMipmapFilter.Mitchell; break;
                    case TextureMipmapFilter.Min: filter = NvttMipmapFilter.Min; break;
                    case TextureMipmapFilter.Max: filter = NvttMipmapFilter.Max; break;
                }

                List<(int width, int height)> sizes = new List<(int, int)>();

                int mipmapCount = 0;
                if (args.GenerateMipmaps)
                {
                    while (mipmapCount < args.MaxMipmapCount)
                    {
                        int mipWidth = Math.Max(1, mip0Width >> mipmapCount);
                        int mipHeight = Math.Max(1, mip0Height >> mipmapCount);
                        sizes.Add((mipWidth, mipHeight));

                        if (mipWidth <= args.MinMipmapSize || mipHeight <= args.MinMipmapSize)
                        {
                            break;
                        }

                        mipmapCount++;
                    }
                }

                mipmapCount = Math.Max(1, mipmapCount);

                switch (args.ImageType)
                {
                    case TextureImageType.Color: break;
                    case TextureImageType.Grayscale: NVTT.nvttSurfaceToGreyScale(surface, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, null); break;
                    case TextureImageType.Normal: NVTT.nvttSetSurfaceNormalMap(surface, NvttBoolean.True); break;
                    case TextureImageType.Specular:
                        {
                            TextureProcessorSpecularArgs specularArgs = args.ImageMetadata.SpecularArgs;
                            switch (specularArgs.Source)
                            {
                                case TextureSpecularSource.Grayscale: NVTT.nvttSurfaceToGreyScale(surface, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, null); break;
                                case TextureSpecularSource.Roughness:
                                    {
                                        int channelCount = (alphaMode > NvttAlphaMode.None && hasAlpha == NvttBoolean.True) ? 4 : 3;
                                        int totalPixels = mip0Width * mip0Height;

                                        for (int i = 0; i < channelCount; i++)
                                        {
                                            float* channel = NVTT.nvttSurfaceChannel(surface, i);
                                            for (int j = 0; j < totalPixels; j++)
                                            {
                                                channel[j] = 1.0f - channel[j];
                                            }
                                        }

                                        break;
                                    }
                            }

                            break;
                        }
                }

                batchList = NVTT.nvttCreateBatchList();

                if (NVTT.nvttSurfaceIsNormalMap(surface) == NvttBoolean.True)
                {
                    TextureProcessorNormalArgs normalArgs = args.ImageMetadata.NormalArgs;
                    if (normalArgs.Source == TextureNormalSource.Object)
                    {
                        NVTT.nvttSurfaceToGreyScale(surface, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, 0.0f, null);
                        NVTT.nvttSurfaceToNormalMap(surface, 1.0f / 1.875f, 0.5f / 1.875f, 0.25f / 1.875f, 0.125f / 1.875f, null);
                    }
                    else if (normalArgs.Source == TextureNormalSource.Bump)
                    {
                        //NVTT.nvttSurfaceToGreyScale(surface, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, 0.0f, null);
                        //NVTT.nvttSurfaceToNormalMap(surface, 1.0f / 1.875f, 0.5f / 1.875f, 0.25f / 1.875f, 0.125f / 1.875f, null);

                        NvttSurface* referenceCopy = NVTT.nvttSurfaceClone(surface);
                        try
                        {
                            float* srcData = NVTT.nvttSurfaceChannel(referenceCopy, 0);

                            float* dstData0 = NVTT.nvttSurfaceChannel(surface, 0);
                            float* dstData1 = NVTT.nvttSurfaceChannel(surface, 1);
                            float* dstData2 = NVTT.nvttSurfaceChannel(surface, 2);

                            Int2 oneLessSize = new Int2(mip0Width, mip0Height) - Int2.One;
                            Vector2 step = Vector2.One / new Vector2(mip0Width, mip0Height);

                            float maxHeight = 0.0f;

                            for (int y = 0; y < mip0Height; y++)
                            {
                                int ySlice = y * mip0Width;
                                for (int x = 0; x < mip0Width; x++)
                                {
                                    Int2 posPu = new Int2(x + 1, y);
                                    Int2 posMu = new Int2(x - 1, y);
                                    Int2 posPv = new Int2(x, y + 1);
                                    Int2 posMv = new Int2(x, y - 1);

                                    posPu.X = (posPu.X < 0) ? oneLessSize.X : ((posPu.X > oneLessSize.X) ? 0 : posPu.X);
                                    posMu.X = (posMu.X < 0) ? oneLessSize.X : ((posMu.X > oneLessSize.X) ? 0 : posMu.X);

                                    posPv.Y = (posPv.Y < 0) ? oneLessSize.Y : ((posPv.Y > oneLessSize.Y) ? 0 : posPv.Y);
                                    posMv.Y = (posMv.Y < 0) ? oneLessSize.Y : ((posMv.Y > oneLessSize.Y) ? 0 : posMv.Y);

                                    float height = srcData[x + ySlice];

                                    Vector2 dxy = new Vector2(height) - new Vector2(
                                        srcData[posPu.X + ySlice],
                                        srcData[posPv.X + posPv.Y * mip0Width]);

                                    Vector2 n = dxy;
                                    if (dxy.X != 0.0f && dxy.Y != 0.0f)
                                    {
                                        Vector3 tempNorm = Vector3.Normalize(new Vector3(dxy, 1.0f));
                                        n = new Vector2(tempNorm.X, tempNorm.Y);

                                        Vector2 absN = Vector2.Abs(n);
                                        maxHeight = MathF.Max(MathF.Max(absN.X, absN.Y), maxHeight);
                                    }

                                    dstData0[x + ySlice] = n.X;
                                    dstData1[x + ySlice] = n.Y;
                                    dstData2[x + ySlice] = 1.0f;

                                    //float samplePu = srcData[posPu.X + ySlice];
                                    //float sampleMu = srcData[posMu.X + ySlice];
                                    //float samplePv = srcData[posPv.X + posPv.Y * mip0Width];
                                    //float sampleMv = srcData[posMv.X + posMv.Y * mip0Width];
                                    //
                                    //float du = sampleMu - samplePu;
                                    //float dv = sampleMv - samplePv;
                                    //
                                    //Vector3 n = Vector3.Normalize(new Vector3(du, dv, 1.0f));
                                    //
                                    //dstVec3[x + ySlice] = n * 0.5f + new Vector3(0.5f);
                                }
                            }

                            int total = mip0Width * mip0Height;
                            for (int i = 0; i < total; i++)
                            {
                                ref float nx = ref dstData0[i];
                                ref float ny = ref dstData1[i];

                                Vector2 n = new Vector2(nx, ny) * 0.5f + new Vector2(0.5f);

                                nx = n.X;
                                ny = n.Y;
                            }
                        }
                        finally
                        {
                            NVTT.nvttDestroySurface(referenceCopy);
                        }
                    }
                }
                else
                {
                    if (mipmapCount > 1 && args.GammaCorrect)
                    {
                        NVTT.nvttSurfaceToLinearFromSrgbUnclamped(surface, null);
                    }
                }

                NvttSurface* temp = NVTT.nvttSurfaceClone(surface);
                if (NVTT.nvttSurfaceIsNormalMap(temp) == NvttBoolean.False && mipmapCount > 1 && args.GammaCorrect)
                {
                    NVTT.nvttSurfaceToSrgbUnclamped(surface, null);
                }

                NVTT.nvttSetContextCudaAcceleration(context, NvttBoolean.False);
                NVTT.nvttContextQuantize(context, temp, compressionOptions);

                NvttSurface* surf = temp;

                NVTT.nvttBatchListAppend(batchList, surf, 0, 0, outputOptions);
                surfaces.Add((nint)surf);

                bool scaleMipAlpha = hasAlpha == NvttBoolean.True && mipmapCount > 1 && args.ScaleAlphaForMipmaps;
                float threshold = args.CutoutThreshold / 255.0f;
                float mip0Coverage = scaleMipAlpha ? NVTT.nvttSurfaceAlphaTestCoverage(surface, threshold, 3) : 0.0f;

                float* kaiserParams = stackalloc float[] { 1.0f, 4.0f };

                for (int m = 0; m < mipmapCount; m++)
                {
                    if (filter == NvttMipmapFilter.Kaiser)
                    {
                        NVTT.nvttSurfaceBuildNextMipmap(surface, NvttMipmapFilter.Kaiser, 3.0f, kaiserParams, 1, null);
                    }
                    else
                    {
                        NVTT.nvttSurfaceBuildNextMipmapDefaults(surface, filter, 1, null);
                    }

                    if (scaleMipAlpha)
                    {
                        NVTT.nvttSurfaceScaleAlphaToCoverage(surface, mip0Coverage, threshold, 3, null);
                    }

                    if (NVTT.nvttSurfaceIsNormalMap(surface) == NvttBoolean.True)
                    {
                        temp = NVTT.nvttSurfaceClone(surface);
                    }
                    else
                    {
                        temp = NVTT.nvttSurfaceClone(surface);
                        if (args.GammaCorrect)
                        {
                            NVTT.nvttSurfaceToSrgbUnclamped(temp, null);
                        }
                    }

                    NVTT.nvttContextQuantize(context, temp, compressionOptions);

                    surf = temp;

                    NVTT.nvttBatchListAppend(batchList, surf, 0, 0, outputOptions);
                    surfaces.Add((nint)surf);
                }

                if (NVTT.nvttContextCompressBatch(context, batchList, compressionOptions) != NvttBoolean.True)
                {
                    return false;
                }

                return true;
            }
            finally
            {
                if (batchList != null)
                    NVTT.nvttDestroyBatchList(batchList);

                for (int i = 0; i < surfaces.Count; i++)
                {
                    if (surfaces[i] != nint.Zero)
                        NVTT.nvttDestroySurface((NvttSurface*)surfaces[i]);
                }
            }
        }

        private unsafe void ErrorReporter(NvttError error)
        {
            byte* str = (byte*)NVTT.nvttErrorString(error);

            int length = 0;
            while (str[length++] != 0) ;
            string text = Encoding.UTF8.GetString(str, length);

            _logger?.Error("[NVTT]: {err}", text);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe void MessageReporter(NvttSeverity severity, NvttError error, sbyte* desc, void* userData)
        {
            GCHandle handle = GCHandle.FromIntPtr((nint)userData);
            TextureProcessor processor = NullableUtility.ThrowIfNull(handle.Target as TextureProcessor);

            byte* str = (byte*)NVTT.nvttErrorString(error);
            byte* ptrDesc = (byte*)desc;

            int length = 0;
            while (str[length++] != 0) ;
            string text = Encoding.UTF8.GetString(str, length);

            length = 0;
            while (ptrDesc[length++] != 0) ;
            string description = Encoding.UTF8.GetString(ptrDesc, length);

            switch (severity)
            {
                case NvttSeverity.Info: processor._logger?.Information("[NVTT]: {err}: {dsc}", text, description); break;
                case NvttSeverity.Warning: processor._logger?.Warning("[NVTT]: {err}: {dsc}", text, description); break;
                case NvttSeverity.Error: processor._logger?.Error("[NVTT]: {err}: {dsc}", text, description); break;
            }
        }

        public class OutputHandler : IDisposable
        {
            private Stream? _stream;
            private bool _skip;

            private TextureHeader _header;

            public OutputHandler(string path, TextureHeader header)
            {
                try
                {
                    _stream = FileUtility.TryWaitOpen(path, FileMode.Create, FileAccess.Write, FileShare.None);
                }
                catch (Exception ex)
                {
                    //bad
                }

                _header = header;
                _stream?.Write(header);
            }

            public void Dispose()
            {
                _stream?.Dispose();
            }

            public void nvttBeginImage(int size, int width, int height, int depth, int face, int miplevel)
            {

            }

            public unsafe NvttBoolean nvttWriteData(void* data, int size)
            {
                if (_skip)
                    return NvttBoolean.True;

                if (_stream != null)
                {
                    _stream.Write(new ReadOnlySpan<byte>(data, size));
                    return NvttBoolean.True;
                }

                return NvttBoolean.False;
            }

            public void nvttEndImage()
            {

            }

            public bool Skip { get => _skip; set => _skip = value; }
        }
    }

    public record struct TextureProcessorArgs
    {
        public Source[] Sources;
        public string AbsoluteOutputPath;

        public ILogger? Logger;

        public TextureImageType ImageType;
        public Metadata ImageMetadata;

        public Swizzle TextureSwizzle;

        public TextureImageFormat ImageFormat;
        public TextureAlphaSource AlphaSource;

        public bool GammaCorrect;
        public bool PremultipliedAlpha;

        public bool CutoutDither;
        public byte CutoutThreshold;

        public bool GenerateMipmaps;
        public bool ScaleAlphaForMipmaps;
        public int MaxMipmapCount;
        public int MinMipmapSize;
        public TextureMipmapFilter MipmapFilter;

        public bool FlipVertical;

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        public record struct Metadata
        {
            [FieldOffset(0)]
            public TextureProcessorNormalArgs NormalArgs;

            [FieldOffset(0)]
            public TextureProcessorSpecularArgs SpecularArgs;
        }

        public record struct Swizzle
        {
            public ushort Code;

            public Swizzle()
                => this = Default;
            public Swizzle(TextureSwizzleChannel r, TextureSwizzleChannel g, TextureSwizzleChannel b, TextureSwizzleChannel a)
                => Code = (ushort)(((int)r << 9) | ((int)g << 6) | ((int)b << 3) | (int)a);

            public Swizzle(ushort code)
                => Code = code;

            public TextureSwizzleChannel R { get => (TextureSwizzleChannel)((Code >> 9) & 0x7); set => Code = (ushort)((Code & ~(0x7 << 9)) | ((int)value << 9)); }
            public TextureSwizzleChannel G { get => (TextureSwizzleChannel)((Code >> 6) & 0x7); set => Code = (ushort)((Code & ~(0x7 << 6)) | ((int)value << 6)); }
            public TextureSwizzleChannel B { get => (TextureSwizzleChannel)((Code >> 3) & 0x7); set => Code = (ushort)((Code & ~(0x7 << 3)) | ((int)value << 3)); }
            public TextureSwizzleChannel A { get => (TextureSwizzleChannel)(Code & 0x7); set => Code = (ushort)((Code & ~0x7) | (int)value); }

            public static readonly Swizzle Default = new Swizzle(TextureSwizzleChannel.R, TextureSwizzleChannel.G, TextureSwizzleChannel.B, TextureSwizzleChannel.A);
        }

        public record struct Source
        {
            public TextureSwizzleChannel Channel;
            public string AbsoluteFilepath;
        }
    }

    public record struct TextureProcessorNormalArgs
    {
        public TextureNormalSource Source;
    }

    public record struct TextureProcessorSpecularArgs
    {
        public TextureSpecularSource Source;
    }

    public record struct TextureCompositeArgs
    {
        public TextureCompositeChannel Channels;

        public TextureCompositeChannelArgs Red;
        public TextureCompositeChannelArgs Green;
        public TextureCompositeChannelArgs Blue;
        public TextureCompositeChannelArgs Alpha;
    }

    public record struct TextureCompositeChannelArgs
    {
        public AssetId Asset;
        public TextureCompositeChannel Source;
        public bool Invert;

        public TextureCompositeChannelArgs()
        {
            Asset = AssetId.Invalid;
            Source = TextureCompositeChannel.Red;
            Invert = false;
        }
    }

    public record struct TextureCubemapArgs
    {
        public TextureCubemapSource Source;

        public AssetId PositiveX;
        public AssetId PositiveY;
        public AssetId PositiveZ;

        public AssetId NegativeX;
        public AssetId NegativeY;
        public AssetId NegativeZ;
    }

    public enum TextureImageFormat : byte
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

    public enum TextureAlphaSource : byte
    {
        None = 0,
        Opaque,
        Source,
        Red
    }

    public enum TextureMipmapFilter : byte
    {
        Box = 0,
        Kaiser,
        Triangle,
        Mitchell,
        Min,
        Max
    }

    public enum TextureImageType : byte
    {
        Color = 0,
        Grayscale,
        Normal,
        Specular,
        Cubemap
    }

    public enum TextureSwizzleChannel : byte
    {
        R = 0,
        G,
        B,
        A,
        Zero,
        One
    }

    public enum TextureNormalSource : byte
    {
        Tangent = 0,
        Object,
        Bump
    }

    public enum TextureSpecularSource : byte
    {
        Colored = 0,
        Grayscale,
        Roughness
    }

    public enum TextureCompositeChannel : byte
    {
        None = 0,

        Red = 1 << 0,
        Green = 1 << 1,
        Blue = 1 << 2,
        Alpha = 1 << 3,
    }

    public enum TextureCubemapSource : byte
    {
        Composited = 0
    }
}
