using Primary;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.Processors.Texture
{
    internal static class CompositeLoader
    {
        public static RawTextureData[] Load(CompositeConfiguration composite, ref TextureSwizzle swizzleRemap)
        {
            CompositeConfiguration.Composite info = composite.CompositeInfo;

            byte[] outputData = Array.Empty<byte>();

            Int2 size = Int2.Zero;
            int stride = 0;

            IAssetIdProvider idProvider = composite.IdProvider!;

            for (int i = 0, j = 0; i < 4; ++i)
            {
                if (Flags.HasFlag(info.Channels, (TextureCompositeChannel)(1 << i)))
                {
                    CompositeConfiguration.CompsiteChannel channel = i switch
                    {
                        0 => info.Red,
                        1 => info.Green,
                        2 => info.Blue,
                        3 => info.Alpha,
                        _ => throw new NotImplementedException()
                    };

                    if (!idProvider.TryGetLocalPathForId(channel.Asset, out string? filePath))
                    {
                        throw new Exception($"Failed to get path for id: {channel.Asset} ({(TextureCompositeChannel)(1 << i)})");
                    }

                    if (outputData.Length == 0)
                    {
                        size = TextureUtil.GetDimensions(filePath);
                        stride = int.PopCount((int)info.Channels);

                        outputData = new byte[size.X * size.Y * stride];
                    }

                    swizzleRemap[i] = (TextureSwizzleChannel)j;

                    TextureChannel data = new TextureChannel(size.X, size.Y, stride, outputData, (int)channel.Source - 1, j++, channel.Invert);
                    TextureUtil.LoadChannelInto(filePath, data);
                }
                else
                {
                    swizzleRemap[i] = TextureSwizzleChannel.Zero;
                }
            }

            const float ByteToFloatMultiplier = 1.0f / 255.0f;

            ScopedPtr<Vector128<float>> pixelData = ScopedMemory.Allocate<Vector128<float>>(64, (nuint)(size.X * size.Y));

            int count = (int)(size.X * size.Y);
            for (int i = 0, j = 0; i < count; ++i, j += stride)
            {
                switch (stride)
                {
                    case 1: pixelData[i] = Vector128.CreateScalar(outputData[j] * ByteToFloatMultiplier); break;
                    case 2: pixelData[i] = Vector128.Create(Vector64.ConvertToSingle(Vector64.Create(outputData[j], outputData[j + 1])) * ByteToFloatMultiplier, Vector64<float>.Zero); break;
                    case 3: pixelData[i] = Vector128.Create(outputData[j], outputData[j + 1], outputData[j + 2], 0.0f) * ByteToFloatMultiplier; break;
                    case 4: pixelData[i] = Vector128.ConvertToSingle(Vector128.Create(outputData[j], outputData[j + 1], outputData[j + 2], outputData[j + 3])) * ByteToFloatMultiplier; break;
                }
            }

            return [new RawTextureData(pixelData, size.X, size.Y, stride)];
        }
    }
}
