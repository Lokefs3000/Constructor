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

        public int Width => _assetData.Width;
        public int Height => _assetData.Height;
        public RHI.TextureFormat Format => _assetData.Format;

        public nint Handle => _assetData.Texture?.Handle ?? nint.Zero;
    }

    internal sealed class TextureAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private RHI.Texture? _texture;

        private int _width;
        private int _height;
        private RHI.TextureFormat _format;

        internal TextureAssetData()
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _texture = null;

            _width = 0;
            _height = 0;
            _format = RHI.TextureFormat.Undefined;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;

            _texture?.Dispose();
            _texture = null;

            _width = 0;
            _height = 0;
            _format = RHI.TextureFormat.Undefined;
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

            _width = (int)texture.Description.Width;
            _height = (int)texture.Description.Height;
            _format = texture.Description.Format;
        }

        internal void UpdateAssetFailed(TextureAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal ResourceStatus Status => _status;

        internal RHI.Texture? Texture => _texture;

        internal int Width => _width;
        internal int Height => _height;
        internal RHI.TextureFormat Format => _format;

        public Type AssetType => typeof(TextureAsset);
    }
}
