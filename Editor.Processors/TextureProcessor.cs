using Editor.Interop.NVTT;
using Primary.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Processors
{
    public class TextureProcessor : IAssetProcessor
    {
        public unsafe bool Execute(object args_in)
        {
            TextureProcessorArgs args = (TextureProcessorArgs)args_in;

            NVTT.nvttErrorHandlerDelegate errorHandlerDelegate = ErrorReporter;
            //NVTT.SetMessageCallback(errorHandlerDelegate, null);

            NvttBoolean hasAlpha = NvttBoolean.False;
            byte alphaBits = 8;

            Span<byte> imageBytes;
            using (FileStream stream = NullableUtility.AlwaysThrowIfNull(FileUtility.TryWaitOpen(args.AbsoluteFilepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                //TODO: use ArrayPool!!
                imageBytes = new Span<byte>(new byte[stream.Length]);
                stream.ReadExactly(imageBytes);
            }

            NvttSurface* surface = NVTT.nvttCreateSurface();
            if (NVTT.nvttSurfaceLoadFromMemory(surface, Unsafe.AsPointer(ref imageBytes[0]), (ulong)imageBytes.Length, &hasAlpha, NvttBoolean.False, null) != NvttBoolean.True)
            {
                //report error
                return false;
            }

            if (args.FlipVertical)
                NVTT.nvttSurfaceFlipY(surface, null);

            NvttCompressionOptions* compressionOptions = NVTT.nvttCreateCompressionOptions();
            NVTT.nvttSetCompressionOptionsQuality(compressionOptions, NvttQuality.Normal);

            NvttOutputOptions* outputOptions = NVTT.nvttCreateOutputOptions();
            NVTT.nvttSetOutputOptionsErrorHandler(outputOptions, errorHandlerDelegate);

            switch (args.ImageFormat)
            {
                case TextureImageFormat.BC7: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC7); break;
                case TextureImageFormat.BC6s: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC6S); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); break;
                case TextureImageFormat.BC6u: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.BC6U); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.UnsignedFloat); break;
                case TextureImageFormat.ASTC: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.ASTC_LDR_4x4); break;
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
                case TextureImageFormat.R16f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 16, 0, 0, 0); break;
                case TextureImageFormat.RG16f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 16, 16, 0, 0); break;
                case TextureImageFormat.RGBA16f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 16, 16, 16, 16); break;
                case TextureImageFormat.R32f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 32, 0, 0, 0); break;
                case TextureImageFormat.RG32f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 32, 32, 0, 0); break;
                case TextureImageFormat.RGBA32f: NVTT.nvttSetCompressionOptionsFormat(compressionOptions, NvttFormat.RGBA); NVTT.nvttSetCompressionOptionsPixelType(compressionOptions, NvttPixelType.Float); NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 32, 32, 32, 32); break;
                default: break;
            }

            NVTT.nvttSetOutputOptionsContainer(outputOptions, args.ImageFormat <= TextureImageFormat.BC6u ? NvttContainer.DDS10 : NvttContainer.DDS);

            if (args.CutoutDither && hasAlpha == NvttBoolean.True && args.ImageFormat != TextureImageFormat.BC2 && args.ImageFormat != TextureImageFormat.BC1a)
            {
                NVTT.nvttSetCompressionOptionsPixelFormat(compressionOptions, 0, 8, 8, 8, alphaBits);
                NVTT.nvttSetCompressionOptionsQuantization(compressionOptions, NvttBoolean.False, NvttBoolean.True, NvttBoolean.False, args.CutoutThreshold);
            }

            if (args.GammaCorrect)
                NVTT.nvttSurfaceToSrgbUnclamped(surface, null);

            NvttAlphaMode alphaMode = NVTT.nvttSurfaceAlphaMode(surface);

            alphaMode = args.PremultipliedAlpha ? NvttAlphaMode.Premultiplied : alphaMode;
            if (args.ImageFormat == TextureImageFormat.BC6u || args.ImageFormat == TextureImageFormat.BC6s)
                alphaMode = NvttAlphaMode.None;

            NVTT.nvttSetSurfaceAlphaMode(surface, alphaMode);

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

            int mip0Width = NVTT.nvttSurfaceWidth(surface);
            int mip0Height = NVTT.nvttSurfaceHeight(surface);
            int mip0Depth = NVTT.nvttSurfaceDepth(surface);

            List<(int width, int height)> sizes = new List<(int, int)>();

            int mipmapCount = 0;
            if (args.GenerateMipmaps)
            {
                while (mipmapCount < args.MaxMipmapCount)
                {
                    int mipWidth = Math.Max(1, mip0Width >> mipmapCount);
                    int mipHeight = Math.Max(1, mip0Height >> mipmapCount);

                    if (mipWidth < args.MinMipmapSize || mipHeight < args.MinMipmapSize)
                    {
                        break;
                    }

                    sizes.Add((mipWidth, mipHeight));
                    mipmapCount++;
                }
            }

            mipmapCount = Math.Max(1, mipmapCount);

            OutputHandler outputHandler = new OutputHandler(args.AbsoluteOutputPath);
            NVTT.nvttSetOutputOptionsOutputHandler(outputOptions, outputHandler.nvttBeginImage, outputHandler.nvttWriteData, outputHandler.nvttEndImage);

            switch (args.ImageType)
            {
                case TextureImageType.Colormap: break;
                case TextureImageType.Grayscale: NVTT.nvttSurfaceToGreyScale(surface, 1.0f, 1.0f, 1.0f, 1.0f, null); break;
                case TextureImageType.NormalMap_TangentSpace: NVTT.nvttSetSurfaceNormalMap(surface, NvttBoolean.True); break;
                case TextureImageType.NormalMap_ObjectSpace: NVTT.nvttSetSurfaceNormalMap(surface, NvttBoolean.True); break;
            }

            NvttBatchList* batchList = NVTT.nvttCreateBatchList();

            if (NVTT.nvttSurfaceIsNormalMap(surface) == NvttBoolean.True)
            {
                if (args.ImageType == TextureImageType.NormalMap_ObjectSpace)
                {
                    NVTT.nvttSurfaceToGreyScale(surface, 1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f, 0.0f, null);
                    NVTT.nvttSurfaceToNormalMap(surface, 1.0f / 1.875f, 0.5f / 1.875f, 0.25f / 1.875f, 0.125f / 1.875f, null);
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

            List<nint> surfaces = new List<nint>();

            NvttContext* context = NVTT.nvttCreateContext();
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

            var cleanup = () =>
            {
                for (int i = 0; i < surfaces.Count; i++)
                {
                    NVTT.nvttDestroySurface((NvttSurface*)surfaces[i]);
                }

                NVTT.nvttDestroyBatchList(batchList);

                NVTT.nvttDestroySurface(surface);
                NVTT.nvttDestroyContext(context);

                NVTT.nvttDestroyOutputOptions(outputOptions);
                NVTT.nvttDestroyCompressionOptions(compressionOptions);

                outputHandler.Dispose();
            };

            if (NVTT.nvttContextOutputHeaderData(context, NvttTextureType._2D, mip0Width, mip0Height, mip0Depth, mipmapCount, NVTT.nvttSurfaceIsNormalMap(surface), compressionOptions, outputOptions) != NvttBoolean.True)
            {
                cleanup();
                //bad
                return false;
            }

            if (NVTT.nvttContextCompressBatch(context, batchList, compressionOptions) != NvttBoolean.True)
            {
                cleanup();
                //bad
                return false;
            }

            cleanup();
            return true;
        }

        private static unsafe void ErrorReporter(NvttError error)
        {
            byte* str = (byte*)NVTT.nvttErrorString(error);

            int length = 0;
            while (str[length++] != 0) ;

            string text = Encoding.UTF8.GetString(str, length);
            throw new Exception(text);
        }

        public class OutputHandler : IDisposable
        {
            private Stream? _stream;

            public OutputHandler(string path)
            {
                try
                {
                    _stream = File.OpenWrite(path);
                }
                catch (Exception ex)
                {
                    //bad
                }
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
        }
    }

    public struct TextureProcessorArgs
    {
        public string AbsoluteFilepath;
        public string AbsoluteOutputPath;

        public bool FlipVertical;
        public TextureImageFormat ImageFormat;
        public bool CutoutDither;
        public byte CutoutThreshold;
        public bool GammaCorrect;
        public bool PremultipliedAlpha;
        public TextureMipmapFilter MipmapFilter;
        public int MaxMipmapCount;
        public int MinMipmapSize;
        public bool GenerateMipmaps;
        public TextureImageType ImageType;
        public bool ScaleAlphaForMipmaps;
    }

    public enum TextureImageFormat : byte
    {
        BC7 = 0,
        BC6s,
        BC6u,
        ASTC,
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
        R16f,
        RG16f,
        RGBA16f,
        R32f,
        RG32f,
        RGBA32f
    }

    public enum TextureMipmapFilter : byte
    {
        Box,
        Kaiser,
        Triangle,
        Mitchell,
        Min,
        Max
    }

    public enum TextureImageType : byte
    {
        Colormap,
        Grayscale,
        NormalMap_TangentSpace,
        NormalMap_ObjectSpace
    }
}
