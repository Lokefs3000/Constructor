using Primary;
using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Processors.Texture
{
    internal static class TextureLoader
    {
        public static RawTextureData[] Load(TextureConfiguration texture)
        {
            IAssetIdProvider idProvider = texture.IdProvider!;

            string? filePath = idProvider.RetrievePathForId(texture.DefaultId);
            if (filePath == null)
            {
                throw new Exception($"Failed to get path for id: {texture.DefaultId}");
            }

            return [TextureUtil.LoadRawData(filePath)];
        }
    }
}
