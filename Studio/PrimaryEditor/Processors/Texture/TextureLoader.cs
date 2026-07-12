using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using Primary;
using Primary.Assets;
using Primary.Assets.Types;

namespace Editor.Processors.Texture
{
    internal static class TextureLoader
    {
        public static RawTextureData[] Load(TextureConfiguration texture)
        {
            IAssetIdProvider idProvider = texture.IdProvider!;

            if (!idProvider.TryGetLocalPathForId(texture.DefaultId, out string? filePath))
            {
                throw new Exception($"Failed to get path for id: {texture.DefaultId}");
            }

            return [TextureUtil.LoadRawData(filePath)];
        }
    }
}
