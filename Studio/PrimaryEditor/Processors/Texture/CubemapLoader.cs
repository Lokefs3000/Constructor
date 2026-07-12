using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using Primary;
using Primary.Assets;
using Primary.Assets.Types;
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
    }
}
