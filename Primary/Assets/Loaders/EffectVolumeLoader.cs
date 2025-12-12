using CsToml;
using CsToml.Values;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering;
using Primary.Rendering.PostProcessing;
using Primary.Serialization.Json;
using System.Text.Json;

namespace Primary.Assets.Loaders
{
    internal unsafe class EffectVolumeLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new PPVolumeAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not PPVolumeAssetData volumeData)
                throw new ArgumentException(nameof(assetData));

            return new PostProcessingVolumeAsset(volumeData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not PostProcessingVolumeAsset effectVolume)
                throw new ArgumentException(nameof(asset));
            if (assetData is not PPVolumeAssetData effectVolumeData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
                if (stream == null)
                {
                    effectVolumeData.UpdateAssetFailed(effectVolume);
                    return;
                }

                ExceptionUtility.Assert(stream != null);

                IPostProcessingData[]? effectData = JsonSerializer.Deserialize(stream!, VolumeEffectJsonContext.Default.IPostProcessingDataArray);
                if (effectData == null)
                {
                    EngLog.Assets.Error("Failed to parse effect volume json: {v}", sourcePath);
                    effectVolumeData.UpdateAssetFailed(effectVolume);
                    return;
                }
                
                effectVolumeData.UpdateAssetData(effectVolume, [.. effectData]);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                materialData.UpdateAssetFailed(material);
                EngLog.Assets.Error(ex, "Failed to load effect volume: {name}", sourcePath);
            }
#endif
        }
    }
}
