using System.Numerics;
using System.Runtime.InteropServices;

namespace Primary.Assets
{
    public sealed class TextureAsset : IAssetDefinition
    {
        private readonly TextureAssetData _assetData;

        internal TextureAsset(TextureAssetData assetData)
        {
            _assetData = assetData;
        }

        internal TextureAssetData AssetData => _assetData;

        internal RHI.Texture? Texture => _assetData.Texture;

        public ResourceStatus Status => _assetData.Status;

        public nint Handle => _assetData.Texture?.Handle ?? nint.Zero;
    }

    internal sealed class TextureAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private RHI.Texture? _texture;

        internal TextureAssetData()
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _texture = null;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;

            _texture?.Dispose();
            _texture = null;
        }

        public void ResetInternalState()
        {
            Dispose();

            _status = ResourceStatus.Pending;
        }

        public void PromoteStateToRunning()
        {
            if (_status == ResourceStatus.Pending)
                _status = ResourceStatus.Running;
        }

        internal void UpdateAssetData(TextureAsset asset, RHI.Texture texture)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;

            _texture = texture;
        }

        internal void UpdateAssetFailed(TextureAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal ResourceStatus Status => _status;

        internal RHI.Texture? Texture => _texture;

        public Type AssetType => typeof(TextureAsset);
    }
}
