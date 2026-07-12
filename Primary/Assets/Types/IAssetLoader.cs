using Primary.Common.Streams;

namespace Primary.Assets.Types
{
    public interface IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id);
        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData);
        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom);
    }

    public class AssetLoadException : Exception
    {
        public AssetLoadException()
        {
        }

        public AssetLoadException(string? message) : base(message)
        {
        }
    }
}
