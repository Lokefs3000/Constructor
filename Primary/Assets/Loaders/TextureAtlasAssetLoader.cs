using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Assets.Loaders
{
    internal sealed class TextureAtlasAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new TextureAtlasAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not TextureAtlasAssetData textureAtlas)
                throw new ArgumentException(null, nameof(assetData));

            return new TextureAtlasAsset(textureAtlas);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not TextureAtlasAsset textureAtlas)
                throw new ArgumentException(null, nameof(asset));
            if (assetData is not TextureAtlasAssetData textureAtlasData)
                throw new ArgumentException(null, nameof(assetData));

            textureAtlasData.Dispose();

            try
            {
                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom) ?? throw new AssetLoadException("Opened stream is null!");

                TexAtlasHeader header = stream.Read<TexAtlasHeader>();

                if (header.FileHeader != TexAtlasHeader.Header)
                    throw new AssetLoadException("Invalid header present");
                if (header.FileVersion != TexAtlasHeader.Version)
                    throw new AssetLoadException($"Incorrect file version {header.FileVersion} expected version {TexAtlasHeader.Version}");

                TextureAsset textureAsset = AssetManager.LoadAsset<TextureAsset>(header.TextureId);
                if (textureAsset.Status == ResourceStatus.Bad || textureAsset.Status == ResourceStatus.Error)
                    EngLog.Assets.Warning("[{p}]: Loaded texture currently has an invalid status {status}", sourcePath, textureAsset.Status);

                Span<char> nameBuffer = stackalloc char[byte.MaxValue];

                Sprite[] sprites = new Sprite[header.SpriteCount];
                for (int i = 0; i < header.SpriteCount; ++i)
                {
                    TexAtlasSprite sprite = stream.Read<TexAtlasSprite>();

                    Span<char> nameSpan = nameBuffer[..sprite.NameLength];
                    if (!nameSpan.IsEmpty)
                        stream.ReadExactly(MemoryMarshal.Cast<char, byte>(nameSpan));

                    sprites[i] = new Sprite(textureAtlas, textureAsset, nameSpan.ToString(), new Rect(sprite.Offset.X, sprite.Offset.Y, sprite.Size.X, sprite.Size.Y));
                }

                textureAtlasData.UpdateAssetData(textureAtlas, [.. sprites]);
            }
            catch (Exception ex)
            {
                textureAtlasData.UpdateAssetFailed(textureAtlas);
                EngLog.Assets.Error(ex, "Failed to load texture atlas: {name}", sourcePath);
            }
        }
    }

    public struct TexAtlasHeader
    {
        public int FileHeader;
        public int FileVersion;

        public AssetId TextureId;
        public ushort SpriteCount;

        public const int Header = 0x4c415854;
        public const int Version = 1;
    }

    public struct TexAtlasSprite
    {
        public byte NameLength;

        public TexAtlasVector Offset;
        public TexAtlasVector Size;
    }

    public struct TexAtlasVector
    {
        public ushort X;
        public ushort Y;
    }
}
