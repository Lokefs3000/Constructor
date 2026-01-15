using Primary.Assets.Types;
using Primary.Utility;
using System.Runtime.CompilerServices;

namespace Primary.Assets
{
    public sealed class PostProcessingVolumeAsset : IAssetDefinition
    {
        private readonly PPVolumeAssetData _assetData;

        internal PostProcessingVolumeAsset(PPVolumeAssetData assetData)
        {
            _assetData = assetData;
        }

        internal PPVolumeAssetData AssetData => _assetData;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;
    }

    internal sealed class PPVolumeAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        internal PPVolumeAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;
        }
        public void SetAssetInternalStatus(ResourceStatus status)
        {
            _status = status;
        }

        public void SetAssetInternalName(string name)
        {
            _name = name;
        }

        internal void UpdateAssetData(PostProcessingVolumeAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;
        }

        internal void UpdateAssetFailed(PostProcessingVolumeAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        public int LoadIndex => 0;

        public Type AssetType => typeof(PostProcessingVolumeAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);

        AssetId IInternalAssetData.Id => Id;
        ResourceStatus IInternalAssetData.Status => Status;
        string IInternalAssetData.Name => Name;
    }
}
