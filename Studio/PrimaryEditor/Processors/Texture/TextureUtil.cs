using CommunityToolkit.HighPerformance;
using PrimaryEditor.Assets;
using Editor.Interop.Ed;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.Processors.Texture
{
    internal unsafe static class TextureUtil
    {
        internal static RawTextureData LoadRawData(string filePath)
        {
            byte[] rawData;
            {
                using Stream? stream = FileUtility.TryWaitOpenNoThrow(FilesystemManager.GetFullPath(filePath)!, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 50);
                if (stream == null)
                {
                    throw new Exception($"Failed to open stream: {filePath}");
                }

                rawData = new byte[stream.Length];
                stream.ReadExactly(rawData);
            }

            fixed (byte* rawDataPtr = rawData)
            {
                TexInterop.ImageLoadData imageData = new TexInterop.ImageLoadData
                {
                    Data = rawDataPtr,
                    Length = (uint)rawData.LongLength
                };

                const float ByteToFloatMultiplier = 1.0f / 255.0f;

                string ext = Path.GetExtension(filePath);
                switch (ext)
                {
                    case ".png":
                        {
                            TexInterop.ImageBitmap bitmap = default;
                            sbyte* errorOutput = null;

                            if (!TexInterop.LoadPNG(ref imageData, ref bitmap, &errorOutput))
                            {
                                string str = GetErrorAsString(errorOutput);
                                TexInterop.FreePNGError(errorOutput);

                                throw new Exception($"Failed to load png: {filePath} because: {str}");
                            }

                            ScopedPtr<Vector128<float>> pixelData = ScopedMemory.Allocate<Vector128<float>>(64, bitmap.Width * bitmap.Height);

                            int count = (int)(bitmap.Width * bitmap.Height);
                            for (int i = 0, j = 0; i < count; ++i, j += bitmap.Stride)
                            {
                                switch (bitmap.Stride)
                                {
                                    case 1: pixelData[i] = Vector128.CreateScalar(bitmap.Pixels[j] * ByteToFloatMultiplier); break;
                                    case 2: pixelData[i] = Vector128.Create(Vector64.ConvertToSingle(Vector64.Create(bitmap.Pixels[j], bitmap.Pixels[j + 1])) * ByteToFloatMultiplier, Vector64<float>.Zero); break;
                                    case 3: pixelData[i] = Vector128.Create(bitmap.Pixels[j], bitmap.Pixels[j + 1], bitmap.Pixels[j + 2], 0.0f) * ByteToFloatMultiplier; break;
                                    case 4: pixelData[i] = Vector128.ConvertToSingle(Vector128.Create(bitmap.Pixels[j], bitmap.Pixels[j + 1], bitmap.Pixels[j + 2], bitmap.Pixels[j + 3])) * ByteToFloatMultiplier; break;
                                }
                            }

                            TexInterop.FreePNG(ref bitmap);

                            return new RawTextureData(pixelData, (int)bitmap.Width, (int)bitmap.Height, bitmap.Stride);
                        }
                    case ".jpeg":
                    case ".jpg":
                        {
                            TexInterop.ImageBitmap bitmap = default;
                            sbyte* errorOutput = null;

                            if (!TexInterop.LoadJPEG(ref imageData, ref bitmap, &errorOutput))
                            {
                                string str = GetErrorAsString(errorOutput);
                                TexInterop.FreeJPEGError(errorOutput);

                                throw new Exception($"Failed to load png: {filePath} because: {str}");
                            }

                            ScopedPtr<Vector128<float>> pixelData = ScopedMemory.Allocate<Vector128<float>>(64, bitmap.Width * bitmap.Height);

                            int count = (int)(bitmap.Width * bitmap.Height);
                            for (int i = 0, j = 0; i < count; ++i, j += bitmap.Stride)
                            {
                                switch (bitmap.Stride)
                                {
                                    case 1: pixelData[i] = Vector128.CreateScalar(bitmap.Pixels[j] * ByteToFloatMultiplier); break;
                                    case 2: pixelData[i] = Vector128.Create(Vector64.ConvertToSingle(Vector64.Create(bitmap.Pixels[j], bitmap.Pixels[j + 1])) * ByteToFloatMultiplier, Vector64<float>.Zero); break;
                                    case 3: pixelData[i] = Vector128.Create(bitmap.Pixels[j], bitmap.Pixels[j + 1], bitmap.Pixels[j + 2], 0.0f) * ByteToFloatMultiplier; break;
                                    case 4: pixelData[i] = Vector128.ConvertToSingle(Vector128.Create(bitmap.Pixels[j], bitmap.Pixels[j + 1], bitmap.Pixels[j + 2], bitmap.Pixels[j + 3])) * ByteToFloatMultiplier; break;
                                }
                            }

                            TexInterop.FreeJPEG(ref bitmap);

                            return new RawTextureData(pixelData, (int)bitmap.Width, (int)bitmap.Height, bitmap.Stride);
                        }
                    default: throw new Exception($"Unknown image file type: {filePath}");
                }
            }
        }

        internal static void LoadChannelInto(string filePath, TextureChannel channel)
        {
            byte[] rawData;
            {
                using Stream? stream = FileUtility.TryWaitOpenNoThrow(FilesystemManager.GetFullPath(filePath)!, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 50);
                if (stream == null)
                {
                    throw new Exception($"Failed to open stream: {filePath}");
                }

                rawData = new byte[stream.Length];
                stream.ReadExactly(rawData);
            }

            fixed (byte* rawDataPtr = rawData)
            {
                TexInterop.ImageLoadData imageData = new TexInterop.ImageLoadData
                {
                    Data = rawDataPtr,
                    Length = (uint)rawData.LongLength
                };

                string ext = Path.GetExtension(filePath);
                switch (ext)
                {
                    case ".png":
                        {
                            TexInterop.ImageBitmap bitmap = default;
                            sbyte* errorOutput = null;

                            if (!TexInterop.LoadPNG(ref imageData, ref bitmap, &errorOutput))
                            {
                                string str = GetErrorAsString(errorOutput);
                                TexInterop.FreePNGError(errorOutput);

                                throw new Exception($"Failed to load png: {filePath} because: {str}");
                            }

                            if (bitmap.Width != channel.Width || bitmap.Height != channel.Height)
                                throw new Exception($"Image must match channel dimensions: {filePath}");

                            int maxIndex = (int)(bitmap.Width * bitmap.Height * channel.Stride);
                            if (channel.DestinationChannelId + maxIndex > channel.Data.Length)
                                throw new Exception($"Not enough data in provided for: {filePath}");

                            if (bitmap.Stride > channel.SourceChannelId)
                            {
                                if (channel.Invert)
                                {
                                    for (int i = channel.SourceChannelId, j = channel.DestinationChannelId; i < maxIndex; i += bitmap.Stride, j += channel.Stride)
                                    {
                                        channel.Data.DangerousGetReferenceAt(j) = (byte)(byte.MaxValue - bitmap.Pixels[i]);
                                    }
                                }
                                else
                                {
                                    for (int i = channel.SourceChannelId, j = channel.DestinationChannelId; i < maxIndex; i += bitmap.Stride, j += channel.Stride)
                                    {
                                        channel.Data.DangerousGetReferenceAt(j) = bitmap.Pixels[i];
                                    }
                                }
                            }

                            TexInterop.FreePNG(ref bitmap);
                            break;
                        }
                    case ".jpeg":
                    case ".jpg":
                        {
                            TexInterop.ImageBitmap bitmap = default;
                            sbyte* errorOutput = null;

                            if (!TexInterop.LoadJPEG(ref imageData, ref bitmap, &errorOutput))
                            {
                                string str = GetErrorAsString(errorOutput);
                                TexInterop.FreeJPEGError(errorOutput);

                                throw new Exception($"Failed to load jpeg: {filePath} because: {str}");
                            }

                            if (bitmap.Width != channel.Width || bitmap.Height != channel.Height)
                                throw new Exception($"Image must match channel dimensions: {filePath}");

                            int maxIndex = (int)(bitmap.Width * bitmap.Height * channel.Stride);
                            if (channel.DestinationChannelId + maxIndex > channel.Data.Length)
                                throw new Exception($"Not enough data in provided for: {filePath}");

                            if (bitmap.Stride > channel.SourceChannelId)
                            {
                                if (channel.Invert)
                                {
                                    for (int i = channel.SourceChannelId, j = channel.DestinationChannelId; i < maxIndex; i += bitmap.Stride, j += channel.Stride)
                                    {
                                        channel.Data.DangerousGetReferenceAt(j) = (byte)(byte.MaxValue - bitmap.Pixels[i]);
                                    }
                                }
                                else
                                {
                                    for (int i = channel.SourceChannelId, j = channel.DestinationChannelId; i < maxIndex; i += bitmap.Stride, j += channel.Stride)
                                    {
                                        channel.Data.DangerousGetReferenceAt(j) = bitmap.Pixels[i];
                                    }
                                }
                            }

                            TexInterop.FreeJPEG(ref bitmap);
                            break;
                        }
                    default: throw new Exception($"Unknown image file type: {filePath}");
                }
            }
        }

        internal static Int2 GetDimensions(string filePath)
        {
            byte[] rawData;
            {
                using Stream? stream = FileUtility.TryWaitOpenNoThrow(FilesystemManager.GetFullPath(filePath)!, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 50);
                if (stream == null)
                {
                    throw new Exception($"Failed to open stream: {filePath}");
                }

                rawData = new byte[stream.Length];
                stream.ReadExactly(rawData);
            }

            fixed (byte* rawDataPtr = rawData)
            {
                TexInterop.ImageLoadData imageData = new TexInterop.ImageLoadData
                {
                    Data = rawDataPtr,
                    Length = (uint)rawData.LongLength
                };

                string ext = Path.GetExtension(filePath);
                switch (ext)
                {
                    case ".png":
                        {
                            TexInterop.ImageMetrics metrics = default;
                            sbyte* errorOutput = null;

                            if (!TexInterop.QueryPNG(ref imageData, ref metrics, &errorOutput))
                            {
                                string str = GetErrorAsString(errorOutput);
                                TexInterop.FreePNGError(errorOutput);

                                throw new Exception($"Failed to query png: {filePath} because: {str}");
                            }

                            return new Int2((int)metrics.Width, (int)metrics.Height);
                        }
                    case ".jpeg":
                    case ".jpg":
                        {
                            TexInterop.ImageMetrics metrics = default;
                            sbyte* errorOutput = null;

                            if (!TexInterop.QueryJPEG(ref imageData, ref metrics, &errorOutput))
                            {
                                string str = GetErrorAsString(errorOutput);
                                TexInterop.FreeJPEGError(errorOutput);

                                throw new Exception($"Failed to query jpeg: {filePath} because: {str}");
                            }

                            return new Int2((int)metrics.Width, (int)metrics.Height);
                        }
                    default: throw new Exception($"Unknown image file type: {filePath}");
                }
            }
        }

        private static string GetErrorAsString(sbyte* ptr)
        {
            return ptr == null ? "No reason provided" : new string(ptr);
        }
    }

    internal ref struct TextureChannel(int width, int height, int stride, Span<byte> data, int srcChannelId, int dstChannelId, bool invert)
    {
        public readonly int Width = width;
        public readonly int Height = height;
        public readonly int Stride = stride;

        public readonly Span<byte> Data = data;

        public readonly int SourceChannelId = srcChannelId;
        public readonly int DestinationChannelId = dstChannelId;

        public readonly bool Invert = invert;
    }
}
