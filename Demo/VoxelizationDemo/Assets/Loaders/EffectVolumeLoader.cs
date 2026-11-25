using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common.Streams;
using VoxelizationDemo.Rendering.Compositing;
using VoxelizationDemo.Rendering.Compositing.Effects;

namespace VoxelizationDemo.Assets.Loaders
{
    internal sealed class EffectVolumeLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id, int uniqueIndex)
        {
            return new EffectVolumeAssetData(id, uniqueIndex);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new EffectVolumeAsset((EffectVolumeAssetData)assetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            EffectVolumeAsset effectVolume = (EffectVolumeAsset)asset;
            EffectVolumeAssetData effectVolumeData = (EffectVolumeAssetData)assetData;

            effectVolumeData.Dispose();

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath);
            if (stream == null)
            {
                throw new AssetLoadException($"Failed to open stream for asset '{localPath}'");
            }

            RenderingEffectData[] effects = JsonSerializer.Deserialize<RenderingEffectData[]>(stream, s_options)!;
            effectVolumeData.UpdateAssetData(effectVolume, effects);
        }

        private readonly static JsonSerializerOptions s_options = new JsonSerializerOptions(EffectVolumeJsonContext.Default.Options)
        {
            TypeInfoResolver = EffectVolumeJsonContext.Default
        };
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        IndentSize = 4,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        RespectNullableAnnotations = true,
        IncludeFields = true,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(RenderingEffectData[]))]
    [JsonSerializable(typeof(SkyboxEffect.Data), TypeInfoPropertyName = "SkyboxEffect")]
    [JsonSerializable(typeof(TonemappingEffect.Data), TypeInfoPropertyName = "TonemappingEffect")]
    internal partial class EffectVolumeJsonContext : JsonSerializerContext;
}
