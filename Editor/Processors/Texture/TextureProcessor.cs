using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using MathNet.Numerics;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Editor.Processors.Texture
{
    public static class TextureProcessor
    {
        static TextureProcessor()
        {
            TexInterop.InitBC15Encoder();
            TexInterop.InitBC7Encoder();
        }

        public static unsafe ProcessedTextureData Execute(TextureConfiguration config, Stream stream)
        {
            using MemoryScope _ = ScopedMemory.PushScope();

            RawTextureData[] rawData;

            TextureSwizzle textureSwizzle = config.VisualInfo.Swizzle;
            TextureSwizzle swizzleRemap = TextureSwizzle.Default;

            if (config is CubemapConfiguration cubemap)
            {
                switch (cubemap.CubemapInfo.Source)
                {
                    case TextureCubemapSource.Composited: rawData = CubemapLoader.LoadComposited(cubemap); break;
                    default: throw new Exception($"Invalid cubemap source: {cubemap.CubemapInfo.Source}");
                }
            }
            else if (config is CompositeConfiguration composite)
            {
                rawData = CompositeLoader.Load(composite, ref swizzleRemap);

                Update_RemapSwizzle(ref textureSwizzle, swizzleRemap);
                swizzleRemap = TextureSwizzle.Default;
            }
            else
            {
                rawData = TextureLoader.Load(config);
            }

            ScopedPtr<Vector128<float>> tempMipMapData = ScopedPtr<Vector128<float>>.Null;

            ScopedPtr<byte> bitmapData = ScopedPtr<byte>.Null;
            ScopedPtr<byte> outputData = ScopedPtr<byte>.Null;

            ProcessedTextureData outputTextureData = default;

            TextureConfiguration.Handling handling = config.HandlingInfo;
            TextureConfiguration.Mipmaps mipmaps = config.MipmapsInfo;

            for (int i = 0; i < rawData.Length; ++i)
            {
                RawTextureData textureData = rawData[i];

                Int2 sourceSize = new Int2(textureData.Width, textureData.Height);
                int mipCount = 0;

                {
                    bool isBlockCompressable = (textureData.Width % 4) == 0 && (textureData.Height % 4) == 0;

                    if (handling.FlipVertical)
                        Process_FlipY(ref textureData);

                    switch (handling.ImageType)
                    {
                        case TextureImageType.Color:
                            {
                                if (textureData.Stride == 4)
                                {
                                    if (handling.AlphaSource == TextureAlphaSource.Red)
                                    {
                                        Process_CopyChannel(ref textureData, 0, 3);

                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC3 : TextureImageFormat.RGBA8;
                                    }
                                    else if (handling.AlphaSource != TextureAlphaSource.Source)
                                    {
                                        textureData.Stride = 3;

                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC1 : TextureImageFormat.RGB8;
                                    }
                                    else
                                    {
                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC3 : TextureImageFormat.RGBA8;
                                    }
                                }
                                else if (textureData.Stride == 3)
                                {
                                    if (handling.AlphaSource == TextureAlphaSource.Red)
                                    {
                                        Process_CopyChannel(ref textureData, 0, 3);
                                        textureData.Stride = 4;

                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC3 : TextureImageFormat.RGBA8;
                                    }
                                    else
                                    {
                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC1 : TextureImageFormat.RGB8;
                                    }
                                }
                                else if (textureData.Stride == 2)
                                {
                                    if (handling.AlphaSource == TextureAlphaSource.Red)
                                    {
                                        Process_CopyChannel(ref textureData, 0, 1);

                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC5u : TextureImageFormat.RG8;
                                    }
                                    else if (handling.AlphaSource != TextureAlphaSource.Source)
                                    {
                                        textureData.Stride = 1;

                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC4u : TextureImageFormat.R8a;
                                    }
                                    else
                                    {
                                        if (handling.ImageFormat == TextureImageFormat.Automatic)
                                            handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC4u : TextureImageFormat.R8a;
                                    }
                                }
                                else
                                {
                                    if (handling.ImageFormat == TextureImageFormat.Automatic)
                                        handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC4u : TextureImageFormat.R8a;
                                }

                                break;
                            }
                        case TextureImageType.Grayscale:
                            {
                                bool wasGrayscaleBefore = textureData.Stride == 1;

                                if (textureData.Stride != 1)
                                {
                                    if (handling.AlphaSource == TextureAlphaSource.Red)
                                    {
                                        Process_CopyChannel(ref textureData, 3, 0);
                                        textureData.Stride = 1;
                                    }
                                    else
                                        Process_ToAvgGrayscale(ref textureData);
                                }

                                if (handling.ImageFormat == TextureImageFormat.Automatic)
                                {
                                    if (handling.AlphaSource == TextureAlphaSource.Source && !wasGrayscaleBefore)
                                    {
                                        handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC5u : TextureImageFormat.RG8;
                                        textureData.Stride = 2;
                                    }
                                    else
                                    {
                                        handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC4u : TextureImageFormat.R8a;
                                    }
                                }

                                swizzleRemap.R = TextureSwizzleChannel.R;
                                swizzleRemap.G = TextureSwizzleChannel.R;
                                swizzleRemap.B = TextureSwizzleChannel.R;

                                break;
                            }
                        case TextureImageType.Normal:
                            {
                                if (handling.Source == TextureImageTypeSource.NormalObject)
                                {
                                    Process_ObjectToTangent(ref textureData);
                                }
                                else if (handling.Source == TextureImageTypeSource.NormalBump)
                                {
                                    Process_BumpToTangent(ref textureData);
                                }
                                else
                                {
                                    textureData.Stride = 2;
                                }

                                handling.ImageFormat = isBlockCompressable ? TextureImageFormat.BC5u : TextureImageFormat.RG8;
                                handling.AlphaSource = TextureAlphaSource.Source;

                                swizzleRemap.R = TextureSwizzleChannel.One;
                                swizzleRemap.G = TextureSwizzleChannel.G;
                                swizzleRemap.B = TextureSwizzleChannel.Zero;
                                swizzleRemap.A = TextureSwizzleChannel.R;

                                break;
                            }
                    }

                    if (i == 0 && handling.ImageType != TextureImageType.Normal)
                    {
                        switch (handling.AlphaSource)
                        {
                            case TextureAlphaSource.None: swizzleRemap.A = TextureSwizzleChannel.Zero; break;
                            case TextureAlphaSource.Opaque: swizzleRemap.A = TextureSwizzleChannel.One; break;
                            case TextureAlphaSource.Source:
                                {
                                    if (textureData.Stride == 1)
                                        swizzleRemap.A = TextureSwizzleChannel.One;
                                    else if (textureData.Stride == 2)
                                        swizzleRemap.A = TextureSwizzleChannel.G;
                                    break;
                                }
                            case TextureAlphaSource.Red:
                                {
                                    if (textureData.Stride == 1)
                                    {
                                        swizzleRemap.R = TextureSwizzleChannel.One;
                                        swizzleRemap.G = TextureSwizzleChannel.One;
                                        swizzleRemap.B = TextureSwizzleChannel.One;
                                        swizzleRemap.A = TextureSwizzleChannel.R;
                                    }

                                    break;
                                }
                        }
                    }

                    Guard.IsFalse(handling.ImageFormat == TextureImageFormat.Automatic);
                }

                {
                    if (!mipmaps.GenerateMipmaps)
                        mipmaps.MaxMipMapCount = 1;

                    RHIFormatInfo fi;
                    if (handling.ImageFormat == TextureImageFormat.RGB8)
                        fi = new RHIFormatInfo(3, 3);
                    else
                    {
                        fi = RHIFormatInfo.Query(handling.ImageFormat switch
                        {
                            TextureImageFormat.BC7 => RHIFormat.BC7_UNorm,
                            TextureImageFormat.BC5u => RHIFormat.BC5_UNorm,
                            TextureImageFormat.BC4u => RHIFormat.BC4_UNorm,
                            TextureImageFormat.BC3 => RHIFormat.BC3_UNorm,
                            TextureImageFormat.BC3n => RHIFormat.BC3_UNorm,
                            TextureImageFormat.BC1 => RHIFormat.BC1_UNorm,
                            TextureImageFormat.R8a => RHIFormat.R8_UNorm,
                            TextureImageFormat.RG8 => RHIFormat.RG8_UNorm,
                            TextureImageFormat.RGBA8 => RHIFormat.RGBA8_UNorm,
                            _ => RHIFormat.Unknown
                        });
                    }

                    if (fi.ChannelCount != textureData.Stride)
                        textureData.Stride = fi.ChannelCount;

                    if (fi.IsBlockCompressed)
                        mipmaps.MinMipMapSize = 4;

                    while (true)
                    {
                        int mipWidth = textureData.Width;
                        int mipHeight = textureData.Height;

                        if (mipCount > 0)
                        {
                            mipWidth /= 2;
                            mipHeight /= 2;

                            if (mipWidth <= mipmaps.MinMipMapSize || mipHeight <= mipmaps.MinMipMapSize || mipCount >= mipmaps.MaxMipMapCount)
                                break;

                            if (tempMipMapData.IsNull)
                                tempMipMapData = ScopedMemory.Allocate<Vector128<float>>(64, (nuint)(mipWidth * mipHeight));

                            switch (mipmaps.MipmapFilter)
                            {
                                case TextureMipmapFilter.Box: Process_GenerateMipmap_Box(ref textureData, tempMipMapData, mipWidth, mipHeight); break;
                                case TextureMipmapFilter.Kaiser: Process_GenerateMipmap_Kaiser(ref textureData, tempMipMapData, mipWidth, mipHeight); break;
                                case TextureMipmapFilter.Triangle: Process_GenerateMipmap_Triangle(ref textureData, tempMipMapData, mipWidth, mipHeight); break;
                                case TextureMipmapFilter.Mitchell: Process_GenerateMipmap_Mitchell(ref textureData, tempMipMapData, mipWidth, mipHeight); break;
                                case TextureMipmapFilter.Min: Process_GenerateMipmap_Min(ref textureData, tempMipMapData, mipWidth, mipHeight); break;
                                case TextureMipmapFilter.Max: Process_GenerateMipmap_Max(ref textureData, tempMipMapData, mipWidth, mipHeight); break;
                            }

                            {
                                ScopedPtr<Vector128<float>> temp = tempMipMapData;

                                tempMipMapData = textureData.Pixels;
                                textureData.Pixels = temp;
                            }
                        }

                        textureData.Width = mipWidth;
                        textureData.Height = mipHeight;

                        if (handling.AlphaCutout && handling.CutoutThreshold > 0)
                        {
                            if (handling.CutoutDither)
                                Process_CutoutAlphaDithered(ref textureData, handling.CutoutThreshold);
                            else
                                Process_CutoutAlpha(ref textureData, handling.CutoutThreshold);
                        }

                        int workingStride = fi.IsBlockCompressed ? 4 : textureData.Stride;
                        int outputDataSize = (int)fi.CalculateSize(mipWidth, mipHeight);

                        if (outputData.IsNull)
                            outputData = ScopedMemory.Allocate((nuint)outputDataSize, 64);
                        if (bitmapData.IsNull)
                            bitmapData = ScopedMemory.Allocate((nuint)(textureData.Width * textureData.Height * workingStride));

                        //TODO: use larger registers such as Vector256 or Vector512 for faster conversion
                        int count = mipWidth * mipHeight;
                        switch (textureData.Stride)
                        {
                            case 1:
                                {
                                    for (int j = 0, k = 0; j < count; ++j, k += workingStride)
                                    {
                                        float pixel = MathF.Min(MathF.Max(textureData.Pixels[j][0], 0.0f), 1.0f) * 255.0f;
                                        bitmapData[k] = (byte)(int)pixel;
                                    }

                                    break;
                                }
                            case 2:
                                {
                                    for (int j = 0, k = 0; j < count; ++j, k += workingStride)
                                    {
                                        Vector128<float> pixel = Vector128.Clamp(textureData.Pixels[j], Vector128<float>.Zero, Vector128<float>.One) * 255.0f;
                                        Vector128<int> integral = Vector128.ConvertToInt32(pixel);

                                        bitmapData[k] = (byte)integral[0];
                                        bitmapData[k + 1] = (byte)integral[1];
                                    }

                                    break;
                                }
                            case 3:
                                {
                                    for (int j = 0, k = 0; j < count; ++j, k += workingStride)
                                    {
                                        Vector128<float> pixel = Vector128.Clamp(textureData.Pixels[j], Vector128<float>.Zero, Vector128<float>.One) * 255.0f;
                                        Vector128<int> integral = Vector128.ConvertToInt32(pixel);

                                        bitmapData[k] = (byte)integral[0];
                                        bitmapData[k + 1] = (byte)integral[1];
                                        bitmapData[k + 2] = (byte)integral[2];
                                    }

                                    break;
                                }
                            case 4:
                                {
                                    for (int j = 0, k = 0; j < count; ++j, k += workingStride)
                                    {
                                        Vector128<float> pixel = Vector128.Clamp(textureData.Pixels[j], Vector128<float>.Zero, Vector128<float>.One) * 255.0f;
                                        Vector128<int> integral = Vector128.ConvertToInt32(pixel);

                                        bitmapData[k] = (byte)integral[0];
                                        bitmapData[k + 1] = (byte)integral[1];
                                        bitmapData[k + 2] = (byte)integral[2];
                                        bitmapData[k + 3] = (byte)integral[3];
                                    }

                                    break;
                                }
                        }

                        TexInterop.ImageBitmap bitmap = new TexInterop.ImageBitmap
                        {
                            Width = (uint)textureData.Width,
                            Height = (uint)textureData.Height,
                            Stride = (byte)workingStride,

                            Pixels = bitmapData.Pointer
                        };

                        TexInterop.ImageOutput output = new TexInterop.ImageOutput
                        {
                            BlocksX = fi.IsBlockCompressed ? (uint)(mipWidth / fi.BlockWidth) : (uint)mipWidth,
                            BlocksY = fi.IsBlockCompressed ? (uint)(mipHeight / fi.BlockWidth) : (uint)mipHeight,

                            Pixels = outputData.Pointer
                        };

                        switch (handling.ImageFormat)
                        {
                            case TextureImageFormat.BC7: TexInterop.EncodeBC7(ref bitmap, ref output); break;
                            case TextureImageFormat.BC5u: TexInterop.EncodeBC5(ref bitmap, ref output); break;
                            case TextureImageFormat.BC4u: TexInterop.EncodeBC4(ref bitmap, ref output); break;
                            case TextureImageFormat.BC3:
                            case TextureImageFormat.BC3n: TexInterop.EncodeBC3(ref bitmap, ref output); break;
                            case TextureImageFormat.BC1: TexInterop.EncodeBC1(ref bitmap, ref output); break;
                            case TextureImageFormat.R8a:
                            case TextureImageFormat.RG8:
                            case TextureImageFormat.RGB8:
                            case TextureImageFormat.RGBA8: (outputData, bitmapData) = (bitmapData, outputData); break;
                            default: throw new NotImplementedException($"Image format not supported: {handling.ImageFormat}");
                        }

                        stream.Write(outputData.AsSpan(outputDataSize));
                        ++mipCount;
                    }
                }

                if (i == 0)
                {
                    Update_RemapSwizzle(ref textureSwizzle, swizzleRemap);
                    outputTextureData = new ProcessedTextureData(sourceSize.X, sourceSize.Y, mipCount, rawData.Length, handling.ImageFormat, textureSwizzle);
                }
            }

            return outputTextureData;
        }

        private static void Update_RemapSwizzle(ref TextureSwizzle swizzle, TextureSwizzle remap)
        {
            for (int i = 0; i < 4; i++)
            {
                if (swizzle[i] <= TextureSwizzleChannel.A)
                    swizzle[i] = remap[(int)swizzle[i]];
            }
        }

        private static void Process_CopyChannel(ref RawTextureData textureData, int source, int destination)
        {
            Vector128<int> shuffle;
            {
                Span<int> shuffleArray = [0, 1, 2, 3];
                shuffleArray[destination] = source;

                shuffle = Vector128.LoadUnsafe(ref shuffleArray[0]);
            }

            int count = textureData.Width * textureData.Height;
            for (int i = 0; i < count; i++)
            {
                textureData.Pixels[i] = Vector128.Shuffle(textureData.Pixels[i], shuffle);
            }
        }

        private static void Process_FlipY(ref RawTextureData textureData)
        {
            int maxYSlice = (textureData.Height - 1) * textureData.Width;
            Vector128<float> temp;
            for (int y = 0; y < textureData.Height / 2; y++)
            {
                int ySlice = y * textureData.Width;
                int flippedYSlice = maxYSlice - ySlice;

                for (int x = 0; x < textureData.Width; x++)
                {
                    int swapA = ySlice + x;
                    int swapB = flippedYSlice + x;

                    (textureData.Pixels[swapA], textureData.Pixels[swapB]) = (textureData.Pixels[swapB], textureData.Pixels[swapA]);

                    //temp = textureData.Pixels[swapB];
                    //
                    //textureData.Pixels[swapB] = textureData.Pixels[swapA];
                    //textureData.Pixels[swapA] = Vector128<float>.Indices;
                }
            }
        }

        private static void Process_ToAvgGrayscale(ref RawTextureData textureData)
        {
            if (textureData.Stride == 1)
                return;

            int count = textureData.Width * textureData.Height;
            float multiplier = textureData.Stride - 1;

            for (int i = 0; i < count; ++i)
            {
                ref Vector128<float> pixel = ref textureData.Pixels[i];

                float average = Vector128.Sum(pixel.WithElement(3, 0.0f)) * multiplier;
                pixel = Vector128.Create(average, pixel[3], 0.0f, 0.0f);
            }

            textureData.Stride = 1;
        }

        private static void Process_ObjectToTangent(ref RawTextureData textureData)
        {
            int count = textureData.Width * textureData.Height;
            for (int i = 0; i < count; ++i)
            {
                ref Vector128<float> pixel = ref textureData.Pixels[i];

                float grayscale = Vector128.Sum(pixel) / 4.0f;
                pixel = Vector128.Create(grayscale) * Vector128.Create(1.0f / 1.875f, 0.5f / 1.875f, 0.25f / 1.875f, 0.125f / 1.875f);
            }

            textureData.Stride = 2;
        }

        private static void Process_BumpToTangent(ref RawTextureData textureData)
        {
            Vector128<int> uppperBound = Vector128.Create(textureData.Width, textureData.Height, textureData.Width, textureData.Height) - Vector128<int>.One;

            //float maxHeight = 0.0f;

            for (int y = 0; y < textureData.Height; ++y)
            {
                int ySlice = y * textureData.Width;
                for (int x = 0; x < textureData.Width; x += 2)
                {
                    int index = ySlice + x;

                    /*
                    //Pu = (X: 0, Y: 1)
                    //Mu = (X: 2, Y: 3)
                    //Pv = (X: 4, Y: 5)
                    //Mv = (X: 6, Y: 7)
                    Vector256<int> positions;

                    {
                        Vector128<int> upper = Vector128.Create(x, x, y, y);
                        Vector128<int> lower = upper + Vector128.Create(1, -1, 1, -1);

                        lower = Vector128.ConditionalSelect(
                            Vector128.LessThan(lower, Vector128<int>.Zero), uppperBound, lower);
                        lower = Vector128.ConditionalSelect(
                            Vector128.GreaterThan(lower, uppperBound), Vector128<int>.Zero, lower);

                        //Pu = (X: 0, Y: 6) > (X: 0, Y: 1)
                        //Mu = (X: 1, Y: 7) > (X: 2, Y: 3)
                        //Pv = (X: 4, Y: 2) > (X: 4, Y: 5)
                        //Mv = (X: 5, Y: 3) > (X: 6, Y: 7)
                        positions = Vector256.Shuffle(Vector256.Create(lower, upper), Vector256.Create(0, 6, 1, 7, 4, 2, 5, 3));
                    }
                    */

                    Vector128<int> indices = Vector128.Create(x, y, x, y) + Vector128.Create(1, 1, 2, 1);

                    indices = Vector128.ConditionalSelect(
                        Vector128.GreaterThan(indices, uppperBound), Vector128<int>.Zero, indices);
                    indices += Vector128.Create(ySlice, indices[1] * textureData.Width, ySlice, indices[3] * textureData.Width);

                    float height0 = textureData.Pixels[index][0];
                    float height1 = textureData.Pixels[index + 1][0];

                    Vector128<float> dxy =
                        Vector128.Create(height0, height0, height1, height1) -
                        Vector128.Create(
                            textureData.Pixels[indices[indices[0]]][0],
                            textureData.Pixels[indices[indices[1]]][0],
                            textureData.Pixels[indices[indices[2]]][0],
                            textureData.Pixels[indices[indices[3]]][0]);

                    if (!Vector128.EqualsAny(dxy, Vector128<float>.Zero))
                    {
                        Vector64<float> lower = dxy.GetLower();
                        Vector64<float> upper = dxy.GetUpper();

                        float lowerLength = Vector64.Dot(lower, lower);
                        float upperLength = Vector64.Dot(upper, upper);

                        dxy /= Vector128.Create(lowerLength, lowerLength, upperLength, upperLength);

                        //Vector128<float> abs = Vector128.Abs(dxy);

                        //find max
                        //https://stackoverflow.com/questions/46125647/finding-maximum-float-in-sse-vector-m128
                        //{
                        //    Vector128<float> max1 = Vector128.Shuffle(abs, Vector128.Create(0, 0, 3, 2));
                        //    Vector128<float> max2 = Vector128.Max(abs, max1);
                        //    Vector128<float> max3 = Vector128.Shuffle(max2, Vector128.Create(0, 0, 0, 1));
                        //    Vector128<float> max4 = Vector128.Max(max2, max3);
                        //    
                        //    maxHeight = MathF.Max(max4[0], maxHeight);
                        //}

                        dxy = dxy * 0.5f + Vector128.Create(0.5f);

                        textureData.Pixels[index] = Vector128.Create(dxy.GetLower(), Vector64<float>.Zero);
                        textureData.Pixels[index + 1] = Vector128.Create(dxy.GetUpper(), Vector64<float>.Zero);
                    }
                }
            }

            textureData.Stride = 2;
        }

        private static void Process_CutoutAlpha(ref RawTextureData textureData, byte threshold)
        {
            float thresholdSingle = threshold / 255.0f;

            int count = textureData.Width * textureData.Height;
            for (int i = 0; i < count; i++)
            {
                ref Vector128<float> pixel = ref textureData.Pixels[i];
                pixel = pixel.WithElement(3, pixel[3] < thresholdSingle ? 0.0f : 1.0f);
            }
        }

        private static void Process_CutoutAlphaDithered(ref RawTextureData textureData, byte threshold)
        {
            float thresholdSingle = threshold / 255.0f;
            float multiplier = 1.0f / thresholdSingle;

            int count = textureData.Width * textureData.Height;
            for (int i = 0; i < count; i++)
            {
                ref Vector128<float> pixel = ref textureData.Pixels[i];

                if (pixel[3] < thresholdSingle)
                {
                    int y = i / textureData.Width;
                    int x = i - y * textureData.Width;

                    float dither = Bayer.Bayer8x8[(x % 8) + (y % 8) * 8];
                    float ditherTheshold = pixel[3] * multiplier;

                    pixel = pixel.WithElement(3, dither >= ditherTheshold ? 1.0f : 0.0f);
                }
                else
                    pixel = pixel.WithElement(3, 1.0f);
            }
        }

        private static void Process_GenerateMipmap_Box(ref RawTextureData textureData, ScopedPtr<Vector128<float>> outputData, int mipWidth, int mipHeight)
        {
            int srcWidth = mipWidth * 2;

            Vector128<int> srcUpperBound = Vector128.Create(mipWidth, mipHeight, mipWidth, mipHeight) * 2 - Vector128<int>.One;
            Vector128<int> sliceMultiplier = Vector128.Create(1, srcWidth, 1, srcWidth);

            for (int dstY = 0, srcY = 0; dstY < mipHeight; ++dstY, srcY += 2)
            {
                int srcYSlice = srcY * srcWidth;
                int dstYSlice = dstY * mipWidth;

                for (int dstX = 0, srcX = 0; dstX < mipWidth; ++dstX, srcX += 2)
                {
                    Vector128<int> positions = Vector128.Create(srcX, srcY, srcX, srcY) + Vector128.Create(-1, -1, 1, 1);

                    positions = Vector128.ConditionalSelect(Vector128.LessThan(positions, Vector128<int>.Zero), srcUpperBound, positions);
                    positions = Vector128.ConditionalSelect(Vector128.GreaterThan(positions, srcUpperBound), Vector128<int>.Zero, positions);
                    positions = positions * sliceMultiplier;

                    Vector128<float> sum = Vector128<float>.Zero;

                    //[1, 1, 1]
                    //[1, 1, 1] / 9 = X
                    //[1, 1, 1]

                    sum += textureData.Pixels[positions[0] + positions[1]];
                    sum += textureData.Pixels[srcX + positions[1]];
                    sum += textureData.Pixels[positions[2] + positions[1]];

                    sum += textureData.Pixels[positions[0] + srcYSlice];
                    sum += textureData.Pixels[srcX + srcYSlice];
                    sum += textureData.Pixels[positions[2] + srcYSlice];

                    sum += textureData.Pixels[positions[0] + positions[3]];
                    sum += textureData.Pixels[srcX + positions[3]];
                    sum += textureData.Pixels[positions[2] + positions[3]];

                    outputData[dstX + dstYSlice] = sum * (1.0f / 9.0f);
                }
            }
        }

        private static void Process_GenerateMipmap_Kaiser(ref RawTextureData textureData, ScopedPtr<Vector128<float>> outputData, int mipWidth, int mipHeight)
        {
            int srcWidth = mipWidth * 2;

            Vector128<int> srcUpperBound = Vector128.Create(mipWidth, mipHeight, mipWidth, mipHeight) * 2 - Vector128<int>.One;
            Vector128<int> sliceMultiplier = Vector128.Create(1, srcWidth, 1, srcWidth);

            for (int dstY = 0, srcY = 0; dstY < mipHeight; ++dstY, srcY += 2)
            {
                int srcYSlice = srcY * srcWidth;
                int dstYSlice = dstY * mipWidth;

                for (int dstX = 0, srcX = 0; dstX < mipWidth; ++dstX, srcX += 2)
                {
                    Vector128<int> positions = Vector128.Create(srcX, srcY, srcX, srcY) + Vector128.Create(-1, -1, 1, 1);

                    positions = Vector128.ConditionalSelect(Vector128.LessThan(positions, Vector128<int>.Zero), srcUpperBound, positions);
                    positions = Vector128.ConditionalSelect(Vector128.GreaterThan(positions, srcUpperBound), Vector128<int>.Zero, positions);
                    positions = positions * sliceMultiplier;

                    Vector128<float> sum = Vector128<float>.Zero;

                    //[1, 1, 1]
                    //[1, 1, 1] / 9 = f(X)
                    //[1, 1, 1]

                    sum += textureData.Pixels[positions[0] + positions[1]];
                    sum += textureData.Pixels[srcX + positions[1]];
                    sum += textureData.Pixels[positions[2] + positions[1]];

                    sum += textureData.Pixels[positions[0] + srcYSlice];
                    sum += textureData.Pixels[srcX + srcYSlice];
                    sum += textureData.Pixels[positions[2] + srcYSlice];

                    sum += textureData.Pixels[positions[0] + positions[3]];
                    sum += textureData.Pixels[srcX + positions[3]];
                    sum += textureData.Pixels[positions[2] + positions[3]];

                    //source from: https://computergraphics.stackexchange.com/questions/6393/kaiser-windowed-sinc-filter-for-mip-mapping

                    sum *= 1.0f / 9.0f;
                    sum = Vector128.Sqrt(Vector128<float>.One - sum * sum) * KaiserAlpha;
                    sum = Vector128.Create(MathUtil.BesselI0(sum[0]), MathUtil.BesselI0(sum[1]), MathUtil.BesselI0(sum[2]), MathUtil.BesselI0(sum[3])) * KaiserBessel;

                    outputData[dstX + dstYSlice] = sum;
                }
            }
        }

        private static void Process_GenerateMipmap_Triangle(ref RawTextureData textureData, ScopedPtr<Vector128<float>> outputData, int mipWidth, int mipHeight)
        {
            int srcWidth = mipWidth * 2;

            Vector128<int> srcUpperBound = Vector128.Create(mipWidth, mipHeight, mipWidth, mipHeight) * 2 - Vector128<int>.One;
            Vector128<int> sliceMultiplier = Vector128.Create(1, srcWidth, 1, srcWidth);

            for (int dstY = 0, srcY = 0; dstY < mipHeight; ++dstY, srcY += 2)
            {
                int srcYSlice = srcY * srcWidth;
                int dstYSlice = dstY * mipWidth;

                for (int dstX = 0, srcX = 0; dstX < mipWidth; ++dstX, srcX += 2)
                {
                    Vector128<int> positions = Vector128.Create(srcX, srcY, srcX, srcY) + Vector128.Create(-1, -1, 1, 1);

                    positions = Vector128.ConditionalSelect(Vector128.LessThan(positions, Vector128<int>.Zero), srcUpperBound, positions);
                    positions = Vector128.ConditionalSelect(Vector128.GreaterThan(positions, srcUpperBound), Vector128<int>.Zero, positions);
                    positions = positions * sliceMultiplier;

                    Vector128<float> sum = Vector128<float>.Zero;

                    //[1, 1, 1]
                    //[1, 1, 1] / 9 = f(X)
                    //[1, 1, 1]

                    sum += textureData.Pixels[positions[0] + positions[1]] * 0.5f;
                    sum += textureData.Pixels[srcX + positions[1]] * 0.75f;
                    sum += textureData.Pixels[positions[2] + positions[1]] * 0.5f;

                    sum += textureData.Pixels[positions[0] + srcYSlice] * 0.75f;
                    sum += textureData.Pixels[srcX + srcYSlice] * 1.25f;
                    sum += textureData.Pixels[positions[2] + srcYSlice] * 0.75f;

                    sum += textureData.Pixels[positions[0] + positions[3]] * 0.5f;
                    sum += textureData.Pixels[srcX + positions[3]] * 0.75f;
                    sum += textureData.Pixels[positions[2] + positions[3]] * 0.5f;

                    outputData[dstX + dstYSlice] = sum * (1.0f / 9.0f);
                }
            }
        }

        private static void Process_GenerateMipmap_Mitchell(ref RawTextureData textureData, ScopedPtr<Vector128<float>> outputData, int mipWidth, int mipHeight)
        {
            throw new NotImplementedException("Cubic filtering not implemented");

            int srcWidth = mipWidth * 2;

            Vector128<int> srcUpperBound = Vector128.Create(mipWidth, mipHeight, mipWidth, mipHeight) * 2 - Vector128<int>.One;
            Vector128<int> sliceMultiplier = Vector128.Create(1, srcWidth, 1, srcWidth);

            for (int dstY = 0, srcY = 0; dstY < mipHeight; ++dstY, srcY += 2)
            {
                int srcYSlice = srcY * srcWidth;
                int dstYSlice = dstY * mipWidth;

                for (int dstX = 0, srcX = 0; dstX < mipWidth; ++dstX, srcX += 2)
                {
                    Vector128<int> positions = Vector128.Create(srcX, srcY, srcX, srcY) + Vector128.Create(-1, -1, 1, 1);

                    positions = Vector128.ConditionalSelect(Vector128.LessThan(positions, Vector128<int>.Zero), srcUpperBound, positions);
                    positions = Vector128.ConditionalSelect(Vector128.GreaterThan(positions, srcUpperBound), Vector128<int>.Zero, positions);
                    positions = positions * sliceMultiplier;

                    Vector128<float> sum = Vector128<float>.Zero;

                    //[1, 1, 1]
                    //[1, 1, 1] / 9 = f(X)
                    //[1, 1, 1]

                    sum += textureData.Pixels[positions[0] + positions[1]] * 0.5f;
                    sum += textureData.Pixels[srcX + positions[1]] * 0.75f;
                    sum += textureData.Pixels[positions[2] + positions[1]] * 0.5f;

                    sum += textureData.Pixels[positions[0] + srcYSlice] * 0.75f;
                    sum += textureData.Pixels[srcX + srcYSlice] * 1.25f;
                    sum += textureData.Pixels[positions[2] + srcYSlice] * 0.75f;

                    sum += textureData.Pixels[positions[0] + positions[3]] * 0.5f;
                    sum += textureData.Pixels[srcX + positions[3]] * 0.75f;
                    sum += textureData.Pixels[positions[2] + positions[3]] * 0.5f;

                    outputData[dstX + dstYSlice] = sum * (1.0f / 9.0f);
                }
            }
        }

        private static void Process_GenerateMipmap_Min(ref RawTextureData textureData, ScopedPtr<Vector128<float>> outputData, int mipWidth, int mipHeight)
        {
            int srcWidth = mipWidth * 2;

            Vector128<int> srcUpperBound = Vector128.Create(mipWidth, mipHeight, mipWidth, mipHeight) * 2 - Vector128<int>.One;
            Vector128<int> sliceMultiplier = Vector128.Create(1, srcWidth, 1, srcWidth);

            for (int dstY = 0, srcY = 0; dstY < mipHeight; ++dstY, srcY += 2)
            {
                int srcYSlice = srcY * srcWidth;
                int dstYSlice = dstY * mipWidth;

                for (int dstX = 0, srcX = 0; dstX < mipWidth; ++dstX, srcX += 2)
                {
                    Vector128<int> positions = Vector128.Create(srcX, srcY, srcX, srcY) + Vector128.Create(-1, -1, 1, 1);

                    positions = Vector128.ConditionalSelect(Vector128.LessThan(positions, Vector128<int>.Zero), srcUpperBound, positions);
                    positions = Vector128.ConditionalSelect(Vector128.GreaterThan(positions, srcUpperBound), Vector128<int>.Zero, positions);
                    positions = positions * sliceMultiplier;

                    Vector128<float> sum = Vector128<float>.Zero;

                    //[1, 1, 1]
                    //[1, 1, 1] / 9 = f(X)
                    //[1, 1, 1]

                    sum = textureData.Pixels[positions[0] + positions[1]];
                    sum = Vector128.Min(sum, textureData.Pixels[srcX + positions[1]]);
                    sum = Vector128.Min(sum, textureData.Pixels[positions[2] + positions[1]]);

                    sum = Vector128.Min(sum, textureData.Pixels[positions[0] + srcYSlice]);
                    sum = Vector128.Min(sum, textureData.Pixels[srcX + srcYSlice]);
                    sum = Vector128.Min(sum, textureData.Pixels[positions[2] + srcYSlice]);

                    sum = Vector128.Min(sum, textureData.Pixels[positions[0] + positions[3]]);
                    sum = Vector128.Min(sum, textureData.Pixels[srcX + positions[3]]);
                    sum = Vector128.Min(sum, textureData.Pixels[positions[2] + positions[3]]);

                    outputData[dstX + dstYSlice] = sum;
                }
            }
        }

        private static void Process_GenerateMipmap_Max(ref RawTextureData textureData, ScopedPtr<Vector128<float>> outputData, int mipWidth, int mipHeight)
        {
            int srcWidth = mipWidth * 2;

            Vector128<int> srcUpperBound = Vector128.Create(mipWidth, mipHeight, mipWidth, mipHeight) * 2 - Vector128<int>.One;
            Vector128<int> sliceMultiplier = Vector128.Create(1, srcWidth, 1, srcWidth);

            for (int dstY = 0, srcY = 0; dstY < mipHeight; ++dstY, srcY += 2)
            {
                int srcYSlice = srcY * srcWidth;
                int dstYSlice = dstY * mipWidth;

                for (int dstX = 0, srcX = 0; dstX < mipWidth; ++dstX, srcX += 2)
                {
                    Vector128<int> positions = Vector128.Create(srcX, srcY, srcX, srcY) + Vector128.Create(-1, -1, 1, 1);

                    positions = Vector128.ConditionalSelect(Vector128.LessThan(positions, Vector128<int>.Zero), srcUpperBound, positions);
                    positions = Vector128.ConditionalSelect(Vector128.GreaterThan(positions, srcUpperBound), Vector128<int>.Zero, positions);
                    positions = positions * sliceMultiplier;

                    Vector128<float> sum = Vector128<float>.Zero;

                    //[1, 1, 1]
                    //[1, 1, 1] / 9 = f(X)
                    //[1, 1, 1]

                    sum = textureData.Pixels[positions[0] + positions[1]];
                    sum = Vector128.Max(sum, textureData.Pixels[srcX + positions[1]]);
                    sum = Vector128.Max(sum, textureData.Pixels[positions[2] + positions[1]]);

                    sum = Vector128.Max(sum, textureData.Pixels[positions[0] + srcYSlice]);
                    sum = Vector128.Max(sum, textureData.Pixels[srcX + srcYSlice]);
                    sum = Vector128.Max(sum, textureData.Pixels[positions[2] + srcYSlice]);

                    sum = Vector128.Max(sum, textureData.Pixels[positions[0] + positions[3]]);
                    sum = Vector128.Max(sum, textureData.Pixels[srcX + positions[3]]);
                    sum = Vector128.Max(sum, textureData.Pixels[positions[2] + positions[3]]);

                    outputData[dstX + dstYSlice] = sum;
                }
            }
        }

        private static readonly Vector128<float> KaiserBessel = Vector128<float>.One / Vector128.Create(MathUtil.BesselI0(KaiserAlpha));

        private const float KaiserAlpha = 4.0f * MathF.PI;
    }

    internal record struct RawTextureData(ScopedPtr<Vector128<float>> Pixels, int Width, int Height, int Stride);

    public readonly record struct ProcessedTextureData(int Width, int Height, int MipCount, int ArraySize, TextureImageFormat Format, TextureSwizzle Swizzle);
}
