using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Common.Streams;
using Primary.Rendering.Assets;
using Primary.Utility;

namespace Primary.Assets.Loaders
{
    internal sealed class ShaderSetAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ShaderSetAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new ShaderSetAsset((ShaderSetAssetData)assetData);
        }

        public void FactoryLoad(IAssetDefinition assetDef, IInternalAssetData assetDataDef, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            ShaderSetAsset asset = (ShaderSetAsset)assetDef;
            ShaderSetAssetData assetData = (ShaderSetAssetData)assetDataDef;

            assetData.Dispose();

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom) ??
                throw new AssetLoadException("Failed to open asset stream");

            FileShaderSetHeader header = stream.Read<FileShaderSetHeader>();
            if (header.FileHeader != FileShaderSetHeader.Header)
                throw new AssetLoadException("Shader set header does not match with expected value");
            if (header.FileVersion != FileShaderSetHeader.Version)
                throw new AssetLoadException("Shader set header is in a different version than what is supported");

            Dictionary<FastStringHash, ShaderAsset> shaders = new Dictionary<FastStringHash, ShaderAsset>(header.ShaderCount);
            for (int i = 0; i < header.ShaderCount; ++i)
            {
                FileShaderEntry entry = stream.Read<FileShaderEntry>();
                string name = stream.ReadStringUtf8(entry.NameLength);

                ShaderAsset shader = AssetManager.LoadAsset<ShaderAsset>(entry.Id);
                shaders.Add(name, shader);
            }

            assetData.UpdateAssetData(asset, shaders.ToFrozenDictionary());
        }
    }

    public struct FileShaderSetHeader
    {
        public uint FileHeader;
        public uint FileVersion;

        public byte ShaderCount;

        public const uint Header = 0x54455353;
        public const uint Version = 1;
    }

    public struct FileShaderEntry
    {
        public byte NameLength;
        public AssetId Id;
    }
}
