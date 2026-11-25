using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Channels;
using CommunityToolkit.Diagnostics;
using Primary;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Mathematics;
using Primary.Memory.Native;
using Silk.NET.Assimp;

namespace Editor.Processors.Texture
{
    internal static class CubemapLoader
    {
        public static RawTextureData[] LoadComposited(CubemapConfiguration cubemap)
        {
            CubemapConfiguration.Cubemap info = cubemap.CubemapInfo;
            CubemapConfiguration.Composited composited = cubemap.CompositedInfo;

            AssetId[] faceIds = [
                composited.PositiveX,
                composited.NegativeX,
                composited.PositiveY,
                composited.NegativeY,
                composited.PositiveZ,
                composited.NegativeZ];
            RawTextureData[] outFaces = new RawTextureData[6];

            IAssetIdProvider idProvider = cubemap.IdProvider!;

            for (int i = 0; i < faceIds.Length; i++)
            {
                if (!idProvider.TryGetLocalPathForId(faceIds[i], out string? filePath))
                {
                    throw new Exception($"Failed to get path for id: {faceIds[i]}");
                }

                outFaces[i] = TextureUtil.LoadRawData(filePath);
            }

            return outFaces;
        }

        public static RawTextureData[] LoadEquilateral(CubemapConfiguration cubemap)
        {
            CubemapConfiguration.Cubemap info = cubemap.CubemapInfo;
            CubemapConfiguration.Panorama panorama = cubemap.PanoramaInfo;

            IAssetIdProvider idProvider = cubemap.IdProvider!;
            if (!idProvider.TryGetLocalPathForId(panorama.Source, out string? filePath))
            {
                throw new Exception($"Failed to get path for id: {panorama.Source}");
            }

            RawTextureData textureData = TextureUtil.LoadRawData(filePath);
            Guard.IsEqualTo(textureData.Width, textureData.Height * 2, "Source is not in a 2:1 size");

            int textureWH = textureData.Width / 4;

            RawTextureData[] outFaces = new RawTextureData[6];
            for (int i = 0; i < 6; ++i)
            {
                RawTextureData cubemapFace = TextureUtil.CreateTexture(textureWH, textureWH, 3);

                int pixelCount = textureWH * textureWH;
                for (int j = 0; j < pixelCount; ++j)
                {
                    ref Vector128<float> destination = ref cubemapFace.Pixels[j];

                    int y = j / textureWH;
                    int x = j - y * textureWH;

                    float px = 2.0f * (x + 0.5f) / textureWH - 1;
                    float py = 2.0f * (y + 0.5f) / textureWH - 1;

                    Vector4 face = i switch
                    {
                        0 => new Vector4(-1.0f, -px, -py, 0.0f), // X+
                        1 => new Vector4(+1.0f, px, -py, 0.0f), // X-
                        2 => new Vector4(+px, -1.0f, -py, 0.0f), // Y+
                        3 => new Vector4(-px, +1.0f, -py, 0.0f), // Y-
                        4 => new Vector4(-py, -px, 1.0f, 0.0f), // Z+
                        5 => new Vector4(+py, -px, -1.0f, 0.0f), // Z-
                        _ => throw new NotSupportedException()
                    };

                    float r = face.Length();
                    float lon = float.Atan2(face.Y, face.X);
                    float lat = float.Acos(face.Z / r);

                    int sourceIndex =
                        (int)(textureData.Width * lon / MathF.PI / 2.0f - 0.5f) +
                        (int)(textureData.Height * lat / MathF.PI - 0.5f) * textureData.Width;


                }
            }

            return outFaces;
        }

        public static RawTextureData[] LoadSequential(CubemapConfiguration cubemap)
        {
            CubemapConfiguration.Cubemap info = cubemap.CubemapInfo;

            IAssetIdProvider idProvider = cubemap.IdProvider!;
            if (!idProvider.TryGetLocalPathForId(cubemap.DefaultId, out string? filePath))
            {
                throw new Exception($"Failed to get path for id: {cubemap.DefaultId}");
            }

            RawTextureData textureData = TextureUtil.LoadRawData(filePath);
            Guard.IsEqualTo(textureData.Width, textureData.Height * 6, "Source is not in a 6:1 size");

            int textureWH = textureData.Height;

            RawTextureData[] outFaces = new RawTextureData[6];
            for (int i = 0; i < 6; ++i)
            {
                ref RawTextureData cubemapFace = ref outFaces[i];
                cubemapFace = TextureUtil.CreateTexture(textureWH, textureWH, 4, true);

                int rowStride = textureData.Height;
                int rowPitch = textureData.Width;

                ScopedPtr<Vector128<float>> source = textureData.Pixels + rowStride * i;
                ScopedPtr<Vector128<float>> destination = cubemapFace.Pixels;

                for (int row = 0; row < textureData.Height; ++row)
                {
                    source.Copy(destination, (nuint)rowStride);

                    source += rowPitch;
                    destination += rowStride;
                }
            }

            return outFaces;
        }
    }
}
