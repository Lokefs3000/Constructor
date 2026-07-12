using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Loaders;
using Primary.Utility;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Core;

namespace PrimaryEditor.Processors.TextureAtlas
{
    public sealed class TextureAtlasProcessor
    {
        public static unsafe void Execute(string localPath, TextureAtlasConfiguration config, Stream outputStream)
        {
            AssetPipeline pipeline = EditorRuntime.Instance.AssetPipeline;

            if (!pipeline.AssetRegistry.IsIdValid(config.Texture))
                throw new Exception("Texture asset id is not valid");

            if (!pipeline.AssetRegistry.TryGetLocalPathForId(config.Texture, out string? texturePath))
                throw new Exception($"Failed to get path to texture from provided id '{config.Texture}'");

            if (!pipeline.ImporterRegistry.TryGetImporterForPath(texturePath, out IAssetImporter? importer) && importer is not TextureImporter)
                throw new Exception($"Texture asset id is not a valid texture file");

            int spriteCount = Math.Min(config.Sprites.Length, ushort.MaxValue);
            if (spriteCount != config.Sprites.Length)
                EdLog.Assets.Information("[{p}]: The sprites in the texture atlas will be trimmed because there are too many", localPath);

            outputStream.Write(new TexAtlasHeader
            {
                FileHeader = TexAtlasHeader.Header,
                FileVersion = TexAtlasHeader.Version,

                TextureId = config.Texture,
                SpriteCount = (ushort)spriteCount
            });

            for (int i = 0; i < spriteCount; ++i)
            {
                TextureAtlasSprite spriteData = config.Sprites[i];

                ReadOnlySpan<char> spriteNameAsSpan = spriteData.Name.AsSpan();
                if (spriteNameAsSpan.IsEmpty)
                    throw new Exception("Sprite name in atlas is empty");

                if (spriteNameAsSpan.Length > byte.MaxValue)
                {
                    EdLog.Assets.Information("[{p}]: The sprite '{n}' will have its name trimmed because it is too long", localPath, spriteData.Name);
                    spriteNameAsSpan = spriteNameAsSpan[..byte.MaxValue];
                }

                outputStream.Write(new TexAtlasSprite
                {
                    NameLength = (byte)spriteNameAsSpan.Length,

                    Offset = new TexAtlasVector { X = (ushort)spriteData.Offset.X, Y = (ushort)spriteData.Offset.Y },
                    Size = new TexAtlasVector { X = (ushort)spriteData.Size.X, Y = (ushort)spriteData.Size.Y },
                });

                outputStream.Write(spriteNameAsSpan);
            }
        }
    }
}
